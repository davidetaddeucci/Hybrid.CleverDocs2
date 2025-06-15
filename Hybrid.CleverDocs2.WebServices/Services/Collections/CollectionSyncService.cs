using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// Service for synchronizing collections with R2R API
/// </summary>
public class CollectionSyncService : ICollectionSyncService
{
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<CollectionSyncService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly CollectionSyncOptions _options;

    // Mock R2R client - in real implementation this would be injected
    private readonly Dictionary<string, object> _mockR2RCollections = new();

    public CollectionSyncService(
        IRateLimitingService rateLimitingService,
        IMultiLevelCacheService cacheService,
        ILogger<CollectionSyncService> logger,
        ICorrelationService correlationService,
        IOptions<CollectionSyncOptions> options)
    {
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
    }

    public async Task<string?> CreateR2RCollectionAsync(UserCollectionDto collection)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Check rate limiting for collection operations
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("collection_operation");
            if (!canProceed)
            {
                _logger.LogWarning("Rate limit exceeded for collection creation, queuing for later processing, CollectionId: {CollectionId}, CorrelationId: {CorrelationId}", 
                    collection.Id, correlationId);
                
                await QueueSyncOperationAsync(new R2RCollectionSyncDto
                {
                    CollectionId = collection.Id,
                    Operation = "CREATE",
                    R2RData = CreateR2RCollectionData(collection),
                    CorrelationId = correlationId,
                    ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes)
                });
                
                return null;
            }

            _logger.LogInformation("Creating R2R collection for {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.Id, correlationId);

            // Simulate R2R API call
            var r2rCollectionData = CreateR2RCollectionData(collection);
            var r2rCollectionId = await SimulateR2RCreateCollectionAsync(r2rCollectionData);

            if (!string.IsNullOrEmpty(r2rCollectionId))
            {
                _logger.LogInformation("R2R collection created successfully: {R2RCollectionId} for {CollectionId}, CorrelationId: {CorrelationId}", 
                    r2rCollectionId, collection.Id, correlationId);

                // Cache the R2R collection mapping
                await _cacheService.SetAsync(
                    $"r2r:collection:mapping:{collection.Id}", 
                    r2rCollectionId, 
                    CacheOptions.ForCollectionData(collection.TenantId));

                return r2rCollectionId;
            }
            else
            {
                _logger.LogError("Failed to create R2R collection for {CollectionId}, CorrelationId: {CorrelationId}", 
                    collection.Id, correlationId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating R2R collection for {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.Id, correlationId);

            // Queue for retry
            await QueueSyncOperationAsync(new R2RCollectionSyncDto
            {
                CollectionId = collection.Id,
                Operation = "CREATE",
                R2RData = CreateR2RCollectionData(collection),
                CorrelationId = correlationId,
                ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes),
                RetryCount = 1,
                ErrorMessage = ex.Message
            });

            return null;
        }
    }

    public async Task<bool> UpdateR2RCollectionAsync(UserCollectionDto collection)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            if (string.IsNullOrEmpty(collection.R2RCollectionId))
            {
                _logger.LogWarning("Collection {CollectionId} has no R2R ID, creating instead of updating, CorrelationId: {CorrelationId}", 
                    collection.Id, correlationId);
                
                var r2rId = await CreateR2RCollectionAsync(collection);
                return !string.IsNullOrEmpty(r2rId);
            }

            // Check rate limiting
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("collection_operation");
            if (!canProceed)
            {
                _logger.LogWarning("Rate limit exceeded for collection update, queuing for later processing, CollectionId: {CollectionId}, CorrelationId: {CorrelationId}", 
                    collection.Id, correlationId);
                
                await QueueSyncOperationAsync(new R2RCollectionSyncDto
                {
                    CollectionId = collection.Id,
                    R2RCollectionId = collection.R2RCollectionId,
                    Operation = "UPDATE",
                    R2RData = CreateR2RCollectionData(collection),
                    CorrelationId = correlationId,
                    ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes)
                });
                
                return false;
            }

            _logger.LogInformation("Updating R2R collection {R2RCollectionId} for {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.R2RCollectionId, collection.Id, correlationId);

            // Simulate R2R API call
            var r2rCollectionData = CreateR2RCollectionData(collection);
            var success = await SimulateR2RUpdateCollectionAsync(collection.R2RCollectionId, r2rCollectionData);

            if (success)
            {
                _logger.LogInformation("R2R collection updated successfully: {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                    collection.R2RCollectionId, correlationId);

                // Invalidate cache
                await _cacheService.InvalidateAsync($"r2r:collection:mapping:{collection.Id}");
            }
            else
            {
                _logger.LogError("Failed to update R2R collection {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                    collection.R2RCollectionId, correlationId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating R2R collection {R2RCollectionId} for {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.R2RCollectionId, collection.Id, correlationId);

            // Queue for retry
            await QueueSyncOperationAsync(new R2RCollectionSyncDto
            {
                CollectionId = collection.Id,
                R2RCollectionId = collection.R2RCollectionId,
                Operation = "UPDATE",
                R2RData = CreateR2RCollectionData(collection),
                CorrelationId = correlationId,
                ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes),
                RetryCount = 1,
                ErrorMessage = ex.Message
            });

            return false;
        }
    }

    public async Task<bool> DeleteR2RCollectionAsync(string r2rCollectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Check rate limiting
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("collection_operation");
            if (!canProceed)
            {
                _logger.LogWarning("Rate limit exceeded for collection deletion, queuing for later processing, R2RCollectionId: {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                    r2rCollectionId, correlationId);
                
                await QueueSyncOperationAsync(new R2RCollectionSyncDto
                {
                    R2RCollectionId = r2rCollectionId,
                    Operation = "DELETE",
                    CorrelationId = correlationId,
                    ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes)
                });
                
                return false;
            }

            _logger.LogInformation("Deleting R2R collection {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                r2rCollectionId, correlationId);

            // Simulate R2R API call
            var success = await SimulateR2RDeleteCollectionAsync(r2rCollectionId);

            if (success)
            {
                _logger.LogInformation("R2R collection deleted successfully: {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                    r2rCollectionId, correlationId);
            }
            else
            {
                _logger.LogError("Failed to delete R2R collection {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                    r2rCollectionId, correlationId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting R2R collection {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                r2rCollectionId, correlationId);

            // Queue for retry
            await QueueSyncOperationAsync(new R2RCollectionSyncDto
            {
                R2RCollectionId = r2rCollectionId,
                Operation = "DELETE",
                CorrelationId = correlationId,
                ScheduledAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes),
                RetryCount = 1,
                ErrorMessage = ex.Message
            });

            return false;
        }
    }

    public async Task<bool> SyncCollectionAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogInformation("Synchronizing collection {CollectionId} with R2R, CorrelationId: {CorrelationId}", 
                collectionId, correlationId);

            // Get collection from cache or database
            var collection = await GetCollectionForSyncAsync(collectionId);
            if (collection == null)
            {
                _logger.LogWarning("Collection {CollectionId} not found for sync, CorrelationId: {CorrelationId}", 
                    collectionId, correlationId);
                return false;
            }

            // Determine sync operation needed
            if (string.IsNullOrEmpty(collection.R2RCollectionId))
            {
                var r2rId = await CreateR2RCollectionAsync(collection);
                return !string.IsNullOrEmpty(r2rId);
            }
            else
            {
                return await UpdateR2RCollectionAsync(collection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing collection {CollectionId}, CorrelationId: {CorrelationId}", 
                collectionId, correlationId);
            return false;
        }
    }

    public async Task<R2RCollectionSyncDto> GetSyncStatusAsync(Guid collectionId)
    {
        var cacheKey = $"r2r:sync:status:{collectionId}";
        
        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            // Mock implementation - in real app this would check sync queue and status
            return new R2RCollectionSyncDto
            {
                CollectionId = collectionId,
                Operation = "SYNCED",
                CorrelationId = _correlationService.GetCorrelationId(),
                ScheduledAt = DateTime.UtcNow,
                RetryCount = 0
            };
        }, new CacheOptions { L1TTL = TimeSpan.FromMinutes(5) }) ?? new R2RCollectionSyncDto();
    }

    public async Task<bool> RetrySyncAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        _logger.LogInformation("Retrying sync for collection {CollectionId}, CorrelationId: {CorrelationId}", 
            collectionId, correlationId);

        return await SyncCollectionAsync(collectionId);
    }

    public async Task ProcessSyncQueueAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();
        
        try
        {
            _logger.LogDebug("Processing R2R collection sync queue, CorrelationId: {CorrelationId}", correlationId);

            // Mock implementation - in real app this would process queued sync operations
            await Task.Delay(100);

            _logger.LogDebug("R2R collection sync queue processing completed, CorrelationId: {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing R2R collection sync queue, CorrelationId: {CorrelationId}", correlationId);
        }
    }

    // Helper methods
    private Dictionary<string, object> CreateR2RCollectionData(UserCollectionDto collection)
    {
        return new Dictionary<string, object>
        {
            ["name"] = collection.Name,
            ["description"] = collection.Description ?? "",
            ["metadata"] = new Dictionary<string, object>
            {
                ["internal_id"] = collection.Id.ToString(),
                ["created_by"] = collection.CreatedBy,
                ["tenant_id"] = collection.TenantId ?? "",
                ["color"] = collection.Color,
                ["icon"] = collection.Icon,
                ["tags"] = collection.Tags,
                ["is_favorite"] = collection.IsFavorite,
                ["created_at"] = collection.CreatedAt.ToString("O"),
                ["updated_at"] = collection.UpdatedAt.ToString("O")
            }
        };
    }

    private async Task<string> SimulateR2RCreateCollectionAsync(Dictionary<string, object> collectionData)
    {
        // Simulate API delay
        await Task.Delay(Random.Shared.Next(100, 500));

        // Simulate success/failure
        if (Random.Shared.NextDouble() > 0.1) // 90% success rate
        {
            var r2rId = $"r2r_collection_{Guid.NewGuid():N}";
            _mockR2RCollections[r2rId] = collectionData;
            return r2rId;
        }

        return string.Empty;
    }

    private async Task<bool> SimulateR2RUpdateCollectionAsync(string r2rCollectionId, Dictionary<string, object> collectionData)
    {
        // Simulate API delay
        await Task.Delay(Random.Shared.Next(50, 300));

        // Simulate success/failure
        if (Random.Shared.NextDouble() > 0.05) // 95% success rate
        {
            _mockR2RCollections[r2rCollectionId] = collectionData;
            return true;
        }

        return false;
    }

    private async Task<bool> SimulateR2RDeleteCollectionAsync(string r2rCollectionId)
    {
        // Simulate API delay
        await Task.Delay(Random.Shared.Next(50, 200));

        // Simulate success/failure
        if (Random.Shared.NextDouble() > 0.05) // 95% success rate
        {
            _mockR2RCollections.Remove(r2rCollectionId);
            return true;
        }

        return false;
    }

    private async Task<UserCollectionDto?> GetCollectionForSyncAsync(Guid collectionId)
    {
        // Mock implementation - in real app this would get from database
        await Task.Delay(10);
        return null;
    }

    private async Task QueueSyncOperationAsync(R2RCollectionSyncDto syncOperation)
    {
        // Mock implementation - in real app this would queue the operation for later processing
        var cacheKey = $"r2r:sync:queue:{syncOperation.CollectionId}";
        await _cacheService.SetAsync(cacheKey, syncOperation, new CacheOptions 
        { 
            L2TTL = TimeSpan.FromHours(24),
            UseL3Cache = true 
        });

        _logger.LogInformation("Queued R2R sync operation for collection {CollectionId}, Operation: {Operation}, CorrelationId: {CorrelationId}", 
            syncOperation.CollectionId, syncOperation.Operation, syncOperation.CorrelationId);
    }
}

/// <summary>
/// Configuration options for collection sync service
/// </summary>
public class CollectionSyncOptions
{
    public bool EnableSync { get; set; } = true;
    public int RetryDelayMinutes { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool EnableBatchSync { get; set; } = true;
    public int BatchSize { get; set; } = 10;
}
