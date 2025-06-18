using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Interface for R2R-specific cache service
/// </summary>
public interface IR2RCacheService
{
    /// <summary>
    /// Gets search results from cache or executes search factory
    /// </summary>
    Task<SearchResultDto> GetSearchResultsAsync(SearchRequestDto request, Func<Task<SearchResultDto>> searchFactory);

    /// <summary>
    /// Gets RAG response from cache or executes RAG factory
    /// </summary>
    Task<RAGResponseDto> GetRAGResponseAsync(RAGRequestDto request, Func<Task<RAGResponseDto>> ragFactory);

    /// <summary>
    /// Gets document metadata from cache or executes metadata factory
    /// </summary>
    Task<DocumentMetadataDto> GetDocumentMetadataAsync(string documentId, Func<Task<DocumentMetadataDto>> metadataFactory);

    /// <summary>
    /// Gets collection metadata from cache or executes collection factory
    /// </summary>
    Task<CollectionMetadataDto> GetCollectionMetadataAsync(string collectionId, Func<Task<CollectionMetadataDto>> collectionFactory);

    /// <summary>
    /// Gets conversation context from cache or executes context factory
    /// </summary>
    Task<ConversationContextDto> GetConversationContextAsync(string conversationId, Func<Task<ConversationContextDto>> contextFactory);

    /// <summary>
    /// Gets embedding vector from cache or executes embedding factory
    /// </summary>
    Task<EmbeddingVectorDto> GetEmbeddingVectorAsync(string text, string model, Func<Task<EmbeddingVectorDto>> embeddingFactory);

    /// <summary>
    /// Invalidates all R2R caches for a tenant
    /// </summary>
    Task InvalidateAllR2RCacheAsync(string tenantId);
}

/// <summary>
/// R2R-specific cache service with optimized strategies for different operation types
/// </summary>
public class R2RCacheService : IR2RCacheService
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<R2RCacheService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly R2RCacheOptions _options;

    public R2RCacheService(
        IMultiLevelCacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<R2RCacheService> logger,
        ICorrelationService correlationService,
        IOptions<R2RCacheOptions> options)
    {
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
    }

    public async Task<SearchResultDto> GetSearchResultsAsync(SearchRequestDto request, Func<Task<SearchResultDto>> searchFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = _keyGenerator.GenerateSearchKey(
            request.Query,
            request.Filters,
            request.CollectionIds,
            request.Limit,
            request.Offset,
            request.TenantId);

        var options = CacheOptions.ForSearch(request.TenantId);
        
        _logger.LogDebug("Getting search results from cache, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, searchFactory, options) ?? new SearchResultDto();
    }

    public async Task<RAGResponseDto> GetRAGResponseAsync(RAGRequestDto request, Func<Task<RAGResponseDto>> ragFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = _keyGenerator.GenerateRAGKey(
            request.Query,
            request.Context,
            request.CollectionIds,
            request.PromptTemplate,
            request.TenantId);

        var options = CacheOptions.ForRAG(request.TenantId);
        
        _logger.LogDebug("Getting RAG response from cache, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, ragFactory, options) ?? new RAGResponseDto();
    }

    public async Task<DocumentMetadataDto> GetDocumentMetadataAsync(string documentId, Func<Task<DocumentMetadataDto>> metadataFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var tenantId = ExtractTenantFromDocumentId(documentId);
        var cacheKey = _keyGenerator.GenerateDocumentKey(documentId, tenantId);

        var options = CacheOptions.ForDocumentMetadata(tenantId);
        
        _logger.LogDebug("Getting document metadata from cache, DocumentId: {DocumentId}, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            documentId, cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, metadataFactory, options) ?? new DocumentMetadataDto();
    }

    public async Task<CollectionMetadataDto> GetCollectionMetadataAsync(string collectionId, Func<Task<CollectionMetadataDto>> collectionFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var tenantId = ExtractTenantFromCollectionId(collectionId);
        var cacheKey = _keyGenerator.GenerateCollectionKey(collectionId, tenantId);

        var options = CacheOptions.ForExpensiveData(tenantId); // Collection metadata is expensive to fetch
        
        _logger.LogDebug("Getting collection metadata from cache, CollectionId: {CollectionId}, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            collectionId, cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, collectionFactory, options) ?? new CollectionMetadataDto();
    }

    public async Task<ConversationContextDto> GetConversationContextAsync(string conversationId, Func<Task<ConversationContextDto>> contextFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var tenantId = ExtractTenantFromConversationId(conversationId);
        var cacheKey = _keyGenerator.GenerateConversationKey(conversationId, tenantId);

        var options = new CacheOptions
        {
            UseL1Cache = true,  // ✅ ENABLED - Fast access for active conversations
            UseL2Cache = true,  // ✅ ENABLED - Redis for intensive chat sessions
            UseL3Cache = false, // ❌ DISABLED - Conversation context changes frequently
            L1TTL = TimeSpan.FromMinutes(10),
            L2TTL = TimeSpan.FromMinutes(30),
            L3TTL = TimeSpan.FromHours(2),
            TenantId = tenantId,
            Priority = CachePriority.High
        };
        
        _logger.LogDebug("Getting conversation context from cache, ConversationId: {ConversationId}, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            conversationId, cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, contextFactory, options) ?? new ConversationContextDto();
    }

    public async Task<EmbeddingVectorDto> GetEmbeddingVectorAsync(string text, string model, Func<Task<EmbeddingVectorDto>> embeddingFactory)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = _keyGenerator.GenerateEmbeddingKey(text, model, null); // Embeddings are not tenant-specific

        var options = CacheOptions.ForExpensiveData(null); // Use optimized config for expensive computations
        
        _logger.LogDebug("Getting embedding vector from cache, Model: {Model}, Key: {CacheKey}, CorrelationId: {CorrelationId}", 
            model, cacheKey, correlationId);

        return await _cacheService.GetAsync(cacheKey, embeddingFactory, options) ?? new EmbeddingVectorDto();
    }

    public async Task InvalidateAllR2RCacheAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Starting R2R cache invalidation for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            var invalidationTasks = new[]
            {
                _cacheService.InvalidateAsync("*search*", tenantId),
                _cacheService.InvalidateAsync("*rag*", tenantId),
                _cacheService.InvalidateAsync("*document*", tenantId),
                _cacheService.InvalidateAsync("*collection*", tenantId),
                _cacheService.InvalidateAsync("*conversation*", tenantId),
                _cacheService.InvalidateAsync("*analytics*", tenantId)
            };

            await Task.WhenAll(invalidationTasks);

            _logger.LogInformation("R2R cache invalidation completed for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating R2R cache for tenant {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
            throw;
        }
    }

    private string? ExtractTenantFromDocumentId(string documentId)
    {
        // Implementation would extract tenant ID from document ID format
        // For now, return null to use global cache
        return null;
    }

    private string? ExtractTenantFromCollectionId(string collectionId)
    {
        // Implementation would extract tenant ID from collection ID format
        // For now, return null to use global cache
        return null;
    }

    private string? ExtractTenantFromConversationId(string conversationId)
    {
        // Implementation would extract tenant ID from conversation ID format
        // For now, return null to use global cache
        return null;
    }
}

// DTOs for R2R operations
public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, string>? Filters { get; set; }
    public IEnumerable<string>? CollectionIds { get; set; }
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
    public string? TenantId { get; set; }
}

public class SearchResultDto
{
    public string Query { get; set; } = string.Empty;
    public List<SearchHitDto> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SearchHitDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RAGRequestDto
{
    public string Query { get; set; } = string.Empty;
    public string? Context { get; set; }
    public IEnumerable<string>? CollectionIds { get; set; }
    public string? PromptTemplate { get; set; }
    public string? TenantId { get; set; }
}

public class RAGResponseDto
{
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<string> Sources { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DocumentMetadataDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CollectionMetadataDto
{
    public string CollectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConversationContextDto
{
    public string ConversationId { get; set; } = string.Empty;
    public List<ConversationMessageDto> Messages { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class ConversationMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class EmbeddingVectorDto
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public int Dimensions { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration options for R2R cache service
/// </summary>
public class R2RCacheOptions
{
    public bool EnableSearchCaching { get; set; } = true;
    public bool EnableRAGCaching { get; set; } = true;
    public bool EnableDocumentCaching { get; set; } = true;
    public bool EnableCollectionCaching { get; set; } = true;
    public bool EnableConversationCaching { get; set; } = true;
    public bool EnableEmbeddingCaching { get; set; } = true;
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxCacheKeyLength { get; set; } = 250;
}
