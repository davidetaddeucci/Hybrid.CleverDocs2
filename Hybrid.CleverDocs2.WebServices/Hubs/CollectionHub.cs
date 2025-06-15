using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Hubs;

/// <summary>
/// SignalR hub for real-time collection updates
/// </summary>
[Authorize]
public class CollectionHub : Hub
{
    private readonly IUserCollectionService _collectionService;
    private readonly ICollectionAnalyticsService _analyticsService;
    private readonly ILogger<CollectionHub> _logger;
    private readonly ICorrelationService _correlationService;

    public CollectionHub(
        IUserCollectionService collectionService,
        ICollectionAnalyticsService analyticsService,
        ILogger<CollectionHub> logger,
        ICorrelationService correlationService)
    {
        _collectionService = collectionService;
        _analyticsService = analyticsService;
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
            _logger.LogInformation("User {UserId} connected to CollectionHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                userId, Context.ConnectionId, correlationId);

            // Join user-specific group for targeted updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Send initial data to the connected client
            await SendInitialDataAsync(userId);

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
            _logger.LogInformation("User {UserId} disconnected from CollectionHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                userId, Context.ConnectionId, correlationId);

            // Remove from user-specific group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
        }
    }

    /// <summary>
    /// Join a collection-specific group for updates
    /// </summary>
    public async Task JoinCollectionGroup(string collectionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Validate user has access to the collection
            var collection = await _collectionService.GetCollectionByIdAsync(Guid.Parse(collectionId), userId);
            if (collection == null)
            {
                _logger.LogWarning("User {UserId} attempted to join collection group {CollectionId} without access, CorrelationId: {CorrelationId}", 
                    userId, collectionId, correlationId);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"collection_{collectionId}");
            
            // Track analytics
            await _analyticsService.TrackActivityAsync(Guid.Parse(collectionId), userId, "collection_viewed");

            _logger.LogDebug("User {UserId} joined collection group {CollectionId}, CorrelationId: {CorrelationId}", 
                userId, collectionId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining collection group {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
        }
    }

    /// <summary>
    /// Leave a collection-specific group
    /// </summary>
    public async Task LeaveCollectionGroup(string collectionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"collection_{collectionId}");
            
            _logger.LogDebug("User {UserId} left collection group {CollectionId}, CorrelationId: {CorrelationId}", 
                userId, collectionId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving collection group {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
        }
    }

    /// <summary>
    /// Get real-time collection statistics
    /// </summary>
    public async Task GetCollectionStats(string collectionId)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var analytics = await _analyticsService.GetUsageStatisticsAsync(Guid.Parse(collectionId), userId);
            await Clients.Caller.SendAsync("CollectionStatsUpdated", collectionId, analytics);

            _logger.LogDebug("Sent collection stats for {CollectionId} to user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection stats {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
        }
    }

    /// <summary>
    /// Notify about collection activity
    /// </summary>
    public async Task NotifyCollectionActivity(string collectionId, string activityType, object? data = null)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Track the activity
            await _analyticsService.TrackActivityAsync(Guid.Parse(collectionId), userId, activityType, 
                data as Dictionary<string, object>);

            // Notify all users in the collection group
            await Clients.Group($"collection_{collectionId}")
                .SendAsync("CollectionActivityNotification", collectionId, activityType, data, userId);

            _logger.LogDebug("Notified collection activity {ActivityType} for collection {CollectionId}, User: {UserId}, CorrelationId: {CorrelationId}", 
                activityType, collectionId, userId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying collection activity {ActivityType} for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                activityType, collectionId, correlationId);
        }
    }

    /// <summary>
    /// Request collection suggestions
    /// </summary>
    public async Task RequestCollectionSuggestions(string? context = null)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var suggestions = await _collectionService.GetCollectionSuggestionsAsync(userId, context);
            await Clients.Caller.SendAsync("CollectionSuggestionsReceived", suggestions);

            _logger.LogDebug("Sent collection suggestions to user {UserId}, Context: {Context}, CorrelationId: {CorrelationId}", 
                userId, context, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection suggestions for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
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
    /// Send typing indicator for collaborative features
    /// </summary>
    public async Task SendTypingIndicator(string collectionId, bool isTyping)
    {
        var userId = GetUserId();
        var userName = GetUserName();

        try
        {
            await Clients.OthersInGroup($"collection_{collectionId}")
                .SendAsync("TypingIndicator", collectionId, userId, userName, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator for collection {CollectionId}, User: {UserId}", 
                collectionId, userId);
        }
    }

    /// <summary>
    /// Broadcast collection update to all relevant clients
    /// </summary>
    public async Task BroadcastCollectionUpdate(UserCollectionDto collection, string updateType)
    {
        var userId = GetUserId();
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Send to user's personal group
            await Clients.Group($"user_{userId}")
                .SendAsync("CollectionUpdated", collection, updateType);

            // Send to collection-specific group if it exists
            await Clients.Group($"collection_{collection.Id}")
                .SendAsync("CollectionUpdated", collection, updateType);

            _logger.LogDebug("Broadcasted collection update {UpdateType} for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                updateType, collection.Id, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting collection update for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.Id, correlationId);
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

    private async Task SendInitialDataAsync(string userId)
    {
        try
        {
            // Send user's collections
            var collections = await _collectionService.GetUserCollectionsAsync(userId);
            await Clients.Caller.SendAsync("InitialCollectionsData", collections);

            // Send favorite collections
            var favorites = await _collectionService.GetFavoriteCollectionsAsync(userId);
            await Clients.Caller.SendAsync("FavoriteCollectionsData", favorites);

            // Send recent collections
            var recent = await _collectionService.GetRecentCollectionsAsync(userId);
            await Clients.Caller.SendAsync("RecentCollectionsData", recent);

            // Send user insights
            var insights = await _analyticsService.GetUserInsightsAsync(userId);
            await Clients.Caller.SendAsync("UserInsightsData", insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial data to user {UserId}", userId);
        }
    }
}

/// <summary>
/// Extension methods for CollectionHub
/// </summary>
public static class CollectionHubExtensions
{
    /// <summary>
    /// Send collection created notification
    /// </summary>
    public static async Task NotifyCollectionCreated(this IHubContext<CollectionHub> hubContext, 
        string userId, UserCollectionDto collection)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("CollectionCreated", collection);
    }

    /// <summary>
    /// Send collection updated notification
    /// </summary>
    public static async Task NotifyCollectionUpdated(this IHubContext<CollectionHub> hubContext, 
        string userId, UserCollectionDto collection)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("CollectionUpdated", collection);
        
        await hubContext.Clients.Group($"collection_{collection.Id}")
            .SendAsync("CollectionUpdated", collection);
    }

    /// <summary>
    /// Send collection deleted notification
    /// </summary>
    public static async Task NotifyCollectionDeleted(this IHubContext<CollectionHub> hubContext, 
        string userId, Guid collectionId)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("CollectionDeleted", collectionId);
        
        await hubContext.Clients.Group($"collection_{collectionId}")
            .SendAsync("CollectionDeleted", collectionId);
    }

    /// <summary>
    /// Send bulk operation notification
    /// </summary>
    public static async Task NotifyBulkOperation(this IHubContext<CollectionHub> hubContext, 
        string userId, string operation, List<Guid> collectionIds, bool success)
    {
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("BulkOperationCompleted", operation, collectionIds, success);
    }

    /// <summary>
    /// Send real-time analytics update
    /// </summary>
    public static async Task NotifyAnalyticsUpdate(this IHubContext<CollectionHub> hubContext, 
        Guid collectionId, CollectionAnalyticsDto analytics)
    {
        await hubContext.Clients.Group($"collection_{collectionId}")
            .SendAsync("AnalyticsUpdated", collectionId, analytics);
    }
}
