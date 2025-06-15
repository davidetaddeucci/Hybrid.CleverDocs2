using Hybrid.CleverDocs2.WebServices.Models.Documents;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service interface for user document management operations
/// </summary>
public interface IUserDocumentService
{
    /// <summary>
    /// Search documents with advanced filtering and pagination
    /// </summary>
    Task<PagedDocumentResultDto> SearchDocumentsAsync(DocumentQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get documents for a specific collection
    /// </summary>
    Task<PagedDocumentResultDto> GetCollectionDocumentsAsync(Guid collectionId, string userId, DocumentQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific document by ID
    /// </summary>
    Task<UserDocumentDto?> GetDocumentByIdAsync(Guid documentId, string userId);

    /// <summary>
    /// Get multiple documents by IDs
    /// </summary>
    Task<List<UserDocumentDto>> GetDocumentsByIdsAsync(List<Guid> documentIds, string userId);

    /// <summary>
    /// Update document metadata
    /// </summary>
    Task<UserDocumentDto?> UpdateDocumentMetadataAsync(Guid documentId, string userId, DocumentMetadataUpdateDto update);

    /// <summary>
    /// Move document to a different collection
    /// </summary>
    Task<bool> MoveDocumentToCollectionAsync(Guid documentId, string userId, Guid? targetCollectionId);

    /// <summary>
    /// Delete a document
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Toggle document favorite status
    /// </summary>
    Task<bool> ToggleFavoriteAsync(Guid documentId, string userId);

    /// <summary>
    /// Get user's favorite documents
    /// </summary>
    Task<PagedDocumentResultDto> GetFavoriteDocumentsAsync(string userId, DocumentQueryDto query);

    /// <summary>
    /// Get recently viewed documents
    /// </summary>
    Task<List<UserDocumentDto>> GetRecentDocumentsAsync(string userId, int limit = 10);

    /// <summary>
    /// Execute batch operations on multiple documents
    /// </summary>
    Task<BatchOperationResultDto> ExecuteBatchOperationAsync(BatchOperationRequestDto request);

    /// <summary>
    /// Get document analytics
    /// </summary>
    Task<DocumentAnalyticsDto> GetDocumentAnalyticsAsync(Guid documentId, string userId);

    /// <summary>
    /// Track document view event
    /// </summary>
    Task TrackDocumentViewAsync(Guid documentId, string userId, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Track document download event
    /// </summary>
    Task TrackDocumentDownloadAsync(Guid documentId, string userId);

    /// <summary>
    /// Get search suggestions based on user input
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(string term, string? userId = null, int limit = 10);

    /// <summary>
    /// Get advanced search suggestions (terms, tags, collections, etc.)
    /// </summary>
    Task<DocumentSearchSuggestionsDto> GetAdvancedSearchSuggestionsAsync(string term, string? userId = null);

    /// <summary>
    /// Export documents to various formats
    /// </summary>
    Task<byte[]> ExportDocumentsAsync(DocumentExportRequestDto request, string userId);

    /// <summary>
    /// Get document download URL
    /// </summary>
    Task<string?> GetDocumentDownloadUrlAsync(Guid documentId, string userId, TimeSpan? expiration = null);

    /// <summary>
    /// Get document preview URL
    /// </summary>
    Task<string?> GetDocumentPreviewUrlAsync(Guid documentId, string userId, TimeSpan? expiration = null);

    /// <summary>
    /// Get document thumbnail URL
    /// </summary>
    Task<string?> GetDocumentThumbnailUrlAsync(Guid documentId, string userId, TimeSpan? expiration = null);

    /// <summary>
    /// Check if user has permission to access document
    /// </summary>
    Task<DocumentPermissions> GetDocumentPermissionsAsync(Guid documentId, string userId);

    /// <summary>
    /// Get document versions
    /// </summary>
    Task<List<UserDocumentDto>> GetDocumentVersionsAsync(Guid documentId, string userId);

    /// <summary>
    /// Create new document version
    /// </summary>
    Task<UserDocumentDto?> CreateDocumentVersionAsync(Guid documentId, string userId, byte[] content, string? versionNote = null);

    /// <summary>
    /// Restore document version
    /// </summary>
    Task<bool> RestoreDocumentVersionAsync(Guid documentId, string userId, string version);

    /// <summary>
    /// Get document content for preview
    /// </summary>
    Task<byte[]?> GetDocumentContentAsync(Guid documentId, string userId);

    /// <summary>
    /// Get document text content for search indexing
    /// </summary>
    Task<string?> GetDocumentTextContentAsync(Guid documentId, string userId);

    /// <summary>
    /// Update document tags
    /// </summary>
    Task<bool> UpdateDocumentTagsAsync(Guid documentId, string userId, List<string> tags);

    /// <summary>
    /// Get all available tags for user's documents
    /// </summary>
    Task<List<string>> GetAvailableTagsAsync(string userId);

    /// <summary>
    /// Get document statistics for user
    /// </summary>
    Task<Dictionary<string, object>> GetUserDocumentStatisticsAsync(string userId);

    /// <summary>
    /// Get document statistics for collection
    /// </summary>
    Task<Dictionary<string, object>> GetCollectionDocumentStatisticsAsync(Guid collectionId, string userId);

    /// <summary>
    /// Duplicate a document
    /// </summary>
    Task<UserDocumentDto?> DuplicateDocumentAsync(Guid documentId, string userId, string? newName = null, Guid? targetCollectionId = null);

    /// <summary>
    /// Archive a document
    /// </summary>
    Task<bool> ArchiveDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Restore an archived document
    /// </summary>
    Task<bool> RestoreDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Get archived documents
    /// </summary>
    Task<PagedDocumentResultDto> GetArchivedDocumentsAsync(string userId, DocumentQueryDto query);

    /// <summary>
    /// Permanently delete a document
    /// </summary>
    Task<bool> PermanentlyDeleteDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Get documents pending R2R processing
    /// </summary>
    Task<List<UserDocumentDto>> GetPendingProcessingDocumentsAsync(string userId);

    /// <summary>
    /// Retry R2R processing for a document
    /// </summary>
    Task<bool> RetryDocumentProcessingAsync(Guid documentId, string userId);

    /// <summary>
    /// Get document processing status
    /// </summary>
    Task<Dictionary<string, object>> GetDocumentProcessingStatusAsync(Guid documentId, string userId);

    /// <summary>
    /// Share document with other users
    /// </summary>
    Task<string> ShareDocumentAsync(Guid documentId, string userId, List<string> targetUserIds, DocumentPermissions permissions, TimeSpan? expiration = null);

    /// <summary>
    /// Get shared documents
    /// </summary>
    Task<PagedDocumentResultDto> GetSharedDocumentsAsync(string userId, DocumentQueryDto query);

    /// <summary>
    /// Get documents shared with user
    /// </summary>
    Task<PagedDocumentResultDto> GetSharedWithMeDocumentsAsync(string userId, DocumentQueryDto query);

    /// <summary>
    /// Revoke document sharing
    /// </summary>
    Task<bool> RevokeDocumentSharingAsync(Guid documentId, string userId, string? targetUserId = null);

    /// <summary>
    /// Get document sharing information
    /// </summary>
    Task<Dictionary<string, object>> GetDocumentSharingInfoAsync(Guid documentId, string userId);

    /// <summary>
    /// Validate document access
    /// </summary>
    Task<bool> ValidateDocumentAccessAsync(Guid documentId, string userId, DocumentAction action);

    /// <summary>
    /// Get document activity log
    /// </summary>
    Task<List<Dictionary<string, object>>> GetDocumentActivityLogAsync(Guid documentId, string userId, int limit = 50);

    /// <summary>
    /// Add document comment
    /// </summary>
    Task<Dictionary<string, object>> AddDocumentCommentAsync(Guid documentId, string userId, string comment);

    /// <summary>
    /// Get document comments
    /// </summary>
    Task<List<Dictionary<string, object>>> GetDocumentCommentsAsync(Guid documentId, string userId);

    /// <summary>
    /// Update document comment
    /// </summary>
    Task<bool> UpdateDocumentCommentAsync(Guid commentId, string userId, string comment);

    /// <summary>
    /// Delete document comment
    /// </summary>
    Task<bool> DeleteDocumentCommentAsync(Guid commentId, string userId);
}
