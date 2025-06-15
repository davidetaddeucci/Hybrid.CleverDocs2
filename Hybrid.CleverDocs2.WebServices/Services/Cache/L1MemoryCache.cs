using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// L1 in-memory cache implementation with pattern invalidation and statistics
/// </summary>
public class L1MemoryCache : IL1MemoryCache, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<L1MemoryCache> _logger;
    private readonly L1CacheOptions _options;
    private readonly ConcurrentDictionary<string, DateTime> _keyTracker;
    private readonly ConcurrentDictionary<string, long> _accessCounts;
    private readonly Timer _cleanupTimer;
    private readonly object _statsLock = new();
    
    private long _hitCount;
    private long _missCount;
    private long _totalAccessTime;
    private long _accessCount;

    public L1MemoryCache(
        IMemoryCache memoryCache,
        ILogger<L1MemoryCache> logger,
        IOptions<L1CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
        _keyTracker = new ConcurrentDictionary<string, DateTime>();
        _accessCounts = new ConcurrentDictionary<string, long>();

        // Setup cleanup timer for expired entries tracking
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                RecordHit(stopwatch.ElapsedTicks);
                RecordAccess(key);
                
                _logger.LogDebug("L1 Cache HIT: {Key} (Type: {Type})", key, typeof(T).Name);
                return value;
            }

            RecordMiss(stopwatch.ElapsedTicks);
            _logger.LogDebug("L1 Cache MISS: {Key} (Type: {Type})", key, typeof(T).Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from L1 cache for key: {Key}", key);
            RecordMiss(stopwatch.ElapsedTicks);
            return null;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal,
                Size = EstimateSize(value)
            };

            // Add eviction callback to track removals
            cacheEntryOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _keyTracker.TryRemove(evictedKey.ToString()!, out _);
                _accessCounts.TryRemove(evictedKey.ToString()!, out _);
                
                _logger.LogDebug("L1 Cache EVICTED: {Key} (Reason: {Reason})", evictedKey, reason);
            });

            _memoryCache.Set(key, value, cacheEntryOptions);
            _keyTracker[key] = DateTime.UtcNow.Add(expiration);
            
            _logger.LogDebug("L1 Cache SET: {Key} (Type: {Type}, Expiration: {Expiration})", 
                key, typeof(T).Name, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in L1 cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);
            _accessCounts.TryRemove(key, out _);
            
            _logger.LogDebug("L1 Cache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from L1 cache for key: {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        try
        {
            var regex = new Regex(pattern.Replace("*", ".*"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();
            
            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }
            
            _logger.LogInformation("L1 Cache PATTERN INVALIDATION: {Pattern} ({Count} keys removed)", 
                pattern, keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating L1 cache pattern: {Pattern}", pattern);
        }
    }

    public async Task<CacheLevel1Statistics> GetStatisticsAsync()
    {
        lock (_statsLock)
        {
            var totalRequests = _hitCount + _missCount;
            var averageAccessTime = _accessCount > 0 
                ? TimeSpan.FromTicks(_totalAccessTime / _accessCount)
                : TimeSpan.Zero;

            return new CacheLevel1Statistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                MemoryUsageBytes = EstimateMemoryUsage(),
                EntryCount = _keyTracker.Count,
                AverageAccessTime = averageAccessTime
            };
        }
    }

    private void RecordHit(long elapsedTicks)
    {
        lock (_statsLock)
        {
            _hitCount++;
            _totalAccessTime += elapsedTicks;
            _accessCount++;
        }
    }

    private void RecordMiss(long elapsedTicks)
    {
        lock (_statsLock)
        {
            _missCount++;
            _totalAccessTime += elapsedTicks;
            _accessCount++;
        }
    }

    private void RecordAccess(string key)
    {
        _accessCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
    }

    private long EstimateSize<T>(T value)
    {
        if (value == null) return 0;

        // Simple size estimation based on type
        return value switch
        {
            string str => str.Length * 2, // Unicode characters
            byte[] bytes => bytes.Length,
            _ => _options.DefaultObjectSize
        };
    }

    private long EstimateMemoryUsage()
    {
        // This is a rough estimation
        // In production, you might want to use more sophisticated memory profiling
        return _keyTracker.Count * _options.AverageEntrySize;
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _keyTracker
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _keyTracker.TryRemove(key, out _);
                _accessCounts.TryRemove(key, out _);
            }

            if (expiredKeys.Any())
            {
                _logger.LogDebug("L1 Cache cleanup removed {Count} expired key trackers", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during L1 cache cleanup");
        }
    }

    /// <summary>
    /// Gets the most frequently accessed keys for cache warming
    /// </summary>
    public async Task<IEnumerable<string>> GetHotKeysAsync(int count = 10)
    {
        return _accessCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Preloads data into cache for warming
    /// </summary>
    public async Task PreloadAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration) where T : class
    {
        var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("L1 Cache preloaded {Count} entries", keyValuePairs.Count);
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _keyTracker.Clear();
        _accessCounts.Clear();
    }
}

/// <summary>
/// Configuration options for L1 memory cache
/// </summary>
public class L1CacheOptions
{
    public long MaxMemorySize { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxEntryCount { get; set; } = 10000;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public long DefaultObjectSize { get; set; } = 1024; // 1KB
    public long AverageEntrySize { get; set; } = 2048; // 2KB
    public bool EnableStatistics { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
