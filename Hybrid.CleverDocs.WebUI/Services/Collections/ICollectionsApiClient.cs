using Hybrid.CleverDocs.WebUI.Models.Collections;
using Hybrid.CleverDocs.WebUI.Models.Common;

namespace Hybrid.CleverDocs.WebUI.Services.Collections;

/// <summary>
/// Interface for Collections API client with enterprise-grade patterns
/// </summary>
public interface ICollectionsApiClient
{
    // Core CRUD Operations
    Task<PagedResult<CollectionViewModel>> SearchCollectionsAsync(CollectionSearchViewModel search);
    Task<CollectionViewModel?> GetCollectionAsync(Guid collectionId);
    Task<CollectionViewModel?> CreateCollectionAsync(CreateCollectionViewModel collection);
    Task<CollectionViewModel?> UpdateCollectionAsync(Guid collectionId, UpdateCollectionViewModel collection);
    Task<bool> DeleteCollectionAsync(Guid collectionId);

    // Favorites and Quick Actions
    Task<PagedResult<CollectionViewModel>> GetFavoriteCollectionsAsync(int page = 1, int pageSize = 20);
    Task<List<CollectionViewModel>> GetRecentCollectionsAsync(int limit = 10);
    Task<bool> ToggleFavoriteAsync(Guid collectionId);

    // Bulk Operations
    Task<bool> BulkOperationAsync(BulkCollectionOperationViewModel operation);
    Task<bool> ReorderCollectionsAsync(List<CollectionOrderViewModel> collections);

    // Search and Suggestions
    Task<List<string>> GetSearchSuggestionsAsync(string term, int limit = 10);
    Task<List<string>> GetTagSuggestionsAsync(string term, int limit = 10);

    // Analytics and Statistics
    Task<CollectionStatsOverviewViewModel> GetStatsOverviewAsync();
    Task<CollectionAnalyticsViewModel> GetCollectionAnalyticsAsync(Guid collectionId);

    // Import/Export
    Task<bool> ExportCollectionsAsync(List<Guid> collectionIds, string format = "json");
    Task<bool> ImportCollectionsAsync(byte[] fileData, string format = "json");

    // Real-time Updates
    Task TrackCollectionViewAsync(Guid collectionId);
    Task<List<CollectionViewModel>> GetSharedCollectionsAsync();
}
