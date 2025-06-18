using Microsoft.Extensions.Options;
using System.Diagnostics;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Multi-level cache service orchestrator that manages L1, L2, and L3 cache levels
/// </summary>
public class MultiLevelCacheService : IMultiLevelCacheService, IDisposable
{
    private readonly IL1MemoryCache _l1Cache;
    private readonly IL2RedisCache _l2Cache;
    private readonly IL3PersistentCache _l3Cache;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly MultiLevelCacheOptions _options;
    private readonly Timer _statisticsTimer;
    
    private readonly object _statsLock = new();
    private long _totalRequests;
    private long _l1Hits;
    private long _l2Hits;
    private long _l3Hits;
    private long _cacheMisses;

    public MultiLevelCacheService(
        IL1MemoryCache l1Cache,
        IL2RedisCache l2Cache,
        IL3PersistentCache l3Cache,
        ICacheKeyGenerator keyGenerator,
        ILogger<MultiLevelCacheService> logger,
        ICorrelationService correlationService,
        IOptions<MultiLevelCacheOptions> options)
    {
        _l1Cache = l1Cache;
        _l2Cache = l2Cache;
        _l3Cache = l3Cache;
        _keyGenerator = keyGenerator;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;

        // Setup statistics logging timer
        _statisticsTimer = new Timer(LogStatistics, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey(key, typeof(T), options.TenantId);
        var correlationId = _correlationService.GetCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            RecordRequest();

            // L1 Cache check
            if (options.UseL1Cache)
            {
                var l1Result = await _l1Cache.GetAsync<T>(fullKey);
                if (l1Result != null)
                {
                    RecordL1Hit();
                    _logger.LogDebug("L1 Cache hit for key {Key}, CorrelationId: {CorrelationId}, Time: {ElapsedMs}ms", 
                        fullKey, correlationId, stopwatch.ElapsedMilliseconds);
                    return l1Result;
                }
            }

            // L2 Cache check
            if (options.UseL2Cache)
            {
                var l2Result = await _l2Cache.GetAsync<T>(fullKey);
                if (l2Result != null)
                {
                    RecordL2Hit();
                    _logger.LogDebug("L2 Cache hit for key {Key}, CorrelationId: {CorrelationId}, Time: {ElapsedMs}ms", 
                        fullKey, correlationId, stopwatch.ElapsedMilliseconds);
                    
                    // Populate L1 cache
                    if (options.UseL1Cache)
                    {
                        await _l1Cache.SetAsync(fullKey, l2Result, options.L1TTL);
                    }
                    return l2Result;
                }
            }

            // L3 Cache check (for expensive operations only)
            if (options.UseL3Cache)
            {
                var l3Result = await _l3Cache.GetAsync<T>(fullKey);
                if (l3Result != null)
                {
                    RecordL3Hit();
                    _logger.LogDebug("L3 Cache hit for key {Key}, CorrelationId: {CorrelationId}, Time: {ElapsedMs}ms", 
                        fullKey, correlationId, stopwatch.ElapsedMilliseconds);
                    
                    // Populate L2 and L1 caches
                    var populationTasks = new List<Task>();
                    
                    if (options.UseL2Cache)
                    {
                        populationTasks.Add(_l2Cache.SetAsync(fullKey, l3Result, options.L2TTL));
                    }
                    
                    if (options.UseL1Cache)
                    {
                        populationTasks.Add(_l1Cache.SetAsync(fullKey, l3Result, options.L1TTL));
                    }
                    
                    if (populationTasks.Any())
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.WhenAll(populationTasks);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error populating cache levels after L3 hit, CorrelationId: {CorrelationId}", correlationId);
                            }
                        });
                    }
                    
                    return l3Result;
                }
            }

            // Cache miss - execute factory
            RecordCacheMiss();
            _logger.LogDebug("Cache miss for key {Key}, executing factory, CorrelationId: {CorrelationId}", 
                fullKey, correlationId);
            
            var result = await factory();

            if (result != null)
            {
                // Store in all applicable cache levels
                await StoreInCacheLevelsAsync(fullKey, result, options, correlationId);
            }

            _logger.LogDebug("Factory execution completed for key {Key}, CorrelationId: {CorrelationId}, Time: {ElapsedMs}ms", 
                fullKey, correlationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multi-level cache get operation for key {Key}, CorrelationId: {CorrelationId}", 
                fullKey, correlationId);
            
            // On error, try to execute factory directly
            try
            {
                return await factory();
            }
            catch (Exception factoryEx)
            {
                _logger.LogError(factoryEx, "Factory execution also failed for key {Key}, CorrelationId: {CorrelationId}", 
                    fullKey, correlationId);
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey(key, typeof(T), options.TenantId);

        // Check L1 first
        if (options.UseL1Cache)
        {
            var l1Result = await _l1Cache.GetAsync<T>(fullKey);
            if (l1Result != null) return l1Result;
        }

        // Check L2
        if (options.UseL2Cache)
        {
            var l2Result = await _l2Cache.GetAsync<T>(fullKey);
            if (l2Result != null)
            {
                // Populate L1
                if (options.UseL1Cache)
                {
                    await _l1Cache.SetAsync(fullKey, l2Result, options.L1TTL);
                }
                return l2Result;
            }
        }

        // Check L3
        if (options.UseL3Cache)
        {
            var l3Result = await _l3Cache.GetAsync<T>(fullKey);
            if (l3Result != null)
            {
                // Populate L2 and L1
                var tasks = new List<Task>();
                if (options.UseL2Cache)
                {
                    tasks.Add(_l2Cache.SetAsync(fullKey, l3Result, options.L2TTL));
                }
                if (options.UseL1Cache)
                {
                    tasks.Add(_l1Cache.SetAsync(fullKey, l3Result, options.L1TTL));
                }
                
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                }
                
                return l3Result;
            }
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey(key, typeof(T), options.TenantId);
        var correlationId = _correlationService.GetCorrelationId();

        await StoreInCacheLevelsAsync(fullKey, value, options, correlationId);
    }

    public async Task RemoveAsync(string key, CacheOptions? options = null)
    {
        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey(key, typeof(object), options.TenantId);
        var correlationId = _correlationService.GetCorrelationId();

        var tasks = new List<Task>();
        
        if (options.UseL1Cache)
        {
            tasks.Add(_l1Cache.RemoveAsync(fullKey));
        }
        
        if (options.UseL2Cache)
        {
            tasks.Add(_l2Cache.RemoveAsync(fullKey));
        }
        
        if (options.UseL3Cache)
        {
            tasks.Add(_l3Cache.RemoveAsync(fullKey));
        }

        await Task.WhenAll(tasks);
        
        _logger.LogDebug("Removed key {Key} from all cache levels, CorrelationId: {CorrelationId}", 
            fullKey, correlationId);
    }

    public async Task InvalidateAsync(string pattern, string? tenantId = null)
    {
        var fullPattern = _keyGenerator.GeneratePattern(pattern, tenantId);
        var correlationId = _correlationService.GetCorrelationId();

        await Task.WhenAll(
            _l1Cache.InvalidatePatternAsync(fullPattern),
            _l2Cache.InvalidatePatternAsync(fullPattern),
            _l3Cache.InvalidatePatternAsync(fullPattern)
        );

        _logger.LogInformation("Invalidated cache pattern {Pattern} across all levels, CorrelationId: {CorrelationId}",
            fullPattern, correlationId);
    }

    public async Task InvalidateByTagsAsync(IEnumerable<string> tags, string? tenantId = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var tagList = tags.ToList();

        // Add tenant prefix to tags if specified
        var processedTags = tagList.AsEnumerable();
        if (!string.IsNullOrEmpty(tenantId))
        {
            processedTags = tagList.Select(tag => $"tenant:{tenantId}:{tag}");
        }

        var finalTags = processedTags.ToList();

        await Task.WhenAll(
            _l1Cache.InvalidateByTagsAsync(finalTags),
            _l2Cache.InvalidateByTagsAsync(finalTags),
            _l3Cache.InvalidateByTagsAsync(finalTags)
        );

        _logger.LogInformation("Invalidated cache by tags {Tags} across all levels, CorrelationId: {CorrelationId}",
            string.Join(", ", finalTags), correlationId);
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var result = new Dictionary<string, T?>();
        var fullKeys = keys.Select(k => _keyGenerator.GenerateKey(k, typeof(T), options.TenantId)).ToList();

        // Try L2 cache for batch operations (L1 doesn't support batch)
        if (options.UseL2Cache)
        {
            var l2Results = await _l2Cache.GetManyAsync<T>(fullKeys);
            
            foreach (var kvp in l2Results)
            {
                if (kvp.Value != null)
                {
                    result[kvp.Key] = kvp.Value;
                    
                    // Populate L1 cache
                    if (options.UseL1Cache)
                    {
                        await _l1Cache.SetAsync(kvp.Key, kvp.Value, options.L1TTL);
                    }
                }
            }
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var fullKeyValuePairs = keyValuePairs.ToDictionary(
            kvp => _keyGenerator.GenerateKey(kvp.Key, typeof(T), options.TenantId),
            kvp => kvp.Value);

        var tasks = new List<Task>();

        // Set in L1 cache
        if (options.UseL1Cache)
        {
            foreach (var kvp in fullKeyValuePairs)
            {
                tasks.Add(_l1Cache.SetAsync(kvp.Key, kvp.Value, options.L1TTL));
            }
        }

        // Set in L2 cache (batch operation)
        if (options.UseL2Cache)
        {
            tasks.Add(_l2Cache.SetManyAsync(fullKeyValuePairs, options.L2TTL));
        }

        // Set in L3 cache
        if (options.UseL3Cache)
        {
            foreach (var kvp in fullKeyValuePairs)
            {
                tasks.Add(_l3Cache.SetAsync(kvp.Key, kvp.Value, options.L3TTL));
            }
        }

        await Task.WhenAll(tasks);
    }

    public async Task<bool> ExistsAsync(string key, CacheOptions? options = null)
    {
        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey(key, typeof(object), options.TenantId);

        // Check L1 first (fastest)
        if (options.UseL1Cache)
        {
            var l1Result = await _l1Cache.GetAsync<object>(fullKey);
            if (l1Result != null) return true;
        }

        // Check L2
        if (options.UseL2Cache)
        {
            return await _l2Cache.ExistsAsync(fullKey);
        }

        return false;
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var l1Stats = await _l1Cache.GetStatisticsAsync();
        var l2Stats = await _l2Cache.GetStatisticsAsync();
        var l3Stats = await _l3Cache.GetStatisticsAsync();

        return new CacheStatistics
        {
            L1Statistics = l1Stats,
            L2Statistics = l2Stats,
            L3Statistics = l3Stats,
            LastUpdated = DateTime.UtcNow,
            UpTime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
        };
    }

    public async Task WarmCacheAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        _logger.LogInformation("Starting cache warming for tenant {TenantId}, CorrelationId: {CorrelationId}", 
            tenantId, correlationId);

        // This would be implemented based on specific warming strategies
        // For now, it's a placeholder
        await Task.Delay(100);
        
        _logger.LogInformation("Cache warming completed for tenant {TenantId}, CorrelationId: {CorrelationId}", 
            tenantId, correlationId);
    }

    private async Task StoreInCacheLevelsAsync<T>(string fullKey, T value, CacheOptions options, string correlationId) where T : class
    {
        var tasks = new List<Task>();

        // Process tags with tenant prefix if needed
        var processedTags = options.Tags.AsEnumerable();
        if (!string.IsNullOrEmpty(options.TenantId))
        {
            processedTags = options.Tags.Select(tag => $"tenant:{options.TenantId}:{tag}");
        }
        var finalTags = processedTags.ToList();

        if (options.UseL1Cache)
        {
            if (finalTags.Any())
                tasks.Add(_l1Cache.SetAsync(fullKey, value, options.L1TTL, finalTags));
            else
                tasks.Add(_l1Cache.SetAsync(fullKey, value, options.L1TTL));
        }

        if (options.UseL2Cache)
        {
            if (finalTags.Any())
                tasks.Add(_l2Cache.SetAsync(fullKey, value, options.L2TTL, finalTags));
            else
                tasks.Add(_l2Cache.SetAsync(fullKey, value, options.L2TTL));
        }

        if (options.UseL3Cache)
        {
            if (finalTags.Any())
                tasks.Add(_l3Cache.SetAsync(fullKey, value, options.L3TTL, finalTags));
            else
                tasks.Add(_l3Cache.SetAsync(fullKey, value, options.L3TTL));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Stored value in cache levels for key {Key} with tags {Tags}, CorrelationId: {CorrelationId}",
                fullKey, string.Join(", ", finalTags), correlationId);
        }
    }

    private void RecordRequest()
    {
        lock (_statsLock)
        {
            _totalRequests++;
        }
    }

    private void RecordL1Hit()
    {
        lock (_statsLock)
        {
            _l1Hits++;
        }
    }

    private void RecordL2Hit()
    {
        lock (_statsLock)
        {
            _l2Hits++;
        }
    }

    private void RecordL3Hit()
    {
        lock (_statsLock)
        {
            _l3Hits++;
        }
    }

    private void RecordCacheMiss()
    {
        lock (_statsLock)
        {
            _cacheMisses++;
        }
    }

    private void LogStatistics(object? state)
    {
        lock (_statsLock)
        {
            if (_totalRequests > 0)
            {
                var l1HitRatio = (double)_l1Hits / _totalRequests * 100;
                var l2HitRatio = (double)_l2Hits / _totalRequests * 100;
                var l3HitRatio = (double)_l3Hits / _totalRequests * 100;
                var overallHitRatio = (double)(_l1Hits + _l2Hits + _l3Hits) / _totalRequests * 100;

                _logger.LogInformation(
                    "Cache Statistics - Total Requests: {TotalRequests}, " +
                    "L1 Hit Ratio: {L1HitRatio:F2}%, L2 Hit Ratio: {L2HitRatio:F2}%, " +
                    "L3 Hit Ratio: {L3HitRatio:F2}%, Overall Hit Ratio: {OverallHitRatio:F2}%",
                    _totalRequests, l1HitRatio, l2HitRatio, l3HitRatio, overallHitRatio);
            }
        }
    }

    public void Dispose()
    {
        _statisticsTimer?.Dispose();
        _l1Cache?.Dispose();
        _l2Cache?.Dispose();
        _l3Cache?.Dispose();
    }
}

/// <summary>
/// Configuration options for multi-level cache service
/// </summary>
public class MultiLevelCacheOptions
{
    public bool EnableStatisticsLogging { get; set; } = true;
    public TimeSpan StatisticsInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnablePerformanceCounters { get; set; } = false;
    public int MaxConcurrentOperations { get; set; } = 100;
}
