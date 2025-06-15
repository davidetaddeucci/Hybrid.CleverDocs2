using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// Service for collection analytics and insights
/// </summary>
public class CollectionAnalyticsService : ICollectionAnalyticsService
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<CollectionAnalyticsService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly CollectionAnalyticsOptions _options;

    // Mock analytics data storage
    private readonly Dictionary<Guid, List<CollectionActivityDto>> _mockActivities = new();
    private readonly Dictionary<Guid, Dictionary<string, int>> _mockMetrics = new();

    public CollectionAnalyticsService(
        IMultiLevelCacheService cacheService,
        ILogger<CollectionAnalyticsService> logger,
        ICorrelationService correlationService,
        IOptions<CollectionAnalyticsOptions> options)
    {
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;

        InitializeMockData();
    }

    public async Task TrackActivityAsync(Guid collectionId, string userId, string activityType, Dictionary<string, object>? metadata = null)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Tracking activity {ActivityType} for collection {CollectionId}, User: {UserId}, CorrelationId: {CorrelationId}", 
                activityType, collectionId, userId, correlationId);

            var activity = new CollectionActivityDto
            {
                ActivityType = activityType,
                Description = GenerateActivityDescription(activityType),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                UserName = await GetUserNameAsync(userId),
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            // Store activity in mock storage
            if (!_mockActivities.ContainsKey(collectionId))
            {
                _mockActivities[collectionId] = new List<CollectionActivityDto>();
            }

            _mockActivities[collectionId].Add(activity);

            // Keep only recent activities (last 100)
            if (_mockActivities[collectionId].Count > 100)
            {
                _mockActivities[collectionId] = _mockActivities[collectionId]
                    .OrderByDescending(a => a.Timestamp)
                    .Take(100)
                    .ToList();
            }

            // Update metrics
            await UpdateMetricsAsync(collectionId, activityType);

            // Invalidate analytics cache
            await _cacheService.InvalidateAsync($"analytics:collection:{collectionId}");

            _logger.LogDebug("Activity {ActivityType} tracked successfully for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                activityType, collectionId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking activity {ActivityType} for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                activityType, collectionId, correlationId);
        }
    }

    public async Task<CollectionAnalyticsDto> GetUsageStatisticsAsync(Guid collectionId, string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"analytics:collection:{collectionId}";

        try
        {
            return await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogDebug("Generating usage statistics for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                    collectionId, correlationId);

                var activities = _mockActivities.GetValueOrDefault(collectionId, new List<CollectionActivityDto>());
                var metrics = _mockMetrics.GetValueOrDefault(collectionId, new Dictionary<string, int>());

                var analytics = new CollectionAnalyticsDto
                {
                    CollectionId = collectionId,
                    CollectionName = await GetCollectionNameAsync(collectionId),
                    ViewCount = metrics.GetValueOrDefault("view_count", 0),
                    DocumentAddCount = metrics.GetValueOrDefault("document_add_count", 0),
                    DocumentRemoveCount = metrics.GetValueOrDefault("document_remove_count", 0),
                    ShareCount = metrics.GetValueOrDefault("share_count", 0),
                    LastActivity = activities.Any() ? activities.Max(a => a.Timestamp) : DateTime.UtcNow.AddDays(-30),
                    RecentActivities = activities.OrderByDescending(a => a.Timestamp).Take(10).ToList(),
                    ActivityByDay = GenerateActivityByDay(activities),
                    DocumentTypeDistribution = GenerateDocumentTypeDistribution(collectionId)
                };

                return analytics;
            }, new CacheOptions { L1TTL = TimeSpan.FromMinutes(15) }) ?? new CollectionAnalyticsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                collectionId, correlationId);
            return new CollectionAnalyticsDto();
        }
    }

    public async Task<List<CollectionAnalyticsDto>> GetUserInsightsAsync(string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"analytics:user:{userId}";

        try
        {
            return await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogDebug("Generating user insights for user {UserId}, CorrelationId: {CorrelationId}", 
                    userId, correlationId);

                var insights = new List<CollectionAnalyticsDto>();

                // Get user's collections (mock implementation)
                var userCollections = await GetUserCollectionsAsync(userId);

                foreach (var collectionId in userCollections)
                {
                    var analytics = await GetUsageStatisticsAsync(collectionId, userId);
                    insights.Add(analytics);
                }

                return insights.OrderByDescending(i => i.ViewCount).ToList();
            }, new CacheOptions { L1TTL = TimeSpan.FromHours(1) }) ?? new List<CollectionAnalyticsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user insights for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            return new List<CollectionAnalyticsDto>();
        }
    }

    public async Task<List<UserCollectionDto>> GetTrendingCollectionsAsync(string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"analytics:trending:{userId}";

        try
        {
            return await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogDebug("Getting trending collections for user {UserId}, CorrelationId: {CorrelationId}", 
                    userId, correlationId);

                // Mock implementation - in real app this would analyze activity patterns
                var trendingCollections = new List<UserCollectionDto>();

                // Get collections with high recent activity
                var userCollections = await GetUserCollectionsAsync(userId);
                foreach (var collectionId in userCollections.Take(5))
                {
                    var collection = await GetCollectionDtoAsync(collectionId);
                    if (collection != null)
                    {
                        trendingCollections.Add(collection);
                    }
                }

                return trendingCollections;
            }, new CacheOptions { L1TTL = TimeSpan.FromMinutes(30) }) ?? new List<UserCollectionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending collections for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            return new List<UserCollectionDto>();
        }
    }

    public async Task<Dictionary<string, object>> GetPerformanceMetricsAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"analytics:performance:{collectionId}";

        try
        {
            return await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogDebug("Getting performance metrics for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                    collectionId, correlationId);

                var metrics = new Dictionary<string, object>();

                // Mock performance metrics
                var activities = _mockActivities.GetValueOrDefault(collectionId, new List<CollectionActivityDto>());
                var recentActivities = activities.Where(a => a.Timestamp > DateTime.UtcNow.AddDays(-7)).ToList();

                metrics["weekly_activity_count"] = recentActivities.Count;
                metrics["average_daily_activity"] = recentActivities.Count / 7.0;
                metrics["most_active_day"] = GetMostActiveDay(recentActivities);
                metrics["activity_trend"] = CalculateActivityTrend(activities);
                metrics["engagement_score"] = CalculateEngagementScore(collectionId);
                metrics["last_activity_hours_ago"] = activities.Any() 
                    ? (DateTime.UtcNow - activities.Max(a => a.Timestamp)).TotalHours 
                    : 0;

                return metrics;
            }, new CacheOptions { L1TTL = TimeSpan.FromMinutes(30) }) ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics for collection {CollectionId}, CorrelationId: {CorrelationId}", 
                collectionId, correlationId);
            return new Dictionary<string, object>();
        }
    }

    // Helper methods
    private string GenerateActivityDescription(string activityType)
    {
        return activityType switch
        {
            "collection_created" => "Collection was created",
            "collection_updated" => "Collection details were updated",
            "collection_deleted" => "Collection was deleted",
            "collection_viewed" => "Collection was viewed",
            "collection_favorited" => "Collection was added to favorites",
            "collection_unfavorited" => "Collection was removed from favorites",
            "document_added" => "Document was added to collection",
            "document_removed" => "Document was removed from collection",
            "collection_shared" => "Collection was shared",
            "collection_unshared" => "Collection sharing was removed",
            _ => $"Activity: {activityType}"
        };
    }

    private async Task<string> GetUserNameAsync(string userId)
    {
        // Mock implementation - in real app this would get user name from user service
        await Task.Delay(1);
        return $"User {userId[..8]}";
    }

    private async Task UpdateMetricsAsync(Guid collectionId, string activityType)
    {
        if (!_mockMetrics.ContainsKey(collectionId))
        {
            _mockMetrics[collectionId] = new Dictionary<string, int>();
        }

        var metrics = _mockMetrics[collectionId];
        var metricKey = GetMetricKey(activityType);
        
        if (!string.IsNullOrEmpty(metricKey))
        {
            metrics[metricKey] = metrics.GetValueOrDefault(metricKey, 0) + 1;
        }

        await Task.CompletedTask;
    }

    private string GetMetricKey(string activityType)
    {
        return activityType switch
        {
            "collection_viewed" => "view_count",
            "document_added" => "document_add_count",
            "document_removed" => "document_remove_count",
            "collection_shared" => "share_count",
            _ => string.Empty
        };
    }

    private async Task<string> GetCollectionNameAsync(Guid collectionId)
    {
        // Mock implementation
        await Task.Delay(1);
        return $"Collection {collectionId.ToString()[..8]}";
    }

    private async Task<List<Guid>> GetUserCollectionsAsync(string userId)
    {
        // Mock implementation
        await Task.Delay(1);
        return new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
    }

    private async Task<UserCollectionDto?> GetCollectionDtoAsync(Guid collectionId)
    {
        // Mock implementation
        await Task.Delay(1);
        return new UserCollectionDto
        {
            Id = collectionId,
            Name = $"Collection {collectionId.ToString()[..8]}",
            Description = "Mock collection for analytics",
            Color = "#3B82F6",
            Icon = "folder",
            DocumentCount = Random.Shared.Next(1, 50),
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 100)),
            UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10))
        };
    }

    private Dictionary<string, int> GenerateActivityByDay(List<CollectionActivityDto> activities)
    {
        var activityByDay = new Dictionary<string, int>();
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd"))
            .ToList();

        foreach (var day in last7Days)
        {
            var dayActivities = activities.Count(a => a.Timestamp.ToString("yyyy-MM-dd") == day);
            activityByDay[day] = dayActivities;
        }

        return activityByDay;
    }

    private Dictionary<string, int> GenerateDocumentTypeDistribution(Guid collectionId)
    {
        // Mock implementation
        return new Dictionary<string, int>
        {
            ["pdf"] = Random.Shared.Next(1, 20),
            ["docx"] = Random.Shared.Next(1, 15),
            ["txt"] = Random.Shared.Next(1, 10),
            ["xlsx"] = Random.Shared.Next(1, 8),
            ["pptx"] = Random.Shared.Next(1, 5)
        };
    }

    private string GetMostActiveDay(List<CollectionActivityDto> activities)
    {
        if (!activities.Any()) return "No activity";

        var dayGroups = activities.GroupBy(a => a.Timestamp.DayOfWeek);
        var mostActiveDay = dayGroups.OrderByDescending(g => g.Count()).First();
        
        return mostActiveDay.Key.ToString();
    }

    private string CalculateActivityTrend(List<CollectionActivityDto> activities)
    {
        if (activities.Count < 2) return "Insufficient data";

        var recentWeek = activities.Where(a => a.Timestamp > DateTime.UtcNow.AddDays(-7)).Count();
        var previousWeek = activities.Where(a => 
            a.Timestamp > DateTime.UtcNow.AddDays(-14) && 
            a.Timestamp <= DateTime.UtcNow.AddDays(-7)).Count();

        if (previousWeek == 0) return recentWeek > 0 ? "Increasing" : "No activity";

        var change = (double)(recentWeek - previousWeek) / previousWeek;
        
        return change switch
        {
            > 0.1 => "Increasing",
            < -0.1 => "Decreasing",
            _ => "Stable"
        };
    }

    private double CalculateEngagementScore(Guid collectionId)
    {
        var metrics = _mockMetrics.GetValueOrDefault(collectionId, new Dictionary<string, int>());
        var activities = _mockActivities.GetValueOrDefault(collectionId, new List<CollectionActivityDto>());

        // Simple engagement score calculation
        var viewCount = metrics.GetValueOrDefault("view_count", 0);
        var documentCount = metrics.GetValueOrDefault("document_add_count", 0);
        var recentActivity = activities.Count(a => a.Timestamp > DateTime.UtcNow.AddDays(-7));

        return Math.Min(100, (viewCount * 0.1) + (documentCount * 0.5) + (recentActivity * 2));
    }

    private void InitializeMockData()
    {
        // Initialize some mock analytics data
        var sampleCollectionId = Guid.NewGuid();
        
        _mockActivities[sampleCollectionId] = new List<CollectionActivityDto>
        {
            new()
            {
                ActivityType = "collection_created",
                Description = "Collection was created",
                Timestamp = DateTime.UtcNow.AddDays(-10),
                UserId = "user1",
                UserName = "John Doe"
            },
            new()
            {
                ActivityType = "document_added",
                Description = "Document was added to collection",
                Timestamp = DateTime.UtcNow.AddDays(-5),
                UserId = "user1",
                UserName = "John Doe"
            },
            new()
            {
                ActivityType = "collection_viewed",
                Description = "Collection was viewed",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserId = "user1",
                UserName = "John Doe"
            }
        };

        _mockMetrics[sampleCollectionId] = new Dictionary<string, int>
        {
            ["view_count"] = 15,
            ["document_add_count"] = 8,
            ["document_remove_count"] = 2,
            ["share_count"] = 3
        };
    }
}

/// <summary>
/// Configuration options for collection analytics service
/// </summary>
public class CollectionAnalyticsOptions
{
    public bool EnableAnalytics { get; set; } = true;
    public int MaxActivitiesPerCollection { get; set; } = 100;
    public TimeSpan AnalyticsRetention { get; set; } = TimeSpan.FromDays(90);
    public bool EnableRealTimeAnalytics { get; set; } = true;
    public int TrendingCollectionsCount { get; set; } = 10;
}
