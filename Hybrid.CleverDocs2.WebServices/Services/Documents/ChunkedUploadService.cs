using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service for handling chunked uploads of large files with resumable capability
/// </summary>
public class ChunkedUploadService : IChunkedUploadService
{
    private readonly IUploadStorageService _storageService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<ChunkedUploadService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly ChunkedUploadOptions _options;

    // Thread-safe storage for chunk sessions
    private readonly ConcurrentDictionary<string, ChunkedUploadInfoDto> _chunkSessions = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _chunkLocks = new();

    public ChunkedUploadService(
        IUploadStorageService storageService,
        IMultiLevelCacheService cacheService,
        ILogger<ChunkedUploadService> logger,
        ICorrelationService correlationService,
        IOptions<ChunkedUploadOptions> options)
    {
        _storageService = storageService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
    }

    public async Task<ChunkedUploadInfoDto> InitializeChunkedUploadAsync(Guid sessionId, Guid fileId, long fileSize, string fileName, int chunkSize)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var chunkKey = GetChunkKey(sessionId, fileId);

        try
        {
            _logger.LogInformation("Initializing chunked upload for file {FileName} ({FileSize} bytes), chunk size: {ChunkSize}, SessionId: {SessionId}, FileId: {FileId}, CorrelationId: {CorrelationId}", 
                fileName, fileSize, chunkSize, sessionId, fileId, correlationId);

            // Calculate chunk information
            var totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);
            
            var chunkInfo = new ChunkedUploadInfoDto
            {
                ChunkSessionId = Guid.NewGuid(),
                TotalChunks = totalChunks,
                ChunkSize = chunkSize,
                IsResumable = true
            };

            // Initialize chunk details
            for (int i = 0; i < totalChunks; i++)
            {
                var startByte = (long)i * chunkSize;
                var endByte = Math.Min(startByte + chunkSize - 1, fileSize - 1);
                var size = endByte - startByte + 1;

                chunkInfo.Chunks.Add(new ChunkInfoDto
                {
                    ChunkNumber = i,
                    StartByte = startByte,
                    EndByte = endByte,
                    Size = size,
                    Status = ChunkStatusDto.Pending
                });
            }

            // Store chunk session in memory only (Redis removed for performance)
            _chunkSessions[chunkKey] = chunkInfo;

            // Create chunk lock
            _chunkLocks[chunkKey] = new SemaphoreSlim(1, 1);

            _logger.LogInformation("Chunked upload initialized: {TotalChunks} chunks for file {FileName}, CorrelationId: {CorrelationId}", 
                totalChunks, fileName, correlationId);

            return chunkInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing chunked upload for file {FileName}, CorrelationId: {CorrelationId}", 
                fileName, correlationId);
            throw;
        }
    }

    public async Task<bool> UploadChunkAsync(Guid sessionId, Guid fileId, int chunkNumber, byte[] chunkData, string? checksum = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var chunkKey = GetChunkKey(sessionId, fileId);

        try
        {
            _logger.LogDebug("Uploading chunk {ChunkNumber} for session {SessionId}, file {FileId}, size: {ChunkSize} bytes, CorrelationId: {CorrelationId}", 
                chunkNumber, sessionId, fileId, chunkData.Length, correlationId);

            // Get chunk session
            var chunkSession = await GetChunkSessionAsync(chunkKey);
            if (chunkSession == null)
            {
                _logger.LogWarning("Chunk session not found for key {ChunkKey}, CorrelationId: {CorrelationId}", 
                    chunkKey, correlationId);
                return false;
            }

            // Get chunk lock to ensure thread safety
            var chunkLock = _chunkLocks.GetOrAdd(chunkKey, _ => new SemaphoreSlim(1, 1));
            await chunkLock.WaitAsync();

            try
            {
                // Find chunk info
                var chunk = chunkSession.Chunks.FirstOrDefault(c => c.ChunkNumber == chunkNumber);
                if (chunk == null)
                {
                    _logger.LogWarning("Chunk {ChunkNumber} not found in session {ChunkKey}, CorrelationId: {CorrelationId}", 
                        chunkNumber, chunkKey, correlationId);
                    return false;
                }

                // Validate chunk size
                if (chunkData.Length != chunk.Size)
                {
                    _logger.LogWarning("Chunk {ChunkNumber} size mismatch: expected {ExpectedSize}, got {ActualSize}, CorrelationId: {CorrelationId}", 
                        chunkNumber, chunk.Size, chunkData.Length, correlationId);
                    return false;
                }

                // Verify checksum if provided
                if (!string.IsNullOrEmpty(checksum))
                {
                    var calculatedChecksum = await CalculateChecksumAsync(chunkData);
                    if (calculatedChecksum != checksum)
                    {
                        _logger.LogWarning("Chunk {ChunkNumber} checksum mismatch: expected {ExpectedChecksum}, got {ActualChecksum}, CorrelationId: {CorrelationId}", 
                            chunkNumber, checksum, calculatedChecksum, correlationId);
                        
                        chunk.Status = ChunkStatusDto.Failed;
                        chunk.ErrorMessage = "Checksum mismatch";
                        return false;
                    }
                    chunk.Checksum = checksum;
                }

                // Store chunk
                var chunkPath = await _storageService.StoreChunkAsync(chunkData, sessionId, fileId, chunkNumber);
                
                // Update chunk status
                chunk.Status = ChunkStatusDto.Completed;
                chunk.UploadedAt = DateTime.UtcNow;
                chunkSession.CompletedChunks++;
                chunkSession.LastChunkTime = DateTime.UtcNow;

                // Update in-memory session (Redis removed for performance)
                _chunkSessions[chunkKey] = chunkSession;

                _logger.LogDebug("Chunk {ChunkNumber} uploaded successfully for session {SessionId}, progress: {CompletedChunks}/{TotalChunks}, CorrelationId: {CorrelationId}", 
                    chunkNumber, sessionId, chunkSession.CompletedChunks, chunkSession.TotalChunks, correlationId);

                return true;
            }
            finally
            {
                chunkLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkNumber} for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                chunkNumber, sessionId, fileId, correlationId);
            return false;
        }
    }

    public async Task<bool> VerifyChunkAsync(Guid sessionId, Guid fileId, int chunkNumber, string checksum)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var chunkKey = GetChunkKey(sessionId, fileId);

        try
        {
            var chunkSession = await GetChunkSessionAsync(chunkKey);
            if (chunkSession == null) return false;

            var chunk = chunkSession.Chunks.FirstOrDefault(c => c.ChunkNumber == chunkNumber);
            if (chunk == null || chunk.Status != ChunkStatusDto.Completed) return false;

            // Get chunk data and verify checksum
            var chunkPath = GetChunkPath(sessionId, fileId, chunkNumber);
            var chunkData = await _storageService.GetFileContentAsync(chunkPath);
            var calculatedChecksum = await CalculateChecksumAsync(chunkData);

            var isValid = calculatedChecksum == checksum;
            
            if (!isValid)
            {
                _logger.LogWarning("Chunk {ChunkNumber} verification failed for session {SessionId}, CorrelationId: {CorrelationId}", 
                    chunkNumber, sessionId, correlationId);
                
                chunk.Status = ChunkStatusDto.Failed;
                chunk.ErrorMessage = "Verification failed";
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying chunk {ChunkNumber} for session {SessionId}, CorrelationId: {CorrelationId}", 
                chunkNumber, sessionId, correlationId);
            return false;
        }
    }

    public async Task<string> AssembleChunksAsync(Guid sessionId, Guid fileId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var chunkKey = GetChunkKey(sessionId, fileId);

        try
        {
            _logger.LogInformation("Assembling chunks for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                sessionId, fileId, correlationId);

            var chunkSession = await GetChunkSessionAsync(chunkKey);
            if (chunkSession == null)
            {
                throw new InvalidOperationException("Chunk session not found");
            }

            // Verify all chunks are completed
            var missingChunks = chunkSession.Chunks.Where(c => c.Status != ChunkStatusDto.Completed).ToList();
            if (missingChunks.Any())
            {
                throw new InvalidOperationException($"Missing chunks: {string.Join(", ", missingChunks.Select(c => c.ChunkNumber))}");
            }

            // Create temporary file for assembly
            var assembledFilePath = Path.Combine(Path.GetTempPath(), $"assembled_{sessionId}_{fileId}_{Guid.NewGuid()}");

            using (var outputStream = new FileStream(assembledFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _options.AssemblyBufferSize))
            {
                // Assemble chunks in order
                for (int i = 0; i < chunkSession.TotalChunks; i++)
                {
                    var chunkPath = GetChunkPath(sessionId, fileId, i);
                    
                    if (!File.Exists(chunkPath))
                    {
                        throw new FileNotFoundException($"Chunk file not found: {chunkPath}");
                    }

                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await chunkStream.CopyToAsync(outputStream);
                    }

                    _logger.LogDebug("Assembled chunk {ChunkNumber}/{TotalChunks} for session {SessionId}, CorrelationId: {CorrelationId}", 
                        i, chunkSession.TotalChunks, sessionId, correlationId);
                }
            }

            // Verify assembled file size
            var fileInfo = new FileInfo(assembledFilePath);
            var expectedSize = chunkSession.Chunks.Sum(c => c.Size);
            
            if (fileInfo.Length != expectedSize)
            {
                File.Delete(assembledFilePath);
                throw new InvalidOperationException($"Assembled file size mismatch: expected {expectedSize}, got {fileInfo.Length}");
            }

            _logger.LogInformation("Chunks assembled successfully for session {SessionId}, file {FileId}, final size: {FileSize} bytes, CorrelationId: {CorrelationId}", 
                sessionId, fileId, fileInfo.Length, correlationId);

            return assembledFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assembling chunks for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                sessionId, fileId, correlationId);
            throw;
        }
    }

    public async Task<List<int>> GetMissingChunksAsync(Guid sessionId, Guid fileId)
    {
        var chunkKey = GetChunkKey(sessionId, fileId);
        var chunkSession = await GetChunkSessionAsync(chunkKey);
        
        if (chunkSession == null) return new List<int>();

        return chunkSession.Chunks
            .Where(c => c.Status != ChunkStatusDto.Completed)
            .Select(c => c.ChunkNumber)
            .OrderBy(n => n)
            .ToList();
    }

    public async Task CleanupChunksAsync(Guid sessionId, Guid fileId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var chunkKey = GetChunkKey(sessionId, fileId);

        try
        {
            _logger.LogInformation("Cleaning up chunks for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                sessionId, fileId, correlationId);

            var chunkSession = await GetChunkSessionAsync(chunkKey);
            if (chunkSession != null)
            {
                // Delete chunk files
                for (int i = 0; i < chunkSession.TotalChunks; i++)
                {
                    var chunkPath = GetChunkPath(sessionId, fileId, i);
                    if (File.Exists(chunkPath))
                    {
                        try
                        {
                            File.Delete(chunkPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete chunk file {ChunkPath}, CorrelationId: {CorrelationId}", 
                                chunkPath, correlationId);
                        }
                    }
                }
            }

            // Remove from cache and memory
            _chunkSessions.TryRemove(chunkKey, out _);
            _chunkLocks.TryRemove(chunkKey, out var semaphore);
            semaphore?.Dispose();

            await _cacheService.InvalidateAsync($"chunk:session:{chunkKey}");

            _logger.LogInformation("Chunk cleanup completed for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                sessionId, fileId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up chunks for session {SessionId}, file {FileId}, CorrelationId: {CorrelationId}", 
                sessionId, fileId, correlationId);
        }
    }

    public async Task<ChunkedUploadInfoDto?> GetChunkProgressAsync(Guid sessionId, Guid fileId)
    {
        var chunkKey = GetChunkKey(sessionId, fileId);
        return await GetChunkSessionAsync(chunkKey);
    }

    // Helper methods
    private string GetChunkKey(Guid sessionId, Guid fileId)
    {
        return $"{sessionId}_{fileId}";
    }

    private async Task<ChunkedUploadInfoDto?> GetChunkSessionAsync(string chunkKey)
    {
        // Try memory first
        if (_chunkSessions.TryGetValue(chunkKey, out var session))
        {
            return session;
        }

        // Try cache
        var cached = await _cacheService.GetAsync<ChunkedUploadInfoDto>($"chunk:session:{chunkKey}");
        if (cached != null)
        {
            _chunkSessions[chunkKey] = cached;
            return cached;
        }

        return null;
    }

    private string GetChunkPath(Guid sessionId, Guid fileId, int chunkNumber)
    {
        var chunkDir = Path.Combine(Path.GetTempPath(), "chunks", sessionId.ToString(), fileId.ToString());
        Directory.CreateDirectory(chunkDir);
        return Path.Combine(chunkDir, $"chunk_{chunkNumber:D6}");
    }

    private async Task<string> CalculateChecksumAsync(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = await Task.Run(() => sha256.ComputeHash(data));
            return Convert.ToBase64String(hash);
        }
    }
}

/// <summary>
/// Configuration options for chunked upload service
/// </summary>
public class ChunkedUploadOptions
{
    public int DefaultChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB
    public int MaxChunkSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MinChunkSize { get; set; } = 1024 * 1024; // 1MB
    public TimeSpan ChunkTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxConcurrentChunks { get; set; } = 5;
    public int AssemblyBufferSize { get; set; } = 64 * 1024; // 64KB
    public bool EnableChecksumVerification { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
