using Hybrid.CleverDocs.WebUI.Models.Documents;

namespace Hybrid.CleverDocs.WebUI.Services.Documents;

/// <summary>
/// Interface for document API client operations
/// </summary>
public interface IDocumentApiClient
{
    /// <summary>
    /// Search documents with advanced filtering and pagination
    /// </summary>
    Task<PagedResult<DocumentViewModel>> SearchDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get documents for a specific collection
    /// </summary>
    Task<PagedResult<DocumentViewModel>> GetCollectionDocumentsAsync(Guid collectionId, DocumentSearchViewModel search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific document by ID
    /// </summary>
    Task<DocumentViewModel?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple documents by IDs
    /// </summary>
    Task<List<DocumentViewModel>> GetDocumentsByIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update document metadata
    /// </summary>
    Task<DocumentViewModel?> UpdateDocumentMetadataAsync(Guid documentId, DocumentEditViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move document to a different collection
    /// </summary>
    Task<bool> MoveDocumentAsync(Guid documentId, Guid? targetCollectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a document
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a new document
    /// </summary>
    Task<DocumentViewModel?> UploadDocumentAsync(DocumentUploadViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle document favorite status
    /// </summary>
    Task<bool> ToggleFavoriteAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's favorite documents
    /// </summary>
    Task<PagedResult<DocumentViewModel>> GetFavoriteDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recently viewed documents
    /// </summary>
    Task<List<DocumentViewModel>> GetRecentDocumentsAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute batch operations on multiple documents
    /// </summary>
    Task<BatchOperationResult> ExecuteBatchOperationAsync(BatchOperationViewModel operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get search suggestions
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track document view
    /// </summary>
    Task<bool> TrackDocumentViewAsync(Guid documentId, CancellationToken cancellationToken = default);

    // Advanced Search Methods

    /// <summary>
    /// Get document name suggestions for autocomplete
    /// </summary>
    Task<List<string>> GetDocumentNameSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get content suggestions for autocomplete
    /// </summary>
    Task<List<string>> GetContentSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tag suggestions for autocomplete
    /// </summary>
    Task<List<string>> GetTagSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get author suggestions for autocomplete
    /// </summary>
    Task<List<string>> GetAuthorSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);

    // Saved Search Methods

    /// <summary>
    /// Save a search for future use
    /// </summary>
    Task<bool> SaveSearchAsync(SavedSearchItem savedSearch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's saved searches
    /// </summary>
    Task<List<SavedSearchItem>> GetSavedSearchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific saved search
    /// </summary>
    Task<SavedSearchItem?> GetSavedSearchAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update saved search usage statistics
    /// </summary>
    Task<bool> UpdateSavedSearchUsageAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a saved search
    /// </summary>
    Task<bool> DeleteSavedSearchAsync(Guid id, CancellationToken cancellationToken = default);

    // Search History Methods

    /// <summary>
    /// Get user's search history
    /// </summary>
    Task<List<SearchHistoryItem>> GetSearchHistoryAsync(int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a search in history
    /// </summary>
    Task<bool> RecordSearchAsync(SearchHistoryItem historyItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear user's search history
    /// </summary>
    Task<bool> ClearSearchHistoryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic paged result class
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
}


