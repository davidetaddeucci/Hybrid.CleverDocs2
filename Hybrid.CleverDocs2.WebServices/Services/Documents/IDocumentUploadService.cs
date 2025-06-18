using Hybrid.CleverDocs2.WebServices.Models.Documents;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Interface for document upload orchestration service
/// </summary>
public interface IDocumentUploadService
{
    /// <summary>
    /// Initializes a new upload session
    /// </summary>
    Task<DocumentUploadSessionDto> InitializeUploadSessionAsync(InitializeUploadSessionDto request);

    /// <summary>
    /// Uploads a single file
    /// </summary>
    Task<UploadResponseDto> UploadFileAsync(IFormFile file, Guid sessionId, string userId, UploadOptionsDto? options = null);

    /// <summary>
    /// Uploads multiple files in batch
    /// </summary>
    Task<UploadResponseDto> UploadBatchAsync(BatchUploadRequestDto request);

    /// <summary>
    /// Uploads a chunk of a large file
    /// </summary>
    Task<UploadResponseDto> UploadChunkAsync(ChunkedUploadRequestDto request);

    /// <summary>
    /// Completes a chunked upload session
    /// </summary>
    Task<UploadResponseDto> CompleteChunkedUploadAsync(Guid sessionId, Guid fileId, string userId);

    /// <summary>
    /// Gets upload session status
    /// </summary>
    Task<DocumentUploadSessionDto?> GetUploadSessionAsync(Guid sessionId, string userId);

    /// <summary>
    /// Gets all upload sessions for a user
    /// </summary>
    Task<List<DocumentUploadSessionDto>> GetUserUploadSessionsAsync(string userId, bool includeCompleted = false);

    /// <summary>
    /// Cancels an upload session
    /// </summary>
    Task<bool> CancelUploadSessionAsync(Guid sessionId, string userId);

    /// <summary>
    /// Retries failed uploads in a session
    /// </summary>
    Task<UploadResponseDto> RetryFailedUploadsAsync(Guid sessionId, string userId);

    /// <summary>
    /// Validates files before upload
    /// </summary>
    Task<UploadValidationResultDto> ValidateFilesAsync(List<IFormFile> files, UploadOptionsDto options, string userId);

    /// <summary>
    /// Gets supported file types
    /// </summary>
    Task<List<string>> GetSupportedFileTypesAsync();

    /// <summary>
    /// Gets upload progress for a session
    /// </summary>
    Task<UploadProgressDto> GetUploadProgressAsync(Guid sessionId, string userId);

    /// <summary>
    /// Cleans up completed upload sessions
    /// </summary>
    Task CleanupCompletedSessionsAsync(TimeSpan olderThan);

    /// <summary>
    /// Gets R2R rate limiting status
    /// </summary>
    Task<R2RRateLimitStatusDto> GetR2RRateLimitStatusAsync();

    /// <summary>
    /// Pauses an upload session
    /// </summary>
    Task<bool> PauseUploadSessionAsync(Guid sessionId, string userId);

    /// <summary>
    /// Resumes a paused upload session
    /// </summary>
    Task<bool> ResumeUploadSessionAsync(Guid sessionId, string userId);
}

/// <summary>
/// Interface for chunked upload service
/// </summary>
public interface IChunkedUploadService
{
    /// <summary>
    /// Initializes chunked upload for a file
    /// </summary>
    Task<ChunkedUploadInfoDto> InitializeChunkedUploadAsync(Guid sessionId, Guid fileId, long fileSize, string fileName, int chunkSize);

    /// <summary>
    /// Uploads a single chunk
    /// </summary>
    Task<bool> UploadChunkAsync(Guid sessionId, Guid fileId, int chunkNumber, byte[] chunkData, string? checksum = null);

    /// <summary>
    /// Verifies chunk integrity
    /// </summary>
    Task<bool> VerifyChunkAsync(Guid sessionId, Guid fileId, int chunkNumber, string checksum);

    /// <summary>
    /// Assembles chunks into final file
    /// </summary>
    Task<string> AssembleChunksAsync(Guid sessionId, Guid fileId);

    /// <summary>
    /// Gets missing chunks for resumable upload
    /// </summary>
    Task<List<int>> GetMissingChunksAsync(Guid sessionId, Guid fileId);

    /// <summary>
    /// Cleans up chunk files
    /// </summary>
    Task CleanupChunksAsync(Guid sessionId, Guid fileId);

    /// <summary>
    /// Gets chunk upload progress
    /// </summary>
    Task<ChunkedUploadInfoDto?> GetChunkProgressAsync(Guid sessionId, Guid fileId);
}

/// <summary>
/// Interface for document processing service
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Queues a document for R2R processing
    /// </summary>
    Task<bool> QueueDocumentForProcessingAsync(R2RProcessingQueueItemDto queueItem);

    /// <summary>
    /// Processes a document with R2R API
    /// </summary>
    Task<bool> ProcessDocumentAsync(R2RProcessingQueueItemDto queueItem);

    /// <summary>
    /// Gets processing queue status
    /// </summary>
    Task<List<R2RProcessingQueueItemDto>> GetProcessingQueueAsync(string? userId = null);

    /// <summary>
    /// Retries failed processing
    /// </summary>
    Task<bool> RetryProcessingAsync(Guid queueItemId);

    /// <summary>
    /// Cancels processing
    /// </summary>
    Task<bool> CancelProcessingAsync(Guid queueItemId);

    /// <summary>
    /// Gets processing statistics
    /// </summary>
    Task<Dictionary<string, object>> GetProcessingStatisticsAsync();

    /// <summary>
    /// Optimizes processing queue based on R2R rate limits
    /// </summary>
    Task OptimizeProcessingQueueAsync();

    /// <summary>
    /// Checks R2R status for a document in Processing state and updates if completed
    /// </summary>
    Task<bool> CheckR2RStatusAndUpdateAsync(R2RProcessingQueueItemDto queueItem);
}

/// <summary>
/// Interface for upload progress tracking service
/// </summary>
public interface IUploadProgressService
{
    /// <summary>
    /// Updates upload progress
    /// </summary>
    Task UpdateProgressAsync(UploadProgressDto progress);

    /// <summary>
    /// Gets current progress for a session
    /// </summary>
    Task<UploadProgressDto?> GetProgressAsync(Guid sessionId);

    /// <summary>
    /// Gets progress for a specific file
    /// </summary>
    Task<UploadProgressDto?> GetFileProgressAsync(Guid sessionId, Guid fileId);

    /// <summary>
    /// Subscribes to progress updates
    /// </summary>
    Task SubscribeToProgressAsync(Guid sessionId, string connectionId);

    /// <summary>
    /// Unsubscribes from progress updates
    /// </summary>
    Task UnsubscribeFromProgressAsync(Guid sessionId, string connectionId);

    /// <summary>
    /// Broadcasts progress update to subscribers
    /// </summary>
    Task BroadcastProgressAsync(UploadProgressDto progress);
}

/// <summary>
/// Interface for upload validation service
/// </summary>
public interface IUploadValidationService
{
    /// <summary>
    /// Validates a single file
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateFileAsync(IFormFile file, UploadOptionsDto options, string userId);

    /// <summary>
    /// Validates multiple files
    /// </summary>
    Task<UploadValidationResultDto> ValidateFilesAsync(List<IFormFile> files, UploadOptionsDto options, string userId);

    /// <summary>
    /// Validates file type
    /// </summary>
    Task<bool> ValidateFileTypeAsync(string fileName, string contentType, List<string> allowedTypes);

    /// <summary>
    /// Validates file size
    /// </summary>
    Task<bool> ValidateFileSizeAsync(long fileSize, long maxSize);

    /// <summary>
    /// Validates file content
    /// </summary>
    Task<bool> ValidateFileContentAsync(byte[] content, string fileName, string contentType);

    /// <summary>
    /// Validates user quota
    /// </summary>
    Task<bool> ValidateUserQuotaAsync(string userId, long additionalSize);

    /// <summary>
    /// Scans file for malware
    /// </summary>
    Task<bool> ScanFileForMalwareAsync(byte[] content, string fileName);

    /// <summary>
    /// Gets validation rules for user
    /// </summary>
    Task<UploadOptionsDto> GetValidationRulesAsync(string userId);
}

/// <summary>
/// Interface for upload storage service
/// </summary>
public interface IUploadStorageService
{
    /// <summary>
    /// Stores uploaded file temporarily
    /// </summary>
    Task<string> StoreTempFileAsync(byte[] content, string fileName, Guid sessionId, Guid fileId);

    /// <summary>
    /// Stores chunk temporarily
    /// </summary>
    Task<string> StoreChunkAsync(byte[] chunkData, Guid sessionId, Guid fileId, int chunkNumber);

    /// <summary>
    /// Moves file to permanent storage
    /// </summary>
    Task<string> MoveToPermanentStorageAsync(string tempPath, Guid documentId);

    /// <summary>
    /// Deletes temporary file
    /// </summary>
    Task DeleteTempFileAsync(string filePath);

    /// <summary>
    /// Deletes all temporary files for session
    /// </summary>
    Task CleanupSessionFilesAsync(Guid sessionId);

    /// <summary>
    /// Gets file content
    /// </summary>
    Task<byte[]> GetFileContentAsync(string filePath);

    /// <summary>
    /// Gets file stream
    /// </summary>
    Task<Stream> GetFileStreamAsync(string filePath);

    /// <summary>
    /// Calculates file checksum
    /// </summary>
    Task<string> CalculateChecksumAsync(byte[] content);

    /// <summary>
    /// Verifies file integrity
    /// </summary>
    Task<bool> VerifyFileIntegrityAsync(string filePath, string expectedChecksum);
}

/// <summary>
/// Interface for upload metrics service
/// </summary>
public interface IUploadMetricsService
{
    /// <summary>
    /// Records upload metrics
    /// </summary>
    Task RecordUploadMetricsAsync(Guid sessionId, UploadStatisticsDto statistics);

    /// <summary>
    /// Gets upload statistics for user
    /// </summary>
    Task<Dictionary<string, object>> GetUserUploadStatisticsAsync(string userId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets system upload statistics
    /// </summary>
    Task<Dictionary<string, object>> GetSystemUploadStatisticsAsync(DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets R2R processing metrics
    /// </summary>
    Task<Dictionary<string, object>> GetR2RProcessingMetricsAsync(DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Records processing time
    /// </summary>
    Task RecordProcessingTimeAsync(Guid documentId, TimeSpan processingTime, bool success);

    /// <summary>
    /// Records error metrics
    /// </summary>
    Task RecordErrorMetricsAsync(string errorType, string errorMessage, Guid? sessionId = null);
}
