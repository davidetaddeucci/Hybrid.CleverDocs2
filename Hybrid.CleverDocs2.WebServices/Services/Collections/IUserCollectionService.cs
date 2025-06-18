using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Models.Common;

namespace Hybrid.CleverDocs2.WebServices.Services.Collections;

/// <summary>
/// Interface for user collection service operations
/// </summary>
public interface IUserCollectionService
{
    /// <summary>
    /// Gets all collections for a user with caching
    /// </summary>
    Task<List<UserCollectionDto>> GetUserCollectionsAsync(string userId);

    /// <summary>
    /// Gets a specific collection by ID
    /// </summary>
    Task<UserCollectionDto?> GetCollectionByIdAsync(Guid collectionId, string userId);

    /// <summary>
    /// Searches collections with advanced filtering
    /// </summary>
    Task<PagedResult<UserCollectionDto>> SearchCollectionsAsync(CollectionSearchDto searchRequest, string userId);

    /// <summary>
    /// Creates a new collection with optimistic UI support
    /// </summary>
    Task<CollectionOperationResponseDto> CreateCollectionAsync(CreateUserCollectionDto request);

    /// <summary>
    /// Updates an existing collection
    /// </summary>
    Task<CollectionOperationResponseDto> UpdateCollectionAsync(UpdateUserCollectionDto request);

    /// <summary>
    /// Deletes a collection (soft delete)
    /// </summary>
    Task<CollectionOperationResponseDto> DeleteCollectionAsync(Guid collectionId, string userId);

    /// <summary>
    /// Toggles favorite status for a collection
    /// </summary>
    Task<CollectionOperationResponseDto> ToggleFavoriteAsync(Guid collectionId, string userId);

    /// <summary>
    /// Reorders collections for a user
    /// </summary>
    Task<CollectionOperationResponseDto> ReorderCollectionsAsync(ReorderCollectionsDto request);

    /// <summary>
    /// Performs bulk operations on multiple collections
    /// </summary>
    Task<CollectionOperationResponseDto> BulkOperationAsync(BulkCollectionOperationDto request);

    /// <summary>
    /// Gets smart suggestions for collection creation
    /// </summary>
    Task<List<CollectionSuggestionDto>> GetCollectionSuggestionsAsync(string userId, string? context = null);

    /// <summary>
    /// Gets collection analytics and statistics
    /// </summary>
    Task<CollectionAnalyticsDto> GetCollectionAnalyticsAsync(Guid collectionId, string userId);

    /// <summary>
    /// Gets user's favorite collections
    /// </summary>
    Task<List<UserCollectionDto>> GetFavoriteCollectionsAsync(string userId);

    /// <summary>
    /// Gets recently accessed collections
    /// </summary>
    Task<List<UserCollectionDto>> GetRecentCollectionsAsync(string userId, int count = 5);

    /// <summary>
    /// Updates last accessed time for a collection
    /// </summary>
    Task UpdateLastAccessedAsync(Guid collectionId, string userId);

    /// <summary>
    /// Validates collection name uniqueness for user
    /// </summary>
    Task<bool> IsCollectionNameUniqueAsync(string name, string userId, Guid? excludeCollectionId = null);

    /// <summary>
    /// Gets available colors for collections
    /// </summary>
    Task<List<string>> GetAvailableColorsAsync();

    /// <summary>
    /// Gets available icons for collections
    /// </summary>
    Task<List<string>> GetAvailableIconsAsync();

    /// <summary>
    /// Duplicates a collection
    /// </summary>
    Task<CollectionOperationResponseDto> DuplicateCollectionAsync(Guid collectionId, string userId, string? newName = null);

    /// <summary>
    /// Archives a collection (soft archive)
    /// </summary>
    Task<CollectionOperationResponseDto> ArchiveCollectionAsync(Guid collectionId, string userId);

    /// <summary>
    /// Restores an archived collection
    /// </summary>
    Task<CollectionOperationResponseDto> RestoreCollectionAsync(Guid collectionId, string userId);

    /// <summary>
    /// Gets archived collections for a user
    /// </summary>
    Task<List<UserCollectionDto>> GetArchivedCollectionsAsync(string userId);

    /// <summary>
    /// Exports collection metadata
    /// </summary>
    Task<byte[]> ExportCollectionAsync(Guid collectionId, string userId, string format = "json");

    /// <summary>
    /// Imports collections from file
    /// </summary>
    Task<CollectionOperationResponseDto> ImportCollectionsAsync(byte[] fileData, string userId, string format = "json");
}

/// <summary>
/// Interface for bidirectional collection synchronization with R2R API
/// Enhanced with Quick Wins and real R2R integration
/// </summary>
public interface ICollectionSyncService
{
    /// <summary>
    /// Creates a collection in R2R API
    /// </summary>
    Task<string?> CreateR2RCollectionAsync(UserCollectionDto collection);

    /// <summary>
    /// Updates a collection in R2R API
    /// </summary>
    Task<bool> UpdateR2RCollectionAsync(UserCollectionDto collection);

    /// <summary>
    /// Deletes a collection from R2R API
    /// </summary>
    Task<bool> DeleteR2RCollectionAsync(string r2rCollectionId);

    /// <summary>
    /// Synchronizes collection with R2R API (legacy method)
    /// </summary>
    Task<bool> SyncCollectionAsync(Guid collectionId);

    /// <summary>
    /// Gets R2R collection status (legacy method)
    /// </summary>
    Task<R2RCollectionSyncDto> GetSyncStatusAsync(Guid collectionId);

    /// <summary>
    /// Retries failed synchronization
    /// </summary>
    Task<bool> RetrySyncAsync(Guid collectionId);

    /// <summary>
    /// Processes sync queue
    /// </summary>
    Task ProcessSyncQueueAsync();

    // ✅ NEW BIDIRECTIONAL SYNC METHODS
    /// <summary>
    /// Performs bidirectional synchronization between local and R2R collection
    /// </summary>
    Task<bool> SyncCollectionWithR2RAsync(Guid collectionId);

    /// <summary>
    /// Creates a new collection in R2R and links it to local collection
    /// </summary>
    Task<string?> CreateR2RCollectionAsync(Guid localCollectionId);

    /// <summary>
    /// Synchronizes all collections for a user
    /// </summary>
    Task<int> SyncAllUserCollectionsAsync(string userId);

    /// <summary>
    /// Pulls updates from R2R collection to local collection
    /// </summary>
    Task<bool> PullFromR2RAsync(Guid collectionId);

    /// <summary>
    /// Pushes local collection changes to R2R
    /// </summary>
    Task<bool> PushToR2RAsync(Guid collectionId);

    /// <summary>
    /// Gets detailed collection sync status
    /// </summary>
    Task<CollectionSyncStatusDto> GetDetailedSyncStatusAsync(Guid collectionId);
}

/// <summary>
/// Interface for collection suggestions and AI assistance
/// </summary>
public interface ICollectionSuggestionService
{
    /// <summary>
    /// Generates smart collection name suggestions
    /// </summary>
    Task<List<string>> SuggestCollectionNamesAsync(string userId, string? context = null);

    /// <summary>
    /// Suggests collection organization improvements
    /// </summary>
    Task<List<CollectionSuggestionDto>> SuggestOrganizationImprovementsAsync(string userId);

    /// <summary>
    /// Suggests collections based on document content
    /// </summary>
    Task<List<CollectionSuggestionDto>> SuggestCollectionsForDocumentsAsync(List<string> documentIds, string userId);

    /// <summary>
    /// Suggests tags for a collection
    /// </summary>
    Task<List<string>> SuggestTagsAsync(Guid collectionId, string userId);

    /// <summary>
    /// Suggests color based on collection content
    /// </summary>
    Task<string> SuggestColorAsync(string collectionName, string? description = null);

    /// <summary>
    /// Suggests icon based on collection content
    /// </summary>
    Task<string> SuggestIconAsync(string collectionName, string? description = null);
}

/// <summary>
/// Interface for collection analytics and insights
/// </summary>
public interface ICollectionAnalyticsService
{
    /// <summary>
    /// Tracks collection activity
    /// </summary>
    Task TrackActivityAsync(Guid collectionId, string userId, string activityType, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets collection usage statistics
    /// </summary>
    Task<CollectionAnalyticsDto> GetUsageStatisticsAsync(Guid collectionId, string userId);

    /// <summary>
    /// Gets user's collection insights
    /// </summary>
    Task<List<CollectionAnalyticsDto>> GetUserInsightsAsync(string userId);

    /// <summary>
    /// Gets trending collections
    /// </summary>
    Task<List<UserCollectionDto>> GetTrendingCollectionsAsync(string userId);

    /// <summary>
    /// Gets collection performance metrics
    /// </summary>
    Task<Dictionary<string, object>> GetPerformanceMetricsAsync(Guid collectionId);
}
