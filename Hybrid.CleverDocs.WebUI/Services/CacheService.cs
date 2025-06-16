using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(
            IMemoryCache memoryCache,
            ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                // Use only memory cache (Redis disabled for stability)
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    _logger.LogDebug("Cache HIT (Memory): {Key}", key);
                    return memoryValue;
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

                // Set only in memory cache (Redis disabled for stability)
                _memoryCache.Set(key, value, defaultExpiration);

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
                return _memoryCache.TryGetValue(key, out _);
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
                // Memory cache doesn't need explicit refresh
                _logger.LogDebug("Cache REFRESH: {Key} (Memory cache - no action needed)", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cached value for key: {Key}", key);
            }
        }
    }
}
