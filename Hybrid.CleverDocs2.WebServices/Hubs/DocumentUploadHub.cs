using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Concurrent;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Hubs;

/// <summary>
/// SignalR hub for real-time document upload progress tracking with event persistence
/// </summary>
[Authorize]
public class DocumentUploadHub : Hub
{
    private readonly IUploadProgressService _progressService;
    private readonly IDocumentUploadService _uploadService;
    private readonly ILogger<DocumentUploadHub> _logger;
    private readonly ICorrelationService _correlationService;

    // Event persistence for handling race conditions between processing completion and connection establishment
    private static readonly ConcurrentDictionary<string, List<PendingSignalREvent>> _pendingEvents = new();
    private static readonly object _eventLock = new object();

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
            var userGroup = $"user_{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);
            _logger.LogInformation("Added connection {ConnectionId} to group {UserGroup}, CorrelationId: {CorrelationId}",
                Context.ConnectionId, userGroup, correlationId);

            // Send initial upload sessions data
            await SendInitialUploadDataAsync(userId);

            // Replay any pending events that were sent before this connection was established
            await ReplayPendingEventsAsync(userId);

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
        // Get the user ID from the JWT token claims (nameid claim contains the user GUID)
        var userIdClaim = Context.User?.FindFirst("nameid")?.Value ??
                         Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim))
        {
            return userIdClaim;
        }

        // Fallback to other identifiers
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

    /// <summary>
    /// Replays any pending events for the user that were sent before connection was established
    /// Only replays very recent events to avoid infinite refresh loops
    /// </summary>
    private async Task ReplayPendingEventsAsync(string userId)
    {
        try
        {
            if (_pendingEvents.TryGetValue(userId, out var events))
            {
                List<PendingSignalREvent> eventsToReplay;

                // Create a copy of events to avoid modification during iteration
                lock (_eventLock)
                {
                    eventsToReplay = events.ToList();
                }

                // Add delay to ensure page is fully loaded before replaying events
                await Task.Delay(2000);

                var recentEventThreshold = TimeSpan.FromSeconds(30); // Only replay very recent events
                var eventsReplayed = 0;

                foreach (var pendingEvent in eventsToReplay)
                {
                    // Only replay very recent events (within last 30 seconds) to avoid refresh loops
                    if (DateTime.UtcNow - pendingEvent.Timestamp <= recentEventThreshold)
                    {
                        _logger.LogInformation("Replaying recent SignalR event {EventType} for user {UserId}, sent {SecondsAgo} seconds ago",
                            pendingEvent.EventType, userId, (DateTime.UtcNow - pendingEvent.Timestamp).TotalSeconds);

                        // Send the event to this specific connection
                        await Clients.Caller.SendAsync(pendingEvent.EventType, pendingEvent.EventData);
                        eventsReplayed++;

                        // Add small delay between events to prevent overwhelming the client
                        await Task.Delay(100);
                    }
                }

                _logger.LogInformation("Replayed {EventCount} recent events for user {UserId}", eventsReplayed, userId);

                // Clean up old events (older than 30 seconds)
                lock (_eventLock)
                {
                    events.RemoveAll(e => DateTime.UtcNow - e.Timestamp > recentEventThreshold);

                    // Remove user from dictionary if no events left
                    if (events.Count == 0)
                    {
                        _pendingEvents.TryRemove(userId, out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying pending events for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Stores a SignalR event for later replay if user is not connected
    /// Only stores events for a short time to prevent infinite refresh loops
    /// </summary>
    public static void StorePendingEvent(string userId, string eventType, object eventData)
    {
        try
        {
            lock (_eventLock)
            {
                if (!_pendingEvents.ContainsKey(userId))
                {
                    _pendingEvents[userId] = new List<PendingSignalREvent>();
                }

                var pendingEvent = new PendingSignalREvent
                {
                    EventType = eventType,
                    EventData = eventData,
                    Timestamp = DateTime.UtcNow
                };

                _pendingEvents[userId].Add(pendingEvent);

                // Clean up old events immediately (older than 30 seconds)
                var cutoffTime = DateTime.UtcNow.AddSeconds(-30);
                _pendingEvents[userId].RemoveAll(e => e.Timestamp < cutoffTime);

                // Limit to last 3 events per user to prevent memory issues and reduce refresh loops
                if (_pendingEvents[userId].Count > 3)
                {
                    _pendingEvents[userId].RemoveRange(0, _pendingEvents[userId].Count - 3);
                }

                // Remove user from dictionary if no recent events
                if (_pendingEvents[userId].Count == 0)
                {
                    _pendingEvents.TryRemove(userId, out _);
                }
            }
        }
        catch (Exception)
        {
            // Silently fail to avoid breaking the main flow
        }
    }
}

/// <summary>
/// Represents a pending SignalR event that needs to be replayed when user connects
/// </summary>
public class PendingSignalREvent
{
    public string EventType { get; set; } = string.Empty;
    public object EventData { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Extension methods for DocumentUploadHub with cache invalidation
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
    /// Broadcasts file upload completion with event persistence
    /// </summary>
    public static async Task BroadcastFileUploadCompleted(this IHubContext<DocumentUploadHub> hubContext,
        Guid sessionId, FileUploadInfoDto fileInfo)
    {
        await hubContext.Clients.Group($"session_{sessionId}")
            .SendAsync("FileUploadCompleted", fileInfo);
    }

    /// <summary>
    /// Broadcasts file upload completion to user group with event persistence for race condition handling
    /// </summary>
    public static async Task BroadcastFileUploadCompletedToUser(this IHubContext<DocumentUploadHub> hubContext,
        string userId, object eventData)
    {
        // Send to connected clients
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("FileUploadCompleted", eventData);

        // Store for replay in case user connects after event is sent
        DocumentUploadHub.StorePendingEvent(userId, "FileUploadCompleted", eventData);
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
    /// Broadcasts R2R processing update with cache invalidation
    /// </summary>
    public static async Task BroadcastR2RProcessingUpdate(this IHubContext<DocumentUploadHub> hubContext,
        string userId, R2RProcessingQueueItemDto queueItem, IMultiLevelCacheService? cacheService = null)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("R2RProcessingUpdate", queueItem);

        // Cache invalidation optimized - only for expensive data
        if (cacheService != null && queueItem.CollectionId.HasValue)
        {
            // Only invalidate collection metadata (expensive data)
            var tags = new List<string> { $"collection:{queueItem.CollectionId.Value}" };
            await cacheService.InvalidateByTagsAsync(tags);
        }
    }

    /// <summary>
    /// Broadcasts R2R progress update for real-time progress tracking
    /// </summary>
    public static async Task BroadcastR2RProgressUpdate(this IHubContext<DocumentUploadHub> hubContext,
        string userId, object progressData)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("R2RProgressUpdate", progressData);
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

    /// <summary>
    /// Broadcasts document deletion progress
    /// </summary>
    public static async Task BroadcastDocumentDeletionProgress(this IHubContext<DocumentUploadHub> hubContext,
        string userId, string documentId, string status, string? message = null)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("DocumentDeletionProgress", new { documentId, status, message, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Broadcasts document deletion completed with cache invalidation
    /// </summary>
    public static async Task BroadcastDocumentDeletionCompleted(this IHubContext<DocumentUploadHub> hubContext,
        string userId, string documentId, bool success, string? message = null,
        string? collectionId = null, IMultiLevelCacheService? cacheService = null)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("DocumentDeletionCompleted", new { documentId, success, message, timestamp = DateTime.UtcNow });

        // Invalidate document caches when deletion is completed successfully
        if (success && cacheService != null)
        {
            var tags = new List<string> { "documents", "document-lists", $"user:{userId}" };
            if (!string.IsNullOrEmpty(collectionId))
            {
                tags.Add($"collection:{collectionId}");
            }

            await cacheService.InvalidateByTagsAsync(tags);
        }
    }

    /// <summary>
    /// Broadcasts document deletion error
    /// </summary>
    public static async Task BroadcastDocumentDeletionError(this IHubContext<DocumentUploadHub> hubContext,
        string userId, string documentId, string error)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("DocumentDeletionError", new { documentId, error, timestamp = DateTime.UtcNow });
    }
}
