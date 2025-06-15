using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Interface for cache warming service
/// </summary>
public interface ICacheWarmingService
{
    /// <summary>
    /// Warms cache with frequently accessed data for a tenant
    /// </summary>
    Task WarmFrequentlyAccessedDataAsync(string tenantId);

    /// <summary>
    /// Warms cache with popular search queries
    /// </summary>
    Task WarmPopularSearchQueriesAsync(string tenantId);

    /// <summary>
    /// Warms cache with recent document metadata
    /// </summary>
    Task WarmRecentDocumentMetadataAsync(string tenantId);

    /// <summary>
    /// Warms cache with active collections
    /// </summary>
    Task WarmActiveCollectionsAsync(string tenantId);

    /// <summary>
    /// Warms cache with common RAG queries
    /// </summary>
    Task WarmCommonRAGQueriesAsync(string tenantId);

    /// <summary>
    /// Schedules cache warming for optimal times
    /// </summary>
    Task ScheduleCacheWarmingAsync(string tenantId, CacheWarmingStrategy strategy);
}

/// <summary>
/// Cache warming service for proactive cache population
/// </summary>
public class CacheWarmingService : ICacheWarmingService, IDisposable
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly CacheWarmingOptions _options;
    private readonly Timer? _scheduledWarmingTimer;
    private readonly SemaphoreSlim _warmingSemaphore;

    public CacheWarmingService(
        IMultiLevelCacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<CacheWarmingService> logger,
        ICorrelationService correlationService,
        IOptions<CacheWarmingOptions> options)
    {
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
        _warmingSemaphore = new SemaphoreSlim(_options.MaxConcurrentWarmingOperations, _options.MaxConcurrentWarmingOperations);

        // Setup scheduled warming timer
        if (_options.EnableScheduledWarming)
        {
            _scheduledWarmingTimer = new Timer(ExecuteScheduledWarming, null, 
                _options.ScheduledWarmingInterval, _options.ScheduledWarmingInterval);
        }
    }

    public async Task WarmFrequentlyAccessedDataAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Starting cache warming for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            await _warmingSemaphore.WaitAsync();
            
            try
            {
                var warmingTasks = new[]
                {
                    WarmPopularSearchQueriesAsync(tenantId),
                    WarmRecentDocumentMetadataAsync(tenantId),
                    WarmActiveCollectionsAsync(tenantId),
                    WarmCommonRAGQueriesAsync(tenantId),
                    WarmUserPreferencesAsync(tenantId),
                    WarmSystemConfigurationAsync(tenantId)
                };

                await Task.WhenAll(warmingTasks);

                _logger.LogInformation("Cache warming completed for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                    tenantId, correlationId);
            }
            finally
            {
                _warmingSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
            throw;
        }
    }

    public async Task WarmPopularSearchQueriesAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogDebug("Warming popular search queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            // Simulate getting popular queries from analytics
            var popularQueries = GetMockPopularSearchQueries(tenantId);
            
            var warmingTasks = popularQueries.Select(async query =>
            {
                try
                {
                    var cacheKey = _keyGenerator.GenerateSearchKey(
                        query.Query, 
                        query.Filters, 
                        query.CollectionIds, 
                        query.Limit, 
                        query.Offset, 
                        tenantId);

                    // Check if already cached
                    var existingResult = await _cacheService.GetAsync<SearchResultMock>(cacheKey, CacheOptions.ForSearch(tenantId));
                    
                    if (existingResult == null)
                    {
                        // Simulate search result for warming
                        var mockResult = new SearchResultMock
                        {
                            Query = query.Query,
                            Results = GenerateMockSearchResults(query.Query),
                            TotalCount = 10,
                            ExecutionTime = TimeSpan.FromMilliseconds(150)
                        };

                        await _cacheService.SetAsync(cacheKey, mockResult, CacheOptions.ForSearch(tenantId));
                        
                        _logger.LogDebug("Warmed search cache for query: {Query}, TenantId: {TenantId}", 
                            query.Query, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for search query {Query}, TenantId: {TenantId}", 
                        query.Query, tenantId);
                }
            });

            await Task.WhenAll(warmingTasks);
            
            _logger.LogDebug("Completed warming {Count} popular search queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                popularQueries.Count, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming popular search queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
    }

    public async Task WarmRecentDocumentMetadataAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogDebug("Warming recent document metadata for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            // Simulate getting recent documents
            var recentDocuments = GetMockRecentDocuments(tenantId);
            
            var warmingTasks = recentDocuments.Select(async documentId =>
            {
                try
                {
                    var cacheKey = _keyGenerator.GenerateDocumentKey(documentId, tenantId);
                    
                    var existingMetadata = await _cacheService.GetAsync<DocumentMetadataMock>(cacheKey, CacheOptions.ForDocumentMetadata(tenantId));
                    
                    if (existingMetadata == null)
                    {
                        var mockMetadata = new DocumentMetadataMock
                        {
                            DocumentId = documentId,
                            Title = $"Document {documentId}",
                            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                            Size = Random.Shared.Next(1000, 100000),
                            Type = "pdf"
                        };

                        await _cacheService.SetAsync(cacheKey, mockMetadata, CacheOptions.ForDocumentMetadata(tenantId));
                        
                        _logger.LogDebug("Warmed document metadata cache for document: {DocumentId}, TenantId: {TenantId}", 
                            documentId, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for document metadata {DocumentId}, TenantId: {TenantId}", 
                        documentId, tenantId);
                }
            });

            await Task.WhenAll(warmingTasks);
            
            _logger.LogDebug("Completed warming {Count} document metadata entries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                recentDocuments.Count, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming recent document metadata for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
    }

    public async Task WarmActiveCollectionsAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogDebug("Warming active collections for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            var activeCollections = GetMockActiveCollections(tenantId);
            
            var warmingTasks = activeCollections.Select(async collectionId =>
            {
                try
                {
                    var cacheKey = _keyGenerator.GenerateCollectionKey(collectionId, tenantId);
                    
                    var existingCollection = await _cacheService.GetAsync<CollectionMetadataMock>(cacheKey, CacheOptions.ForCollectionData(tenantId));
                    
                    if (existingCollection == null)
                    {
                        var mockCollection = new CollectionMetadataMock
                        {
                            CollectionId = collectionId,
                            Name = $"Collection {collectionId}",
                            DocumentCount = Random.Shared.Next(10, 1000),
                            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)),
                            LastUpdated = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 24))
                        };

                        await _cacheService.SetAsync(cacheKey, mockCollection, CacheOptions.ForCollectionData(tenantId));
                        
                        _logger.LogDebug("Warmed collection cache for collection: {CollectionId}, TenantId: {TenantId}", 
                            collectionId, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for collection {CollectionId}, TenantId: {TenantId}", 
                        collectionId, tenantId);
                }
            });

            await Task.WhenAll(warmingTasks);
            
            _logger.LogDebug("Completed warming {Count} active collections for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                activeCollections.Count, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming active collections for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
    }

    public async Task WarmCommonRAGQueriesAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogDebug("Warming common RAG queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            var commonRAGQueries = GetMockCommonRAGQueries(tenantId);
            
            var warmingTasks = commonRAGQueries.Select(async ragQuery =>
            {
                try
                {
                    var cacheKey = _keyGenerator.GenerateRAGKey(
                        ragQuery.Query, 
                        ragQuery.Context, 
                        ragQuery.CollectionIds, 
                        ragQuery.PromptTemplate, 
                        tenantId);

                    var existingResult = await _cacheService.GetAsync<RAGResultMock>(cacheKey, CacheOptions.ForRAG(tenantId));
                    
                    if (existingResult == null)
                    {
                        var mockResult = new RAGResultMock
                        {
                            Query = ragQuery.Query,
                            Response = $"Mock RAG response for: {ragQuery.Query}",
                            Context = ragQuery.Context,
                            Sources = GenerateMockSources(),
                            ExecutionTime = TimeSpan.FromMilliseconds(500)
                        };

                        await _cacheService.SetAsync(cacheKey, mockResult, CacheOptions.ForRAG(tenantId));
                        
                        _logger.LogDebug("Warmed RAG cache for query: {Query}, TenantId: {TenantId}", 
                            ragQuery.Query, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for RAG query {Query}, TenantId: {TenantId}", 
                        ragQuery.Query, tenantId);
                }
            });

            await Task.WhenAll(warmingTasks);
            
            _logger.LogDebug("Completed warming {Count} common RAG queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                commonRAGQueries.Count, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming common RAG queries for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
    }

    public async Task ScheduleCacheWarmingAsync(string tenantId, CacheWarmingStrategy strategy)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Scheduling cache warming for tenant {TenantId} with strategy {Strategy}, CorrelationId: {CorrelationId}", 
                tenantId, strategy, correlationId);

            switch (strategy)
            {
                case CacheWarmingStrategy.Immediate:
                    await WarmFrequentlyAccessedDataAsync(tenantId);
                    break;
                    
                case CacheWarmingStrategy.Delayed:
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(_options.DelayedWarmingDelay);
                        await WarmFrequentlyAccessedDataAsync(tenantId);
                    });
                    break;
                    
                case CacheWarmingStrategy.OffPeak:
                    // Schedule for off-peak hours (e.g., 2 AM)
                    var now = DateTime.Now;
                    var nextOffPeak = now.Date.AddDays(1).AddHours(2);
                    if (now.Hour < 2)
                    {
                        nextOffPeak = now.Date.AddHours(2);
                    }
                    
                    var delay = nextOffPeak - now;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        await WarmFrequentlyAccessedDataAsync(tenantId);
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling cache warming for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
            throw;
        }
    }

    private async Task WarmUserPreferencesAsync(string tenantId)
    {
        // Implementation for warming user preferences cache
        await Task.Delay(50); // Simulate work
    }

    private async Task WarmSystemConfigurationAsync(string tenantId)
    {
        // Implementation for warming system configuration cache
        await Task.Delay(50); // Simulate work
    }

    private void ExecuteScheduledWarming(object? state)
    {
        try
        {
            // In a full implementation, this would get a list of active tenants
            // and warm their caches during off-peak hours
            _logger.LogInformation("Executing scheduled cache warming");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled cache warming");
        }
    }

    // Mock data generation methods
    private List<SearchQueryMock> GetMockPopularSearchQueries(string tenantId) =>
        new()
        {
            new() { Query = "machine learning", Limit = 10, Offset = 0 },
            new() { Query = "artificial intelligence", Limit = 10, Offset = 0 },
            new() { Query = "data science", Limit = 10, Offset = 0 },
            new() { Query = "neural networks", Limit = 10, Offset = 0 },
            new() { Query = "deep learning", Limit = 10, Offset = 0 }
        };

    private List<string> GetMockRecentDocuments(string tenantId) =>
        Enumerable.Range(1, 20).Select(i => $"doc_{tenantId}_{i}").ToList();

    private List<string> GetMockActiveCollections(string tenantId) =>
        Enumerable.Range(1, 10).Select(i => $"collection_{tenantId}_{i}").ToList();

    private List<RAGQueryMock> GetMockCommonRAGQueries(string tenantId) =>
        new()
        {
            new() { Query = "What is machine learning?", Context = "technical documentation" },
            new() { Query = "How to implement neural networks?", Context = "programming guides" },
            new() { Query = "Best practices for data science", Context = "methodology documents" }
        };

    private List<string> GenerateMockSearchResults(string query) =>
        Enumerable.Range(1, 5).Select(i => $"Result {i} for {query}").ToList();

    private List<string> GenerateMockSources() =>
        new() { "Document 1", "Document 2", "Document 3" };

    public void Dispose()
    {
        _scheduledWarmingTimer?.Dispose();
        _warmingSemaphore?.Dispose();
    }
}

// Mock classes for cache warming
public class SearchQueryMock
{
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, string>? Filters { get; set; }
    public IEnumerable<string>? CollectionIds { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class SearchResultMock
{
    public string Query { get; set; } = string.Empty;
    public List<string> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

public class DocumentMetadataMock
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class CollectionMetadataMock
{
    public string CollectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class RAGQueryMock
{
    public string Query { get; set; } = string.Empty;
    public string? Context { get; set; }
    public IEnumerable<string>? CollectionIds { get; set; }
    public string? PromptTemplate { get; set; }
}

public class RAGResultMock
{
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<string> Sources { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}

public enum CacheWarmingStrategy
{
    Immediate,
    Delayed,
    OffPeak
}

public class CacheWarmingOptions
{
    public bool EnableScheduledWarming { get; set; } = true;
    public TimeSpan ScheduledWarmingInterval { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan DelayedWarmingDelay { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentWarmingOperations { get; set; } = 5;
    public int PopularQueriesCount { get; set; } = 20;
    public int RecentDocumentsCount { get; set; } = 50;
    public int ActiveCollectionsCount { get; set; } = 10;
}
