using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Models.Common;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Interface for document service operations
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets all documents for a user with optional filtering
    /// </summary>
    Task<List<DocumentDto>> GetUserDocumentsAsync(string userId, Guid? collectionId = null);

    /// <summary>
    /// Gets a specific document by ID
    /// </summary>
    Task<DocumentDto?> GetDocumentByIdAsync(Guid documentId, string userId);

    /// <summary>
    /// Searches documents with advanced filtering
    /// </summary>
    Task<PagedResult<DocumentDto>> SearchDocumentsAsync(DocumentSearchDto searchRequest, string userId);

    /// <summary>
    /// Uploads a new document with processing
    /// </summary>
    Task<DocumentOperationResponseDto> UploadDocumentAsync(DocumentUploadDto uploadRequest, string userId);

    /// <summary>
    /// Uploads multiple documents
    /// </summary>
    Task<List<DocumentOperationResponseDto>> UploadMultipleDocumentsAsync(List<DocumentUploadDto> uploadRequests, string userId);

    /// <summary>
    /// Updates an existing document
    /// </summary>
    Task<DocumentOperationResponseDto> UpdateDocumentAsync(UpdateDocumentDto request);

    /// <summary>
    /// Deletes a document (soft delete)
    /// </summary>
    Task<DocumentOperationResponseDto> DeleteDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Moves a document to a different collection
    /// </summary>
    Task<DocumentOperationResponseDto> MoveDocumentAsync(Guid documentId, Guid? targetCollectionId, string userId);

    /// <summary>
    /// Creates a new version of an existing document
    /// </summary>
    Task<DocumentOperationResponseDto> CreateDocumentVersionAsync(Guid documentId, DocumentUploadDto uploadRequest, string userId, string? versionComment = null);

    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId, string userId);

    /// <summary>
    /// Restores a specific version of a document
    /// </summary>
    Task<DocumentOperationResponseDto> RestoreDocumentVersionAsync(Guid documentId, Guid versionId, string userId);

    /// <summary>
    /// Downloads a document
    /// </summary>
    Task<(byte[] Content, string ContentType, string FileName)> DownloadDocumentAsync(Guid documentId, string userId, Guid? versionId = null);

    /// <summary>
    /// Gets document preview URL
    /// </summary>
    Task<string?> GetDocumentPreviewUrlAsync(Guid documentId, string userId);

    /// <summary>
    /// Performs bulk operations on multiple documents
    /// </summary>
    Task<DocumentOperationResponseDto> BulkOperationAsync(BulkDocumentOperationDto request);

    /// <summary>
    /// Gets document analytics and statistics
    /// </summary>
    Task<DocumentAnalyticsDto> GetDocumentAnalyticsAsync(Guid documentId, string userId);

    /// <summary>
    /// Gets documents by collection
    /// </summary>
    Task<List<DocumentDto>> GetDocumentsByCollectionAsync(Guid collectionId, string userId);

    /// <summary>
    /// Gets recently accessed documents
    /// </summary>
    Task<List<DocumentDto>> GetRecentDocumentsAsync(string userId, int count = 10);

    /// <summary>
    /// Gets documents by tags
    /// </summary>
    Task<List<DocumentDto>> GetDocumentsByTagsAsync(List<string> tags, string userId);

    /// <summary>
    /// Updates last accessed time for a document
    /// </summary>
    Task UpdateLastAccessedAsync(Guid documentId, string userId);

    /// <summary>
    /// Validates document name uniqueness within collection
    /// </summary>
    Task<bool> IsDocumentNameUniqueAsync(string name, Guid? collectionId, string userId, Guid? excludeDocumentId = null);

    /// <summary>
    /// Gets supported file types
    /// </summary>
    Task<List<string>> GetSupportedFileTypesAsync();

    /// <summary>
    /// Validates file upload
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateFileUploadAsync(IFormFile file, string userId);

    /// <summary>
    /// Archives a document
    /// </summary>
    Task<DocumentOperationResponseDto> ArchiveDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Restores an archived document
    /// </summary>
    Task<DocumentOperationResponseDto> RestoreDocumentAsync(Guid documentId, string userId);

    /// <summary>
    /// Gets archived documents for a user
    /// </summary>
    Task<List<DocumentDto>> GetArchivedDocumentsAsync(string userId);

    /// <summary>
    /// Duplicates a document
    /// </summary>
    Task<DocumentOperationResponseDto> DuplicateDocumentAsync(Guid documentId, string userId, string? newName = null);

    /// <summary>
    /// Shares a document with other users
    /// </summary>
    Task<DocumentOperationResponseDto> ShareDocumentAsync(Guid documentId, List<string> userIds, string userId, DocumentPermissionsDto permissions);

    /// <summary>
    /// Gets shared documents for a user
    /// </summary>
    Task<List<DocumentDto>> GetSharedDocumentsAsync(string userId);

    /// <summary>
    /// Exports document metadata
    /// </summary>
    Task<byte[]> ExportDocumentMetadataAsync(Guid documentId, string userId, string format = "json");

    /// <summary>
    /// Gets document processing status
    /// </summary>
    Task<DocumentProcessingInfoDto> GetDocumentProcessingStatusAsync(Guid documentId, string userId);

    /// <summary>
    /// Retries document processing
    /// </summary>
    Task<DocumentOperationResponseDto> RetryDocumentProcessingAsync(Guid documentId, string userId);
}

/// <summary>
/// Interface for document metadata extraction service
/// </summary>
public interface IDocumentMetadataService
{
    /// <summary>
    /// Extracts metadata from a document
    /// </summary>
    Task<DocumentMetadataDto> ExtractMetadataAsync(byte[] fileContent, string fileName, string contentType);

    /// <summary>
    /// Extracts text content from a document
    /// </summary>
    Task<string> ExtractTextAsync(byte[] fileContent, string fileName, string contentType);

    /// <summary>
    /// Performs OCR on a document
    /// </summary>
    Task<string> PerformOCRAsync(byte[] fileContent, string fileName, string? language = null);

    /// <summary>
    /// Detects language of document content
    /// </summary>
    Task<List<string>> DetectLanguageAsync(string content);

    /// <summary>
    /// Extracts entities from document content
    /// </summary>
    Task<List<DocumentEntityDto>> ExtractEntitiesAsync(string content);

    /// <summary>
    /// Analyzes sentiment of document content
    /// </summary>
    Task<DocumentSentimentDto> AnalyzeSentimentAsync(string content);

    /// <summary>
    /// Generates summary of document content
    /// </summary>
    Task<string> GenerateSummaryAsync(string content, int maxLength = 500);

    /// <summary>
    /// Extracts keywords from document content
    /// </summary>
    Task<List<string>> ExtractKeywordsAsync(string content);

    /// <summary>
    /// Validates document format
    /// </summary>
    Task<bool> ValidateDocumentFormatAsync(byte[] fileContent, string fileName, string contentType);
}

/// <summary>
/// Interface for document versioning service
/// </summary>
public interface IDocumentVersioningService
{
    /// <summary>
    /// Creates a new version of a document
    /// </summary>
    Task<DocumentVersionDto> CreateVersionAsync(Guid documentId, byte[] content, string userId, string? comment = null);

    /// <summary>
    /// Gets all versions of a document
    /// </summary>
    Task<List<DocumentVersionDto>> GetVersionsAsync(Guid documentId);

    /// <summary>
    /// Gets a specific version of a document
    /// </summary>
    Task<DocumentVersionDto?> GetVersionAsync(Guid documentId, Guid versionId);

    /// <summary>
    /// Restores a specific version as current
    /// </summary>
    Task<bool> RestoreVersionAsync(Guid documentId, Guid versionId, string userId);

    /// <summary>
    /// Deletes a specific version
    /// </summary>
    Task<bool> DeleteVersionAsync(Guid documentId, Guid versionId, string userId);

    /// <summary>
    /// Compares two versions of a document
    /// </summary>
    Task<DocumentVersionComparisonDto> CompareVersionsAsync(Guid documentId, Guid version1Id, Guid version2Id);

    /// <summary>
    /// Gets version content
    /// </summary>
    Task<byte[]> GetVersionContentAsync(Guid documentId, Guid versionId);
}

/// <summary>
/// Interface for document search service
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Performs full-text search across documents
    /// </summary>
    Task<PagedResult<DocumentDto>> SearchAsync(string query, DocumentSearchDto filters, string userId);

    /// <summary>
    /// Performs semantic search using R2R
    /// </summary>
    Task<PagedResult<DocumentDto>> SemanticSearchAsync(string query, DocumentSearchDto filters, string userId);

    /// <summary>
    /// Gets search suggestions
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(string partialQuery, string userId);

    /// <summary>
    /// Indexes a document for search
    /// </summary>
    Task<bool> IndexDocumentAsync(DocumentDto document);

    /// <summary>
    /// Removes a document from search index
    /// </summary>
    Task<bool> RemoveDocumentFromIndexAsync(Guid documentId);

    /// <summary>
    /// Updates document index
    /// </summary>
    Task<bool> UpdateDocumentIndexAsync(DocumentDto document);

    /// <summary>
    /// Rebuilds search index for user
    /// </summary>
    Task<bool> RebuildUserIndexAsync(string userId);
}

/// <summary>
/// Interface for document analytics service
/// </summary>
public interface IDocumentAnalyticsService
{
    /// <summary>
    /// Tracks document activity
    /// </summary>
    Task TrackActivityAsync(Guid documentId, string userId, string activityType, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets document usage statistics
    /// </summary>
    Task<DocumentAnalyticsDto> GetUsageStatisticsAsync(Guid documentId, string userId);

    /// <summary>
    /// Gets user's document insights
    /// </summary>
    Task<List<DocumentAnalyticsDto>> GetUserInsightsAsync(string userId);

    /// <summary>
    /// Gets trending documents
    /// </summary>
    Task<List<DocumentDto>> GetTrendingDocumentsAsync(string userId);

    /// <summary>
    /// Gets document performance metrics
    /// </summary>
    Task<Dictionary<string, object>> GetPerformanceMetricsAsync(Guid documentId);
}

/// <summary>
/// DTO for document version comparison
/// </summary>
public class DocumentVersionComparisonDto
{
    public DocumentVersionDto Version1 { get; set; } = new();
    public DocumentVersionDto Version2 { get; set; } = new();
    public List<DocumentDifferenceDto> Differences { get; set; } = new();
    public double SimilarityScore { get; set; }
}

/// <summary>
/// DTO for document differences
/// </summary>
public class DocumentDifferenceDto
{
    public string Type { get; set; } = string.Empty; // "added", "removed", "modified"
    public string Section { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int? Position { get; set; }
}
