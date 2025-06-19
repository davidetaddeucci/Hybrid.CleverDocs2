using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Collection;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// Service for bidirectional synchronization between Hybrid Collections and R2R Collections
/// Enhanced with Quick Wins and real R2R integration
/// </summary>
public class CollectionSyncService : ICollectionSyncService
{
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<CollectionSyncService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly CollectionSyncOptions _options;

    // ✅ COLLECTIONS SYNC - Real R2R clients
    private readonly ApplicationDbContext _context;
    private readonly ICollectionClient _r2rCollectionClient;
    private readonly IDocumentClient _r2rDocumentClient;

    public CollectionSyncService(
        IRateLimitingService rateLimitingService,
        IMultiLevelCacheService cacheService,
        ILogger<CollectionSyncService> logger,
        ICorrelationService correlationService,
        IOptions<CollectionSyncOptions> options,
        ApplicationDbContext context,
        ICollectionClient r2rCollectionClient,
        IDocumentClient r2rDocumentClient)
    {
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
        _context = context;
        _r2rCollectionClient = r2rCollectionClient;
        _r2rDocumentClient = r2rDocumentClient;
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

            // Real R2R API call
            var r2rRequest = new CollectionRequest
            {
                Name = collection.Name,
                Description = collection.Description ?? "",
                Metadata = CreateR2RCollectionData(collection)
            };

            var r2rResponse = await _r2rCollectionClient.CreateCollectionAsync(r2rRequest);
            var r2rCollectionId = r2rResponse?.Results?.CollectionId;

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

            // Real R2R API call
            var r2rRequest = new CollectionUpdateRequest
            {
                Name = collection.Name,
                Description = collection.Description ?? "",
                Metadata = CreateR2RCollectionData(collection)
            };

            var r2rResponse = await _r2rCollectionClient.UpdateCollectionAsync(collection.R2RCollectionId, r2rRequest);
            var success = r2rResponse != null;

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

            // Real R2R API call
            await _r2rCollectionClient.DeleteCollectionAsync(r2rCollectionId);
            var success = true; // DeleteCollectionAsync doesn't return a value, assume success if no exception

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

    // ✅ COLLECTIONS SYNC - New bidirectional sync methods
    public async Task<bool> SyncCollectionWithR2RAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting bidirectional sync for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);

            var localCollection = await _context.Collections
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (localCollection == null)
            {
                _logger.LogWarning("Local collection {CollectionId} not found, CorrelationId: {CorrelationId}",
                    collectionId, correlationId);
                return false;
            }

            // If no R2R collection exists, create one
            if (string.IsNullOrEmpty(localCollection.R2RCollectionId))
            {
                var r2rCollectionId = await CreateR2RCollectionAsync(collectionId);
                if (string.IsNullOrEmpty(r2rCollectionId))
                {
                    return false;
                }
                localCollection.R2RCollectionId = r2rCollectionId;
                await _context.SaveChangesAsync();
            }

            // Perform bidirectional sync
            var pullSuccess = await PullFromR2RAsync(collectionId);
            var pushSuccess = await PushToR2RAsync(collectionId);

            // Update sync timestamp
            localCollection.LastSyncedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateCollectionCacheAsync(collectionId, localCollection.CompanyId);

            _logger.LogInformation("Collection sync completed for {CollectionId}, Pull: {PullSuccess}, Push: {PushSuccess}, CorrelationId: {CorrelationId}",
                collectionId, pullSuccess, pushSuccess, correlationId);

            return pullSuccess && pushSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing collection {CollectionId} with R2R, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return false;
        }
    }

    public async Task<string?> CreateR2RCollectionAsync(Guid localCollectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var localCollection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == localCollectionId);

            if (localCollection == null)
            {
                _logger.LogWarning("Local collection {CollectionId} not found for R2R creation, CorrelationId: {CorrelationId}",
                    localCollectionId, correlationId);
                return null;
            }

            // 🔄 BIDIRECTIONAL SYNC: First check if collection already exists in R2R
            var existingR2RCollectionId = await FindExistingR2RCollectionAsync(localCollection.Name);
            if (!string.IsNullOrEmpty(existingR2RCollectionId))
            {
                _logger.LogInformation("Found existing R2R collection {R2RCollectionId} for local collection {LocalCollectionId}, CorrelationId: {CorrelationId}",
                    existingR2RCollectionId, localCollectionId, correlationId);
                return existingR2RCollectionId;
            }

            // Create R2R collection request with enhanced metadata
            var r2rRequest = new CollectionRequest
            {
                Name = localCollection.Name,
                Description = localCollection.Description ?? "",
                Metadata = new Dictionary<string, object>
                {
                    ["hybrid_collection_id"] = localCollectionId.ToString(),
                    ["created_by"] = localCollection.CreatedBy ?? localCollection.UserId.ToString(),
                    ["company_id"] = localCollection.CompanyId.ToString(),
                    ["tags"] = localCollection.Tags ?? new List<string>(),
                    ["color"] = localCollection.Color ?? "",
                    ["icon"] = localCollection.Icon ?? "",
                    ["sync_timestamp"] = DateTime.UtcNow,
                    ["correlation_id"] = correlationId
                }
            };

            var r2rCollectionResponse = await _r2rCollectionClient.CreateCollectionAsync(r2rRequest);
            var r2rCollection = r2rCollectionResponse?.Results;

            if (r2rCollection != null && !string.IsNullOrEmpty(r2rCollection.CollectionId))
            {
                _logger.LogInformation("Created R2R collection {R2RCollectionId} for local collection {LocalCollectionId}, CorrelationId: {CorrelationId}",
                    r2rCollection.CollectionId, localCollectionId, correlationId);
                return r2rCollection.CollectionId;
            }

            _logger.LogError("Failed to create R2R collection for local collection {LocalCollectionId}, CorrelationId: {CorrelationId}",
                localCollectionId, correlationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating R2R collection for local collection {LocalCollectionId}, CorrelationId: {CorrelationId}",
                localCollectionId, correlationId);
            return null;
        }
    }

    public async Task<int> SyncAllUserCollectionsAsync(string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting sync for all collections of user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogError("Invalid user ID format: {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
                return 0;
            }

            var userCollections = await _context.Collections
                .Where(c => c.UserId == userGuid)
                .ToListAsync();

            var syncedCount = 0;
            foreach (var collection in userCollections)
            {
                var success = await SyncCollectionWithR2RAsync(collection.Id);
                if (success)
                {
                    syncedCount++;
                }
            }

            _logger.LogInformation("Synced {SyncedCount}/{TotalCount} collections for user {UserId}, CorrelationId: {CorrelationId}",
                syncedCount, userCollections.Count, userId, correlationId);

            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
            return 0;
        }
    }

    public async Task<bool> PullFromR2RAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var localCollection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (localCollection == null || string.IsNullOrEmpty(localCollection.R2RCollectionId))
            {
                _logger.LogWarning("Cannot pull from R2R - collection {CollectionId} not found or not linked, CorrelationId: {CorrelationId}",
                    collectionId, correlationId);
                return false;
            }

            // Get R2R collection data
            var r2rCollection = await _r2rCollectionClient.GetCollectionAsync(localCollection.R2RCollectionId);
            if (r2rCollection == null)
            {
                _logger.LogWarning("R2R collection {R2RCollectionId} not found, CorrelationId: {CorrelationId}",
                    localCollection.R2RCollectionId, correlationId);
                return false;
            }

            // Update local collection with R2R data
            var hasChanges = false;

            if (localCollection.Name != r2rCollection.Name)
            {
                localCollection.Name = r2rCollection.Name;
                hasChanges = true;
            }

            if (localCollection.Description != r2rCollection.Description)
            {
                localCollection.Description = r2rCollection.Description;
                hasChanges = true;
            }

            if (hasChanges)
            {
                localCollection.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Pulled updates from R2R for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling from R2R for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return false;
        }
    }

    public async Task<bool> PushToR2RAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var localCollection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (localCollection == null || string.IsNullOrEmpty(localCollection.R2RCollectionId))
            {
                _logger.LogWarning("Cannot push to R2R - collection {CollectionId} not found or not linked, CorrelationId: {CorrelationId}",
                    collectionId, correlationId);
                return false;
            }

            // Update R2R collection with local data
            var updateRequest = new CollectionUpdateRequest
            {
                Name = localCollection.Name,
                Description = localCollection.Description ?? "",
                Metadata = new Dictionary<string, object>
                {
                    ["hybrid_collection_id"] = collectionId.ToString(),
                    ["last_updated"] = localCollection.UpdatedAt,
                    ["sync_timestamp"] = DateTime.UtcNow,
                    ["correlation_id"] = correlationId
                }
            };

            var updateResponse = await _r2rCollectionClient.UpdateCollectionAsync(localCollection.R2RCollectionId, updateRequest);
            var success = updateResponse != null;

            _logger.LogInformation("Pushed updates to R2R for collection {CollectionId}, Success: {Success}, CorrelationId: {CorrelationId}",
                collectionId, success, correlationId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to R2R for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return false;
        }
    }

    public async Task<CollectionSyncStatusDto> GetDetailedSyncStatusAsync(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var localCollection = await _context.Collections
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (localCollection == null)
            {
                return new CollectionSyncStatusDto
                {
                    CollectionId = collectionId,
                    Status = SyncStatus.NotFound,
                    Message = "Local collection not found"
                };
            }

            if (string.IsNullOrEmpty(localCollection.R2RCollectionId))
            {
                return new CollectionSyncStatusDto
                {
                    CollectionId = collectionId,
                    Status = SyncStatus.NotSynced,
                    Message = "No R2R collection linked",
                    LocalDocumentCount = localCollection.Documents?.Count ?? 0
                };
            }

            // Check R2R collection status
            var r2rCollection = await _r2rCollectionClient.GetCollectionAsync(localCollection.R2RCollectionId);
            if (r2rCollection == null)
            {
                return new CollectionSyncStatusDto
                {
                    CollectionId = collectionId,
                    R2RCollectionId = localCollection.R2RCollectionId,
                    Status = SyncStatus.R2RNotFound,
                    Message = "R2R collection not found",
                    LocalDocumentCount = localCollection.Documents?.Count ?? 0
                };
            }

            return new CollectionSyncStatusDto
            {
                CollectionId = collectionId,
                R2RCollectionId = localCollection.R2RCollectionId,
                Status = SyncStatus.Synced,
                Message = "Collection is synced",
                LastSyncedAt = localCollection.LastSyncedAt,
                LocalDocumentCount = localCollection.Documents?.Count ?? 0,
                R2RDocumentCount = r2rCollection.DocumentCount,
                Metadata = new Dictionary<string, object>
                {
                    ["correlation_id"] = correlationId,
                    ["last_checked"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return new CollectionSyncStatusDto
            {
                CollectionId = collectionId,
                Status = SyncStatus.Error,
                Message = ex.Message,
                SyncErrors = new List<string> { ex.Message }
            };
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

    // ✅ COLLECTIONS SYNC - Additional helper methods
    private async Task InvalidateCollectionCacheAsync(Guid collectionId, Guid companyId)
    {
        var tags = new List<string>
        {
            "collections",
            $"collection:{collectionId}",
            "collection-lists"
        };

        await _cacheService.InvalidateByTagsAsync(tags, companyId.ToString());
    }

    private async Task SyncCollectionDocumentsAsync(Collection localCollection, CollectionResponse r2rCollection)
    {
        // Placeholder for document synchronization
        // In a full implementation, this would sync documents between local and R2R collections
        _logger.LogDebug("Document sync for collection {CollectionId} - placeholder implementation", localCollection.Id);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Finds an existing R2R collection by name to support bidirectional sync
    /// </summary>
    private async Task<string?> FindExistingR2RCollectionAsync(string collectionName)
    {
        try
        {
            _logger.LogInformation("🔍 Searching for existing R2R collection: {CollectionName}", collectionName);

            // List all collections from R2R
            var r2rCollections = await _r2rCollectionClient.ListCollectionsAsync();

            if (r2rCollections?.Results?.Any() == true)
            {
                _logger.LogInformation("📋 Found {Count} R2R collections total", r2rCollections.Results.Count);

                // Log all collections for debugging
                foreach (var collection in r2rCollections.Results)
                {
                    _logger.LogInformation("📁 R2R Collection: Name='{Name}', ID='{Id}'",
                        collection.Name, collection.CollectionId);
                }

                // Find collection by name (case-insensitive match)
                var matchingCollection = r2rCollections.Results
                    .FirstOrDefault(c => string.Equals(c.Name, collectionName, StringComparison.OrdinalIgnoreCase));

                if (matchingCollection != null)
                {
                    _logger.LogInformation("✅ Found existing R2R collection: {CollectionName} -> {R2RCollectionId}",
                        collectionName, matchingCollection.CollectionId);
                    return matchingCollection.CollectionId;
                }
                else
                {
                    _logger.LogInformation("❌ No matching R2R collection found for name: {CollectionName}", collectionName);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No R2R collections returned from API");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error searching for existing R2R collection: {CollectionName}", collectionName);
            return null;
        }
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
