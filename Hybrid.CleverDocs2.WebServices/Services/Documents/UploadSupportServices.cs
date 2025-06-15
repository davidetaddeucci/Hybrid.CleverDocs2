using Microsoft.AspNetCore.SignalR;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Hubs;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service for tracking upload progress with real-time updates
/// </summary>
public class UploadProgressService : IUploadProgressService
{
    private readonly IHubContext<DocumentUploadHub> _hubContext;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<UploadProgressService> _logger;

    // Thread-safe storage for progress tracking
    private readonly ConcurrentDictionary<Guid, UploadProgressDto> _progressCache = new();
    private readonly ConcurrentDictionary<Guid, List<string>> _sessionSubscribers = new();

    public UploadProgressService(
        IHubContext<DocumentUploadHub> hubContext,
        IMultiLevelCacheService cacheService,
        ILogger<UploadProgressService> logger)
    {
        _hubContext = hubContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task UpdateProgressAsync(UploadProgressDto progress)
    {
        try
        {
            // Update in-memory cache
            _progressCache[progress.SessionId] = progress;

            // Cache for persistence
            await _cacheService.SetAsync($"upload:progress:{progress.SessionId}", progress, 
                new CacheOptions { L1TTL = TimeSpan.FromMinutes(30) });

            // Broadcast to subscribers
            await BroadcastProgressAsync(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating upload progress for session {SessionId}", progress.SessionId);
        }
    }

    public async Task<UploadProgressDto?> GetProgressAsync(Guid sessionId)
    {
        // Try in-memory cache first
        if (_progressCache.TryGetValue(sessionId, out var progress))
        {
            return progress;
        }

        // Try persistent cache
        return await _cacheService.GetAsync<UploadProgressDto>($"upload:progress:{sessionId}");
    }

    public async Task<UploadProgressDto?> GetFileProgressAsync(Guid sessionId, Guid fileId)
    {
        var progress = await GetProgressAsync(sessionId);
        if (progress?.FileId == fileId)
        {
            return progress;
        }
        return null;
    }

    public async Task SubscribeToProgressAsync(Guid sessionId, string connectionId)
    {
        var subscribers = _sessionSubscribers.GetOrAdd(sessionId, _ => new List<string>());
        lock (subscribers)
        {
            if (!subscribers.Contains(connectionId))
            {
                subscribers.Add(connectionId);
            }
        }
        await Task.CompletedTask;
    }

    public async Task UnsubscribeFromProgressAsync(Guid sessionId, string connectionId)
    {
        if (_sessionSubscribers.TryGetValue(sessionId, out var subscribers))
        {
            lock (subscribers)
            {
                subscribers.Remove(connectionId);
                if (!subscribers.Any())
                {
                    _sessionSubscribers.TryRemove(sessionId, out _);
                }
            }
        }
        await Task.CompletedTask;
    }

    public async Task BroadcastProgressAsync(UploadProgressDto progress)
    {
        try
        {
            await _hubContext.BroadcastUploadProgress(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting upload progress for session {SessionId}", progress.SessionId);
        }
    }
}

/// <summary>
/// Service for validating uploads
/// </summary>
public class UploadValidationService : IUploadValidationService
{
    private readonly ILogger<UploadValidationService> _logger;

    // Supported file types
    private readonly HashSet<string> _supportedTypes = new()
    {
        "application/pdf", "text/plain", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/jpeg", "image/png", "image/gif", "image/bmp", "image/tiff"
    };

    public UploadValidationService(ILogger<UploadValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateFileAsync(IFormFile file, UploadOptionsDto options, string userId)
    {
        var errors = new List<string>();

        // Validate file size
        if (file.Length > options.MaxFileSize)
        {
            errors.Add($"File size {file.Length} exceeds maximum allowed size {options.MaxFileSize}");
        }

        // Validate file type
        if (options.ValidateFileTypes && options.AllowedFileTypes.Any())
        {
            if (!options.AllowedFileTypes.Contains(file.ContentType))
            {
                errors.Add($"File type {file.ContentType} is not allowed");
            }
        }
        else if (!_supportedTypes.Contains(file.ContentType))
        {
            errors.Add($"File type {file.ContentType} is not supported");
        }

        // Validate file name
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            errors.Add("File name is required");
        }

        await Task.CompletedTask;
        return (!errors.Any(), errors);
    }

    public async Task<UploadValidationResultDto> ValidateFilesAsync(List<IFormFile> files, UploadOptionsDto options, string userId)
    {
        var result = new UploadValidationResultDto
        {
            FileCount = files.Count,
            TotalSize = files.Sum(f => f.Length),
            SupportedTypes = _supportedTypes.ToList()
        };

        foreach (var file in files)
        {
            var (isValid, errors) = await ValidateFileAsync(file, options, userId);
            if (!isValid)
            {
                result.Errors.AddRange(errors);
                result.UnsupportedFiles.Add(file.FileName);
            }
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    public async Task<bool> ValidateFileTypeAsync(string fileName, string contentType, List<string> allowedTypes)
    {
        if (allowedTypes.Any())
        {
            return allowedTypes.Contains(contentType);
        }
        return _supportedTypes.Contains(contentType);
    }

    public async Task<bool> ValidateFileSizeAsync(long fileSize, long maxSize)
    {
        return fileSize <= maxSize;
    }

    public async Task<bool> ValidateFileContentAsync(byte[] content, string fileName, string contentType)
    {
        // Basic content validation - in real implementation this would be more sophisticated
        return content.Length > 0;
    }

    public async Task<bool> ValidateUserQuotaAsync(string userId, long additionalSize)
    {
        // Mock implementation - in real app this would check user quota
        return true;
    }

    public async Task<bool> ScanFileForMalwareAsync(byte[] content, string fileName)
    {
        // Mock implementation - in real app this would integrate with antivirus
        return true;
    }

    public async Task<UploadOptionsDto> GetValidationRulesAsync(string userId)
    {
        return new UploadOptionsDto
        {
            MaxFileSize = 100 * 1024 * 1024, // 100MB
            AllowedFileTypes = _supportedTypes.ToList(),
            ValidateFileTypes = true
        };
    }
}

/// <summary>
/// Service for storing uploaded files
/// </summary>
public class UploadStorageService : IUploadStorageService
{
    private readonly ILogger<UploadStorageService> _logger;

    public UploadStorageService(ILogger<UploadStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<string> StoreTempFileAsync(byte[] content, string fileName, Guid sessionId, Guid fileId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "uploads", sessionId.ToString());
        Directory.CreateDirectory(tempDir);
        
        var filePath = Path.Combine(tempDir, $"{fileId}_{fileName}");
        await File.WriteAllBytesAsync(filePath, content);
        
        return filePath;
    }

    public async Task<string> StoreChunkAsync(byte[] chunkData, Guid sessionId, Guid fileId, int chunkNumber)
    {
        var chunkDir = Path.Combine(Path.GetTempPath(), "chunks", sessionId.ToString(), fileId.ToString());
        Directory.CreateDirectory(chunkDir);
        
        var chunkPath = Path.Combine(chunkDir, $"chunk_{chunkNumber:D6}");
        await File.WriteAllBytesAsync(chunkPath, chunkData);
        
        return chunkPath;
    }

    public async Task<string> MoveToPermanentStorageAsync(string tempPath, Guid documentId)
    {
        var permanentDir = Path.Combine(Path.GetTempPath(), "documents");
        Directory.CreateDirectory(permanentDir);
        
        var permanentPath = Path.Combine(permanentDir, documentId.ToString());
        File.Move(tempPath, permanentPath);
        
        return permanentPath;
    }

    public async Task DeleteTempFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        await Task.CompletedTask;
    }

    public async Task CleanupSessionFilesAsync(Guid sessionId)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "uploads", sessionId.ToString());
        if (Directory.Exists(sessionDir))
        {
            Directory.Delete(sessionDir, true);
        }
        await Task.CompletedTask;
    }

    public async Task<byte[]> GetFileContentAsync(string filePath)
    {
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<string> CalculateChecksumAsync(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hash = await Task.Run(() => sha256.ComputeHash(content));
        return Convert.ToBase64String(hash);
    }

    public async Task<bool> VerifyFileIntegrityAsync(string filePath, string expectedChecksum)
    {
        var content = await GetFileContentAsync(filePath);
        var actualChecksum = await CalculateChecksumAsync(content);
        return actualChecksum == expectedChecksum;
    }
}

/// <summary>
/// Service for upload metrics and analytics
/// </summary>
public class UploadMetricsService : IUploadMetricsService
{
    private readonly ILogger<UploadMetricsService> _logger;
    private readonly ConcurrentDictionary<string, object> _metrics = new();

    public UploadMetricsService(ILogger<UploadMetricsService> logger)
    {
        _logger = logger;
    }

    public async Task RecordUploadMetricsAsync(Guid sessionId, UploadStatisticsDto statistics)
    {
        _metrics[$"session_{sessionId}"] = statistics;
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetUserUploadStatisticsAsync(string userId, DateTime? from = null, DateTime? to = null)
    {
        return new Dictionary<string, object>
        {
            ["total_uploads"] = 0,
            ["total_bytes"] = 0L,
            ["success_rate"] = 100.0,
            ["average_speed"] = 0.0
        };
    }

    public async Task<Dictionary<string, object>> GetSystemUploadStatisticsAsync(DateTime? from = null, DateTime? to = null)
    {
        return new Dictionary<string, object>
        {
            ["total_sessions"] = _metrics.Count,
            ["active_uploads"] = 0,
            ["total_bytes_uploaded"] = 0L,
            ["average_upload_time"] = 0.0
        };
    }

    public async Task<Dictionary<string, object>> GetR2RProcessingMetricsAsync(DateTime? from = null, DateTime? to = null)
    {
        return new Dictionary<string, object>
        {
            ["total_processed"] = 0,
            ["success_rate"] = 95.0,
            ["average_processing_time"] = 3000.0,
            ["queue_length"] = 0
        };
    }

    public async Task RecordProcessingTimeAsync(Guid documentId, TimeSpan processingTime, bool success)
    {
        _metrics[$"processing_{documentId}"] = new { processingTime, success, timestamp = DateTime.UtcNow };
        await Task.CompletedTask;
    }

    public async Task RecordErrorMetricsAsync(string errorType, string errorMessage, Guid? sessionId = null)
    {
        var errorKey = $"error_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        _metrics[errorKey] = new { errorType, errorMessage, sessionId, timestamp = DateTime.UtcNow };
        
        _logger.LogWarning("Upload error recorded: {ErrorType} - {ErrorMessage}, SessionId: {SessionId}", 
            errorType, errorMessage, sessionId);
        
        await Task.CompletedTask;
    }
}
