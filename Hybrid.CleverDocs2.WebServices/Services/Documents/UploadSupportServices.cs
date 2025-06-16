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

    // R2R Supported file types only - based on official R2R documentation
    private readonly HashSet<string> _supportedTypes = new()
    {
        // Documents
        "application/pdf",                                                                      // .pdf
        "text/plain",                                                                          // .txt
        "text/markdown",                                                                       // .md
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",           // .docx
        "application/msword",                                                                  // .doc
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",                 // .xlsx
        "application/vnd.ms-excel",                                                           // .xls
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",         // .pptx
        "application/vnd.ms-powerpoint",                                                      // .ppt
        "text/html",                                                                          // .html
        "text/csv",                                                                           // .csv
        "application/rtf",                                                                    // .rtf
        "application/epub+zip",                                                               // .epub
        "application/vnd.oasis.opendocument.text",                                           // .odt
        "text/x-rst",                                                                         // .rst
        "text/x-org",                                                                         // .org
        "text/tab-separated-values",                                                          // .tsv

        // Email
        "message/rfc822",                                                                     // .eml
        "application/vnd.ms-outlook",                                                         // .msg

        // Images
        "image/png",                                                                          // .png
        "image/jpeg",                                                                         // .jpeg, .jpg
        "image/bmp",                                                                          // .bmp
        "image/tiff",                                                                         // .tiff
        "image/heic",                                                                         // .heic

        // Audio
        "audio/mpeg",                                                                         // .mp3

        // Code files
        "text/x-python",                                                                      // .py
        "application/javascript",                                                             // .js
        "application/typescript",                                                             // .ts
        "text/css"                                                                            // .css
    };

    public UploadValidationService(ILogger<UploadValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateFileAsync(IFormFile file, UploadOptionsDto options, string userId)
    {
        var errors = new List<string>();

        _logger.LogInformation("Validating file: {FileName}, ContentType: {ContentType}, Size: {Size}",
            file.FileName, file.ContentType, file.Length);

        // Validate file size
        if (file.Length > options.MaxFileSize)
        {
            errors.Add($"File size {file.Length} exceeds maximum allowed size {options.MaxFileSize}");
            _logger.LogWarning("File size validation failed: {FileName}, Size: {Size}, MaxSize: {MaxSize}",
                file.FileName, file.Length, options.MaxFileSize);
        }

        // Validate file type with smart content type handling
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

        _logger.LogInformation("File validation details: Extension: {Extension}, ContentType: {ContentType}",
            fileExtension, contentType);

        if (options.ValidateFileTypes && options.AllowedFileTypes.Any())
        {
            if (!options.AllowedFileTypes.Contains(contentType))
            {
                errors.Add($"File type {contentType} is not allowed");
                _logger.LogWarning("Content type not in allowed list: {ContentType}, AllowedTypes: {AllowedTypes}",
                    contentType, string.Join(", ", options.AllowedFileTypes));
            }
        }
        else if (!IsContentTypeValidForExtension(contentType, fileExtension))
        {
            errors.Add($"File type {contentType} is not supported for extension {fileExtension}");
            _logger.LogWarning("Content type validation failed: {ContentType} not valid for extension {Extension}",
                contentType, fileExtension);
        }

        // Validate file name
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            errors.Add("File name is required");
        }

        _logger.LogInformation("File validation result: {IsValid}, Errors: {Errors}",
            !errors.Any(), string.Join("; ", errors));

        await Task.CompletedTask;
        return (!errors.Any(), errors);
    }

    private bool IsContentTypeValidForExtension(string contentType, string fileExtension)
    {
        _logger.LogDebug("Checking content type validity: {ContentType} for extension {Extension}",
            contentType, fileExtension);

        // Direct match with supported types
        if (_supportedTypes.Contains(contentType))
        {
            _logger.LogDebug("Content type {ContentType} found in supported types list", contentType);
            return true;
        }

        // Handle browser quirks for specific file types
        bool isValid = false;
        switch (fileExtension)
        {
            case ".md":
                // Browsers often send text/plain for .md files instead of text/markdown
                isValid = contentType == "text/plain" || contentType == "text/markdown";
                _logger.LogDebug("Markdown file validation: {ContentType} -> {IsValid}", contentType, isValid);
                break;

            case ".txt":
                // Text files should be text/plain
                isValid = contentType == "text/plain";
                break;

            case ".csv":
                // CSV files might be sent as text/plain or application/vnd.ms-excel
                isValid = contentType == "text/csv" || contentType == "text/plain" || contentType == "application/vnd.ms-excel";
                break;

            case ".tsv":
                // TSV files might be sent as text/plain
                isValid = contentType == "text/tab-separated-values" || contentType == "text/plain";
                break;

            case ".py":
            case ".js":
            case ".ts":
            case ".css":
            case ".rst":
            case ".org":
                // Code files might be sent as text/plain
                isValid = contentType == "text/plain" || _supportedTypes.Contains(contentType);
                break;

            default:
                // For all other extensions, require exact match with supported types
                isValid = false;
                _logger.LogDebug("Extension {Extension} requires exact content type match, result: {IsValid}",
                    fileExtension, isValid);
                break;
        }

        _logger.LogDebug("Final content type validation result: {ContentType} for {Extension} -> {IsValid}",
            contentType, fileExtension, isValid);
        return isValid;
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
