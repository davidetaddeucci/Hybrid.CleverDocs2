using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Hubs;

/// <summary>
/// SignalR hub for real-time document upload progress tracking
/// </summary>
[Authorize]
public class DocumentUploadHub : Hub
{
    private readonly IUploadProgressService _progressService;
    private readonly IDocumentUploadService _uploadService;
    private readonly ILogger<DocumentUploadHub> _logger;
    private readonly ICorrelationService _correlationService;

    public DocumentUploadHub(
        IUploadProgressService progressService,
        IDocumentUploadService uploadService,
        ILogger<DocumentUploadHub> logger,
        ICorrelationService correlationService)
    {
        _progressService = progressService;
        _uploadService = uploadService;
        _logger = logger;
        _correlationService = correlationService;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("User {UserId} connected to DocumentUploadHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                userId, Context.ConnectionId, correlationId);

            // Join user-specific group for targeted updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Send initial upload sessions data
            await SendInitialUploadDataAsync(userId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
        }
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("User {UserId} disconnected from DocumentUploadHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                userId, Context.ConnectionId, correlationId);

            // Remove from user-specific group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Unsubscribe from any active upload sessions
            await UnsubscribeFromAllSessionsAsync();

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
        }
    }

    /// <summary>
    /// Subscribes to progress updates for a specific upload session
    /// </summary>
    public async Task SubscribeToUploadSession(string sessionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID format");
                return;
            }

            // Validate user has access to the session
            var session = await _uploadService.GetUploadSessionAsync(sessionGuid, userId);
            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to subscribe to upload session {SessionId} without access, CorrelationId: {CorrelationId}", 
                    userId, sessionId, correlationId);
                
                await Clients.Caller.SendAsync("Error", "Upload session not found or access denied");
                return;
            }

            // Join session-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Subscribe to progress service
            await _progressService.SubscribeToProgressAsync(sessionGuid, Context.ConnectionId);

            // Send current session status
            await Clients.Caller.SendAsync("UploadSessionStatus", session);

            // Send current progress if available
            var progress = await _progressService.GetProgressAsync(sessionGuid);
            if (progress != null)
            {
                await Clients.Caller.SendAsync("UploadProgress", progress);
            }

            _logger.LogDebug("User {UserId} subscribed to upload session {SessionId}, CorrelationId: {CorrelationId}", 
                userId, sessionId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to upload session {SessionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to upload session");
        }
    }

    /// <summary>
    /// Unsubscribes from progress updates for a specific upload session
    /// </summary>
    public async Task UnsubscribeFromUploadSession(string sessionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                return;
            }

            // Leave session-specific group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Unsubscribe from progress service
            await _progressService.UnsubscribeFromProgressAsync(sessionGuid, Context.ConnectionId);

            _logger.LogDebug("User {UserId} unsubscribed from upload session {SessionId}, CorrelationId: {CorrelationId}", 
                userId, sessionId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from upload session {SessionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
        }
    }

    /// <summary>
    /// Gets real-time upload statistics
    /// </summary>
    public async Task GetUploadStatistics(string sessionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID format");
                return;
            }

            var session = await _uploadService.GetUploadSessionAsync(sessionGuid, userId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Upload session not found");
                return;
            }

            await Clients.Caller.SendAsync("UploadStatistics", session.Statistics);

            _logger.LogDebug("Sent upload statistics for session {SessionId} to user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload statistics for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            await Clients.Caller.SendAsync("Error", "Failed to get upload statistics");
        }
    }

    /// <summary>
    /// Requests R2R processing status
    /// </summary>
    public async Task GetR2RProcessingStatus()
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var status = await _uploadService.GetR2RRateLimitStatusAsync();
            await Clients.Caller.SendAsync("R2RProcessingStatus", status);

            _logger.LogDebug("Sent R2R processing status to user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting R2R processing status for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            await Clients.Caller.SendAsync("Error", "Failed to get R2R processing status");
        }
    }

    /// <summary>
    /// Ping to keep connection alive
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    /// <summary>
    /// Cancels an upload session
    /// </summary>
    public async Task CancelUploadSession(string sessionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID format");
                return;
            }

            var success = await _uploadService.CancelUploadSessionAsync(sessionGuid, userId);
            
            if (success)
            {
                await Clients.Caller.SendAsync("UploadSessionCancelled", sessionId);
                
                // Notify all subscribers to this session
                await Clients.Group($"session_{sessionId}")
                    .SendAsync("UploadSessionCancelled", sessionId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Failed to cancel upload session");
            }

            _logger.LogInformation("Upload session {SessionId} cancelled by user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling upload session {SessionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            await Clients.Caller.SendAsync("Error", "Failed to cancel upload session");
        }
    }

    /// <summary>
    /// Retries failed uploads in a session
    /// </summary>
    public async Task RetryFailedUploads(string sessionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid session ID format");
                return;
            }

            var result = await _uploadService.RetryFailedUploadsAsync(sessionGuid, userId);
            
            if (result.Success)
            {
                await Clients.Caller.SendAsync("FailedUploadsRetried", sessionId, result.Message);
                
                // Notify all subscribers to this session
                await Clients.Group($"session_{sessionId}")
                    .SendAsync("FailedUploadsRetried", sessionId, result.Message);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }

            _logger.LogInformation("Failed uploads retried for session {SessionId} by user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed uploads for session {SessionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                sessionId, userId, correlationId);
            
            await Clients.Caller.SendAsync("Error", "Failed to retry uploads");
        }
    }

    // Helper methods
    private string GetUserId()
    {
        return Context.User?.Identity?.Name ?? Context.UserIdentifier ?? "anonymous";
    }

    private string GetUserName()
    {
        return Context.User?.FindFirst("name")?.Value ?? 
               Context.User?.FindFirst("preferred_username")?.Value ?? 
               GetUserId();
    }

    private async Task SendInitialUploadDataAsync(string userId)
    {
        try
        {
            // Send user's active upload sessions
            var sessions = await _uploadService.GetUserUploadSessionsAsync(userId, includeCompleted: false);
            await Clients.Caller.SendAsync("InitialUploadSessions", sessions);

            // Send R2R processing status
            var r2rStatus = await _uploadService.GetR2RRateLimitStatusAsync();
            await Clients.Caller.SendAsync("R2RProcessingStatus", r2rStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial upload data to user {UserId}", userId);
        }
    }

    private async Task UnsubscribeFromAllSessionsAsync()
    {
        // Implementation would track and unsubscribe from all active sessions for this connection
        // This is a simplified version
        await Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for DocumentUploadHub
/// </summary>
public static class DocumentUploadHubExtensions
{
    /// <summary>
    /// Broadcasts upload progress update
    /// </summary>
    public static async Task BroadcastUploadProgress(this IHubContext<DocumentUploadHub> hubContext, 
        UploadProgressDto progress)
    {
        await hubContext.Clients.Group($"session_{progress.SessionId}")
            .SendAsync("UploadProgress", progress);
    }

    /// <summary>
    /// Broadcasts upload session status update
    /// </summary>
    public static async Task BroadcastUploadSessionUpdate(this IHubContext<DocumentUploadHub> hubContext, 
        DocumentUploadSessionDto session)
    {
        await hubContext.Clients.Group($"user_{session.UserId}")
            .SendAsync("UploadSessionUpdated", session);
        
        await hubContext.Clients.Group($"session_{session.SessionId}")
            .SendAsync("UploadSessionUpdated", session);
    }

    /// <summary>
    /// Broadcasts file upload completion
    /// </summary>
    public static async Task BroadcastFileUploadCompleted(this IHubContext<DocumentUploadHub> hubContext, 
        Guid sessionId, FileUploadInfoDto fileInfo)
    {
        await hubContext.Clients.Group($"session_{sessionId}")
            .SendAsync("FileUploadCompleted", fileInfo);
    }

    /// <summary>
    /// Broadcasts file upload error
    /// </summary>
    public static async Task BroadcastFileUploadError(this IHubContext<DocumentUploadHub> hubContext, 
        Guid sessionId, FileUploadInfoDto fileInfo, string errorMessage)
    {
        await hubContext.Clients.Group($"session_{sessionId}")
            .SendAsync("FileUploadError", fileInfo, errorMessage);
    }

    /// <summary>
    /// Broadcasts R2R processing update
    /// </summary>
    public static async Task BroadcastR2RProcessingUpdate(this IHubContext<DocumentUploadHub> hubContext, 
        string userId, R2RProcessingQueueItemDto queueItem)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("R2RProcessingUpdate", queueItem);
    }

    /// <summary>
    /// Broadcasts system-wide R2R status update
    /// </summary>
    public static async Task BroadcastR2RStatusUpdate(this IHubContext<DocumentUploadHub> hubContext, 
        R2RRateLimitStatusDto status)
    {
        await hubContext.Clients.All
            .SendAsync("R2RStatusUpdate", status);
    }
}
