using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs.WebUI.Models.Shared;

/// <summary>
/// DTO for document upload session
/// </summary>
public class DocumentUploadSessionDto
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? CollectionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public UploadSessionStatusDto Status { get; set; } = UploadSessionStatusDto.Initializing;
    public List<FileUploadInfoDto> Files { get; set; } = new();
    public UploadOptionsDto Options { get; set; } = new();
    public UploadStatisticsDto Statistics { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalFiles => Files.Count;
    public double ProgressPercentage => Statistics.ProgressPercentage;
    public bool IsCompleted => Status == UploadSessionStatusDto.Completed;
    public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage) || Files.Any(f => !string.IsNullOrEmpty(f.ErrorMessage));
}

/// <summary>
/// DTO for individual file upload information
/// </summary>
public class FileUploadInfoDto
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long UploadedSize { get; set; }
    public FileUploadStatusDto Status { get; set; } = FileUploadStatusDto.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public ChunkedUploadInfoDto? ChunkedInfo { get; set; }
    public DocumentProcessingInfoDto ProcessingInfo { get; set; } = new();
    public string? TempFilePath { get; set; }
    public string? Checksum { get; set; }
    public Guid? DocumentId { get; set; }
    public double ProgressPercentage => TotalSize > 0 ? (double)UploadedSize / TotalSize * 100 : 0;
    public bool IsLargeFile => TotalSize > 10 * 1024 * 1024; // >10MB
    public bool RequiresChunking => IsLargeFile;
}

/// <summary>
/// DTO for chunked upload information
/// </summary>
public class ChunkedUploadInfoDto
{
    public Guid ChunkSessionId { get; set; } = Guid.NewGuid();
    public int TotalChunks { get; set; }
    public int CompletedChunks { get; set; }
    public int ChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB default
    public List<ChunkInfoDto> Chunks { get; set; } = new();
    public bool IsResumable { get; set; } = true;
    public DateTime? LastChunkTime { get; set; }
    public string? UploadUrl { get; set; }
}

/// <summary>
/// DTO for individual chunk information
/// </summary>
public class ChunkInfoDto
{
    public int ChunkNumber { get; set; }
    public long StartByte { get; set; }
    public long EndByte { get; set; }
    public int Size { get; set; }
    public string? Checksum { get; set; }
    public bool IsUploaded { get; set; }
    public DateTime? UploadedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for upload options
/// </summary>
public class UploadOptionsDto
{
    public bool ExtractMetadata { get; set; } = true;
    public bool PerformOCR { get; set; } = true;
    public bool AutoDetectLanguage { get; set; } = true;
    public string? PreferredLanguage { get; set; }
    public bool GenerateThumbnails { get; set; } = true;
    public bool EnableVersioning { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxParallelUploads { get; set; } = 10; // Increased from 3 to 10 for better bulk upload performance
    public bool EnableChunkedUpload { get; set; } = true;
    public int ChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB
    public bool EnableProgressTracking { get; set; } = true;
    public bool ValidateFileTypes { get; set; } = true;
    public List<string> AllowedFileTypes { get; set; } = new();
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
}

/// <summary>
/// DTO for upload statistics
/// </summary>
public class UploadStatisticsDto
{
    public long TotalBytes { get; set; }
    public long UploadedBytes { get; set; }
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public int FailedFiles { get; set; }
    public double ProgressPercentage => TotalBytes > 0 ? (double)UploadedBytes / TotalBytes * 100 : 0;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double UploadSpeedBytesPerSecond { get; set; }
}

/// <summary>
/// DTO for upload progress update
/// </summary>
public class UploadProgressDto
{
    public Guid SessionId { get; set; }
    public Guid? FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public FileUploadStatusDto Status { get; set; }
    public double ProgressPercentage { get; set; }
    public long UploadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Speed { get; set; } // bytes per second
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DTO for R2R processing queue item
/// </summary>
public class R2RProcessingQueueItemDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public R2RProcessingStatusDto Status { get; set; } = R2RProcessingStatusDto.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }

    /// <summary>
    /// R2R Ingestion Job ID for audit trail and debugging
    /// </summary>
    public string? JobId { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? R2RDocumentId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public UploadOptionsDto ProcessingOptions { get; set; } = new();
}

/// <summary>
/// DTO for upload response
/// </summary>
public class UploadResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? SessionId { get; set; }
    public List<FileUploadInfoDto> Files { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public UploadStatisticsDto Statistics { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DTO for batch upload request
/// </summary>
public class BatchUploadRequestDto
{
    public List<IFormFile> Files { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
    public List<string> Tags { get; set; } = new();
    public UploadOptionsDto Options { get; set; } = new();
}

/// <summary>
/// DTO for chunked upload request
/// </summary>
public class ChunkedUploadRequestDto
{
    public Guid SessionId { get; set; }
    public Guid FileId { get; set; }
    public int ChunkNumber { get; set; }
    public int TotalChunks { get; set; }
    public IFormFile ChunkData { get; set; } = null!;
    public string? Checksum { get; set; }
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for upload session initialization
/// </summary>
public class InitializeUploadSessionDto
{
    [Required]
    public List<FileInfoDto> Files { get; set; } = new();

    public Guid? CollectionId { get; set; }

    public List<string> Tags { get; set; } = new();

    public UploadOptionsDto Options { get; set; } = new();

    public string UserId { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }
}

/// <summary>
/// DTO for file information during initialization
/// </summary>
public class FileInfoDto
{
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public long Size { get; set; }
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public string? Checksum { get; set; }
    
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Upload session status enumeration
/// </summary>
public enum UploadSessionStatusDto
{
    Initializing = 0,
    Ready = 1,
    Uploading = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

/// <summary>
/// File upload status enumeration
/// </summary>
public enum FileUploadStatusDto
{
    Pending = 0,
    Uploading = 1,
    Uploaded = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

/// <summary>
/// R2R processing status enumeration
/// </summary>
public enum R2RProcessingStatusDto
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Retrying = 4,
    Cancelled = 5
}

/// <summary>
/// DTO for document processing information
/// </summary>
public class DocumentProcessingInfoDto
{
    public DateTime? ProcessingStarted { get; set; }
    public DateTime? ProcessingCompleted { get; set; }
    public string? ProcessingStatus { get; set; }
    public double? ProcessingProgress { get; set; }
    public string? ProcessingError { get; set; }
    public Dictionary<string, object> ProcessingMetadata { get; set; } = new();
}
