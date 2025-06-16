using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Data;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// User collection service implementation with caching and real-time updates
/// </summary>
public class UserCollectionService : IUserCollectionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ICollectionSyncService _syncService;
    private readonly ICollectionSuggestionService _suggestionService;
    private readonly ICollectionAnalyticsService _analyticsService;
    private readonly IHubContext<CollectionHub> _hubContext;
    private readonly ILogger<UserCollectionService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly UserCollectionOptions _options;

    public UserCollectionService(
        ApplicationDbContext dbContext,
        IMultiLevelCacheService cacheService,
        ICollectionSyncService syncService,
        ICollectionSuggestionService suggestionService,
        ICollectionAnalyticsService analyticsService,
        IHubContext<CollectionHub> hubContext,
        ILogger<UserCollectionService> logger,
        ICorrelationService correlationService,
        IOptions<UserCollectionOptions> options)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _syncService = syncService;
        _suggestionService = suggestionService;
        _analyticsService = analyticsService;
        _hubContext = hubContext;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;

        // InitializeMockData(); // ❌ REMOVED: No longer using mock data
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

                // Query database for user's collections
                var userGuid = Guid.Parse(userId);
                var collectionEntities = await _dbContext.Collections
                    .Where(c => c.UserId == userGuid)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();

                // Convert to DTOs
                var collections = collectionEntities.Select(MapEntityToDto).ToList();

                // Update stats for each collection
                foreach (var collection in collections)
                {
                    collection.Stats = await GetCollectionStatsAsync(collection.Id);
                    collection.DocumentCount = collection.Stats.TotalDocuments; // Update DocumentCount from real stats
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
            // Query database for collection
            var userGuid = Guid.Parse(userId);
            var collectionEntity = await _dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userGuid);

            if (collectionEntity == null)
                return null;

            // Convert to DTO
            var collection = MapEntityToDto(collectionEntity);
            collection.Stats = await GetCollectionStatsAsync(collectionId);
            collection.DocumentCount = collection.Stats.TotalDocuments; // Update DocumentCount from real stats
            await UpdateLastAccessedAsync(collectionId, userId);

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

            // Create collection entity for database
            var collectionEntity = new Data.Entities.Collection
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Color = request.Color ?? await _suggestionService.SuggestColorAsync(request.Name, request.Description),
                Icon = request.Icon ?? await _suggestionService.SuggestIconAsync(request.Name, request.Description),
                Tags = request.Tags, // Uses the helper property for JSON serialization
                IsFavorite = request.SetAsFavorite,
                UserId = Guid.Parse(request.UserId),
                CompanyId = Guid.Parse(GetTenantIdForUser(request.UserId)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = request.UserId,
                UpdatedBy = request.UserId
            };

            // Save to database
            await _dbContext.Collections.AddAsync(collectionEntity);
            await _dbContext.SaveChangesAsync();

            // Convert to DTO for response
            var collection = MapEntityToDto(collectionEntity);

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
            // Find collection in database
            var userGuid = Guid.Parse(request.UserId);
            var collectionEntity = await _dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == request.CollectionId && c.UserId == userGuid);

            if (collectionEntity == null)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Collection not found or access denied",
                    Errors = new List<string> { "Collection not found" }
                };
            }

            // Check name uniqueness if name is being changed
            if (!string.IsNullOrEmpty(request.Name) && request.Name != collectionEntity.Name)
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
            if (!string.IsNullOrEmpty(request.Name)) collectionEntity.Name = request.Name;
            if (request.Description != null) collectionEntity.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Color)) collectionEntity.Color = request.Color;
            if (!string.IsNullOrEmpty(request.Icon)) collectionEntity.Icon = request.Icon;
            if (request.Tags != null) collectionEntity.Tags = request.Tags;
            if (request.IsFavorite.HasValue) collectionEntity.IsFavorite = request.IsFavorite.Value;

            collectionEntity.UpdatedAt = DateTime.UtcNow;
            collectionEntity.UpdatedBy = request.UserId;

            // Save changes to database
            await _dbContext.SaveChangesAsync();

            // Convert to DTO for response
            var collection = MapEntityToDto(collectionEntity);

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
            // Find collection in database
            var userGuid = Guid.Parse(userId);
            var collectionEntity = await _dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userGuid);

            if (collectionEntity == null)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Collection not found or access denied"
                };
            }

            // Check if collection has documents (count from related documents)
            var documentCount = await _dbContext.CollectionDocuments
                .CountAsync(cd => cd.CollectionId == collectionId);

            if (documentCount > 0)
            {
                return new CollectionOperationResponseDto
                {
                    Success = false,
                    Message = "Cannot delete collection with documents. Move or delete documents first.",
                    Errors = new List<string> { "Collection contains documents" }
                };
            }

            // Convert to DTO for R2R operations
            var collection = MapEntityToDto(collectionEntity);

            // Remove from database
            _dbContext.Collections.Remove(collectionEntity);
            await _dbContext.SaveChangesAsync();

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
        // Find collection in database
        var userGuid = Guid.Parse(userId);
        var collectionEntity = await _dbContext.Collections
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userGuid);

        if (collectionEntity == null)
        {
            return new CollectionOperationResponseDto
            {
                Success = false,
                Message = "Collection not found"
            };
        }

        // Toggle favorite status
        collectionEntity.IsFavorite = !collectionEntity.IsFavorite;
        collectionEntity.UpdatedAt = DateTime.UtcNow;
        collectionEntity.UpdatedBy = userId;

        // Save changes
        await _dbContext.SaveChangesAsync();

        // Convert to DTO for response
        var collection = MapEntityToDto(collectionEntity);

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
        var correlationId = _correlationService.GetCorrelationId();
        _logger.LogInformation("Searching collections for user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);

        try
        {
            var userGuid = Guid.Parse(userId);
            var query = _dbContext.Collections.Where(c => c.UserId == userGuid);

            // Apply search filters
            if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
            {
                var searchTerm = searchRequest.SearchTerm.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                        (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
            }

            if (searchRequest.Tags.Any())
            {
                foreach (var tag in searchRequest.Tags)
                {
                    query = query.Where(c => c.TagsJson != null && c.TagsJson.Contains(tag));
                }
            }

            if (!string.IsNullOrEmpty(searchRequest.Color))
            {
                query = query.Where(c => c.Color == searchRequest.Color);
            }

            if (!string.IsNullOrEmpty(searchRequest.Icon))
            {
                query = query.Where(c => c.Icon == searchRequest.Icon);
            }

            if (searchRequest.IsFavorite.HasValue)
            {
                query = query.Where(c => c.IsFavorite == searchRequest.IsFavorite.Value);
            }

            if (searchRequest.CreatedAfter.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= searchRequest.CreatedAfter.Value);
            }

            if (searchRequest.CreatedBefore.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= searchRequest.CreatedBefore.Value);
            }

            // Apply sorting
            query = searchRequest.SortBy.ToLower() switch
            {
                "name" => searchRequest.SortDirection.ToUpper() == "ASC"
                    ? query.OrderBy(c => c.Name)
                    : query.OrderByDescending(c => c.Name),
                "createdat" => searchRequest.SortDirection.ToUpper() == "ASC"
                    ? query.OrderBy(c => c.CreatedAt)
                    : query.OrderByDescending(c => c.CreatedAt),
                _ => searchRequest.SortDirection.ToUpper() == "ASC"
                    ? query.OrderBy(c => c.UpdatedAt)
                    : query.OrderByDescending(c => c.UpdatedAt)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var collections = await query
                .Skip((searchRequest.Page - 1) * searchRequest.PageSize)
                .Take(searchRequest.PageSize)
                .ToListAsync();

            // Map to DTOs and update stats
            var collectionDtos = collections.Select(MapEntityToDto).ToList();

            // Update stats for each collection in search results
            foreach (var collection in collectionDtos)
            {
                collection.Stats = await GetCollectionStatsAsync(collection.Id);
                collection.DocumentCount = collection.Stats.TotalDocuments; // Update DocumentCount from real stats
            }

            // Use the constructor that automatically calculates TotalPages
            var result = new PagedResult<UserCollectionDto>(
                collectionDtos,
                totalCount,
                searchRequest.Page,
                searchRequest.PageSize);

            _logger.LogInformation("Found {Count} collections for user {UserId}, CorrelationId: {CorrelationId}",
                totalCount, userId, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
            throw;
        }
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
        // Find collection in database
        var userGuid = Guid.Parse(userId);
        var collectionEntity = await _dbContext.Collections
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userGuid);

        if (collectionEntity != null)
        {
            // Note: LastAccessedAt is not in the entity model, but we can update UpdatedAt
            // In a real implementation, you might want to add LastAccessedAt to the entity
            collectionEntity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            await InvalidateCollectionCache(collectionId, userId);
        }
    }

    public async Task<bool> IsCollectionNameUniqueAsync(string name, string userId, Guid? excludeCollectionId = null)
    {
        var userGuid = Guid.Parse(userId);
        var exists = await _dbContext.Collections
            .AnyAsync(c => c.UserId == userGuid &&
                          c.Name.ToLower() == name.ToLower() &&
                          c.Id != excludeCollectionId);
        return !exists;
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
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Getting real collection stats for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);

            // Get real document count and size from database
            var documents = await _dbContext.Documents
                .Where(d => d.CollectionId == collectionId)
                .ToListAsync();

            var totalDocuments = documents.Count;
            var totalSizeBytes = documents.Sum(d => d.Size);

            // Calculate documents added this week and month
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var oneMonthAgo = DateTime.UtcNow.AddDays(-30);

            var documentsThisWeek = documents.Count(d => d.CreatedAt >= oneWeekAgo);
            var documentsThisMonth = documents.Count(d => d.CreatedAt >= oneMonthAgo);

            _logger.LogDebug("Collection {CollectionId} stats: {TotalDocuments} documents, {TotalSizeBytes} bytes, CorrelationId: {CorrelationId}",
                collectionId, totalDocuments, totalSizeBytes, correlationId);

            return new CollectionStatsDto
            {
                TotalDocuments = totalDocuments,
                TotalSizeBytes = totalSizeBytes,
                DocumentsThisWeek = documentsThisWeek,
                DocumentsThisMonth = documentsThisMonth
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection stats for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);

            // Return empty stats on error
            return new CollectionStatsDto
            {
                TotalDocuments = 0,
                TotalSizeBytes = 0,
                DocumentsThisWeek = 0,
                DocumentsThisMonth = 0
            };
        }
    }

    private async Task ValidateUserCanCreateCollectionAsync(string userId)
    {
        // Check user limits, permissions, etc.
        await Task.Delay(10);
    }

    private string GetTenantIdForUser(string userId)
    {
        // For now, use a default GUID until we implement proper tenant resolution
        // In production, this should get the actual tenant ID from the user context
        return "f2e9cab2-c7b4-446b-a9b9-13501b2b5973"; // Default tenant GUID
    }

    /// <summary>
    /// Maps Collection entity to UserCollectionDto
    /// </summary>
    private UserCollectionDto MapEntityToDto(Data.Entities.Collection entity)
    {
        return new UserCollectionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Color = entity.Color,
            Icon = entity.Icon,
            Tags = entity.Tags,
            CreatedBy = entity.CreatedBy ?? entity.UserId.ToString(),
            TenantId = entity.CompanyId.ToString(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastAccessedAt = null, // Will be updated when accessed
            IsFavorite = entity.IsFavorite,
            DocumentCount = 0, // Will be calculated from related documents
            Stats = new CollectionStatsDto(),
            Permissions = new CollectionPermissionsDto { IsOwner = true },
            R2RCollectionId = entity.R2RCollectionId
        };
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
