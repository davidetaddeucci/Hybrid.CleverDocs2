using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Service for generating consistent cache keys with tenant isolation
/// </summary>
public class CacheKeyGenerator : ICacheKeyGenerator
{
    private const string KeySeparator = ":";
    private const string TenantPrefix = "tenant";
    private const string TypePrefix = "type";
    private const string GlobalPrefix = "cleverdocs2";

    public string GenerateKey(string baseKey, Type type, string? tenantId = null)
    {
        var keyParts = new List<string> { GlobalPrefix };

        if (!string.IsNullOrEmpty(tenantId))
        {
            keyParts.Add(TenantPrefix);
            keyParts.Add(tenantId);
        }

        keyParts.Add(TypePrefix);
        keyParts.Add(type.Name.ToLowerInvariant());
        keyParts.Add(baseKey);

        return string.Join(KeySeparator, keyParts);
    }

    public string GeneratePattern(string basePattern, string? tenantId = null)
    {
        var patternParts = new List<string> { GlobalPrefix };

        if (!string.IsNullOrEmpty(tenantId))
        {
            patternParts.Add(TenantPrefix);
            patternParts.Add(tenantId);
            patternParts.Add("*");  // Only add wildcard when there's a tenant
        }

        patternParts.Add(basePattern);

        return string.Join(KeySeparator, patternParts);
    }

    public string GenerateSearchKey(string query, Dictionary<string, string>? filters, IEnumerable<string>? collectionIds, int limit, int offset, string? tenantId = null)
    {
        var keyComponents = new List<string>
        {
            "search",
            HashString(query),
            HashFilters(filters),
            HashCollectionIds(collectionIds),
            limit.ToString(),
            offset.ToString()
        };

        var baseKey = string.Join(KeySeparator, keyComponents);
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    public string GenerateRAGKey(string query, string? context, IEnumerable<string>? collectionIds, string? promptTemplate, string? tenantId = null)
    {
        var keyComponents = new List<string>
        {
            "rag",
            HashString(query),
            HashString(context ?? ""),
            HashCollectionIds(collectionIds),
            HashString(promptTemplate ?? "default")
        };

        var baseKey = string.Join(KeySeparator, keyComponents);
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    public string GenerateDocumentKey(string documentId, string? tenantId = null)
    {
        var baseKey = $"document:metadata:{documentId}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    public string GenerateCollectionKey(string collectionId, string? tenantId = null)
    {
        var baseKey = $"collection:metadata:{collectionId}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for conversation context
    /// </summary>
    public string GenerateConversationKey(string conversationId, string? tenantId = null)
    {
        var baseKey = $"conversation:context:{conversationId}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for user preferences
    /// </summary>
    public string GenerateUserPreferencesKey(string userId, string? tenantId = null)
    {
        var baseKey = $"user:preferences:{userId}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for embedding vectors
    /// </summary>
    public string GenerateEmbeddingKey(string text, string model, string? tenantId = null)
    {
        var textHash = HashString(text);
        var baseKey = $"embedding:{model}:{textHash}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for analytics data
    /// </summary>
    public string GenerateAnalyticsKey(string metricType, DateTime date, string? tenantId = null)
    {
        var dateKey = date.ToString("yyyy-MM-dd");
        var baseKey = $"analytics:{metricType}:{dateKey}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for API rate limiting
    /// </summary>
    public string GenerateRateLimitKey(string operation, string identifier, string? tenantId = null)
    {
        var baseKey = $"ratelimit:{operation}:{identifier}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    /// <summary>
    /// Generates cache key for health check results
    /// </summary>
    public string GenerateHealthCheckKey(string serviceName, string? tenantId = null)
    {
        var baseKey = $"health:{serviceName}";
        return GenerateKey(baseKey, typeof(object), tenantId);
    }

    private string HashString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "empty";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    private string HashFilters(Dictionary<string, string>? filters)
    {
        if (filters == null || !filters.Any())
            return "nofilters";

        var sortedFilters = filters.OrderBy(f => f.Key).ToList();
        var filtersJson = JsonSerializer.Serialize(sortedFilters);
        return HashString(filtersJson);
    }

    private string HashCollectionIds(IEnumerable<string>? collectionIds)
    {
        if (collectionIds == null || !collectionIds.Any())
            return "allcollections";

        var sortedIds = collectionIds.OrderBy(x => x).ToList();
        var idsString = string.Join(",", sortedIds);
        return HashString(idsString);
    }

    /// <summary>
    /// Extracts tenant ID from a cache key
    /// </summary>
    public string? ExtractTenantId(string cacheKey)
    {
        var parts = cacheKey.Split(KeySeparator);
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == TenantPrefix && i + 1 < parts.Length)
            {
                return parts[i + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// Validates if a cache key follows the expected format
    /// </summary>
    public bool IsValidCacheKey(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
            return false;

        var parts = cacheKey.Split(KeySeparator);
        
        // Must start with global prefix
        if (parts.Length < 2 || parts[0] != GlobalPrefix)
            return false;

        // Must contain type prefix
        return parts.Contains(TypePrefix);
    }

    /// <summary>
    /// Gets cache key metadata
    /// </summary>
    public CacheKeyMetadata GetKeyMetadata(string cacheKey)
    {
        var parts = cacheKey.Split(KeySeparator);
        var metadata = new CacheKeyMetadata
        {
            OriginalKey = cacheKey,
            IsValid = IsValidCacheKey(cacheKey)
        };

        if (!metadata.IsValid)
            return metadata;

        metadata.TenantId = ExtractTenantId(cacheKey);
        
        // Extract type
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == TypePrefix && i + 1 < parts.Length)
            {
                metadata.DataType = parts[i + 1];
                break;
            }
        }

        return metadata;
    }
}

/// <summary>
/// Metadata about a cache key
/// </summary>
public class CacheKeyMetadata
{
    public string OriginalKey { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? TenantId { get; set; }
    public string? DataType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
