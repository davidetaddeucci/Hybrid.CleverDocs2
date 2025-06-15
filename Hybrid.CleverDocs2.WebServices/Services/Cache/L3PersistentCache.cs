using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// L3 persistent file-based cache implementation for expensive operations
/// </summary>
public class L3PersistentCache : IL3PersistentCache, IDisposable
{
    private readonly ILogger<L3PersistentCache> _logger;
    private readonly L3CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, CacheEntry> _index;
    private readonly Timer _cleanupTimer;
    private readonly SemaphoreSlim _fileLock;
    private readonly object _statsLock = new();
    
    private long _hitCount;
    private long _missCount;
    private long _totalAccessTime;
    private long _accessCount;

    public L3PersistentCache(
        ILogger<L3PersistentCache> logger,
        IOptions<L3CacheOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _index = new ConcurrentDictionary<string, CacheEntry>();
        _fileLock = new SemaphoreSlim(_options.MaxConcurrentOperations, _options.MaxConcurrentOperations);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Ensure cache directory exists
        Directory.CreateDirectory(_options.CacheDirectory);
        
        // Load existing index
        LoadIndex();
        
        // Setup cleanup timer
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
            _options.CleanupInterval, _options.CleanupInterval);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!_index.TryGetValue(key, out var entry))
            {
                RecordMiss(stopwatch.ElapsedTicks);
                _logger.LogDebug("L3 Cache MISS (not in index): {Key}", key);
                return null;
            }

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                // Entry expired, remove it
                await RemoveAsync(key);
                RecordMiss(stopwatch.ElapsedTicks);
                _logger.LogDebug("L3 Cache MISS (expired): {Key}", key);
                return null;
            }

            await _fileLock.WaitAsync();
            try
            {
                var filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                {
                    // File missing, remove from index
                    _index.TryRemove(key, out _);
                    RecordMiss(stopwatch.ElapsedTicks);
                    _logger.LogDebug("L3 Cache MISS (file missing): {Key}", key);
                    return null;
                }

                var data = await File.ReadAllBytesAsync(filePath);
                var json = await DecompressAndDecryptAsync(data, entry.IsCompressed, entry.IsEncrypted);
                var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                // Update access time
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;
                
                RecordHit(stopwatch.ElapsedTicks);
                _logger.LogDebug("L3 Cache HIT: {Key} (Type: {Type}, Size: {Size} bytes)", 
                    key, typeof(T).Name, data.Length);
                
                return value;
            }
            finally
            {
                _fileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from L3 cache for key: {Key}", key);
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
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var shouldCompress = json.Length > _options.CompressionThreshold;
            var shouldEncrypt = _options.EnableEncryption;
            
            var data = await CompressAndEncryptAsync(json, shouldCompress, shouldEncrypt);
            
            await _fileLock.WaitAsync();
            try
            {
                var filePath = GetFilePath(key);
                await File.WriteAllBytesAsync(filePath, data);
                
                var entry = new CacheEntry
                {
                    Key = key,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    LastAccessedAt = DateTime.UtcNow,
                    Size = data.Length,
                    IsCompressed = shouldCompress,
                    IsEncrypted = shouldEncrypt,
                    AccessCount = 0
                };
                
                _index[key] = entry;
                await SaveIndexAsync();
                
                _logger.LogDebug("L3 Cache SET: {Key} (Type: {Type}, Size: {Size} bytes, Compressed: {Compressed}, Expiration: {Expiration})", 
                    key, typeof(T).Name, data.Length, shouldCompress, expiration);
            }
            finally
            {
                _fileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in L3 cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _fileLock.WaitAsync();
            try
            {
                var filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                _index.TryRemove(key, out _);
                await SaveIndexAsync();
                
                _logger.LogDebug("L3 Cache REMOVE: {Key}", key);
            }
            finally
            {
                _fileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from L3 cache for key: {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        try
        {
            var regex = new System.Text.RegularExpressions.Regex(
                pattern.Replace("*", ".*"), 
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            var keysToRemove = _index.Keys.Where(key => regex.IsMatch(key)).ToList();
            
            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }
            
            _logger.LogInformation("L3 Cache PATTERN INVALIDATION: {Pattern} ({Count} keys removed)", 
                pattern, keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating L3 cache pattern: {Pattern}", pattern);
        }
    }

    public async Task<CacheLevel3Statistics> GetStatisticsAsync()
    {
        lock (_statsLock)
        {
            var totalDiskUsage = _index.Values.Sum(e => e.Size);
            var averageAccessTime = _accessCount > 0 
                ? TimeSpan.FromTicks(_totalAccessTime / _accessCount)
                : TimeSpan.Zero;

            return new CacheLevel3Statistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                DiskUsageBytes = totalDiskUsage,
                AverageAccessTime = averageAccessTime,
                FileCount = _index.Count
            };
        }
    }

    private string GetFilePath(string key)
    {
        var safeKey = GetSafeFileName(key);
        return Path.Combine(_options.CacheDirectory, $"{safeKey}.cache");
    }

    private string GetSafeFileName(string key)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task<byte[]> CompressAndEncryptAsync(string data, bool compress, bool encrypt)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        
        if (compress)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
            {
                await gzip.WriteAsync(bytes, 0, bytes.Length);
            }
            bytes = output.ToArray();
        }
        
        if (encrypt && !string.IsNullOrEmpty(_options.EncryptionKey))
        {
            bytes = await EncryptAsync(bytes);
        }
        
        return bytes;
    }

    private async Task<string> DecompressAndDecryptAsync(byte[] data, bool isCompressed, bool isEncrypted)
    {
        var bytes = data;
        
        if (isEncrypted && !string.IsNullOrEmpty(_options.EncryptionKey))
        {
            bytes = await DecryptAsync(bytes);
        }
        
        if (isCompressed)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output);
            bytes = output.ToArray();
        }
        
        return Encoding.UTF8.GetString(bytes);
    }

    private async Task<byte[]> EncryptAsync(byte[] data)
    {
        // Simple AES encryption implementation
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_options.EncryptionKey.PadRight(32).Substring(0, 32));
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var output = new MemoryStream();
        
        // Write IV first
        await output.WriteAsync(aes.IV, 0, aes.IV.Length);
        
        using (var cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
        {
            await cryptoStream.WriteAsync(data, 0, data.Length);
        }
        
        return output.ToArray();
    }

    private async Task<byte[]> DecryptAsync(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_options.EncryptionKey.PadRight(32).Substring(0, 32));
        
        using var input = new MemoryStream(data);
        
        // Read IV
        var iv = new byte[16];
        await input.ReadAsync(iv, 0, 16);
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();
        
        await cryptoStream.CopyToAsync(output);
        return output.ToArray();
    }

    private void LoadIndex()
    {
        try
        {
            var indexPath = Path.Combine(_options.CacheDirectory, "index.json");
            if (File.Exists(indexPath))
            {
                var json = File.ReadAllText(indexPath);
                var entries = JsonSerializer.Deserialize<CacheEntry[]>(json, _jsonOptions);
                
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        _index[entry.Key] = entry;
                    }
                }
                
                _logger.LogInformation("L3 Cache loaded {Count} entries from index", _index.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading L3 cache index");
        }
    }

    private async Task SaveIndexAsync()
    {
        try
        {
            var indexPath = Path.Combine(_options.CacheDirectory, "index.json");
            var entries = _index.Values.ToArray();
            var json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(indexPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving L3 cache index");
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _index
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _ = Task.Run(() => RemoveAsync(key));
            }

            if (expiredKeys.Any())
            {
                _logger.LogInformation("L3 Cache cleanup scheduled removal of {Count} expired entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during L3 cache cleanup");
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

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _fileLock?.Dispose();
        
        // Save index on disposal
        try
        {
            SaveIndexAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving index during disposal");
        }
    }
}

/// <summary>
/// Cache entry metadata for L3 persistent cache
/// </summary>
public class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public long Size { get; set; }
    public bool IsCompressed { get; set; }
    public bool IsEncrypted { get; set; }
    public long AccessCount { get; set; }
}

/// <summary>
/// Configuration options for L3 persistent cache
/// </summary>
public class L3CacheOptions
{
    public string CacheDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "CleverDocs2Cache");
    public bool EnableCompression { get; set; } = true;
    public int CompressionThreshold { get; set; } = 1024; // 1KB
    public bool EnableEncryption { get; set; } = false;
    public string EncryptionKey { get; set; } = string.Empty;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public int MaxConcurrentOperations { get; set; } = 10;
    public long MaxDiskUsage { get; set; } = 1024 * 1024 * 1024; // 1GB
}
