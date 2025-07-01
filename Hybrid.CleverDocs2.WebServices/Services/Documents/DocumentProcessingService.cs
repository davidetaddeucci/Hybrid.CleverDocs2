using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service for processing documents with R2R API integration and intelligent rate limiting
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly DocumentProcessingOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<DocumentUploadHub> _hubContext;
    private readonly IDocumentClient _documentClient;
    private readonly IR2RComplianceService _complianceService;
    private readonly HttpClient _httpClient;

    // Thread-safe queue management
    private readonly ConcurrentQueue<R2RProcessingQueueItemDto> _processingQueue = new();
    private readonly ConcurrentDictionary<Guid, R2RProcessingQueueItemDto> _activeProcessing = new();
    private readonly ConcurrentDictionary<Guid, R2RProcessingQueueItemDto> _failedItems = new();

    // Rate limiting and circuit breaker
    private readonly SemaphoreSlim _processingLimiter;
    private DateTime _lastR2RFailure = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private bool _circuitBreakerOpen = false;

    // CRITICAL FIX: Static flag to ensure RestoreProcessingQueueFromDatabaseAsync runs only once
    private static bool _queueRestorationCompleted = false;
    private static readonly object _queueRestorationLock = new object();

    public DocumentProcessingService(
        IRateLimitingService rateLimitingService,
        IMultiLevelCacheService cacheService,
        ILogger<DocumentProcessingService> logger,
        ICorrelationService correlationService,
        IOptions<DocumentProcessingOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<DocumentUploadHub> hubContext,
        IDocumentClient documentClient,
        IR2RComplianceService complianceService,
        HttpClient httpClient)
    {
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _documentClient = documentClient;
        _complianceService = complianceService;
        _httpClient = httpClient;

        _processingLimiter = new SemaphoreSlim(_options.MaxConcurrentProcessing, _options.MaxConcurrentProcessing);

        // CRITICAL FIX: Restore processing queue from database ONLY ONCE on first service creation
        lock (_queueRestorationLock)
        {
            if (!_queueRestorationCompleted)
            {
                _queueRestorationCompleted = true;

                // Use Task.Run with proper error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RestoreProcessingQueueFromDatabaseAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ STARTUP: Critical error in RestoreProcessingQueueFromDatabaseAsync");
                        // Reset flag on error so it can be retried
                        lock (_queueRestorationLock)
                        {
                            _queueRestorationCompleted = false;
                        }
                    }
                });
            }
        }
    }

    public async Task<bool> QueueDocumentForProcessingAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Queuing document for R2R processing: {FileName} ({FileSize} bytes), DocumentId: {DocumentId}, Priority: {Priority}, CorrelationId: {CorrelationId}", 
                queueItem.FileName, queueItem.FileSize, queueItem.DocumentId, queueItem.Priority, correlationId);

            // Set initial status
            queueItem.Status = R2RProcessingStatusDto.Queued;
            queueItem.CreatedAt = DateTime.UtcNow;

            // Add to queue based on priority
            _processingQueue.Enqueue(queueItem);

            // Cache for persistence
            await _cacheService.SetAsync($"r2r:queue:{queueItem.Id}", queueItem,
                new CacheOptions
                {
                    UseL1Cache = true,  // ✅ ENABLED - Fast access for active processing
                    UseL2Cache = true,  // ✅ ENABLED - Redis for R2R processing queue
                    UseL3Cache = true,  // ✅ ENABLED - Persistent storage for processing state
                    L1TTL = TimeSpan.FromMinutes(30),
                    L2TTL = TimeSpan.FromHours(24),
                    L3TTL = TimeSpan.FromDays(7)
                });

            // Broadcast queued status via SignalR with cache invalidation
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem, _cacheService);

            _logger.LogInformation("Document queued successfully: {DocumentId}, Queue position: {QueueSize}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, _processingQueue.Count, correlationId);

            // Trigger processing if not at capacity
            _ = Task.Run(async () => await ProcessQueueAsync());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing document for processing: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, correlationId);
            return false;
        }
    }

    public async Task<bool> ProcessDocumentAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting R2R processing for document: {FileName}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.FileName, queueItem.DocumentId, correlationId);

            // Check circuit breaker
            if (IsCircuitBreakerOpen())
            {
                _logger.LogWarning("Circuit breaker is open, delaying processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    queueItem.DocumentId, correlationId);
                
                queueItem.NextRetryAt = DateTime.UtcNow.AddMinutes(_options.CircuitBreakerDelayMinutes);
                return false;
            }

            // Check rate limiting
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("r2r_document_ingestion");
            if (!canProceed)
            {
                _logger.LogInformation("Rate limit reached, delaying processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    queueItem.DocumentId, correlationId);
                
                queueItem.NextRetryAt = DateTime.UtcNow.AddSeconds(_options.RateLimitDelaySeconds);
                return false;
            }

            // Update status
            queueItem.Status = R2RProcessingStatusDto.Processing;
            queueItem.StartedAt = DateTime.UtcNow;
            _activeProcessing[queueItem.Id] = queueItem;

            // Broadcast processing status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            // Process with R2R API
            var success = await ProcessWithR2RAsync(queueItem);

            if (success)
            {
                // CRITICAL FIX: All successful R2R responses now use async verification pattern
                // This ensures R2RDocumentId is properly validated before marking as completed
                if (queueItem.R2RDocumentId != null && queueItem.R2RDocumentId.StartsWith("pending_"))
                {
                    // Async processing - document stays in Processing state for worker to verify
                    queueItem.Status = R2RProcessingStatusDto.Processing;
                    // Keep in _activeProcessing for worker to monitor

                    _logger.LogInformation("R2R async processing/verification started for document: {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);
                }
                else
                {
                    // This should not happen with the new logic, but keep as fallback
                    _logger.LogWarning("Unexpected R2R processing path for document: {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);

                    queueItem.Status = R2RProcessingStatusDto.Processing; // Force verification
                }

                _consecutiveFailures = 0;

                // Save document to database (with correct status)
                _logger.LogInformation("About to call SaveDocumentToDatabaseAsync for document {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
                await SaveDocumentToDatabaseAsync(queueItem);
            }
            else
            {
                await HandleProcessingFailure(queueItem, "R2R processing failed");
            }

            // CRITICAL FIX: Only remove from active processing if completed or failed
            // Keep in active processing if still needs R2R verification/synchronization
            if (queueItem.Status != R2RProcessingStatusDto.Processing)
            {
                _activeProcessing.TryRemove(queueItem.Id, out _);
                _logger.LogDebug("Removed document {DocumentId} from active processing (Status: {Status}), CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, queueItem.Status, correlationId);
            }
            else
            {
                // Keep in active processing for worker to monitor and sync
                _activeProcessing[queueItem.Id] = queueItem;
                _logger.LogInformation("Keeping document {DocumentId} in active processing for R2R verification (Status: {Status}), CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, queueItem.Status, correlationId);
            }

            // Update cache
            await _cacheService.SetAsync($"r2r:queue:{queueItem.Id}", queueItem,
                new CacheOptions
                {
                    L2TTL = TimeSpan.FromHours(24),
                    UseL3Cache = true
                });

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document with R2R: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, correlationId);

            await HandleProcessingFailure(queueItem, ex.Message);
            _activeProcessing.TryRemove(queueItem.Id, out _);

            return false;
        }
    }

    public async Task<List<R2RProcessingQueueItemDto>> GetProcessingQueueAsync(string? userId = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var allItems = new List<R2RProcessingQueueItemDto>();

        // Add queued items
        var queuedItems = _processingQueue.Where(item => userId == null || item.UserId == userId).ToList();
        allItems.AddRange(queuedItems);

        // Add active processing items
        var activeItems = _activeProcessing.Values.Where(item => userId == null || item.UserId == userId).ToList();
        allItems.AddRange(activeItems);

        // Add failed items
        var failedItems = _failedItems.Values.Where(item => userId == null || item.UserId == userId).ToList();
        allItems.AddRange(failedItems);

        // CRITICAL DEBUG: Log queue status for troubleshooting
        _logger.LogDebug("GetProcessingQueueAsync: Queued={QueuedCount}, Active={ActiveCount}, Failed={FailedCount}, Total={TotalCount}, CorrelationId: {CorrelationId}",
            queuedItems.Count, activeItems.Count, failedItems.Count, allItems.Count, correlationId);

        if (activeItems.Any())
        {
            _logger.LogDebug("Active processing items: {ActiveItems}, CorrelationId: {CorrelationId}",
                string.Join(", ", activeItems.Select(i => $"{i.DocumentId}({i.Status})")), correlationId);
        }

        return allItems.OrderBy(item => item.CreatedAt).ToList();
    }

    public async Task<bool> RetryProcessingAsync(Guid queueItemId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Try to find in failed items first
            if (_failedItems.TryRemove(queueItemId, out var failedItem))
            {
                failedItem.Status = R2RProcessingStatusDto.Queued;
                failedItem.RetryCount++;
                failedItem.NextRetryAt = null;
                failedItem.ErrorMessage = null;

                _processingQueue.Enqueue(failedItem);

                _logger.LogInformation("Retrying processing for document {DocumentId}, attempt {RetryCount}, CorrelationId: {CorrelationId}", 
                    failedItem.DocumentId, failedItem.RetryCount, correlationId);

                return true;
            }

            // Try to find in cache
            var cachedItem = await _cacheService.GetAsync<R2RProcessingQueueItemDto>($"r2r:queue:{queueItemId}");
            if (cachedItem != null && cachedItem.Status == R2RProcessingStatusDto.Failed)
            {
                cachedItem.Status = R2RProcessingStatusDto.Queued;
                cachedItem.RetryCount++;
                cachedItem.NextRetryAt = null;
                cachedItem.ErrorMessage = null;

                _processingQueue.Enqueue(cachedItem);

                _logger.LogInformation("Retrying processing for cached document {DocumentId}, attempt {RetryCount}, CorrelationId: {CorrelationId}", 
                    cachedItem.DocumentId, cachedItem.RetryCount, correlationId);

                return true;
            }

            _logger.LogWarning("Queue item {QueueItemId} not found for retry, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying processing for queue item {QueueItemId}, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);
            return false;
        }
    }

    public async Task<bool> CancelProcessingAsync(Guid queueItemId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Remove from active processing
            if (_activeProcessing.TryRemove(queueItemId, out var activeItem))
            {
                activeItem.Status = R2RProcessingStatusDto.Cancelled;
                await _cacheService.SetAsync($"r2r:queue:{queueItemId}", activeItem, 
                    new CacheOptions { L2TTL = TimeSpan.FromHours(24) });

                _logger.LogInformation("Cancelled active processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    activeItem.DocumentId, correlationId);
                return true;
            }

            // Remove from failed items
            if (_failedItems.TryRemove(queueItemId, out var failedItem))
            {
                failedItem.Status = R2RProcessingStatusDto.Cancelled;
                await _cacheService.SetAsync($"r2r:queue:{queueItemId}", failedItem, 
                    new CacheOptions { L2TTL = TimeSpan.FromHours(24) });

                _logger.LogInformation("Cancelled failed processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    failedItem.DocumentId, correlationId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling processing for queue item {QueueItemId}, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetProcessingStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["queued_items"] = _processingQueue.Count,
            ["active_processing"] = _activeProcessing.Count,
            ["failed_items"] = _failedItems.Count,
            ["circuit_breaker_open"] = _circuitBreakerOpen,
            ["consecutive_failures"] = _consecutiveFailures,
            ["last_failure"] = _lastR2RFailure,
            ["processing_capacity"] = _options.MaxConcurrentProcessing,
            ["available_capacity"] = _processingLimiter.CurrentCount
        };

        return stats;
    }

    public async Task OptimizeProcessingQueueAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Optimizing R2R processing queue, CorrelationId: {CorrelationId}", correlationId);

            // Reorder queue by priority and retry time
            var queueItems = new List<R2RProcessingQueueItemDto>();
            while (_processingQueue.TryDequeue(out var item))
            {
                queueItems.Add(item);
            }

            // Sort by priority (higher first) and then by retry time
            var optimizedItems = queueItems
                .Where(item => item.NextRetryAt == null || item.NextRetryAt <= DateTime.UtcNow)
                .OrderByDescending(item => (int)item.Priority)
                .ThenBy(item => item.CreatedAt)
                .ToList();

            // Re-enqueue optimized items
            foreach (var item in optimizedItems)
            {
                _processingQueue.Enqueue(item);
            }

            // Re-enqueue items that are not ready for retry
            var delayedItems = queueItems
                .Where(item => item.NextRetryAt > DateTime.UtcNow)
                .ToList();

            foreach (var item in delayedItems)
            {
                _processingQueue.Enqueue(item);
            }

            _logger.LogDebug("Queue optimization completed: {OptimizedCount} items ready, {DelayedCount} items delayed, CorrelationId: {CorrelationId}", 
                optimizedItems.Count, delayedItems.Count, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing processing queue, CorrelationId: {CorrelationId}", correlationId);
        }
    }

    // Private helper methods
    private async Task ProcessQueueAsync()
    {
        while (_processingQueue.TryDequeue(out var queueItem))
        {
            // Check if we can process more items
            if (_processingLimiter.CurrentCount == 0)
            {
                // Re-queue the item and break
                _processingQueue.Enqueue(queueItem);
                break;
            }

            // Check if item is ready for processing
            if (queueItem.NextRetryAt > DateTime.UtcNow)
            {
                // Re-queue for later
                _processingQueue.Enqueue(queueItem);
                continue;
            }

            // Process item asynchronously
            _ = Task.Run(async () =>
            {
                await _processingLimiter.WaitAsync();
                try
                {
                    await ProcessDocumentAsync(queueItem);
                }
                finally
                {
                    _processingLimiter.Release();
                }
            });
        }
    }

    private async Task<bool> ProcessWithR2RAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting R2R processing for document {DocumentId}, File: {FileName}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.FileName, correlationId);

            // ✅ QUICK WIN 1 - Validate file for R2R compliance
            var (isValid, validationErrors) = await _complianceService.ValidateFileForR2RAsync(queueItem.FilePath, queueItem.ContentType);
            if (!isValid)
            {
                var errorMessage = $"File validation failed: {string.Join(", ", validationErrors)}";
                _logger.LogWarning("R2R validation failed for document {DocumentId}: {ErrorMessage}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, errorMessage, correlationId);

                queueItem.ErrorCategory = ErrorCategory.Validation;
                queueItem.ErrorMessage = errorMessage;
                return false;
            }

            // CRITICAL FIX: Get corrected content type for R2R processing
            var correctedContentType = _complianceService.GetCorrectedContentType(queueItem.FilePath, queueItem.ContentType);
            if (correctedContentType != queueItem.ContentType)
            {
                _logger.LogInformation("Content type corrected for R2R processing: {OriginalContentType} -> {CorrectedContentType}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.ContentType, correctedContentType, queueItem.DocumentId, correlationId);
            }

            // ✅ QUICK WIN 2 - Generate R2R compliant filename and checksum
            var fileBytes = await File.ReadAllBytesAsync(queueItem.FilePath);
            var r2rCompliantFilename = _complianceService.GenerateR2RCompliantFilename(queueItem.FileName, queueItem.DocumentId);
            var checksum = _complianceService.ComputeChecksum(fileBytes);

            // Store enhanced metadata
            queueItem.OriginalFileName = queueItem.FileName;
            queueItem.FileName = r2rCompliantFilename;
            queueItem.Checksum = checksum;
            queueItem.UploadTimestamp = DateTime.UtcNow;

            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", r2rCompliantFilename)
            {
                Headers = new HeaderDictionary(),
                ContentType = correctedContentType // CRITICAL FIX: Use corrected content type
            };

            // ✅ QUICK WIN 3 - Enhanced metadata for R2R compliance
            var documentRequest = new DocumentRequest
            {
                File = formFile,
                Title = queueItem.OriginalFileName ?? queueItem.FileName,
                DocumentType = correctedContentType, // CRITICAL FIX: Use corrected content type
                Metadata = new Dictionary<string, object>
                {
                    ["original_filename"] = queueItem.OriginalFileName ?? queueItem.FileName,
                    ["r2r_compliant_filename"] = queueItem.FileName,
                    ["file_size"] = queueItem.FileSize,
                    ["checksum"] = queueItem.Checksum ?? "",
                    ["upload_timestamp"] = queueItem.UploadTimestamp,
                    ["user_id"] = queueItem.UserId,
                    ["company_id"] = queueItem.CompanyId,
                    ["document_id"] = queueItem.DocumentId,
                    ["file_id"] = queueItem.FileId,
                    ["content_type"] = correctedContentType, // CRITICAL FIX: Use corrected content type
                    ["original_content_type"] = queueItem.ContentType, // Keep original for reference
                    ["processing_priority"] = queueItem.Priority.ToString()
                }
            };

            // Add R2R collection ID if provided
            if (queueItem.CollectionId.HasValue)
            {
                // Get R2R collection ID from cache or database
                var r2rCollectionId = await GetR2RCollectionIdAsync(queueItem.CollectionId.Value);
                if (!string.IsNullOrEmpty(r2rCollectionId))
                {
                    documentRequest.CollectionIds = new List<string> { r2rCollectionId };
                    _logger.LogInformation("Using R2R collection ID {R2RCollectionId} for document {DocumentId}, CorrelationId: {CorrelationId}",
                        r2rCollectionId, queueItem.DocumentId, correlationId);
                }
                else
                {
                    _logger.LogWarning("No R2R collection ID found for collection {CollectionId}, document {DocumentId} will be uploaded without collection, CorrelationId: {CorrelationId}",
                        queueItem.CollectionId.Value, queueItem.DocumentId, correlationId);
                }
            }

            // Call real R2R API via DocumentClient
            var documentResponse = await _documentClient.CreateAsync(documentRequest);

            // ✅ CRITICAL FIX: Enhanced R2R response handling with comprehensive validation
            if (documentResponse != null)
            {
                // CRITICAL FIX: Log the complete API response for debugging
                _logger.LogInformation("R2R API Response received for document {DocumentId}: Id={R2RId}, TaskId={TaskId}, IngestionStatus={IngestionStatus}, ExtractionStatus={ExtractionStatus}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, documentResponse.Id ?? "NULL", documentResponse.TaskId ?? "NULL", documentResponse.IngestionStatus ?? "NULL", documentResponse.ExtractionStatus ?? "NULL", correlationId);

                if (!string.IsNullOrEmpty(documentResponse.Id))
                {
                    // CRITICAL FIX: Validate R2R document ID before proceeding
                    if (documentResponse.Id.Length < 10 || documentResponse.Id.Contains("error") || documentResponse.Id.Contains("failed"))
                    {
                        _logger.LogError("R2R returned invalid document ID '{R2RId}' for document {DocumentId}, treating as failed, CorrelationId: {CorrelationId}",
                            documentResponse.Id, queueItem.DocumentId, correlationId);

                        queueItem.ErrorCategory = ErrorCategory.Permanent;
                        queueItem.ErrorMessage = $"Invalid R2R document ID: {documentResponse.Id}";
                        return false;
                    }

                    // CRITICAL FIX: Treat ALL files as potentially async processing to ensure proper R2R completion verification
                    // This prevents premature completion marking and ensures R2RDocumentId is properly validated
                    _logger.LogInformation("R2R processing initiated for document {DocumentId}, R2R ID: {R2RDocumentId}, Size: {FileSize}KB, treating as async for verification, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, documentResponse.Id, queueItem.FileSize / 1024.0, correlationId);

                    // Set as pending for verification by background worker
                    queueItem.R2RDocumentId = $"pending_{documentResponse.Id}";
                    queueItem.TaskId = $"verify_{queueItem.DocumentId}";
                    queueItem.Status = R2RProcessingStatusDto.Processing;

                    return true;
                }
                else if (!string.IsNullOrEmpty(documentResponse.TaskId))
                {
                    // Async processing (HTTP 202) with TaskId - R2R standard approach
                    queueItem.TaskId = documentResponse.TaskId;
                    queueItem.R2RDocumentId = null; // Will be populated when task completes
                    queueItem.Status = R2RProcessingStatusDto.Processing;

                    _logger.LogInformation("R2R processing accepted (async) for document {DocumentId}, TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.TaskId, correlationId);

                    return true;
                }
                else
                {
                    // Fallback for older R2R versions without TaskId
                    queueItem.TaskId = $"legacy_{queueItem.DocumentId}";
                    queueItem.R2RDocumentId = null;
                    queueItem.Status = R2RProcessingStatusDto.Processing;

                    _logger.LogInformation("R2R processing accepted (legacy async) for document {DocumentId}, Legacy TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.TaskId, correlationId);

                    return true;
                }
            }
            else
            {
                _logger.LogError("R2R API returned null response for document {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            // ✅ QUICK WIN 5 - Enhanced error categorization
            queueItem.ErrorCategory = _complianceService.CategorizeError(httpEx.Message, httpEx);
            queueItem.ErrorMessage = httpEx.Message;

            _logger.LogError(httpEx, "HTTP error during R2R processing for document {DocumentId}, Category: {ErrorCategory}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.ErrorCategory, correlationId);
            return false;
        }
        catch (Exception ex)
        {
            queueItem.ErrorCategory = _complianceService.CategorizeError(ex.Message, ex);
            queueItem.ErrorMessage = ex.Message;

            _logger.LogError(ex, "R2R processing failed for document {DocumentId}, Category: {ErrorCategory}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.ErrorCategory, correlationId);
            return false;
        }
    }

    private async Task HandleProcessingFailure(R2RProcessingQueueItemDto queueItem, string errorMessage)
    {
        queueItem.Status = R2RProcessingStatusDto.Failed;
        queueItem.ErrorMessage = errorMessage;
        queueItem.RetryCount++;

        // ✅ QUICK WIN 6 - Enhanced error categorization for intelligent retry
        if (queueItem.ErrorCategory == ErrorCategory.None)
        {
            queueItem.ErrorCategory = _complianceService.CategorizeError(errorMessage);
        }

        _consecutiveFailures++;
        _lastR2RFailure = DateTime.UtcNow;

        // Broadcast failure status via SignalR with cache invalidation
        await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem, _cacheService);

        // Check if we should open circuit breaker
        if (_consecutiveFailures >= _options.CircuitBreakerThreshold)
        {
            _circuitBreakerOpen = true;
            _logger.LogWarning("Circuit breaker opened due to {ConsecutiveFailures} consecutive failures", _consecutiveFailures);
        }

        // ✅ QUICK WIN 7 - Intelligent retry based on error category
        var shouldRetry = ShouldRetryBasedOnErrorCategory(queueItem);

        if (shouldRetry && queueItem.RetryCount < queueItem.MaxRetries)
        {
            var delay = CalculateRetryDelayBasedOnCategory(queueItem);
            queueItem.NextRetryAt = DateTime.UtcNow.Add(delay);
            queueItem.Status = R2RProcessingStatusDto.Retrying;

            // Broadcast retry status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogInformation("Scheduling retry {RetryCount}/{MaxRetries} for document {DocumentId} in {Delay}, Category: {ErrorCategory}",
                queueItem.RetryCount, queueItem.MaxRetries, queueItem.DocumentId, delay, queueItem.ErrorCategory);

            // Re-queue for retry
            _processingQueue.Enqueue(queueItem);
        }
        else
        {
            // Max retries reached or permanent error, move to failed items
            _failedItems[queueItem.Id] = queueItem;

            // Broadcast final failure status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogError("Document processing failed permanently: {DocumentId}, Category: {ErrorCategory}, Error: {ErrorMessage}",
                queueItem.DocumentId, queueItem.ErrorCategory, errorMessage);
        }
    }

    private bool IsCircuitBreakerOpen()
    {
        if (!_circuitBreakerOpen) return false;

        // Check if we should close the circuit breaker
        var timeSinceLastFailure = DateTime.UtcNow - _lastR2RFailure;
        if (timeSinceLastFailure > TimeSpan.FromMinutes(_options.CircuitBreakerResetMinutes))
        {
            _circuitBreakerOpen = false;
            _consecutiveFailures = 0;
            _logger.LogInformation("Circuit breaker closed after {TimeSinceLastFailure}", timeSinceLastFailure);
            return false;
        }

        return true;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(_options.BaseRetryDelaySeconds);
        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

        return exponentialDelay.Add(jitter);
    }

    // ✅ QUICK WIN 8 - Intelligent retry logic based on error category
    private bool ShouldRetryBasedOnErrorCategory(R2RProcessingQueueItemDto queueItem)
    {
        return queueItem.ErrorCategory switch
        {
            ErrorCategory.Transient => true,        // Always retry transient errors
            ErrorCategory.RateLimit => true,        // Retry rate limit errors with longer delay
            ErrorCategory.Authentication => false,   // Don't retry auth errors
            ErrorCategory.Validation => false,      // Don't retry validation errors
            ErrorCategory.FileFormat => false,      // Don't retry format errors
            ErrorCategory.FileSize => false,        // Don't retry size errors
            ErrorCategory.Permanent => false,       // Don't retry permanent errors
            ErrorCategory.None => true,             // Default to retry for unknown errors
            _ => true
        };
    }

    private TimeSpan CalculateRetryDelayBasedOnCategory(R2RProcessingQueueItemDto queueItem)
    {
        var baseDelay = CalculateRetryDelay(queueItem.RetryCount);

        return queueItem.ErrorCategory switch
        {
            ErrorCategory.RateLimit => TimeSpan.FromMinutes(2 * queueItem.RetryCount), // Longer delay for rate limits
            ErrorCategory.Transient => baseDelay,                                      // Standard exponential backoff
            _ => baseDelay
        };
    }

    private async Task<string?> GetR2RCollectionIdAsync(Guid collectionId)
    {
        try
        {
            // First check cache
            var cacheKey = $"r2r:collection:mapping:{collectionId}";
            var cachedId = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedId))
            {
                return cachedId;
            }

            // Check database
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var collection = await context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (collection != null && !string.IsNullOrEmpty(collection.R2RCollectionId))
            {
                // Cache for future use
                await _cacheService.SetAsync(cacheKey, collection.R2RCollectionId,
                    new CacheOptions { L1TTL = TimeSpan.FromMinutes(30), L2TTL = TimeSpan.FromHours(2) });

                return collection.R2RCollectionId;
            }

            return null;
        }
        catch (Exception ex)
        {
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogError(ex, "Error getting R2R collection ID for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return null;
        }
    }

    private async Task SaveDocumentToDatabaseAsync(R2RProcessingQueueItemDto queueItem)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var correlationId = _correlationService.GetCorrelationId();

            _logger.LogInformation("Saving document to database: {DocumentId}, Status: {Status}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.Status, queueItem.R2RDocumentId ?? "NULL", correlationId);

            // CRITICAL FIX: Declare existingDocument at higher scope for SignalR event data
            Document? existingDocument = null;

            // CRITICAL FIX: Use execution strategy to wrap database transaction for NpgsqlRetryingExecutionStrategy compatibility
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Check if document already exists
                existingDocument = await context.Documents
                    .FirstOrDefaultAsync(d => d.Id == queueItem.DocumentId);

                if (existingDocument != null)
                {
                    // CRITICAL FIX: Validate R2RDocumentId before updating status to Ready
                    if (queueItem.Status == R2RProcessingStatusDto.Completed)
                    {
                        if (string.IsNullOrEmpty(queueItem.R2RDocumentId) || queueItem.R2RDocumentId.StartsWith("pending_"))
                        {
                            _logger.LogError("Cannot mark document as Ready - R2RDocumentId is null or pending: {R2RDocumentId}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}",
                                queueItem.R2RDocumentId ?? "NULL", queueItem.DocumentId, correlationId);

                            // Keep in processing state until valid R2RDocumentId is available
                            existingDocument.Status = (int)Data.Entities.DocumentStatus.Processing;
                            existingDocument.IsProcessing = true;
                            existingDocument.ProcessingError = "R2RDocumentId validation failed";
                        }
                        else
                        {
                            // Valid R2RDocumentId - mark as Ready
                            existingDocument.R2RDocumentId = queueItem.R2RDocumentId;
                            existingDocument.R2RIngestionJobId = queueItem.JobId; // Update JobId for audit trail
                            existingDocument.R2RTaskId = queueItem.TaskId; // Update TaskId for progress tracking
                            existingDocument.R2RProcessedAt = DateTime.UtcNow;
                            existingDocument.Status = (int)Data.Entities.DocumentStatus.Ready;
                            existingDocument.IsProcessing = false;
                            existingDocument.ProcessingError = null;
                            existingDocument.ProcessingProgress = 100.0; // Mark as 100% complete

                            _logger.LogInformation("Document marked as Ready with valid R2RDocumentId: {R2RDocumentId}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}",
                                queueItem.R2RDocumentId, queueItem.DocumentId, correlationId);
                        }
                    }
                    else if (queueItem.Status == R2RProcessingStatusDto.Processing)
                    {
                        existingDocument.Status = (int)Data.Entities.DocumentStatus.Processing;
                        existingDocument.IsProcessing = true;

                        // Calculate processing progress based on R2R status
                        existingDocument.ProcessingProgress = CalculateProcessingProgress(queueItem);

                        // Update R2RDocumentId even if pending for tracking
                        if (!string.IsNullOrEmpty(queueItem.R2RDocumentId))
                        {
                            existingDocument.R2RDocumentId = queueItem.R2RDocumentId;
                        }
                        // Update JobId for audit trail
                        if (!string.IsNullOrEmpty(queueItem.JobId))
                        {
                            existingDocument.R2RIngestionJobId = queueItem.JobId;
                        }
                        // Update TaskId for progress tracking
                        if (!string.IsNullOrEmpty(queueItem.TaskId))
                        {
                            existingDocument.R2RTaskId = queueItem.TaskId;
                        }
                    }
                    else if (queueItem.Status == R2RProcessingStatusDto.Failed)
                    {
                        existingDocument.Status = (int)Data.Entities.DocumentStatus.Error;
                        existingDocument.IsProcessing = false;
                        existingDocument.ProcessingError = queueItem.ErrorMessage ?? "R2R processing failed";
                    }

                    existingDocument.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Updated existing document {DocumentId} with R2R data, Status: {Status}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, existingDocument.Status, existingDocument.R2RDocumentId ?? "NULL", correlationId);
                }
            else
            {
                // Create new document record
                // Try to parse UserId as Guid, if it fails, log the value and use a default
                Guid userGuid;
                if (!Guid.TryParse(queueItem.UserId, out userGuid))
                {
                    _logger.LogError("Invalid UserId format: '{UserId}', using empty Guid, CorrelationId: {CorrelationId}",
                        queueItem.UserId, correlationId);
                    userGuid = Guid.Empty;
                }

                // Get user's company ID from database using scoped context
                User? user;
                using (var userScope = _serviceScopeFactory.CreateScope())
                {
                    var userContext = userScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    user = await userContext.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
                }

                if (user == null)
                {
                    _logger.LogError("User {UserId} not found in database, CorrelationId: {CorrelationId}",
                        queueItem.UserId, correlationId);
                    return;
                }

                    // CRITICAL FIX: Apply same validation logic for new documents
                    int documentStatus;
                    string? r2rDocumentId = null;

                    if (queueItem.Status == R2RProcessingStatusDto.Completed)
                    {
                        if (string.IsNullOrEmpty(queueItem.R2RDocumentId) || queueItem.R2RDocumentId.StartsWith("pending_"))
                        {
                            _logger.LogError("Cannot create document as Ready - R2RDocumentId is null or pending: {R2RDocumentId}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}",
                                queueItem.R2RDocumentId ?? "NULL", queueItem.DocumentId, correlationId);

                            documentStatus = (int)Data.Entities.DocumentStatus.Processing;
                            r2rDocumentId = queueItem.R2RDocumentId; // Keep pending ID for tracking
                        }
                        else
                        {
                            documentStatus = (int)Data.Entities.DocumentStatus.Ready;
                            r2rDocumentId = queueItem.R2RDocumentId;
                        }
                    }
                    else if (queueItem.Status == R2RProcessingStatusDto.Processing)
                    {
                        documentStatus = (int)Data.Entities.DocumentStatus.Processing;
                        r2rDocumentId = queueItem.R2RDocumentId; // Keep pending ID for tracking
                    }
                    else if (queueItem.Status == R2RProcessingStatusDto.Failed)
                    {
                        documentStatus = (int)Data.Entities.DocumentStatus.Error;
                    }
                    else
                    {
                        documentStatus = (int)Data.Entities.DocumentStatus.Draft;
                    }

                    var document = new Document
                    {
                        Id = queueItem.DocumentId,
                        Name = queueItem.FileName,
                        FileName = queueItem.FileName,
                        OriginalFileName = queueItem.FileName,
                        SizeInBytes = queueItem.FileSize,
                        ContentType = queueItem.ContentType,
                        FileHash = queueItem.Checksum ?? string.Empty, // CRITICAL FIX: Set FileHash from checksum
                        Status = documentStatus,
                        R2RDocumentId = r2rDocumentId,
                        R2RIngestionJobId = queueItem.JobId, // Set JobId for audit trail
                        R2RTaskId = queueItem.TaskId, // Set TaskId for progress tracking
                        R2RProcessedAt = documentStatus == (int)Data.Entities.DocumentStatus.Ready ? DateTime.UtcNow : (DateTime?)null,
                        ProcessingProgress = documentStatus == (int)Data.Entities.DocumentStatus.Ready ? 100.0 : CalculateProcessingProgress(queueItem),
                        UserId = userGuid,
                        CompanyId = user.CompanyId,
                        CollectionId = queueItem.CollectionId,
                        FilePath = string.Empty,
                        Tags = new List<string>(),
                        Metadata = new Dictionary<string, object>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsProcessing = documentStatus == (int)Data.Entities.DocumentStatus.Processing
                    };

                    context.Documents.Add(document);

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Created new document record {DocumentId}, Status: {Status}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, documentStatus, r2rDocumentId ?? "NULL", correlationId);
                }
                }
                catch (Exception transactionEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(transactionEx, "Database transaction failed for document {DocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, correlationId);
                    throw;
                }
            });

            // CRITICAL: Invalidate document caches BEFORE SignalR broadcast using ONLY tag-based invalidation
            var tags = new List<string> { "documents", "document-lists", $"user:{queueItem.UserId}" };
            if (queueItem.CollectionId.HasValue)
            {
                tags.Add($"collection:{queueItem.CollectionId}");
            }
            // CRITICAL FIX: Pass tenantId to ensure tag transformation matches SET operation
            // Use CompanyId as TenantId (they are the same in our multi-tenant architecture)
            var tenantId = queueItem.CompanyId?.ToString();

            // UNIFIED CACHE INVALIDATION STRATEGY:
            // 1. Tag-based invalidation for new cache entries (ForDocumentLists)
            await _cacheService.InvalidateByTagsAsync(tags, tenantId);

            // 2. Pattern-based invalidation for ALL document cache patterns to ensure complete invalidation
            var patterns = new[]
            {
                "*documents:search:*",
                "*documents*",
                "*document-lists*",
                $"*collection:{queueItem.CollectionId}*",
                $"*user:{queueItem.UserId}*",
                "cleverdocs2:type:pageddocumentresultdto:documents:search:*"
            };

            foreach (var pattern in patterns)
            {
                await _cacheService.InvalidateAsync(pattern, tenantId);
            }

            _logger.LogInformation("Document {DocumentId} saved to database successfully and cache invalidated (both tag-based and pattern-based), CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);

            // Broadcast document upload completion to refresh collection view with event persistence
            _logger.LogInformation("Broadcasting FileUploadCompleted with event persistence for user: {UserId}, document: {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.UserId, queueItem.DocumentId, correlationId);

            // CRITICAL FIX: Determine correct status based on R2R processing state
            var signalRStatus = "processing"; // Default to processing until R2R completes

            // Only mark as completed if we have a valid R2RDocumentId (not null, not pending)
            if (!string.IsNullOrEmpty(queueItem.R2RDocumentId) &&
                !queueItem.R2RDocumentId.StartsWith("pending_") &&
                queueItem.Status == R2RProcessingStatusDto.Completed)
            {
                signalRStatus = "completed";
            }
            else if (queueItem.Status == R2RProcessingStatusDto.Failed)
            {
                signalRStatus = "failed";
            }
            // Otherwise keep as "processing"

            var eventData = new {
                documentId = queueItem.DocumentId,
                collectionId = queueItem.CollectionId,
                action = existingDocument != null ? "updated" : "added",
                timestamp = DateTime.UtcNow,
                r2rDocumentId = queueItem.R2RDocumentId,
                status = signalRStatus,
                progress = signalRStatus == "completed" ? 100.0 : CalculateProcessingProgress(queueItem),
                currentStep = GetCurrentProcessingStep(queueItem)
            };

            _logger.LogInformation("Sending SignalR event data with persistence: {@EventData} to user: {UserId}, CorrelationId: {CorrelationId}",
                eventData, queueItem.UserId, correlationId);

            // Use the new method that includes event persistence for race condition handling
            await _hubContext.BroadcastFileUploadCompletedToUser(queueItem.UserId, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document {DocumentId} to database, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, _correlationService.GetCorrelationId());
            throw;
        }
    }

    public async Task<bool> CheckR2RStatusAndUpdateAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Checking R2R status for document {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);

            // CRITICAL FIX: Check for documents with empty R2RDocumentId (not just pending_*)
            // This handles the case where documents were uploaded but R2R processing completed without updating our database
            if (string.IsNullOrEmpty(queueItem.R2RDocumentId))
            {
                _logger.LogInformation("Document {DocumentId} has empty R2RDocumentId, searching R2R for completed processing, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);

                // Search R2R documents to find one with our document_id in metadata
                try
                {
                    var r2rDocuments = await _documentClient.ListAsync();

                    if (r2rDocuments?.Results != null)
                    {
                        // Look for a document with our document_id in metadata
                        var matchingDocument = r2rDocuments.Results.FirstOrDefault(doc =>
                            doc.Metadata != null &&
                            doc.Metadata.ContainsKey("document_id") &&
                            doc.Metadata["document_id"]?.ToString() == queueItem.DocumentId.ToString());

                        if (matchingDocument != null)
                        {
                            _logger.LogInformation("Found matching R2R document for {DocumentId}, R2R ID: {R2RDocumentId}, Status: {IngestionStatus}, CorrelationId: {CorrelationId}",
                                queueItem.DocumentId, matchingDocument.Id, matchingDocument.IngestionStatus, correlationId);

                            // Check if R2R processing is completed
                            if (matchingDocument.IngestionStatus == "success" && matchingDocument.ExtractionStatus == "success")
                            {
                                // Update queue item with actual R2R document ID
                                queueItem.R2RDocumentId = matchingDocument.Id;
                                queueItem.Status = R2RProcessingStatusDto.Completed;
                                queueItem.CompletedAt = DateTime.UtcNow;

                                // Save document to database with completed status
                                await SaveDocumentToDatabaseAsync(queueItem);

                                // Broadcast completion status via SignalR (R2R processing update)
                                await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

                                // Also broadcast file upload completion for UI refresh with event persistence
                                _logger.LogInformation("Broadcasting FileUploadCompleted (R2R completion) with event persistence for user: {UserId}, document: {DocumentId}, CorrelationId: {CorrelationId}",
                                    queueItem.UserId, queueItem.DocumentId, correlationId);

                                var eventData = new {
                                    documentId = queueItem.DocumentId,
                                    collectionId = queueItem.CollectionId,
                                    action = "completed",
                                    timestamp = DateTime.UtcNow,
                                    r2rDocumentId = queueItem.R2RDocumentId,
                                    status = "completed"
                                };

                                _logger.LogInformation("Sending SignalR event data (R2R completion) with persistence: {@EventData} to user: {UserId}, CorrelationId: {CorrelationId}",
                                    eventData, queueItem.UserId, correlationId);

                                // Use the new method that includes event persistence for race condition handling
                                await _hubContext.BroadcastFileUploadCompletedToUser(queueItem.UserId, eventData);

                                // Remove from active processing
                                _activeProcessing.TryRemove(queueItem.Id, out _);

                                return true;
                            }
                            else
                            {
                                _logger.LogDebug("R2R document {R2RDocumentId} for document {DocumentId} still processing: ingestion={IngestionStatus}, extraction={ExtractionStatus}, CorrelationId: {CorrelationId}",
                                    matchingDocument.Id, queueItem.DocumentId, matchingDocument.IngestionStatus, matchingDocument.ExtractionStatus, correlationId);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("No matching R2R document found for document {DocumentId}, still processing, CorrelationId: {CorrelationId}",
                                queueItem.DocumentId, correlationId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching R2R documents for document {DocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, correlationId);
                }
            }
            // Legacy support: For documents with pending R2R IDs, we need to check if R2R has completed processing
            else if (queueItem.R2RDocumentId.StartsWith("pending_"))
            {
                // Extract the actual document ID from the pending ID
                var actualDocumentId = queueItem.R2RDocumentId.Replace("pending_", "");

                // Try to get the document from R2R API to see if it's been processed
                try
                {
                    var r2rDocument = await _documentClient.GetAsync(actualDocumentId);

                    if (r2rDocument != null && !string.IsNullOrEmpty(r2rDocument.Id))
                    {
                        // Document found in R2R - processing completed successfully
                        _logger.LogInformation("R2R processing completed for document {DocumentId}, R2R ID: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                            queueItem.DocumentId, r2rDocument.Id, correlationId);

                        // Update queue item with actual R2R document ID
                        queueItem.R2RDocumentId = r2rDocument.Id;
                        queueItem.Status = R2RProcessingStatusDto.Completed;
                        queueItem.CompletedAt = DateTime.UtcNow;

                        // Save document to database with completed status
                        await SaveDocumentToDatabaseAsync(queueItem);

                        // Broadcast completion status via SignalR (R2R processing update)
                        await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

                        // Also broadcast file upload completion for UI refresh with event persistence
                        _logger.LogInformation("Broadcasting FileUploadCompleted (R2R completion) with event persistence for user: {UserId}, document: {DocumentId}, CorrelationId: {CorrelationId}",
                            queueItem.UserId, queueItem.DocumentId, correlationId);

                        var eventData = new {
                            documentId = queueItem.DocumentId,
                            collectionId = queueItem.CollectionId,
                            action = "completed",
                            timestamp = DateTime.UtcNow,
                            r2rDocumentId = queueItem.R2RDocumentId,
                            status = "completed"
                        };

                        _logger.LogInformation("Sending SignalR event data (R2R completion) with persistence: {@EventData} to user: {UserId}, CorrelationId: {CorrelationId}",
                            eventData, queueItem.UserId, correlationId);

                        // Use the new method that includes event persistence for race condition handling
                        await _hubContext.BroadcastFileUploadCompletedToUser(queueItem.UserId, eventData);

                        // Remove from active processing
                        _activeProcessing.TryRemove(queueItem.Id, out _);

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // If we get a 404 or other error, the document might still be processing
                    _logger.LogDebug("R2R document {DocumentId} not yet available (still processing), CorrelationId: {CorrelationId}",
                        actualDocumentId, correlationId);
                }
            }

            // NEW: Check R2R progress using TaskId if available
            if (!string.IsNullOrEmpty(queueItem.TaskId))
            {
                var progressData = await GetR2RProgressAsync(queueItem.TaskId);
                if (progressData != null)
                {
                    // Broadcast progress update via SignalR
                    var progressEventData = new {
                        documentId = queueItem.DocumentId,
                        collectionId = queueItem.CollectionId,
                        action = "progress_update",
                        timestamp = DateTime.UtcNow,
                        taskId = progressData.TaskId,
                        progress = progressData.Progress,
                        currentStep = progressData.CurrentStep,
                        status = progressData.Status,
                        estimatedCompletion = progressData.EstimatedCompletion
                    };

                    _logger.LogDebug("Broadcasting R2R progress update: DocumentId={DocumentId}, Progress={Progress}%, Step={Step}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, progressData.Progress, progressData.CurrentStep, correlationId);

                    await _hubContext.BroadcastR2RProgressUpdate(queueItem.UserId, progressEventData);

                    // Check if processing is completed based on R2R status
                    if (progressData.Status == "completed" && progressData.Progress >= 100)
                    {
                        _logger.LogInformation("R2R processing completed based on progress API for document {DocumentId}, CorrelationId: {CorrelationId}",
                            queueItem.DocumentId, correlationId);

                        queueItem.Status = R2RProcessingStatusDto.Completed;
                        queueItem.CompletedAt = DateTime.UtcNow;

                        // Save document to database with completed status
                        await SaveDocumentToDatabaseAsync(queueItem);

                        // Remove from active processing
                        _activeProcessing.TryRemove(queueItem.Id, out _);

                        return true;
                    }
                }
            }

            // Check if document has been processing for too long (timeout)
            var processingDuration = DateTime.UtcNow - queueItem.StartedAt;
            if (processingDuration > TimeSpan.FromMinutes(10)) // 10 minute timeout
            {
                _logger.LogWarning("R2R processing timeout for document {DocumentId} after {Duration}, marking as failed, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, processingDuration, correlationId);

                await HandleProcessingFailure(queueItem, $"R2R processing timeout after {processingDuration}");
                return false;
            }

            // Document is still processing, no action needed
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking R2R status for document {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);
            return false;
        }
    }

    /// <summary>
    /// CRITICAL FIX: Restore processing queue from database on startup
    /// This method finds documents that are stuck in "Processing" status and re-queues them for R2R processing
    /// </summary>
    private async Task RestoreProcessingQueueFromDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("🔄 STARTUP: RestoreProcessingQueueFromDatabaseAsync method started");

            // Wait a bit for the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(5));

            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogInformation("🔄 STARTUP: Restoring R2R processing queue from database, CorrelationId: {CorrelationId}", correlationId);

            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // CRITICAL FIX: Find documents that are stuck in Processing status ONLY
            // Do NOT include documents marked as Ready - they should not be reprocessed
            // Only reprocess documents that are explicitly in Processing state
            _logger.LogInformation("🔄 STARTUP: Searching for stuck documents in Processing status, CorrelationId: {CorrelationId}", correlationId);

            var stuckDocuments = await context.Documents
                .Where(d => d.Status == (int)Data.Entities.DocumentStatus.Processing && d.IsProcessing == true)
                .ToListAsync();

            // CRITICAL FIX: Separately identify documents with data inconsistency (Ready status but no R2RDocumentId)
            var inconsistentDocuments = await context.Documents
                .Where(d => d.Status == (int)Data.Entities.DocumentStatus.Ready &&
                           (d.R2RDocumentId == null || d.R2RDocumentId == ""))
                .ToListAsync();

            if (inconsistentDocuments.Any())
            {
                _logger.LogError("🚨 DATA INCONSISTENCY: Found {Count} documents marked as Ready but missing R2RDocumentId. These need manual review: {DocumentIds}, CorrelationId: {CorrelationId}",
                    inconsistentDocuments.Count,
                    string.Join(", ", inconsistentDocuments.Select(d => d.Id)),
                    correlationId);

                // Mark these documents as Processing so they can be properly reprocessed
                foreach (var doc in inconsistentDocuments)
                {
                    doc.Status = (int)Data.Entities.DocumentStatus.Processing;
                    doc.IsProcessing = true;
                    doc.ProcessingError = "Data inconsistency detected - Ready status without R2RDocumentId";
                    doc.UpdatedAt = DateTime.UtcNow;
                    doc.R2RProcessedAt = null; // Clear the incorrect timestamp
                }
                await context.SaveChangesAsync();

                // Add inconsistent documents to the stuck documents list for reprocessing
                stuckDocuments.AddRange(inconsistentDocuments);
            }

            _logger.LogInformation("🔄 STARTUP: Found {Count} documents matching stuck criteria, CorrelationId: {CorrelationId}",
                stuckDocuments.Count, correlationId);

            if (stuckDocuments.Any())
            {
                _logger.LogInformation("🔄 STARTUP: Found {Count} documents stuck in processing status, re-queuing for R2R processing, CorrelationId: {CorrelationId}",
                    stuckDocuments.Count, correlationId);

                foreach (var document in stuckDocuments)
                {
                    try
                    {
                        // Create R2R processing queue item from database document
                        var queueItem = new R2RProcessingQueueItemDto
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = document.Id,
                            FileName = document.FileName ?? document.Name,
                            FilePath = document.FilePath ?? "",
                            FileSize = document.SizeInBytes,
                            Checksum = document.FileHash, // CRITICAL FIX: Use existing FileHash for restoration
                            UserId = document.UserId.ToString(),
                            CollectionId = document.CollectionId,
                            CompanyId = document.CompanyId,
                            ContentType = document.ContentType ?? "application/octet-stream",
                            Priority = R2RProcessingPriorityDto.Normal,
                            Status = R2RProcessingStatusDto.Queued,
                            CreatedAt = DateTime.UtcNow,
                            RetryCount = 0,
                            MaxRetries = 3,
                            JobId = document.R2RIngestionJobId ?? Guid.NewGuid().ToString() // Use existing JobId or generate new one
                        };

                        // Add to processing queue
                        _processingQueue.Enqueue(queueItem);

                        // Cache for persistence
                        await _cacheService.SetAsync($"r2r:queue:{queueItem.Id}", queueItem,
                            new CacheOptions
                            {
                                UseL1Cache = true,
                                UseL2Cache = true,
                                UseL3Cache = true,
                                L1TTL = TimeSpan.FromMinutes(30),
                                L2TTL = TimeSpan.FromHours(24),
                                L3TTL = TimeSpan.FromDays(7)
                            });

                        _logger.LogInformation("🔄 STARTUP: Re-queued document {DocumentId} ({FileName}) for R2R processing, CorrelationId: {CorrelationId}",
                            document.Id, document.Name, correlationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "🔄 STARTUP: Error re-queuing document {DocumentId} for R2R processing, CorrelationId: {CorrelationId}",
                            document.Id, correlationId);
                    }
                }

                // Trigger processing
                _ = Task.Run(async () => await ProcessQueueAsync());

                _logger.LogInformation("✅ STARTUP: Successfully restored {Count} documents to R2R processing queue, CorrelationId: {CorrelationId}",
                    stuckDocuments.Count, correlationId);
            }
            else
            {
                _logger.LogInformation("✅ STARTUP: No documents found in processing status, queue restoration complete, CorrelationId: {CorrelationId}", correlationId);
            }
        }
        catch (Exception ex)
        {
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogError(ex, "❌ STARTUP: Error restoring R2R processing queue from database, CorrelationId: {CorrelationId}", correlationId);
        }
    }

    /// <summary>
    /// Get R2R processing progress for a document using TaskId
    /// </summary>
    public async Task<R2RProgressDto?> GetR2RProgressAsync(string taskId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Getting R2R progress for TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                taskId, correlationId);

            // Call R2R API endpoint for task status
            var response = await _httpClient.GetAsync($"/ingest/status/{taskId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var statusData = JsonSerializer.Deserialize<R2RTaskStatusResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (statusData != null)
                {
                    var progressDto = new R2RProgressDto
                    {
                        TaskId = statusData.TaskId,
                        Status = statusData.Status,
                        Progress = statusData.Progress,
                        CurrentStep = statusData.CurrentStep,
                        EstimatedCompletion = statusData.EstimatedCompletion
                    };

                    _logger.LogDebug("R2R progress retrieved: TaskId={TaskId}, Progress={Progress}%, Step={Step}, CorrelationId: {CorrelationId}",
                        taskId, progressDto.Progress, progressDto.CurrentStep, correlationId);

                    return progressDto;
                }
            }
            else
            {
                _logger.LogWarning("R2R API returned {StatusCode} for TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                    response.StatusCode, taskId, correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting R2R progress for TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                taskId, correlationId);
        }

        return null;
    }

    /// <summary>
    /// Calculate processing progress percentage based on R2R queue item status
    /// </summary>
    private double CalculateProcessingProgress(R2RProcessingQueueItemDto queueItem)
    {
        // Base progress on status and R2R document ID state
        if (queueItem.Status == R2RProcessingStatusDto.Completed)
        {
            return 100.0;
        }

        if (queueItem.Status == R2RProcessingStatusDto.Failed)
        {
            return 0.0;
        }

        if (queueItem.Status == R2RProcessingStatusDto.Processing)
        {
            // Check R2R document ID state for more granular progress
            if (string.IsNullOrEmpty(queueItem.R2RDocumentId))
            {
                return 10.0; // Initial processing
            }
            else if (queueItem.R2RDocumentId.StartsWith("pending_"))
            {
                return 50.0; // R2R processing in progress
            }
            else
            {
                return 90.0; // Almost complete, final verification
            }
        }

        if (queueItem.Status == R2RProcessingStatusDto.Queued)
        {
            return 5.0;
        }

        if (queueItem.Status == R2RProcessingStatusDto.Retrying)
        {
            return 25.0;
        }

        return 0.0; // Default for unknown states
    }

    /// <summary>
    /// Get current processing step description based on R2R queue item status
    /// </summary>
    private string GetCurrentProcessingStep(R2RProcessingQueueItemDto queueItem)
    {
        if (queueItem.Status == R2RProcessingStatusDto.Completed)
        {
            return "Processing completed";
        }

        if (queueItem.Status == R2RProcessingStatusDto.Failed)
        {
            return "Processing failed";
        }

        if (queueItem.Status == R2RProcessingStatusDto.Processing)
        {
            // Check R2R document ID state for more specific step
            if (string.IsNullOrEmpty(queueItem.R2RDocumentId))
            {
                return "Initializing R2R processing";
            }
            else if (queueItem.R2RDocumentId.StartsWith("pending_"))
            {
                return "R2R ingestion and extraction";
            }
            else
            {
                return "Finalizing processing";
            }
        }

        if (queueItem.Status == R2RProcessingStatusDto.Queued)
        {
            return "Queued for processing";
        }

        if (queueItem.Status == R2RProcessingStatusDto.Retrying)
        {
            return $"Retrying (attempt {queueItem.RetryCount})";
        }

        return "Processing";
    }
}

/// <summary>
/// R2R Task Status Response from API
/// </summary>
public class R2RTaskStatusResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Progress { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>
/// R2R Progress DTO for internal use
/// </summary>
public class R2RProgressDto
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Progress { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>
/// Configuration options for document processing service
/// </summary>
public class DocumentProcessingOptions
{
    public int MaxConcurrentProcessing { get; set; } = 5;
    public int RateLimitDelaySeconds { get; set; } = 10;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDelayMinutes { get; set; } = 10;
    public int CircuitBreakerResetMinutes { get; set; } = 30;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnableOptimization { get; set; } = true;
    public TimeSpan OptimizationInterval { get; set; } = TimeSpan.FromMinutes(5);

    // Background worker configuration
    public int ProcessingIntervalMs { get; set; } = 5000; // 5 seconds when queue has items
    public int IdleIntervalMs { get; set; } = 30000; // 30 seconds when queue is empty
}
