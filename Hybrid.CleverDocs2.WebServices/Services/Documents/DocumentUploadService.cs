using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using System.Collections.Concurrent;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Enterprise-grade document upload service with bulletproof R2R integration
/// </summary>
public class DocumentUploadService : IDocumentUploadService
{
    private readonly IChunkedUploadService _chunkedUploadService;
    private readonly IDocumentProcessingService _processingService;
    private readonly IUploadProgressService _progressService;
    private readonly IUploadValidationService _validationService;
    private readonly IUploadStorageService _storageService;
    private readonly IUploadMetricsService _metricsService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly IHubContext<DocumentUploadHub> _hubContext;
    private readonly ILogger<DocumentUploadService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DocumentUploadOptions _options;

    // Thread-safe storage for upload sessions
    private readonly ConcurrentDictionary<Guid, DocumentUploadSessionDto> _activeSessions = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _userUploadSemaphores = new();

    public DocumentUploadService(
        IChunkedUploadService chunkedUploadService,
        IDocumentProcessingService processingService,
        IUploadProgressService progressService,
        IUploadValidationService validationService,
        IUploadStorageService storageService,
        IUploadMetricsService metricsService,
        IRateLimitingService rateLimitingService,
        IMultiLevelCacheService cacheService,
        IHubContext<DocumentUploadHub> hubContext,
        ILogger<DocumentUploadService> logger,
        ICorrelationService correlationService,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<DocumentUploadOptions> options)
    {
        _chunkedUploadService = chunkedUploadService;
        _processingService = processingService;
        _progressService = progressService;
        _validationService = validationService;
        _storageService = storageService;
        _metricsService = metricsService;
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _logger = logger;
        _correlationService = correlationService;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
    }

    public async Task<DocumentUploadSessionDto> InitializeUploadSessionAsync(InitializeUploadSessionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Initializing upload session for user {UserId}, {FileCount} files, CorrelationId: {CorrelationId}", 
                request.UserId, request.Files.Count, correlationId);

            // Validate request
            var validation = await ValidateInitializationRequestAsync(request);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Create upload session
            var session = new DocumentUploadSessionDto
            {
                SessionId = Guid.NewGuid(),
                UserId = request.UserId,
                CompanyId = request.CompanyId,
                CollectionId = request.CollectionId,
                Options = request.Options,
                Status = UploadSessionStatusDto.Ready
            };

            // Initialize file upload info
            foreach (var fileInfo in request.Files)
            {
                var fileUploadInfo = new FileUploadInfoDto
                {
                    SessionId = session.SessionId,
                    OriginalFileName = fileInfo.FileName,
                    ContentType = fileInfo.ContentType,
                    TotalSize = fileInfo.Size,
                    Checksum = fileInfo.Checksum
                };

                // Initialize chunked upload if needed
                if (fileUploadInfo.RequiresChunking && request.Options.EnableChunkedUpload)
                {
                    fileUploadInfo.ChunkedInfo = await _chunkedUploadService.InitializeChunkedUploadAsync(
                        session.SessionId, fileUploadInfo.FileId, fileInfo.Size, fileInfo.FileName, request.Options.ChunkSize);
                }

                session.Files.Add(fileUploadInfo);
            }

            // Calculate statistics
            session.Statistics.TotalBytes = session.Files.Sum(f => f.TotalSize);
            session.Statistics.PendingUploads = session.Files.Count;
            session.Statistics.StartTime = DateTime.UtcNow;

            // Store session in memory for fast access
            _activeSessions[session.SessionId] = session;

            // Also store in cache for consistency with GetUploadSessionAsync
            await _cacheService.SetAsync($"upload:session:{session.SessionId}", session,
                new CacheOptions
                {
                    UseL1Cache = true,  // Fast access
                    UseL2Cache = true,  // Redis for consistency
                    L1TTL = TimeSpan.FromMinutes(30),
                    L2TTL = TimeSpan.FromMinutes(30)
                });

            // Initialize progress tracking
            await _progressService.UpdateProgressAsync(new UploadProgressDto
            {
                SessionId = session.SessionId,
                Status = FileUploadStatusDto.Pending,
                ProgressPercentage = 0,
                Message = "Upload session initialized"
            });

            _logger.LogInformation("Upload session {SessionId} initialized successfully (memory-only, Redis optimized), CorrelationId: {CorrelationId}",
                session.SessionId, correlationId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing upload session for user {UserId}, CorrelationId: {CorrelationId}", 
                request.UserId, correlationId);
            
            await _metricsService.RecordErrorMetricsAsync("session_initialization_failed", ex.Message);
            throw;
        }
    }

    public async Task<UploadResponseDto> UploadFileAsync(IFormFile file, Guid sessionId, string userId, UploadOptionsDto? options = null)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting file upload: {FileName} ({FileSize} bytes) for session {SessionId}, CorrelationId: {CorrelationId}", 
                file.FileName, file.Length, sessionId, correlationId);

            // Get session
            var session = await GetUploadSessionAsync(sessionId, userId);
            if (session == null)
            {
                return new UploadResponseDto
                {
                    Success = false,
                    Message = "Upload session not found",
                    Errors = new List<string> { "Invalid session ID" }
                };
            }

            // Get user upload semaphore for rate limiting
            var userSemaphore = _userUploadSemaphores.GetOrAdd(userId, _ => new SemaphoreSlim(_options.MaxConcurrentUploadsPerUser, _options.MaxConcurrentUploadsPerUser));

            await userSemaphore.WaitAsync();
            try
            {
                // Validate file
                var (isValid, errors) = await _validationService.ValidateFileAsync(file, options ?? session.Options, userId);
                if (!isValid)
                {
                    return new UploadResponseDto
                    {
                        Success = false,
                        Message = "File validation failed",
                        Errors = errors
                    };
                }

                // Find or create file info
                var fileInfo = session.Files.FirstOrDefault(f => f.OriginalFileName == file.FileName) ??
                              CreateFileInfo(sessionId, file);

                if (!session.Files.Contains(fileInfo))
                {
                    session.Files.Add(fileInfo);
                }

                // Update file status
                fileInfo.Status = FileUploadStatusDto.Uploading;
                fileInfo.StartedAt = DateTime.UtcNow;

                // Read file content
                byte[] fileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }

                // Calculate checksum
                var checksum = await _storageService.CalculateChecksumAsync(fileContent);
                fileInfo.Checksum = checksum;

                // Store file temporarily
                var tempPath = await _storageService.StoreTempFileAsync(fileContent, file.FileName, sessionId, fileInfo.FileId);
                fileInfo.TempFilePath = tempPath;

                // Update progress
                fileInfo.UploadedSize = fileInfo.TotalSize;
                fileInfo.Status = FileUploadStatusDto.Uploaded;
                fileInfo.CompletedAt = DateTime.UtcNow;

                await UpdateSessionProgress(session, fileInfo);

                // Queue for R2R processing
                await QueueForR2RProcessingAsync(fileInfo, session);

                _logger.LogInformation("File upload completed: {FileName} for session {SessionId}, CorrelationId: {CorrelationId}", 
                    file.FileName, sessionId, correlationId);

                return new UploadResponseDto
                {
                    Success = true,
                    Message = "File uploaded successfully",
                    SessionId = sessionId,
                    Files = new List<FileUploadInfoDto> { fileInfo }
                };
            }
            finally
            {
                userSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} for session {SessionId}, CorrelationId: {CorrelationId}", 
                file.FileName, sessionId, correlationId);

            await _metricsService.RecordErrorMetricsAsync("file_upload_failed", ex.Message, sessionId);

            return new UploadResponseDto
            {
                Success = false,
                Message = "File upload failed",
                Errors = new List<string> { ex.Message },
                SessionId = sessionId
            };
        }
    }

    public async Task<UploadResponseDto> UploadBatchAsync(BatchUploadRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("🚀 BATCH UPLOAD STARTED: {FileCount} files for user {UserId}, MaxParallel: {MaxParallel}, CorrelationId: {CorrelationId}",
                request.Files.Count, request.UserId, request.Options.MaxParallelUploads, correlationId);

            // Log system resources before starting
            var memoryUsed = GC.GetTotalMemory(false);
            _logger.LogInformation("📊 SYSTEM RESOURCES: Memory={MemoryMB}MB, CorrelationId: {CorrelationId}",
                memoryUsed / 1024 / 1024, correlationId);

            // Log each file being queued
            foreach (var file in request.Files)
            {
                _logger.LogDebug("📁 FILE QUEUED: {FileName}, Size: {SizeKB}KB, ContentType: {ContentType}, CorrelationId: {CorrelationId}",
                    file.FileName, file.Length / 1024, file.ContentType, correlationId);
            }

            // Initialize session
            var initRequest = new InitializeUploadSessionDto
            {
                Files = request.Files.Select(f => new FileInfoDto
                {
                    FileName = f.FileName,
                    Size = f.Length,
                    ContentType = f.ContentType
                }).ToList(),
                CollectionId = request.CollectionId,
                Tags = request.Tags,
                Options = request.Options,
                UserId = request.UserId
            };

            var session = await InitializeUploadSessionAsync(initRequest);
            _logger.LogInformation("✅ SESSION INITIALIZED: {SessionId}, CorrelationId: {CorrelationId}",
                session.SessionId, correlationId);

            // CHUNKED PROCESSING STRATEGY: Process files in smaller batches to prevent system overload
            const int BATCH_SIZE = 5; // Process 5 files at a time to prevent memory/resource issues
            var allResults = new List<UploadResponseDto>();
            var fileList = request.Files.ToList();
            var totalBatches = (int)Math.Ceiling((double)fileList.Count / BATCH_SIZE);
            var semaphore = new SemaphoreSlim(request.Options.MaxParallelUploads, request.Options.MaxParallelUploads);

            _logger.LogInformation("🔄 CHUNKED PROCESSING STRATEGY: {TotalFiles} files divided into {TotalBatches} batches of {BatchSize} files each, CorrelationId: {CorrelationId}",
                fileList.Count, totalBatches, BATCH_SIZE, correlationId);

            // Process files in batches to prevent system overload
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batchStart = batchIndex * BATCH_SIZE;
                var batchFiles = fileList.Skip(batchStart).Take(BATCH_SIZE).ToList();
                var batchNumber = batchIndex + 1;

                _logger.LogInformation("🚀 BATCH {BatchNumber}/{TotalBatches} STARTED: Processing {BatchFileCount} files, CorrelationId: {CorrelationId}",
                    batchNumber, totalBatches, batchFiles.Count, correlationId);

                // Create upload tasks for current batch only
                var uploadTasks = new List<Task<UploadResponseDto>>();
                foreach (var file in batchFiles)
                {
                    _logger.LogInformation("📁 FILE QUEUED (Batch {BatchNumber}): {FileName} ({FileSize} bytes), CorrelationId: {CorrelationId}",
                        batchNumber, file.FileName, file.Length, correlationId);

                    uploadTasks.Add(UploadFileWithSemaphoreAsync(file, session.SessionId, request.UserId, request.Options, semaphore, correlationId));
                }

                _logger.LogInformation("⏳ BATCH {BatchNumber} PARALLEL EXECUTION: {TaskCount} tasks with semaphore capacity {SemaphoreCapacity}, CorrelationId: {CorrelationId}",
                    batchNumber, uploadTasks.Count, semaphore.CurrentCount, correlationId);

                // Execute current batch with enhanced error handling
                UploadResponseDto[] batchResults;
                try
                {
                    batchResults = await Task.WhenAll(uploadTasks);
                    _logger.LogInformation("🏁 BATCH {BatchNumber} COMPLETED: {ResultCount} results received, CorrelationId: {CorrelationId}",
                        batchNumber, batchResults.Length, correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 BATCH {BatchNumber} FAILED: Exception during Task.WhenAll, CorrelationId: {CorrelationId}", batchNumber, correlationId);

                    // Collect results from completed tasks in this batch
                    var completedResults = new List<UploadResponseDto>();
                    foreach (var task in uploadTasks)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            completedResults.Add(task.Result);
                        }
                        else if (task.IsFaulted)
                        {
                            _logger.LogError(task.Exception, "❌ BATCH {BatchNumber} TASK FAILED: Individual upload task failed, CorrelationId: {CorrelationId}", batchNumber, correlationId);
                            completedResults.Add(new UploadResponseDto
                            {
                                Success = false,
                                Message = $"Upload task failed in batch {batchNumber}",
                                Errors = new List<string> { task.Exception?.GetBaseException().Message ?? "Unknown error" }
                            });
                        }
                    }
                    batchResults = completedResults.ToArray();
                }

                // Add batch results to overall results
                allResults.AddRange(batchResults);

                // Log batch completion
                var batchSuccessCount = batchResults.Count(r => r.Success);
                var batchFailureCount = batchResults.Count(r => !r.Success);
                _logger.LogInformation("📈 BATCH {BatchNumber} RESULTS: Success={Success}, Failures={Failures}, CorrelationId: {CorrelationId}",
                    batchNumber, batchSuccessCount, batchFailureCount, correlationId);

                // Small delay between batches to prevent overwhelming the system
                if (batchIndex < totalBatches - 1) // Don't delay after the last batch
                {
                    await Task.Delay(500, CancellationToken.None); // 500ms delay between batches
                    _logger.LogDebug("⏸️ BATCH DELAY: 500ms delay before next batch, CorrelationId: {CorrelationId}", correlationId);
                }
            }

            // Convert all results to array for compatibility with existing code
            var results = allResults.ToArray();

            // Detailed result analysis with enhanced logging
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            var totalFiles = results.SelectMany(r => r.Files).Count();
            var expectedFiles = request.Files.Count;

            _logger.LogInformation("📈 CHUNKED PROCESSING FINAL RESULTS: Success={Success}, Failures={Failures}, TotalFiles={TotalFiles}, Expected={Expected}, Batches={TotalBatches}, CorrelationId: {CorrelationId}",
                successCount, failureCount, totalFiles, expectedFiles, totalBatches, correlationId);

            // Critical check for missing files
            if (totalFiles < expectedFiles)
            {
                var missingCount = expectedFiles - totalFiles;
                _logger.LogError("🚨 MISSING FILES DETECTED: {MissingCount} files were lost during chunked processing! Expected={Expected}, Actual={Actual}, Batches={TotalBatches}, CorrelationId: {CorrelationId}",
                    missingCount, expectedFiles, totalFiles, totalBatches, correlationId);
            }
            else
            {
                _logger.LogInformation("✅ ALL FILES PROCESSED: No missing files detected in chunked processing, CorrelationId: {CorrelationId}", correlationId);
            }

            if (failureCount > 0)
            {
                var allErrors = results.SelectMany(r => r.Errors).ToList();
                foreach (var error in allErrors)
                {
                    _logger.LogError("❌ BATCH ERROR: {Error}, CorrelationId: {CorrelationId}", error, correlationId);
                }
            }

            // Aggregate results
            var response = new UploadResponseDto
            {
                Success = results.All(r => r.Success),
                SessionId = session.SessionId,
                Files = results.SelectMany(r => r.Files).ToList(),
                Errors = results.SelectMany(r => r.Errors).ToList()
            };

            // Update session status
            session.Status = response.Success ? UploadSessionStatusDto.Processing : UploadSessionStatusDto.Failed;
            session.Statistics.SuccessfulUploads = results.Count(r => r.Success);
            session.Statistics.FailedUploads = results.Count(r => !r.Success);

            UpdateSessionInMemory(session);

            _logger.LogInformation("🎉 CHUNKED BATCH UPLOAD COMPLETED: {SuccessCount}/{TotalCount} files successful across {TotalBatches} batches for session {SessionId}, CorrelationId: {CorrelationId}",
                response.Files.Count(f => f.Status == FileUploadStatusDto.Completed), request.Files.Count, totalBatches, session.SessionId, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch upload for user {UserId}, CorrelationId: {CorrelationId}", 
                request.UserId, correlationId);

            await _metricsService.RecordErrorMetricsAsync("batch_upload_failed", ex.Message);

            return new UploadResponseDto
            {
                Success = false,
                Message = "Batch upload failed",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<UploadResponseDto> UploadChunkAsync(ChunkedUploadRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Uploading chunk {ChunkNumber}/{TotalChunks} for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                request.ChunkNumber, request.TotalChunks, request.FileId, request.SessionId, correlationId);

            // Read chunk data
            byte[] chunkData;
            using (var memoryStream = new MemoryStream())
            {
                await request.ChunkData.CopyToAsync(memoryStream);
                chunkData = memoryStream.ToArray();
            }

            // Upload chunk
            var success = await _chunkedUploadService.UploadChunkAsync(
                request.SessionId, request.FileId, request.ChunkNumber, chunkData, request.Checksum);

            if (!success)
            {
                return new UploadResponseDto
                {
                    Success = false,
                    Message = "Chunk upload failed",
                    Errors = new List<string> { "Failed to upload chunk" }
                };
            }

            // Update progress
            var session = _activeSessions.GetValueOrDefault(request.SessionId);
            if (session != null)
            {
                var fileInfo = session.Files.FirstOrDefault(f => f.FileId == request.FileId);
                if (fileInfo?.ChunkedInfo != null)
                {
                    fileInfo.ChunkedInfo.CompletedChunks++;
                    fileInfo.UploadedSize = (long)fileInfo.ChunkedInfo.CompletedChunks * fileInfo.ChunkedInfo.ChunkSize;
                    
                    if (fileInfo.UploadedSize > fileInfo.TotalSize)
                        fileInfo.UploadedSize = fileInfo.TotalSize;

                    await UpdateSessionProgress(session, fileInfo);
                }
            }

            // Complete upload if this is the last chunk
            if (request.IsLastChunk)
            {
                return await CompleteChunkedUploadAsync(request.SessionId, request.FileId, "system");
            }

            return new UploadResponseDto
            {
                Success = true,
                Message = "Chunk uploaded successfully",
                SessionId = request.SessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkNumber} for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                request.ChunkNumber, request.FileId, request.SessionId, correlationId);

            await _metricsService.RecordErrorMetricsAsync("chunk_upload_failed", ex.Message, request.SessionId);

            return new UploadResponseDto
            {
                Success = false,
                Message = "Chunk upload failed",
                Errors = new List<string> { ex.Message },
                SessionId = request.SessionId
            };
        }
    }

    public async Task<UploadResponseDto> CompleteChunkedUploadAsync(Guid sessionId, Guid fileId, string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Completing chunked upload for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                fileId, sessionId, correlationId);

            // Assemble chunks
            var filePath = await _chunkedUploadService.AssembleChunksAsync(sessionId, fileId);

            // Update file info
            var session = _activeSessions.GetValueOrDefault(sessionId);
            if (session != null)
            {
                var fileInfo = session.Files.FirstOrDefault(f => f.FileId == fileId);
                if (fileInfo != null)
                {
                    fileInfo.TempFilePath = filePath;
                    fileInfo.Status = FileUploadStatusDto.Uploaded;
                    fileInfo.CompletedAt = DateTime.UtcNow;
                    fileInfo.UploadedSize = fileInfo.TotalSize;

                    await UpdateSessionProgress(session, fileInfo);

                    // Queue for R2R processing
                    await QueueForR2RProcessingAsync(fileInfo, session);

                    // Cleanup chunks
                    await _chunkedUploadService.CleanupChunksAsync(sessionId, fileId);
                }
            }

            _logger.LogInformation("Chunked upload completed for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                fileId, sessionId, correlationId);

            return new UploadResponseDto
            {
                Success = true,
                Message = "Chunked upload completed successfully",
                SessionId = sessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing chunked upload for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                fileId, sessionId, correlationId);

            await _metricsService.RecordErrorMetricsAsync("chunked_upload_completion_failed", ex.Message, sessionId);

            return new UploadResponseDto
            {
                Success = false,
                Message = "Failed to complete chunked upload",
                Errors = new List<string> { ex.Message },
                SessionId = sessionId
            };
        }
    }

    // Additional methods implementation continues...
    public async Task<DocumentUploadSessionDto?> GetUploadSessionAsync(Guid sessionId, string userId)
    {
        // Try cache first
        var cached = await _cacheService.GetAsync<DocumentUploadSessionDto>($"upload:session:{sessionId}");
        if (cached != null && cached.UserId == userId)
        {
            return cached;
        }

        // Try active sessions
        var session = _activeSessions.GetValueOrDefault(sessionId);
        if (session?.UserId == userId)
        {
            return session;
        }

        return null;
    }

    public async Task<List<DocumentUploadSessionDto>> GetUserUploadSessionsAsync(string userId, bool includeCompleted = false)
    {
        var sessions = _activeSessions.Values
            .Where(s => s.UserId == userId)
            .Where(s => includeCompleted || s.Status != UploadSessionStatusDto.Completed)
            .ToList();

        return sessions;
    }

    public async Task<bool> CancelUploadSessionAsync(Guid sessionId, string userId)
    {
        var session = await GetUploadSessionAsync(sessionId, userId);
        if (session == null) return false;

        session.Status = UploadSessionStatusDto.Cancelled;
        
        // Cancel all pending files
        foreach (var file in session.Files.Where(f => f.Status == FileUploadStatusDto.Pending || f.Status == FileUploadStatusDto.Uploading))
        {
            file.Status = FileUploadStatusDto.Cancelled;
        }

        UpdateSessionInMemory(session);
        await _storageService.CleanupSessionFilesAsync(sessionId);

        return true;
    }

    public async Task<UploadResponseDto> RetryFailedUploadsAsync(Guid sessionId, string userId)
    {
        var session = await GetUploadSessionAsync(sessionId, userId);
        if (session == null)
        {
            return new UploadResponseDto { Success = false, Message = "Session not found" };
        }

        var failedFiles = session.Files.Where(f => f.Status == FileUploadStatusDto.Failed).ToList();
        
        foreach (var file in failedFiles)
        {
            file.Status = FileUploadStatusDto.Pending;
            file.RetryCount++;
            file.ErrorMessage = null;
        }

        UpdateSessionInMemory(session);

        return new UploadResponseDto
        {
            Success = true,
            Message = $"Retrying {failedFiles.Count} failed uploads",
            SessionId = sessionId
        };
    }

    public async Task<UploadValidationResultDto> ValidateFilesAsync(List<IFormFile> files, UploadOptionsDto options, string userId)
    {
        return await _validationService.ValidateFilesAsync(files, options, userId);
    }

    public async Task<List<string>> GetSupportedFileTypesAsync()
    {
        return await _validationService.GetValidationRulesAsync("default").ContinueWith(t => t.Result.AllowedFileTypes);
    }

    public async Task<UploadProgressDto> GetUploadProgressAsync(Guid sessionId, string userId)
    {
        return await _progressService.GetProgressAsync(sessionId) ?? new UploadProgressDto { SessionId = sessionId };
    }

    public async Task CleanupCompletedSessionsAsync(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var sessionsToCleanup = _activeSessions.Values
            .Where(s => s.Status == UploadSessionStatusDto.Completed && s.CompletedAt < cutoffTime)
            .ToList();

        foreach (var session in sessionsToCleanup)
        {
            _activeSessions.TryRemove(session.SessionId, out _);
            await _storageService.CleanupSessionFilesAsync(session.SessionId);
            // Cache invalidation removed - using memory-only storage
        }
    }

    public async Task<R2RRateLimitStatusDto> GetR2RRateLimitStatusAsync()
    {
        // Implementation would check current R2R rate limiting status
        return new R2RRateLimitStatusDto
        {
            CurrentRequests = 0,
            MaxRequests = 10,
            IsLimited = false,
            QueuedItems = 0
        };
    }

    public async Task<bool> PauseUploadSessionAsync(Guid sessionId, string userId)
    {
        var session = await GetUploadSessionAsync(sessionId, userId);
        if (session == null) return false;

        // Implementation would pause active uploads
        return true;
    }

    public async Task<bool> ResumeUploadSessionAsync(Guid sessionId, string userId)
    {
        var session = await GetUploadSessionAsync(sessionId, userId);
        if (session == null) return false;

        // Implementation would resume paused uploads
        return true;
    }

    // Helper methods
    private async Task<UploadValidationResultDto> ValidateInitializationRequestAsync(InitializeUploadSessionDto request)
    {
        var result = new UploadValidationResultDto { IsValid = true };

        if (!request.Files.Any())
        {
            result.IsValid = false;
            result.Errors.Add("No files provided");
        }

        if (request.Files.Sum(f => f.Size) > _options.MaxTotalUploadSize)
        {
            result.IsValid = false;
            result.Errors.Add($"Total upload size exceeds limit of {_options.MaxTotalUploadSize} bytes");
        }

        return result;
    }

    private FileUploadInfoDto CreateFileInfo(Guid sessionId, IFormFile file)
    {
        return new FileUploadInfoDto
        {
            SessionId = sessionId,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            TotalSize = file.Length
        };
    }

    private async Task UpdateSessionProgress(DocumentUploadSessionDto session, FileUploadInfoDto fileInfo)
    {
        // Update session statistics
        session.Statistics.UploadedBytes = session.Files.Sum(f => f.UploadedSize);
        session.Statistics.ElapsedTime = DateTime.UtcNow - (session.Statistics.StartTime ?? DateTime.UtcNow);

        if (session.Statistics.ElapsedTime.TotalSeconds > 0)
        {
            session.Statistics.CurrentSpeed = session.Statistics.UploadedBytes / session.Statistics.ElapsedTime.TotalSeconds;
        }

        // Update progress
        var progress = new UploadProgressDto
        {
            SessionId = session.SessionId,
            FileId = fileInfo.FileId,
            FileName = fileInfo.OriginalFileName,
            Status = fileInfo.Status,
            ProgressPercentage = fileInfo.ProgressPercentage,
            UploadedBytes = fileInfo.UploadedSize,
            TotalBytes = fileInfo.TotalSize,
            Speed = session.Statistics.CurrentSpeed
        };

        await _progressService.UpdateProgressAsync(progress);
        await _progressService.BroadcastProgressAsync(progress);
    }

    private async Task QueueForR2RProcessingAsync(FileUploadInfoDto fileInfo, DocumentUploadSessionDto session)
    {
        var queueItem = new R2RProcessingQueueItemDto
        {
            DocumentId = Guid.NewGuid(),
            FileId = fileInfo.FileId,
            UserId = session.UserId,
            CompanyId = session.CompanyId,
            CollectionId = session.CollectionId,
            FileName = fileInfo.OriginalFileName,
            FilePath = fileInfo.TempFilePath!,
            FileSize = fileInfo.TotalSize,
            ContentType = fileInfo.ContentType,
            Priority = R2RProcessingPriorityDto.Normal,
            ProcessingOptions = session.Options,
            JobId = Guid.NewGuid().ToString() // Generate JobId for R2R ingestion tracking
        };

        fileInfo.DocumentId = queueItem.DocumentId;
        fileInfo.Status = FileUploadStatusDto.Processing;

        // CRITICAL FIX: Save document to database IMMEDIATELY so frontend can see it
        await SaveDocumentToDatabaseImmediatelyAsync(queueItem);

        await _processingService.QueueDocumentForProcessingAsync(queueItem);
    }

    private static int _activeOperations = 0;

    private async Task<UploadResponseDto> UploadFileWithSemaphoreAsync(IFormFile file, Guid sessionId, string userId, UploadOptionsDto options, SemaphoreSlim semaphore, string correlationId)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogDebug("🔒 SEMAPHORE WAIT: {FileName}, Available={Available}/{Total}, CorrelationId: {CorrelationId}",
            file.FileName, semaphore.CurrentCount, options.MaxParallelUploads, correlationId);

        // Add timeout to semaphore wait to prevent indefinite blocking (reduced from 5 minutes to 2 minutes)
        var semaphoreAcquired = await semaphore.WaitAsync(TimeSpan.FromMinutes(2));

        if (!semaphoreAcquired)
        {
            _logger.LogError("⏰ SEMAPHORE TIMEOUT: {FileName} failed to acquire semaphore within 2 minutes, CorrelationId: {CorrelationId}",
                file.FileName, correlationId);
            return new UploadResponseDto
            {
                Success = false,
                Message = "Upload timeout - semaphore acquisition failed",
                Errors = new List<string> { $"Semaphore timeout for file: {file.FileName}" }
            };
        }

        var currentActive = Interlocked.Increment(ref _activeOperations);

        try
        {
            _logger.LogInformation("🚀 UPLOAD STARTED: {FileName}, Active={Active}, Available={Available}, CorrelationId: {CorrelationId}",
                file.FileName, currentActive, semaphore.CurrentCount, correlationId);

            var result = await UploadFileAsync(file, sessionId, userId, options);

            var duration = DateTime.UtcNow - startTime;

            if (result.Success)
            {
                _logger.LogInformation("✅ UPLOAD SUCCESS: {FileName}, Duration={Duration}ms, CorrelationId: {CorrelationId}",
                    file.FileName, duration.TotalMilliseconds, correlationId);
            }
            else
            {
                _logger.LogError("❌ UPLOAD FAILED: {FileName}, Duration={Duration}ms, Errors={Errors}, CorrelationId: {CorrelationId}",
                    file.FileName, duration.TotalMilliseconds, string.Join("; ", result.Errors), correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "💥 UPLOAD EXCEPTION: {FileName}, Duration={Duration}ms, CorrelationId: {CorrelationId}",
                file.FileName, duration.TotalMilliseconds, correlationId);

            return new UploadResponseDto
            {
                Success = false,
                Message = $"Upload failed with exception: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            var remainingActive = Interlocked.Decrement(ref _activeOperations);
            semaphore.Release();

            _logger.LogDebug("🔓 SEMAPHORE RELEASED: {FileName}, Active={Active}, Available={Available}, CorrelationId: {CorrelationId}",
                file.FileName, remainingActive, semaphore.CurrentCount, correlationId);
        }
    }

    private async void UpdateSessionInMemory(DocumentUploadSessionDto session)
    {
        // Store in memory for fast access
        _activeSessions[session.SessionId] = session;

        // Also update cache for consistency
        try
        {
            await _cacheService.SetAsync($"upload:session:{session.SessionId}", session,
                new CacheOptions
                {
                    UseL1Cache = true,  // Fast access
                    UseL2Cache = true,  // Redis for consistency
                    L1TTL = TimeSpan.FromMinutes(30),
                    L2TTL = TimeSpan.FromMinutes(30)
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update session cache for session {SessionId}", session.SessionId);
        }
    }

    private async Task SaveDocumentToDatabaseImmediatelyAsync(R2RProcessingQueueItemDto queueItem)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Saving document to database immediately after upload: {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);

            // Get user's company ID from database
            var userGuid = Guid.Parse(queueItem.UserId);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userGuid);

            if (user == null)
            {
                _logger.LogError("User {UserId} not found in database, CorrelationId: {CorrelationId}",
                    queueItem.UserId, correlationId);
                return;
            }

            // Create document record with Processing status
            var document = new Document
            {
                Id = queueItem.DocumentId,
                Name = queueItem.FileName,
                FileName = queueItem.FileName,
                OriginalFileName = queueItem.FileName,
                SizeInBytes = queueItem.FileSize,
                ContentType = queueItem.ContentType,
                Status = (int)Data.Entities.DocumentStatus.Processing, // Processing status
                R2RDocumentId = null, // Will be set when R2R completes
                R2RIngestionJobId = queueItem.JobId, // Set JobId for audit trail
                R2RProcessedAt = null, // Will be set when R2R completes
                UserId = userGuid,
                CompanyId = user.CompanyId,
                CollectionId = queueItem.CollectionId,
                FilePath = queueItem.FilePath,
                Tags = new List<string>(),
                Metadata = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsProcessing = true
            };

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            // CRITICAL: Invalidate cache using ONLY tag-based invalidation so frontend sees the new document immediately
            var tags = new List<string> { "documents", "document-lists", $"user:{queueItem.UserId}" };
            if (queueItem.CollectionId.HasValue)
            {
                tags.Add($"collection:{queueItem.CollectionId}");
            }
            // CRITICAL FIX: Pass tenantId to ensure tag transformation matches SET operation
            // Use CompanyId as TenantId (they are the same in our multi-tenant architecture)
            var tenantId = queueItem.CompanyId?.ToString();
            await _cacheService.InvalidateByTagsAsync(tags, tenantId);

            _logger.LogInformation("Document {DocumentId} saved to database immediately with Processing status and cache invalidated, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);

            // CRITICAL FIX: Send SignalR notification to update frontend immediately
            try
            {
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DocumentUploadHub>>();
                await hubContext.Clients.User(queueItem.UserId).SendAsync("FileUploadCompleted", new
                {
                    DocumentId = queueItem.DocumentId,
                    FileName = queueItem.FileName,
                    Status = "Processing",
                    CollectionId = queueItem.CollectionId,
                    Message = "Document uploaded and saved to database"
                });

                _logger.LogInformation("SignalR notification sent for document {DocumentId}, event: FileUploadCompleted, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
            }
            catch (Exception signalREx)
            {
                _logger.LogError(signalREx, "Failed to send SignalR notification for document {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
                // Don't throw - SignalR failure shouldn't break the upload
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document {DocumentId} to database immediately, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);
            throw;
        }
    }
}

/// <summary>
/// Configuration options for document upload service
/// </summary>
public class DocumentUploadOptions
{
    public int MaxConcurrentUploadsPerUser { get; set; } = 5;
    public long MaxTotalUploadSize { get; set; } = 1024L * 1024 * 1024; // 1GB
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
    public int DefaultChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(24);
    public bool EnableProgressTracking { get; set; } = true;
    public bool EnableChunkedUpload { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}
