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
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
    Task<CacheLevel1Statistics> GetStatisticsAsync();
}

/// <summary>
/// Interface for L2 Redis cache
/// </summary>
public interface IL2RedisCache : IDisposable
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration) where T : class;
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
    Task RemoveAsync(string key);
    Task InvalidatePatternAsync(string pattern);
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

    public static CacheOptions Default => new();
    
    public static CacheOptions ForSearch(string? tenantId = null) => new()
    {
        L1TTL = TimeSpan.FromMinutes(5),
        L2TTL = TimeSpan.FromMinutes(15),
        L3TTL = TimeSpan.FromHours(1),
        UseL3Cache = false,
        TenantId = tenantId,
        Priority = CachePriority.High
    };

    public static CacheOptions ForRAG(string? tenantId = null) => new()
    {
        L1TTL = TimeSpan.FromMinutes(10),
        L2TTL = TimeSpan.FromHours(1),
        L3TTL = TimeSpan.FromHours(6),
        UseL3Cache = true,
        TenantId = tenantId,
        Priority = CachePriority.High,
        CompressData = true
    };

    public static CacheOptions ForDocumentMetadata(string? tenantId = null) => new()
    {
        L1TTL = TimeSpan.FromHours(1),
        L2TTL = TimeSpan.FromHours(24),
        L3TTL = TimeSpan.FromDays(7),
        UseL3Cache = true,
        TenantId = tenantId,
        Priority = CachePriority.Normal
    };

    public static CacheOptions ForCollectionData(string? tenantId = null) => new()
    {
        L1TTL = TimeSpan.FromMinutes(30),
        L2TTL = TimeSpan.FromHours(6),
        L3TTL = TimeSpan.FromDays(1),
        UseL3Cache = true,
        TenantId = tenantId,
        Priority = CachePriority.Normal
    };
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
