namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Interface for multi-level caching service with L1 (memory), L2 (Redis), L3 (persistent) support
/// </summary>
public interface IMultiLevelCacheService
{
    /// <summary>
    /// Gets a value from cache or executes factory if not found
    /// </summary>
    Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Gets a value from cache without factory fallback
    /// </summary>
    Task<T?> GetAsync<T>(string key, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Sets a value in all applicable cache levels
    /// </summary>
    Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Removes a specific key from all cache levels
    /// </summary>
    Task RemoveAsync(string key, CacheOptions? options = null);

    /// <summary>
    /// Invalidates cache entries matching a pattern
    /// </summary>
    Task InvalidateAsync(string pattern, string? tenantId = null);

    /// <summary>
    /// Invalidates cache entries by tags - atomic invalidation across all cache layers
    /// </summary>
    Task InvalidateByTagsAsync(IEnumerable<string> tags, string? tenantId = null);

    /// <summary>
    /// Gets multiple values from cache
    /// </summary>
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Sets multiple values in cache
    /// </summary>
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Checks if a key exists in any cache level
    /// </summary>
    Task<bool> ExistsAsync(string key, CacheOptions? options = null);

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Warms cache with frequently accessed data
    /// </summary>
    Task WarmCacheAsync(string tenantId);
}

/// <summary>
/// Interface for L1 memory cache
/// </summary>
public interface IL1MemoryCache : IDisposable
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, IEnumerable<string> tags) where T : class;
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
    Task InvalidateByTagsAsync(IEnumerable<string> tags);
    Task<CacheLevel1Statistics> GetStatisticsAsync();
}

/// <summary>
/// Interface for L2 Redis cache
/// </summary>
public interface IL2RedisCache : IDisposable
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, IEnumerable<string> tags) where T : class;
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
    Task InvalidateByTagsAsync(IEnumerable<string> tags);
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration, IEnumerable<string> tags) where T : class;
    Task<bool> ExistsAsync(string key);
    Task<CacheLevel2Statistics> GetStatisticsAsync();
}

/// <summary>
/// Interface for L3 persistent cache
/// </summary>
public interface IL3PersistentCache : IDisposable
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, IEnumerable<string> tags) where T : class;
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
    Task InvalidateByTagsAsync(IEnumerable<string> tags);
    Task<CacheLevel3Statistics> GetStatisticsAsync();
}

/// <summary>
/// Interface for cache key generation
/// </summary>
public interface ICacheKeyGenerator
{
    string GenerateKey(string baseKey, Type type, string? tenantId = null);
    string GeneratePattern(string basePattern, string? tenantId = null);
    string GenerateSearchKey(string query, Dictionary<string, string>? filters, IEnumerable<string>? collectionIds, int limit, int offset, string? tenantId = null);
    string GenerateRAGKey(string query, string? context, IEnumerable<string>? collectionIds, string? promptTemplate, string? tenantId = null);
    string GenerateDocumentKey(string documentId, string? tenantId = null);
    string GenerateCollectionKey(string collectionId, string? tenantId = null);
    string GenerateConversationKey(string conversationId, string? tenantId = null);
    string GenerateEmbeddingKey(string text, string model, string? tenantId = null);
}

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    public TimeSpan L1TTL { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan L2TTL { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan L3TTL { get; set; } = TimeSpan.FromHours(1);
    public bool UseL1Cache { get; set; } = true;
    public bool UseL2Cache { get; set; } = true;
    public bool UseL3Cache { get; set; } = false;
    public string? TenantId { get; set; }
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public bool CompressData { get; set; } = false;
    public bool EncryptData { get; set; } = false;

    /// <summary>
    /// Tags for cache invalidation - enables atomic invalidation across all cache layers
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    public static CacheOptions Default => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - L1 cache for frequent data
        UseL2Cache = false, // ❌ DISABLED - Redis only for expensive data
        UseL3Cache = false, // ❌ DISABLED - Not needed for most operations
        L1TTL = TimeSpan.FromMinutes(15) // Reasonable TTL for memory cache
    };
    
    public static CacheOptions ForSearch(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for repeated searches
        UseL2Cache = false, // ❌ DISABLED - No Redis for basic search operations
        UseL3Cache = false, // ❌ DISABLED - Search results are dynamic
        L1TTL = TimeSpan.FromMinutes(5),  // Short TTL for dynamic data
        L2TTL = TimeSpan.FromMinutes(15), // Not used since L2 disabled
        TenantId = tenantId,
        Priority = CachePriority.Normal
    };

    public static CacheOptions ForRAG(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for RAG results
        UseL2Cache = true,  // ✅ ENABLED - Redis for expensive RAG computations
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for expensive RAG data
        L1TTL = TimeSpan.FromMinutes(10),
        L2TTL = TimeSpan.FromHours(1), // Redis for expensive RAG operations
        L3TTL = TimeSpan.FromHours(6),
        TenantId = tenantId,
        Priority = CachePriority.High,
        CompressData = true
    };

    /// <summary>
    /// Cache options for expensive computational data (embeddings, metadata)
    /// </summary>
    public static CacheOptions ForExpensiveData(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ Fast access for recent data
        UseL2Cache = true,  // ✅ Redis for expensive computations
        UseL3Cache = true,  // ✅ Persistent storage for very expensive data
        L1TTL = TimeSpan.FromHours(1),   // Keep in memory for frequent access
        L2TTL = TimeSpan.FromDays(1),    // Redis for expensive operations
        L3TTL = TimeSpan.FromDays(7),    // Long-term storage for embeddings
        TenantId = tenantId,
        Priority = CachePriority.High,
        CompressData = true // Compress large embedding vectors
    };

    public static CacheOptions ForDocumentMetadata(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for document metadata
        UseL2Cache = false, // ❌ DISABLED - No Redis for basic document operations
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for document metadata
        L1TTL = TimeSpan.FromHours(1),
        L2TTL = TimeSpan.FromHours(24), // Not used since L2 disabled
        L3TTL = TimeSpan.FromDays(7),
        TenantId = tenantId,
        Priority = CachePriority.Normal
    };

    public static CacheOptions ForCollectionData(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for collection data
        UseL2Cache = false, // ❌ DISABLED - No Redis for basic collection operations
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for collection metadata
        L1TTL = TimeSpan.FromMinutes(30),
        L2TTL = TimeSpan.FromHours(6), // Not used since L2 disabled
        L3TTL = TimeSpan.FromDays(1),
        TenantId = tenantId,
        Priority = CachePriority.Normal
    };

    /// <summary>
    /// Cache options for static document chunks and embeddings (high cache value)
    /// </summary>
    public static CacheOptions ForDocumentChunks(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for document chunks
        UseL2Cache = true,  // ✅ ENABLED - Redis for expensive chunk processing
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for chunks
        L1TTL = TimeSpan.FromHours(2),
        L2TTL = TimeSpan.FromDays(1),
        L3TTL = TimeSpan.FromDays(7),
        TenantId = tenantId,
        Priority = CachePriority.High,
        CompressData = true
    };

    /// <summary>
    /// Cache options for embeddings (very high cache value, static after generation)
    /// </summary>
    public static CacheOptions ForEmbeddings(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for embeddings
        UseL2Cache = true,  // ✅ ENABLED - Redis for expensive embedding computations
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for embeddings
        L1TTL = TimeSpan.FromHours(6),
        L2TTL = TimeSpan.FromDays(7),
        L3TTL = TimeSpan.FromDays(30),
        TenantId = tenantId,
        Priority = CachePriority.Critical,
        CompressData = true
    };

    /// <summary>
    /// Cache options for semantic query results (medium cache value)
    /// </summary>
    public static CacheOptions ForSemanticQueries(string? tenantId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for semantic queries
        UseL2Cache = true,  // ✅ ENABLED - Redis for expensive semantic processing
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage for semantic results
        L1TTL = TimeSpan.FromMinutes(30),
        L2TTL = TimeSpan.FromHours(4),
        L3TTL = TimeSpan.FromDays(1),
        TenantId = tenantId,
        Priority = CachePriority.High,
        CompressData = true
    };

    /// <summary>
    /// Cache options for dynamic document lists with tag-based invalidation
    /// </summary>
    public static CacheOptions ForDocumentLists(string? tenantId = null, string? userId = null, string? collectionId = null) => new()
    {
        UseL1Cache = true,  // ✅ ENABLED - Fast access for document lists
        UseL2Cache = false, // ❌ DISABLED - No Redis for basic document list operations
        UseL3Cache = true,  // ✅ ENABLED - Persistent storage with tag-based invalidation
        L1TTL = TimeSpan.FromMinutes(5), // Increased TTL since we have reliable tag-based invalidation
        L2TTL = TimeSpan.FromMinutes(15), // Not used since L2 disabled
        L3TTL = TimeSpan.FromHours(1),
        TenantId = tenantId,
        Priority = CachePriority.Normal,
        Tags = CreateDocumentListTags(userId, collectionId)
    };

    /// <summary>
    /// Creates standardized tags for document list caching
    /// </summary>
    private static HashSet<string> CreateDocumentListTags(string? userId = null, string? collectionId = null)
    {
        var tags = new HashSet<string> { "documents", "document-lists" };

        if (!string.IsNullOrEmpty(userId))
            tags.Add($"user:{userId}");

        if (!string.IsNullOrEmpty(collectionId))
            tags.Add($"collection:{collectionId}");

        return tags;
    }
}

/// <summary>
/// Cache priority levels
/// </summary>
public enum CachePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public CacheLevel1Statistics L1Statistics { get; set; } = new();
    public CacheLevel2Statistics L2Statistics { get; set; } = new();
    public CacheLevel3Statistics L3Statistics { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public TimeSpan UpTime { get; set; }
}

public class CacheLevel1Statistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests => HitCount + MissCount;
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public long MemoryUsageBytes { get; set; }
    public int EntryCount { get; set; }
    public TimeSpan AverageAccessTime { get; set; }
}

public class CacheLevel2Statistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests => HitCount + MissCount;
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public long NetworkLatencyMs { get; set; }
    public bool IsConnected { get; set; }
    public int DatabaseIndex { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int KeyCount { get; set; }
}

public class CacheLevel3Statistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests => HitCount + MissCount;
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public long DiskUsageBytes { get; set; }
    public TimeSpan AverageAccessTime { get; set; }
    public int FileCount { get; set; }
}
