using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Models.Queue;

namespace Hybrid.CleverDocs2.WebServices.Services.Cache;

/// <summary>
/// Interface for smart cache invalidation service
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates all caches related to a document
    /// </summary>
    Task InvalidateDocumentCacheAsync(string documentId, string tenantId);

    /// <summary>
    /// Invalidates all caches related to a collection
    /// </summary>
    Task InvalidateCollectionCacheAsync(string collectionId, string tenantId);

    /// <summary>
    /// Invalidates all caches related to a conversation
    /// </summary>
    Task InvalidateConversationCacheAsync(string conversationId, string tenantId);

    /// <summary>
    /// Invalidates all caches for a tenant
    /// </summary>
    Task InvalidateTenantCacheAsync(string tenantId);

    /// <summary>
    /// Invalidates search caches based on query patterns
    /// </summary>
    Task InvalidateSearchCacheAsync(string? query = null, string? tenantId = null);

    /// <summary>
    /// Invalidates RAG caches based on context changes
    /// </summary>
    Task InvalidateRAGCacheAsync(string? context = null, string? tenantId = null);

    /// <summary>
    /// Schedules cache invalidation for later execution
    /// </summary>
    Task ScheduleInvalidationAsync(CacheInvalidationRequest request);
}

/// <summary>
/// Smart cache invalidation service with dependency tracking
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly CacheInvalidationOptions _options;

    public CacheInvalidationService(
        IMultiLevelCacheService cacheService,
        IRateLimitingService rateLimitingService,
        ILogger<CacheInvalidationService> logger,
        ICorrelationService correlationService,
        IOptions<CacheInvalidationOptions> options)
    {
        _cacheService = cacheService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
    }

    public async Task InvalidateDocumentCacheAsync(string documentId, string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Starting document cache invalidation for DocumentId: {DocumentId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                documentId, tenantId, correlationId);

            var invalidationTasks = new List<Task>
            {
                // Document-specific caches
                _cacheService.InvalidateAsync($"*document*{documentId}*", tenantId),
                
                // Search caches that might include this document
                _cacheService.InvalidateAsync("*search*", tenantId),
                
                // RAG caches that might use this document
                _cacheService.InvalidateAsync("*rag*", tenantId),
                
                // Embedding caches for this document
                _cacheService.InvalidateAsync($"*embedding*{documentId}*", tenantId),
                
                // Analytics caches that might include this document
                _cacheService.InvalidateAsync("*analytics*", tenantId)
            };

            // Invalidate collection caches if document belongs to collections
            var collectionInvalidationTask = InvalidateCollectionCachesForDocumentAsync(documentId, tenantId);
            invalidationTasks.Add(collectionInvalidationTask);

            await Task.WhenAll(invalidationTasks);

            // Publish invalidation event for other services
            await PublishInvalidationEventAsync(new CacheInvalidationEvent
            {
                Type = CacheInvalidationType.Document,
                EntityId = documentId,
                TenantId = tenantId,
                InvalidatedAt = DateTime.UtcNow,
                Reason = "DocumentUpdated",
                CorrelationId = correlationId
            });

            _logger.LogInformation("Document cache invalidation completed for DocumentId: {DocumentId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                documentId, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating document cache for DocumentId: {DocumentId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                documentId, tenantId, correlationId);
            throw;
        }
    }

    public async Task InvalidateCollectionCacheAsync(string collectionId, string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Starting collection cache invalidation for CollectionId: {CollectionId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                collectionId, tenantId, correlationId);

            await Task.WhenAll(
                _cacheService.InvalidateAsync($"*collection*{collectionId}*", tenantId),
                _cacheService.InvalidateAsync($"*search*collection*{collectionId}*", tenantId),
                _cacheService.InvalidateAsync($"*rag*collection*{collectionId}*", tenantId),
                _cacheService.InvalidateAsync("*analytics*", tenantId) // Analytics might aggregate collection data
            );

            await PublishInvalidationEventAsync(new CacheInvalidationEvent
            {
                Type = CacheInvalidationType.Collection,
                EntityId = collectionId,
                TenantId = tenantId,
                InvalidatedAt = DateTime.UtcNow,
                Reason = "CollectionUpdated",
                CorrelationId = correlationId
            });

            _logger.LogInformation("Collection cache invalidation completed for CollectionId: {CollectionId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                collectionId, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating collection cache for CollectionId: {CollectionId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                collectionId, tenantId, correlationId);
            throw;
        }
    }

    public async Task InvalidateConversationCacheAsync(string conversationId, string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            await Task.WhenAll(
                _cacheService.InvalidateAsync($"*conversation*{conversationId}*", tenantId),
                _cacheService.InvalidateAsync($"*rag*conversation*{conversationId}*", tenantId)
            );

            await PublishInvalidationEventAsync(new CacheInvalidationEvent
            {
                Type = CacheInvalidationType.Conversation,
                EntityId = conversationId,
                TenantId = tenantId,
                InvalidatedAt = DateTime.UtcNow,
                Reason = "ConversationUpdated",
                CorrelationId = correlationId
            });

            _logger.LogInformation("Conversation cache invalidation completed for ConversationId: {ConversationId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                conversationId, tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating conversation cache for ConversationId: {ConversationId}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                conversationId, tenantId, correlationId);
            throw;
        }
    }

    public async Task InvalidateTenantCacheAsync(string tenantId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Starting tenant cache invalidation for TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);

            // Invalidate all caches for this tenant
            await _cacheService.InvalidateAsync("*", tenantId);

            await PublishInvalidationEventAsync(new CacheInvalidationEvent
            {
                Type = CacheInvalidationType.Tenant,
                EntityId = tenantId,
                TenantId = tenantId,
                InvalidatedAt = DateTime.UtcNow,
                Reason = "TenantDataUpdated",
                CorrelationId = correlationId
            });

            _logger.LogInformation("Tenant cache invalidation completed for TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tenant cache for TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                tenantId, correlationId);
            throw;
        }
    }

    public async Task InvalidateSearchCacheAsync(string? query = null, string? tenantId = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            var pattern = string.IsNullOrEmpty(query) ? "*search*" : $"*search*{query}*";
            await _cacheService.InvalidateAsync(pattern, tenantId);

            _logger.LogInformation("Search cache invalidation completed for Query: {Query}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                query ?? "ALL", tenantId ?? "ALL", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating search cache for Query: {Query}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                query, tenantId, correlationId);
            throw;
        }
    }

    public async Task InvalidateRAGCacheAsync(string? context = null, string? tenantId = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            var pattern = string.IsNullOrEmpty(context) ? "*rag*" : $"*rag*{context}*";
            await _cacheService.InvalidateAsync(pattern, tenantId);

            _logger.LogInformation("RAG cache invalidation completed for Context: {Context}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                context ?? "ALL", tenantId ?? "ALL", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating RAG cache for Context: {Context}, TenantId: {TenantId}, CorrelationId: {CorrelationId}", 
                context, tenantId, correlationId);
            throw;
        }
    }

    public async Task ScheduleInvalidationAsync(CacheInvalidationRequest request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            // Check rate limiting for invalidation operations
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("cache_invalidation");
            if (!canProceed)
            {
                _logger.LogWarning("Cache invalidation rate limit exceeded, scheduling for later, CorrelationId: {CorrelationId}", correlationId);
                
                // In a full implementation, this would queue the request for later processing
                await Task.Delay(_options.RateLimitDelayMs);
            }

            // Execute the invalidation based on request type
            switch (request.Type)
            {
                case CacheInvalidationType.Document:
                    await InvalidateDocumentCacheAsync(request.EntityId, request.TenantId);
                    break;
                case CacheInvalidationType.Collection:
                    await InvalidateCollectionCacheAsync(request.EntityId, request.TenantId);
                    break;
                case CacheInvalidationType.Conversation:
                    await InvalidateConversationCacheAsync(request.EntityId, request.TenantId);
                    break;
                case CacheInvalidationType.Tenant:
                    await InvalidateTenantCacheAsync(request.TenantId);
                    break;
                default:
                    _logger.LogWarning("Unknown cache invalidation type: {Type}, CorrelationId: {CorrelationId}", 
                        request.Type, correlationId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling cache invalidation, CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
    }

    private async Task InvalidateCollectionCachesForDocumentAsync(string documentId, string tenantId)
    {
        try
        {
            // In a full implementation, this would query the database to find collections
            // that contain this document and invalidate their caches
            
            // For now, we'll invalidate all collection caches as a safe approach
            await _cacheService.InvalidateAsync("*collection*", tenantId);
            
            _logger.LogDebug("Invalidated collection caches for document {DocumentId} in tenant {TenantId}", 
                documentId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating collection caches for document {DocumentId} in tenant {TenantId}", 
                documentId, tenantId);
        }
    }

    private async Task PublishInvalidationEventAsync(CacheInvalidationEvent invalidationEvent)
    {
        try
        {
            // In a full implementation, this would publish to RabbitMQ
            // For now, we'll just log the event
            
            _logger.LogInformation("Cache invalidation event: {Type} - {EntityId} in tenant {TenantId}, Reason: {Reason}, CorrelationId: {CorrelationId}",
                invalidationEvent.Type, invalidationEvent.EntityId, invalidationEvent.TenantId, 
                invalidationEvent.Reason, invalidationEvent.CorrelationId);
                
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing cache invalidation event, CorrelationId: {CorrelationId}", 
                invalidationEvent.CorrelationId);
        }
    }
}

/// <summary>
/// Cache invalidation request
/// </summary>
public class CacheInvalidationRequest
{
    public CacheInvalidationType Type { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Cache invalidation event for messaging
/// </summary>
public class CacheInvalidationEvent : R2RJobMessage
{
    public CacheInvalidationType Type { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime InvalidatedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Types of cache invalidation
/// </summary>
public enum CacheInvalidationType
{
    Document,
    Collection,
    Conversation,
    Tenant,
    Search,
    RAG,
    Analytics,
    User
}

/// <summary>
/// Configuration options for cache invalidation service
/// </summary>
public class CacheInvalidationOptions
{
    public bool EnableEventPublishing { get; set; } = true;
    public int RateLimitDelayMs { get; set; } = 1000;
    public int MaxConcurrentInvalidations { get; set; } = 10;
    public TimeSpan InvalidationTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
