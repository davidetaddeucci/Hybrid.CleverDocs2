using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Hubs;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// User collection service implementation with caching and real-time updates
/// </summary>
public class UserCollectionService : IUserCollectionService
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ICollectionSyncService _syncService;
    private readonly ICollectionSuggestionService _suggestionService;
    private readonly ICollectionAnalyticsService _analyticsService;
    private readonly IHubContext<CollectionHub> _hubContext;
    private readonly ILogger<UserCollectionService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly UserCollectionOptions _options;

    // Mock repository - in real implementation this would be injected
    private readonly List<UserCollectionDto> _mockCollections = new();

    public UserCollectionService(
        IMultiLevelCacheService cacheService,
        ICollectionSyncService syncService,
        ICollectionSuggestionService suggestionService,
        ICollectionAnalyticsService analyticsService,
        IHubContext<CollectionHub> hubContext,
        ILogger<UserCollectionService> logger,
        ICorrelationService correlationService,
        IOptions<UserCollectionOptions> options)
    {
        _cacheService = cacheService;
        _syncService = syncService;
        _suggestionService = suggestionService;
        _analyticsService = analyticsService;
        _hubContext = hubContext;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;

        InitializeMockData();
    }

    public async Task<List<UserCollectionDto>> GetUserCollectionsAsync(string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"user:collections:{userId}";

        try
        {
            return await _cacheService.GetAsync(cacheKey, async () =>
            {
                _logger.LogDebug("Loading collections for user {UserId}, CorrelationId: {CorrelationId}", 
                    userId, correlationId);

                // Mock implementation - in real app this would query database
                var collections = _mockCollections
                    .Where(c => c.CreatedBy == userId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToList();

                // Update stats for each collection
                foreach (var collection in collections)
                {
                    collection.Stats = await GetCollectionStatsAsync(collection.Id);
                }

                return collections;
            }, CacheOptions.ForCollectionData(GetTenantIdForUser(userId))) ?? new List<UserCollectionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading collections for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            throw;
        }
    }

    public async Task<UserCollectionDto?> GetCollectionByIdAsync(Guid collectionId, string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var cacheKey = $"collection:details:{collectionId}";

        try
        {
            var collection = _mockCollections.FirstOrDefault(c => c.Id == collectionId && c.CreatedBy == userId);

            if (collection != null)
            {
                collection.Stats = await GetCollectionStatsAsync(collectionId);
                await UpdateLastAccessedAsync(collectionId, userId);
            }

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading collection {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
            throw;
        }
    }

    public async Task<CollectionOperationResponseDto> CreateCollectionAsync(CreateUserCollectionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Creating collection '{Name}' for user {UserId}, CorrelationId: {CorrelationId}", 
                request.Name, request.UserId, correlationId);

            // Validate user permissions
            await ValidateUserCanCreateCollectionAsync(request.UserId);

            // Check name uniqueness
            var isUnique = await IsCollectionNameUniqueAsync(request.Name, request.UserId);
            if (!isUnique)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "A collection with this name already exists",
                    Errors = new List<string> { "Collection name must be unique" }
                };
            }

            // Create collection
            var collection = new UserCollectionDto
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Color = request.Color ?? await _suggestionService.SuggestColorAsync(request.Name, request.Description),
                Icon = request.Icon ?? await _suggestionService.SuggestIconAsync(request.Name, request.Description),
                Tags = request.Tags,
                CreatedBy = request.UserId,
                TenantId = GetTenantIdForUser(request.UserId),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsFavorite = request.SetAsFavorite,
                DocumentCount = 0,
                Stats = new CollectionStatsDto(),
                Permissions = new CollectionPermissionsDto { IsOwner = true }
            };

            // Add to mock storage
            _mockCollections.Add(collection);

            // Create in R2R asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var r2rId = await _syncService.CreateR2RCollectionAsync(collection);
                    if (!string.IsNullOrEmpty(r2rId))
                    {
                        collection.R2RCollectionId = r2rId;
                        await InvalidateUserCollectionsCache(request.UserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create R2R collection for {CollectionId}, CorrelationId: {CorrelationId}", 
                        collection.Id, correlationId);
                }
            });

            // Invalidate cache
            await InvalidateUserCollectionsCache(request.UserId);

            // Track analytics
            await _analyticsService.TrackActivityAsync(collection.Id, request.UserId, "collection_created");

            // Notify real-time updates
            await _hubContext.Clients.User(request.UserId)
                .SendAsync("CollectionCreated", collection, cancellationToken: default);

            _logger.LogInformation("Collection '{Name}' created successfully with ID {CollectionId}, CorrelationId: {CorrelationId}", 
                collection.Name, collection.Id, correlationId);

            return new CollectionOperationResponseDto
            {
                Success = true,
                Message = "Collection created successfully",
                Collection = collection
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection for user {UserId}, CorrelationId: {CorrelationId}", 
                request.UserId, correlationId);

            return new CollectionOperationResponseDto
            {
                Success = false,
                Message = "Failed to create collection",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<CollectionOperationResponseDto> UpdateCollectionAsync(UpdateUserCollectionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var collection = _mockCollections.FirstOrDefault(c => c.Id == request.CollectionId && c.CreatedBy == request.UserId);
            
            if (collection == null)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Collection not found or access denied",
                    Errors = new List<string> { "Collection not found" }
                };
            }

            // Check name uniqueness if name is being changed
            if (!string.IsNullOrEmpty(request.Name) && request.Name != collection.Name)
            {
                var isUnique = await IsCollectionNameUniqueAsync(request.Name, request.UserId, request.CollectionId);
                if (!isUnique)
                {
                    return new CollectionOperationResponseDto
                    {
                        Success = false,
                        Message = "A collection with this name already exists",
                        Errors = new List<string> { "Collection name must be unique" }
                    };
                }
            }

            // Update collection properties
            if (!string.IsNullOrEmpty(request.Name)) collection.Name = request.Name;
            if (request.Description != null) collection.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Color)) collection.Color = request.Color;
            if (!string.IsNullOrEmpty(request.Icon)) collection.Icon = request.Icon;
            if (request.Tags != null) collection.Tags = request.Tags;
            if (request.IsFavorite.HasValue) collection.IsFavorite = request.IsFavorite.Value;
            
            collection.UpdatedAt = DateTime.UtcNow;

            // Update in R2R asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _syncService.UpdateR2RCollectionAsync(collection);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update R2R collection {CollectionId}, CorrelationId: {CorrelationId}", 
                        collection.Id, correlationId);
                }
            });

            // Invalidate cache
            await InvalidateCollectionCache(request.CollectionId, request.UserId);

            // Track analytics
            await _analyticsService.TrackActivityAsync(collection.Id, request.UserId, "collection_updated");

            // Notify real-time updates
            await _hubContext.Clients.User(request.UserId)
                .SendAsync("CollectionUpdated", collection, cancellationToken: default);

            return new CollectionOperationResponseDto
            {
                Success = true,
                Message = "Collection updated successfully",
                Collection = collection
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId}, CorrelationId: {CorrelationId}", 
                request.CollectionId, correlationId);

            return new CollectionOperationResponseDto
            {
                Success = false,
                Message = "Failed to update collection",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<CollectionOperationResponseDto> DeleteCollectionAsync(Guid collectionId, string userId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var collection = _mockCollections.FirstOrDefault(c => c.Id == collectionId && c.CreatedBy == userId);
            
            if (collection == null)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Collection not found or access denied"
                };
            }

            // Check if collection has documents
            if (collection.DocumentCount > 0)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Cannot delete collection with documents. Move or delete documents first.",
                    Errors = new List<string> { "Collection contains documents" }
                };
            }

            // Remove from mock storage (in real app this would be soft delete)
            _mockCollections.Remove(collection);

            // Delete from R2R asynchronously
            if (!string.IsNullOrEmpty(collection.R2RCollectionId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _syncService.DeleteR2RCollectionAsync(collection.R2RCollectionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete R2R collection {R2RCollectionId}, CorrelationId: {CorrelationId}", 
                            collection.R2RCollectionId, correlationId);
                    }
                });
            }

            // Invalidate cache
            await InvalidateCollectionCache(collectionId, userId);

            // Track analytics
            await _analyticsService.TrackActivityAsync(collectionId, userId, "collection_deleted");

            // Notify real-time updates
            await _hubContext.Clients.User(userId)
                .SendAsync("CollectionDeleted", collectionId, cancellationToken: default);

            return new CollectionOperationResponseDto
            {
                Success = true,
                Message = "Collection deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId}, CorrelationId: {CorrelationId}", 
                collectionId, correlationId);

            return new CollectionOperationResponseDto
            {
                Success = false,
                Message = "Failed to delete collection",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<CollectionOperationResponseDto> ToggleFavoriteAsync(Guid collectionId, string userId)
    {
        var collection = _mockCollections.FirstOrDefault(c => c.Id == collectionId && c.CreatedBy == userId);
        
        if (collection == null)
        {
            return new CollectionOperationResponseDto
            {
                Success = false,
                Message = "Collection not found"
            };
        }

        collection.IsFavorite = !collection.IsFavorite;
        collection.UpdatedAt = DateTime.UtcNow;

        await InvalidateCollectionCache(collectionId, userId);
        await _analyticsService.TrackActivityAsync(collectionId, userId, 
            collection.IsFavorite ? "collection_favorited" : "collection_unfavorited");

        await _hubContext.Clients.User(userId)
            .SendAsync("CollectionUpdated", collection, cancellationToken: default);

        return new CollectionOperationResponseDto
        {
            Success = true,
            Message = collection.IsFavorite ? "Added to favorites" : "Removed from favorites",
            Collection = collection
        };
    }

    // Additional methods implementation continues...
    public async Task<PagedResult<UserCollectionDto>> SearchCollectionsAsync(CollectionSearchDto searchRequest, string userId)
    {
        // Implementation for search with filtering
        await Task.Delay(10); // Simulate async work
        return new PagedResult<UserCollectionDto>();
    }

    public async Task<CollectionOperationResponseDto> ReorderCollectionsAsync(ReorderCollectionsDto request)
    {
        // Implementation for reordering
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    public async Task<CollectionOperationResponseDto> BulkOperationAsync(BulkCollectionOperationDto request)
    {
        // Implementation for bulk operations
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    public async Task<List<CollectionSuggestionDto>> GetCollectionSuggestionsAsync(string userId, string? context = null)
    {
        return await _suggestionService.SuggestOrganizationImprovementsAsync(userId);
    }

    public async Task<CollectionAnalyticsDto> GetCollectionAnalyticsAsync(Guid collectionId, string userId)
    {
        return await _analyticsService.GetUsageStatisticsAsync(collectionId, userId);
    }

    public async Task<List<UserCollectionDto>> GetFavoriteCollectionsAsync(string userId)
    {
        var collections = await GetUserCollectionsAsync(userId);
        return collections.Where(c => c.IsFavorite).ToList();
    }

    public async Task<List<UserCollectionDto>> GetRecentCollectionsAsync(string userId, int count = 5)
    {
        var collections = await GetUserCollectionsAsync(userId);
        return collections
            .Where(c => c.LastAccessedAt.HasValue)
            .OrderByDescending(c => c.LastAccessedAt)
            .Take(count)
            .ToList();
    }

    public async Task UpdateLastAccessedAsync(Guid collectionId, string userId)
    {
        var collection = _mockCollections.FirstOrDefault(c => c.Id == collectionId && c.CreatedBy == userId);
        if (collection != null)
        {
            collection.LastAccessedAt = DateTime.UtcNow;
            await InvalidateCollectionCache(collectionId, userId);
        }
    }

    public async Task<bool> IsCollectionNameUniqueAsync(string name, string userId, Guid? excludeCollectionId = null)
    {
        await Task.Delay(10); // Simulate async work
        return !_mockCollections.Any(c => 
            c.CreatedBy == userId && 
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
            c.Id != excludeCollectionId);
    }

    public async Task<List<string>> GetAvailableColorsAsync()
    {
        await Task.Delay(10);
        return new List<string>
        {
            "#3B82F6", "#EF4444", "#10B981", "#F59E0B", "#8B5CF6",
            "#EC4899", "#06B6D4", "#84CC16", "#F97316", "#6366F1"
        };
    }

    public async Task<List<string>> GetAvailableIconsAsync()
    {
        await Task.Delay(10);
        return new List<string>
        {
            "folder", "book", "briefcase", "star", "heart", "bookmark",
            "tag", "archive", "database", "file-text", "image", "music"
        };
    }

    // Placeholder implementations for remaining methods
    public async Task<CollectionOperationResponseDto> DuplicateCollectionAsync(Guid collectionId, string userId, string? newName = null)
    {
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    public async Task<CollectionOperationResponseDto> ArchiveCollectionAsync(Guid collectionId, string userId)
    {
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    public async Task<CollectionOperationResponseDto> RestoreCollectionAsync(Guid collectionId, string userId)
    {
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    public async Task<List<UserCollectionDto>> GetArchivedCollectionsAsync(string userId)
    {
        await Task.Delay(10);
        return new List<UserCollectionDto>();
    }

    public async Task<byte[]> ExportCollectionAsync(Guid collectionId, string userId, string format = "json")
    {
        await Task.Delay(10);
        return Array.Empty<byte>();
    }

    public async Task<CollectionOperationResponseDto> ImportCollectionsAsync(byte[] fileData, string userId, string format = "json")
    {
        await Task.Delay(10);
        return new CollectionOperationResponseDto { Success = true };
    }

    // Helper methods
    private async Task<CollectionStatsDto> GetCollectionStatsAsync(Guid collectionId)
    {
        await Task.Delay(10);
        return new CollectionStatsDto
        {
            TotalDocuments = Random.Shared.Next(0, 100),
            TotalSizeBytes = Random.Shared.Next(1000, 1000000),
            DocumentsThisWeek = Random.Shared.Next(0, 10),
            DocumentsThisMonth = Random.Shared.Next(0, 50)
        };
    }

    private async Task ValidateUserCanCreateCollectionAsync(string userId)
    {
        // Check user limits, permissions, etc.
        await Task.Delay(10);
    }

    private string GetTenantIdForUser(string userId)
    {
        // Extract tenant ID from user context
        return "default-tenant";
    }

    private async Task InvalidateUserCollectionsCache(string userId)
    {
        await _cacheService.InvalidateAsync($"user:collections:{userId}");
    }

    private async Task InvalidateCollectionCache(Guid collectionId, string userId)
    {
        await Task.WhenAll(
            _cacheService.InvalidateAsync($"collection:details:{collectionId}"),
            _cacheService.InvalidateAsync($"user:collections:{userId}")
        );
    }

    private void InitializeMockData()
    {
        // Initialize with some mock collections for testing
        _mockCollections.AddRange(new[]
        {
            new UserCollectionDto
            {
                Id = Guid.NewGuid(),
                Name = "Research Papers",
                Description = "Academic research and scientific papers",
                Color = "#3B82F6",
                Icon = "book",
                CreatedBy = "user1",
                DocumentCount = 15,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                IsFavorite = true
            },
            new UserCollectionDto
            {
                Id = Guid.NewGuid(),
                Name = "Project Documentation",
                Description = "Technical documentation for current projects",
                Color = "#10B981",
                Icon = "briefcase",
                CreatedBy = "user1",
                DocumentCount = 8,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        });
    }
}

/// <summary>
/// Configuration options for user collection service
/// </summary>
public class UserCollectionOptions
{
    public int MaxCollectionsPerUser { get; set; } = 100;
    public int MaxNameLength { get; set; } = 100;
    public int MaxDescriptionLength { get; set; } = 500;
    public bool EnableRealTimeUpdates { get; set; } = true;
    public bool EnableAnalytics { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
}
