using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

/// <summary>
/// Controller for enterprise-grade document upload operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Admin")]
public class DocumentUploadController : ControllerBase
{
    private readonly IDocumentUploadService _uploadService;
    private readonly IUploadProgressService _progressService;
    private readonly IUploadValidationService _validationService;
    private readonly ILogger<DocumentUploadController> _logger;
    private readonly ICorrelationService _correlationService;

    public DocumentUploadController(
        IDocumentUploadService uploadService,
        IUploadProgressService progressService,
        IUploadValidationService validationService,
        ILogger<DocumentUploadController> logger,
        ICorrelationService correlationService)
    {
        _uploadService = uploadService;
        _progressService = progressService;
        _validationService = validationService;
        _logger = logger;
        _correlationService = correlationService;
    }

    /// <summary>
    /// Initializes a new upload session
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeUploadSession([FromBody] InitializeUploadSessionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Initializing upload session for user {UserId}, {FileCount} files, CorrelationId: {CorrelationId}",
                userId, request.Files.Count, correlationId);

            request.UserId = userId;
            request.CompanyId = this.GetCurrentCompanyId();
            var session = await _uploadService.InitializeUploadSessionAsync(request);

            return this.Success(session, "Upload session initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing upload session for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to initialize upload session", ex.Message, 500);
        }
    }

    /// <summary>
    /// Uploads a single file
    /// </summary>
    [HttpPost("file")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] Guid sessionId, [FromForm] UploadOptionsDto? options = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (file == null || file.Length == 0)
            {
                return this.Error("No file provided or file is empty");
            }

            _logger.LogInformation("Uploading file: {FileName} ({FileSize} bytes) for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                file.FileName, file.Length, sessionId, userId, correlationId);

            var result = await _uploadService.UploadFileAsync(file, sessionId, userId, options);

            if (result.Success)
            {
                return this.Success(result, "File uploaded successfully");
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                file?.FileName, sessionId, userId, correlationId);
            
            return this.Error("Failed to upload file", ex.Message, 500);
        }
    }

    /// <summary>
    /// Uploads multiple files in batch
    /// </summary>
    [HttpPost("batch")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB limit for batch
    public async Task<IActionResult> UploadBatch([FromForm] BatchUploadRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (request.Files == null || !request.Files.Any())
            {
                return this.Error("No files provided");
            }

            _logger.LogInformation("Starting batch upload: {FileCount} files for user {UserId}, CorrelationId: {CorrelationId}", 
                request.Files.Count, userId, correlationId);

            request.UserId = userId;
            var result = await _uploadService.UploadBatchAsync(request);

            if (result.Success)
            {
                return this.Success(result, $"Batch upload completed: {result.Files.Count} files processed");
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch upload for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to upload batch", ex.Message, 500);
        }
    }

    /// <summary>
    /// Uploads a chunk of a large file
    /// </summary>
    [HttpPost("chunk")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit for chunks
    public async Task<IActionResult> UploadChunk([FromForm] ChunkedUploadRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (request.ChunkData == null || request.ChunkData.Length == 0)
            {
                return this.Error("No chunk data provided");
            }

            _logger.LogDebug("Uploading chunk {ChunkNumber}/{TotalChunks} for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                request.ChunkNumber, request.TotalChunks, request.FileId, request.SessionId, correlationId);

            var result = await _uploadService.UploadChunkAsync(request);

            if (result.Success)
            {
                return this.Success(result, "Chunk uploaded successfully");
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkNumber} for file {FileId}, session {SessionId}, CorrelationId: {CorrelationId}", 
                request.ChunkNumber, request.FileId, request.SessionId, correlationId);
            
            return this.Error("Failed to upload chunk", ex.Message, 500);
        }
    }

    /// <summary>
    /// Completes a chunked upload
    /// </summary>
    [HttpPost("chunk/complete")]
    public async Task<IActionResult> CompleteChunkedUpload([FromBody] CompleteChunkedUploadDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Completing chunked upload for file {FileId}, session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                request.FileId, request.SessionId, userId, correlationId);

            var result = await _uploadService.CompleteChunkedUploadAsync(request.SessionId, request.FileId, userId);

            if (result.Success)
            {
                return this.Success(result, "Chunked upload completed successfully");
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing chunked upload for file {FileId}, session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                request.FileId, request.SessionId, userId, correlationId);
            
            return this.Error("Failed to complete chunked upload", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets upload session status
    /// </summary>
    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetUploadSession(Guid sessionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var session = await _uploadService.GetUploadSessionAsync(sessionId, userId);
            
            if (session == null)
            {
                return this.NotFound<DocumentUploadSessionDto>("Upload session not found");
            }

            return this.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload session {SessionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            return this.Error("Failed to get upload session", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets all upload sessions for the current user
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetUserUploadSessions([FromQuery] bool includeCompleted = false)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var sessions = await _uploadService.GetUserUploadSessionsAsync(userId, includeCompleted);
            return this.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload sessions for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to get upload sessions", ex.Message, 500);
        }
    }

    /// <summary>
    /// Cancels an upload session
    /// </summary>
    [HttpPost("session/{sessionId:guid}/cancel")]
    public async Task<IActionResult> CancelUploadSession(Guid sessionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var success = await _uploadService.CancelUploadSessionAsync(sessionId, userId);
            
            if (success)
            {
                return this.Success("Upload session cancelled successfully");
            }
            else
            {
                return this.Error("Failed to cancel upload session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling upload session {SessionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            return this.Error("Failed to cancel upload session", ex.Message, 500);
        }
    }

    /// <summary>
    /// Retries failed uploads in a session
    /// </summary>
    [HttpPost("session/{sessionId:guid}/retry")]
    public async Task<IActionResult> RetryFailedUploads(Guid sessionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var result = await _uploadService.RetryFailedUploadsAsync(sessionId, userId);
            
            if (result.Success)
            {
                return this.Success(result, "Failed uploads retried successfully");
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed uploads for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            return this.Error("Failed to retry uploads", ex.Message, 500);
        }
    }

    /// <summary>
    /// Validates files before upload
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateFiles([FromForm] List<IFormFile> files, [FromForm] UploadOptionsDto options)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (files == null || !files.Any())
            {
                return this.Error("No files provided for validation");
            }

            var validation = await _uploadService.ValidateFilesAsync(files, options, userId);
            return this.Success(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating files for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to validate files", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets upload progress for a session
    /// </summary>
    [HttpGet("session/{sessionId:guid}/progress")]
    public async Task<IActionResult> GetUploadProgress(Guid sessionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var progress = await _uploadService.GetUploadProgressAsync(sessionId, userId);
            return this.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload progress for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            return this.Error("Failed to get upload progress", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets supported file types
    /// </summary>
    [HttpGet("supported-types")]
    public async Task<IActionResult> GetSupportedFileTypes()
    {
        try
        {
            var supportedTypes = await _uploadService.GetSupportedFileTypesAsync();
            return this.Success(supportedTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported file types");
            return this.Error("Failed to get supported file types", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets R2R rate limiting status
    /// </summary>
    [HttpGet("r2r-status")]
    public async Task<IActionResult> GetR2RRateLimitStatus()
    {
        try
        {
            var status = await _uploadService.GetR2RRateLimitStatusAsync();
            return this.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting R2R rate limit status");
            return this.Error("Failed to get R2R status", ex.Message, 500);
        }
    }

    /// <summary>
    /// Pauses an upload session
    /// </summary>
    [HttpPost("session/{sessionId:guid}/pause")]
    public async Task<IActionResult> PauseUploadSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            var success = await _uploadService.PauseUploadSessionAsync(sessionId, userId);
            
            if (success)
            {
                return this.Success("Upload session paused successfully");
            }
            else
            {
                return this.Error("Failed to pause upload session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing upload session {SessionId} for user {UserId}", 
                sessionId, userId);
            
            return this.Error("Failed to pause upload session", ex.Message, 500);
        }
    }

    /// <summary>
    /// Resumes a paused upload session
    /// </summary>
    [HttpPost("session/{sessionId:guid}/resume")]
    public async Task<IActionResult> ResumeUploadSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            var success = await _uploadService.ResumeUploadSessionAsync(sessionId, userId);
            
            if (success)
            {
                return this.Success("Upload session resumed successfully");
            }
            else
            {
                return this.Error("Failed to resume upload session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming upload session {SessionId} for user {UserId}", 
                sessionId, userId);
            
            return this.Error("Failed to resume upload session", ex.Message, 500);
        }
    }

    // Helper methods
    private string GetCurrentUserId()
    {
        // Get the user ID from the NameIdentifier claim (which contains the GUID)
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
               User.FindFirst("sub")?.Value ??
               "anonymous";
    }
}

/// <summary>
/// DTO for completing chunked upload
/// </summary>
public class CompleteChunkedUploadDto
{
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    public Guid FileId { get; set; }
}
