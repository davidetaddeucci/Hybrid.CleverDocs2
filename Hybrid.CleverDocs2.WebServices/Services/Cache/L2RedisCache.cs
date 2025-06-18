using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// L2 Redis distributed cache implementation with compression and advanced features
/// </summary>
public class L2RedisCache : IL2RedisCache, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IDatabase? _database;
    private readonly ILogger<L2RedisCache> _logger;
    private readonly L2CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _statsLock = new();
    
    private long _hitCount;
    private long _missCount;
    private long _totalNetworkTime;
    private long _networkOperationCount;

    public L2RedisCache(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<L2RedisCache> logger,
        IOptions<L2CacheOptions> options)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer?.GetDatabase();
        _logger = logger;
        _options = options.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    private bool IsRedisAvailable => _database != null && _connectionMultiplexer?.IsConnected == true;

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var value = await _distributedCache.GetStringAsync(key);
            
            RecordNetworkOperation(stopwatch.ElapsedTicks);
            
            if (!string.IsNullOrEmpty(value))
            {
                RecordHit();
                var deserializedValue = await DeserializeAsync<T>(value);
                
                _logger.LogDebug("L2 Cache HIT: {Key} (Type: {Type}, Size: {Size} bytes)", 
                    key, typeof(T).Name, value.Length);
                
                return deserializedValue;
            }

            RecordMiss();
            _logger.LogDebug("L2 Cache MISS: {Key} (Type: {Type})", key, typeof(T).Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from L2 cache for key: {Key}", key);
            RecordMiss();
            return null;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        await SetAsync(key, value, expiration, Enumerable.Empty<string>());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, IEnumerable<string> tags) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var serializedValue = await SerializeAsync(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options);

            // Handle tags using Redis sets (only if Redis is available)
            var tagList = tags.ToList();
            if (tagList.Any() && IsRedisAvailable)
            {
                var batch = _database!.CreateBatch();
                var tagTasks = new List<Task>();

                foreach (var tag in tagList)
                {
                    var tagKey = $"tag:{tag}";
                    tagTasks.Add(batch.SetAddAsync(tagKey, key));
                    tagTasks.Add(batch.KeyExpireAsync(tagKey, expiration.Add(TimeSpan.FromMinutes(5)))); // Tag expires slightly after data
                }

                batch.Execute();
                await Task.WhenAll(tagTasks);
            }

            RecordNetworkOperation(stopwatch.ElapsedTicks);

            _logger.LogDebug("L2 Cache SET: {Key} (Type: {Type}, Size: {Size} bytes, Expiration: {Expiration}, Tags: {Tags})",
                key, typeof(T).Name, serializedValue.Length, expiration, string.Join(", ", tagList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in L2 cache for key: {Key}", key);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("L2 Cache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from L2 cache for key: {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        if (!IsRedisAvailable)
        {
            _logger.LogWarning("L2 Cache PATTERN INVALIDATION skipped - Redis not available: {Pattern}", pattern);
            return;
        }

        try
        {
            var server = _connectionMultiplexer!.GetServer(_connectionMultiplexer.GetEndPoints().First());

            // Convert pattern to Redis glob format if needed
            // Redis KEYS command expects glob patterns (*, ?, [abc])
            // Our patterns already use * for wildcards, so they should work as-is
            var redisPattern = pattern;

            _logger.LogDebug("L2 Cache PATTERN INVALIDATION starting: {Pattern}", redisPattern);

            var keys = server.Keys(pattern: redisPattern, pageSize: _options.ScanPageSize);

            var keyArray = keys.ToArray();
            if (keyArray.Length > 0)
            {
                await _database!.KeyDeleteAsync(keyArray);
                _logger.LogInformation("L2 Cache PATTERN INVALIDATION: {Pattern} ({Count} keys removed)",
                    pattern, keyArray.Length);
            }
            else
            {
                _logger.LogInformation("L2 Cache PATTERN INVALIDATION: {Pattern} (0 keys removed)", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating L2 cache pattern: {Pattern}", pattern);
        }
    }

    public async Task InvalidateByTagsAsync(IEnumerable<string> tags)
    {
        if (!IsRedisAvailable)
        {
            _logger.LogWarning("L2 Cache TAG INVALIDATION skipped - Redis not available: {Tags}", string.Join(", ", tags));
            return;
        }

        try
        {
            var tagList = tags.ToList();
            var allKeysToRemove = new HashSet<RedisKey>();

            // Get all keys for each tag
            foreach (var tag in tagList)
            {
                var tagKey = $"tag:{tag}";
                var keys = await _database!.SetMembersAsync(tagKey);

                foreach (var key in keys)
                {
                    allKeysToRemove.Add((RedisKey)key.ToString());
                }

                // Remove the tag set itself
                await _database.KeyDeleteAsync(tagKey);
            }

            // Remove all the cached data keys
            if (allKeysToRemove.Any())
            {
                await _database.KeyDeleteAsync(allKeysToRemove.ToArray());
            }

            _logger.LogInformation("L2 Cache TAG INVALIDATION: {Tags} ({Count} keys removed)",
                string.Join(", ", tagList), allKeysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating L2 cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
    {
        var result = new Dictionary<string, T?>();
        var keyArray = keys.ToArray();

        if (!keyArray.Any())
            return result;

        if (!IsRedisAvailable)
        {
            // Fallback to individual gets using distributed cache
            foreach (var key in keyArray)
            {
                result[key] = await GetAsync<T>(key);
            }
            return result;
        }

        try
        {
            var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();
            var values = await _database!.StringGetAsync(redisKeys);
            
            for (int i = 0; i < keyArray.Length; i++)
            {
                var key = keyArray[i];
                var value = values[i];
                
                if (value.HasValue)
                {
                    try
                    {
                        result[key] = await DeserializeAsync<T>(value!);
                        RecordHit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing value for key: {Key}", key);
                        result[key] = null;
                        RecordMiss();
                    }
                }
                else
                {
                    result[key] = null;
                    RecordMiss();
                }
            }
            
            _logger.LogDebug("L2 Cache GET_MANY: {KeyCount} keys, {HitCount} hits", 
                keyArray.Length, result.Values.Count(v => v != null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple values from L2 cache");
            
            // Return empty results for all keys on error
            foreach (var key in keyArray)
            {
                result[key] = null;
                RecordMiss();
            }
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration) where T : class
    {
        await SetManyAsync(keyValuePairs, expiration, Enumerable.Empty<string>());
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan expiration, IEnumerable<string> tags) where T : class
    {
        if (!keyValuePairs.Any())
            return;

        if (!IsRedisAvailable)
        {
            // Fallback to individual sets using distributed cache
            foreach (var kvp in keyValuePairs)
            {
                await SetAsync(kvp.Key, kvp.Value, expiration, tags);
            }
            return;
        }

        try
        {
            var batch = _database!.CreateBatch();
            var tasks = new List<Task>();
            var tagList = tags.ToList();

            foreach (var kvp in keyValuePairs)
            {
                var serializedValue = await SerializeAsync(kvp.Value);
                tasks.Add(batch.StringSetAsync(kvp.Key, serializedValue, expiration));

                // Handle tags for each key
                if (tagList.Any())
                {
                    foreach (var tag in tagList)
                    {
                        var tagKey = $"tag:{tag}";
                        tasks.Add(batch.SetAddAsync(tagKey, kvp.Key));
                        tasks.Add(batch.KeyExpireAsync(tagKey, expiration.Add(TimeSpan.FromMinutes(5))));
                    }
                }
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogDebug("L2 Cache SET_MANY: {Count} keys set with tags {Tags}",
                keyValuePairs.Count, string.Join(", ", tagList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple values in L2 cache");
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (!IsRedisAvailable)
        {
            // Fallback: try to get the value to check existence
            var value = await GetAsync<object>(key);
            return value != null;
        }

        try
        {
            return await _database!.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists in L2 cache: {Key}", key);
            return false;
        }
    }

    public async Task<CacheLevel2Statistics> GetStatisticsAsync()
    {
        long averageNetworkLatency;
        long hitCount, missCount;

        lock (_statsLock)
        {
            averageNetworkLatency = _networkOperationCount > 0
                ? (long)(_totalNetworkTime / _networkOperationCount / TimeSpan.TicksPerMillisecond)
                : 0;
            hitCount = _hitCount;
            missCount = _missCount;
        }

        if (!IsRedisAvailable)
        {
            return new CacheLevel2Statistics
            {
                HitCount = hitCount,
                MissCount = missCount,
                NetworkLatencyMs = averageNetworkLatency,
                IsConnected = false,
                DatabaseIndex = 0,
                MemoryUsageBytes = 0,
                KeyCount = 0
            };
        }

        try
        {
            var info = await _database!.ExecuteAsync("INFO", "memory");
            var keyspaceInfo = await _database.ExecuteAsync("INFO", "keyspace");

            return new CacheLevel2Statistics
            {
                HitCount = hitCount,
                MissCount = missCount,
                NetworkLatencyMs = averageNetworkLatency,
                IsConnected = _connectionMultiplexer!.IsConnected,
                DatabaseIndex = _database.Database,
                MemoryUsageBytes = ParseMemoryUsage(info.ToString()),
                KeyCount = ParseKeyCount(keyspaceInfo.ToString())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting L2 cache statistics");

            return new CacheLevel2Statistics
            {
                HitCount = hitCount,
                MissCount = missCount,
                NetworkLatencyMs = averageNetworkLatency,
                IsConnected = _connectionMultiplexer?.IsConnected ?? false,
                DatabaseIndex = _database?.Database ?? 0
            };
        }
    }

    private async Task<string> SerializeAsync<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        
        if (_options.EnableCompression && json.Length > _options.CompressionThreshold)
        {
            var compressed = await CompressStringAsync(json);
            return $"COMPRESSED:{Convert.ToBase64String(compressed)}";
        }
        
        return json;
    }

    private async Task<T?> DeserializeAsync<T>(string value) where T : class
    {
        if (value.StartsWith("COMPRESSED:"))
        {
            var base64Data = value.Substring("COMPRESSED:".Length);
            var compressedData = Convert.FromBase64String(base64Data);
            var decompressed = await DecompressStringAsync(compressedData);
            return JsonSerializer.Deserialize<T>(decompressed, _jsonOptions);
        }
        
        return JsonSerializer.Deserialize<T>(value, _jsonOptions);
    }

    private async Task<byte[]> CompressStringAsync(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            await gzip.WriteAsync(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    private async Task<string> DecompressStringAsync(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private void RecordHit()
    {
        lock (_statsLock)
        {
            _hitCount++;
        }
    }

    private void RecordMiss()
    {
        lock (_statsLock)
        {
            _missCount++;
        }
    }

    private void RecordNetworkOperation(long elapsedTicks)
    {
        lock (_statsLock)
        {
            _totalNetworkTime += elapsedTicks;
            _networkOperationCount++;
        }
    }

    private long ParseMemoryUsage(string info)
    {
        // Parse Redis INFO memory output
        var lines = info.Split('\n');
        var memoryLine = lines.FirstOrDefault(l => l.StartsWith("used_memory:"));
        if (memoryLine != null && long.TryParse(memoryLine.Split(':')[1].Trim(), out var memory))
        {
            return memory;
        }
        return 0;
    }

    private int ParseKeyCount(string keyspaceInfo)
    {
        if (_database == null) return 0;

        // Parse Redis INFO keyspace output
        var lines = keyspaceInfo.Split('\n');
        var dbLine = lines.FirstOrDefault(l => l.StartsWith($"db{_database.Database}:"));
        if (dbLine != null)
        {
            var parts = dbLine.Split(',');
            var keysPart = parts.FirstOrDefault(p => p.Contains("keys="));
            if (keysPart != null && int.TryParse(keysPart.Split('=')[1], out var keys))
            {
                return keys;
            }
        }
        return 0;
    }

    public void Dispose()
    {
        // Connection multiplexer is managed by DI container
    }
}

/// <summary>
/// Configuration options for L2 Redis cache
/// </summary>
public class L2CacheOptions
{
    public bool EnableCompression { get; set; } = true;
    public int CompressionThreshold { get; set; } = 1024; // 1KB
    public int ScanPageSize { get; set; } = 1000;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxBatchSize { get; set; } = 100;
    public bool EnableStatistics { get; set; } = true;
}
