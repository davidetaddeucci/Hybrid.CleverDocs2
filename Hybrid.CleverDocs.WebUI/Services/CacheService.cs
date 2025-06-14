using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CacheService(
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ILogger<CacheService> logger)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                // Try memory cache first (faster)
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    _logger.LogDebug("Cache HIT (Memory): {Key}", key);
                    return memoryValue;
                }

                // Try distributed cache (Redis)
                var distributedValue = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                    
                    // Store in memory cache for faster subsequent access
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    
                    _logger.LogDebug("Cache HIT (Distributed): {Key}", key);
                    return deserializedValue;
                }

                _logger.LogDebug("Cache MISS: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var defaultExpiration = expiration ?? TimeSpan.FromMinutes(15);
                
                // Set in memory cache
                _memoryCache.Set(key, value, defaultExpiration);

                // Set in distributed cache (Redis)
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = defaultExpiration
                };

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                
                _logger.LogDebug("Cache SET: {Key} (Expiration: {Expiration})", key, defaultExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
                
                _logger.LogDebug("Cache REMOVE: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                // Note: This is a simplified implementation
                // In production, you might want to use Redis SCAN command for better performance
                _logger.LogDebug("Cache REMOVE BY PATTERN: {Pattern}", pattern);
                
                // For now, we'll just log the pattern removal
                // Full implementation would require Redis connection to scan keys
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var cachedValue = await GetAsync<T>(key);
                if (cachedValue != null)
                {
                    return cachedValue;
                }

                _logger.LogDebug("Cache MISS - Executing factory for key: {Key}", key);
                var value = await factory();
                
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
                
                // Fallback to factory method
                try
                {
                    return await factory();
                }
                catch (Exception factoryEx)
                {
                    _logger.LogError(factoryEx, "Error in factory method for key: {Key}", key);
                    return null;
                }
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                var distributedValue = await _distributedCache.GetStringAsync(key);
                return !string.IsNullOrEmpty(distributedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {Key}", key);
                return false;
            }
        }

        public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
        {
            var result = new Dictionary<string, T?>();
            
            foreach (var key in keys)
            {
                result[key] = await GetAsync<T>(key);
            }

            return result;
        }

        public async Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null) where T : class
        {
            var tasks = values.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
            await Task.WhenAll(tasks);
        }

        public async Task RefreshAsync(string key)
        {
            try
            {
                // Refresh distributed cache entry
                var value = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(value))
                {
                    await _distributedCache.RefreshAsync(key);
                    _logger.LogDebug("Cache REFRESH: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cached value for key: {Key}", key);
            }
        }
    }
}
