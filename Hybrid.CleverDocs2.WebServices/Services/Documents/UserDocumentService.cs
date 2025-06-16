using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service for user document management operations
/// </summary>
public class UserDocumentService : IUserDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<UserDocumentService> _logger;
    private readonly ICorrelationService _correlationService;

    /// <summary>
    /// Helper method to convert string userId to Guid for database compatibility
    /// </summary>
    private Guid ConvertUserIdToGuid(string userId)
    {
        if (Guid.TryParse(userId, out var guidUserId))
        {
            return guidUserId;
        }
        // For backward compatibility, if it's not a valid Guid, return empty Guid
        return Guid.Empty;
    }

    public UserDocumentService(
        ApplicationDbContext context,
        IMultiLevelCacheService cacheService,
        ILogger<UserDocumentService> logger,
        ICorrelationService correlationService)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
    }

    public async Task<PagedDocumentResultDto> SearchDocumentsAsync(DocumentQueryDto query, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Searching documents with query: {Query}, CorrelationId: {CorrelationId}", 
                JsonSerializer.Serialize(query), correlationId);

            // Build cache key
            var cacheKey = $"documents:search:{GenerateQueryHash(query)}";
            
            // Try cache first
            var cachedResult = await _cacheService.GetAsync<PagedDocumentResultDto>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Returning cached search results, CorrelationId: {CorrelationId}", correlationId);
                return cachedResult;
            }

            // Build query
            var documentsQuery = _context.Documents.AsQueryable();

            // Apply user filter
            if (query.UserId.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.UserId == query.UserId.Value);
            }

            // Apply collection filter
            if (query.CollectionId.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.CollectionId == query.CollectionId.Value);
            }

            // Apply search term
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                documentsQuery = documentsQuery.Where(d => 
                    d.Name.ToLower().Contains(searchTerm) ||
                    (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                    d.Tags.Any(t => t.ToLower().Contains(searchTerm)));
            }

            // Apply content type filters
            if (query.ContentTypes.Any())
            {
                documentsQuery = documentsQuery.Where(d => query.ContentTypes.Contains(d.ContentType));
            }

            // Apply status filters
            if (query.Statuses.Any())
            {
                var statusValues = query.Statuses.Cast<int>().ToList();
                documentsQuery = documentsQuery.Where(d => statusValues.Contains((int)d.Status));
            }

            // Apply date filters
            if (query.CreatedAfter.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.CreatedAt >= query.CreatedAfter.Value);
            }

            if (query.CreatedBefore.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.CreatedAt <= query.CreatedBefore.Value);
            }

            if (query.UpdatedAfter.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.UpdatedAt >= query.UpdatedAfter.Value);
            }

            if (query.UpdatedBefore.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.UpdatedAt <= query.UpdatedBefore.Value);
            }

            // Apply size filters
            if (query.MinSize.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.Size >= query.MinSize.Value);
            }

            if (query.MaxSize.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.Size <= query.MaxSize.Value);
            }

            // Apply favorite filter
            if (query.IsFavorite.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.IsFavorite == query.IsFavorite.Value);
            }

            // Apply tags filter
            if (query.Tags.Any())
            {
                foreach (var tag in query.Tags)
                {
                    documentsQuery = documentsQuery.Where(d => d.Tags.Contains(tag));
                }
            }

            // Apply additional filters from Filters dictionary
            documentsQuery = ApplyAdvancedFilters(documentsQuery, query.Filters);

            // Get total count before pagination
            var totalCount = await documentsQuery.CountAsync(cancellationToken);

            // Apply sorting
            documentsQuery = ApplySorting(documentsQuery, query.SortBy, query.SortDirection);

            // Apply pagination
            var skip = (query.Page - 1) * query.PageSize;
            var documents = await documentsQuery
                .Skip(skip)
                .Take(query.PageSize)
                .Select(d => new UserDocumentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ContentType = d.ContentType,
                    Size = d.Size,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    UserId = d.UserId,
                    CollectionId = d.CollectionId,
                    Tags = d.Tags,
                    Status = (DocumentStatus)d.Status,
                    R2RDocumentId = d.R2RDocumentId,
                    IsFavorite = d.IsFavorite,
                    ViewCount = d.ViewCount,
                    LastViewedAt = d.LastViewedAt,
                    Version = d.Version,
                    HasVersions = d.HasVersions,
                    Metadata = query.IncludeMetadata ? d.Metadata : new Dictionary<string, object>(),
                    Permissions = query.IncludePermissions ? GetDocumentPermissions(d, query.UserId) : new DocumentPermissions()
                })
                .ToListAsync(cancellationToken);

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var hasNextPage = query.Page < totalPages;
            var hasPreviousPage = query.Page > 1;

            // Build result
            var result = new PagedDocumentResultDto
            {
                Items = documents,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                Aggregations = await BuildAggregations(documentsQuery, cancellationToken)
            };

            // Cache result
            await _cacheService.SetAsync(cacheKey, result, new CacheOptions
            {
                L1TTL = TimeSpan.FromMinutes(5),
                L2TTL = TimeSpan.FromMinutes(15),
                UseL3Cache = true
            });

            _logger.LogInformation("Document search completed: {TotalCount} results, CorrelationId: {CorrelationId}", 
                totalCount, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents, CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<PagedDocumentResultDto> GetCollectionDocumentsAsync(Guid collectionId, Guid userId, DocumentQueryDto query, CancellationToken cancellationToken = default)
    {
        query.CollectionId = collectionId;
        query.UserId = userId; // Now both are Guid
        return await SearchDocumentsAsync(query, cancellationToken);
    }

    public async Task<UserDocumentDto?> GetDocumentByIdAsync(Guid documentId, Guid userId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var cacheKey = $"document:{documentId}:{userId}";
            
            var cachedDocument = await _cacheService.GetAsync<UserDocumentDto>(cacheKey);
            if (cachedDocument != null)
            {
                return cachedDocument;
            }

            var document = await _context.Documents
                .Where(d => d.Id == documentId && d.UserId == userId)
                .Select(d => new UserDocumentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ContentType = d.ContentType,
                    Size = d.Size,
                    ThumbnailUrl = GenerateThumbnailUrl(d.Id),
                    PreviewUrl = GeneratePreviewUrl(d.Id),
                    DownloadUrl = GenerateDownloadUrl(d.Id),
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    UserId = d.UserId,
                    CollectionId = d.CollectionId,
                    Tags = d.Tags,
                    Metadata = d.Metadata,
                    Status = (DocumentStatus)d.Status,
                    R2RDocumentId = d.R2RDocumentId,
                    IsProcessing = d.IsProcessing,
                    ProcessingProgress = d.ProcessingProgress,
                    ProcessingError = d.ProcessingError,
                    IsFavorite = d.IsFavorite,
                    ViewCount = d.ViewCount,
                    LastViewedAt = d.LastViewedAt,
                    Version = d.Version,
                    HasVersions = d.HasVersions,
                    Permissions = GetDocumentPermissions(d, userId)
                })
                .FirstOrDefaultAsync();

            if (document != null)
            {
                // Add collection name if available
                if (document.CollectionId.HasValue)
                {
                    var collection = await _context.Collections
                        .Where(c => c.Id == document.CollectionId.Value)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync();
                    document.CollectionName = collection;
                }

                await _cacheService.SetAsync(cacheKey, document, new CacheOptions
                {
                    L1TTL = TimeSpan.FromMinutes(10),
                    L2TTL = TimeSpan.FromMinutes(30)
                });
            }

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId} for user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            throw;
        }
    }

    public async Task<List<UserDocumentDto>> GetDocumentsByIdsAsync(List<Guid> documentIds, Guid userId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            var documents = await _context.Documents
                .Where(d => documentIds.Contains(d.Id) && d.UserId == userId)
                .Select(d => new UserDocumentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ContentType = d.ContentType,
                    Size = d.Size,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    UserId = d.UserId,
                    CollectionId = d.CollectionId,
                    Tags = d.Tags,
                    Status = (DocumentStatus)d.Status,
                    R2RDocumentId = d.R2RDocumentId,
                    IsFavorite = d.IsFavorite,
                    ViewCount = d.ViewCount,
                    LastViewedAt = d.LastViewedAt,
                    Version = d.Version,
                    HasVersions = d.HasVersions,
                    Permissions = GetDocumentPermissions(d, userId)
                })
                .ToListAsync();

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by IDs for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            throw;
        }
    }

    // Helper methods
    private IQueryable<Data.Entities.Document> ApplyAdvancedFilters(IQueryable<Data.Entities.Document> query, Dictionary<string, object> filters)
    {
        foreach (var filter in filters)
        {
            switch (filter.Key.ToLower())
            {
                case "content_type":
                    if (filter.Value is string contentType)
                    {
                        query = query.Where(d => d.ContentType == contentType);
                    }
                    break;
                case "has_thumbnail":
                    if (filter.Value is bool hasThumbnail)
                    {
                        query = query.Where(d => d.HasThumbnail == hasThumbnail);
                    }
                    break;
                case "processing_status":
                    if (filter.Value is string processingStatus)
                    {
                        var status = Enum.Parse<DocumentStatus>(processingStatus, true);
                        query = query.Where(d => d.Status == (int)status);
                    }
                    break;
            }
        }

        return query;
    }

    private IQueryable<Data.Entities.Document> ApplySorting(IQueryable<Data.Entities.Document> query, string sortBy, SortDirection direction)
    {
        return sortBy.ToLower() switch
        {
            "name" => direction == SortDirection.Asc ? query.OrderBy(d => d.Name) : query.OrderByDescending(d => d.Name),
            "size" => direction == SortDirection.Asc ? query.OrderBy(d => d.Size) : query.OrderByDescending(d => d.Size),
            "type" => direction == SortDirection.Asc ? query.OrderBy(d => d.ContentType) : query.OrderByDescending(d => d.ContentType),
            "created_at" => direction == SortDirection.Asc ? query.OrderBy(d => d.CreatedAt) : query.OrderByDescending(d => d.CreatedAt),
            "view_count" => direction == SortDirection.Asc ? query.OrderBy(d => d.ViewCount) : query.OrderByDescending(d => d.ViewCount),
            _ => direction == SortDirection.Asc ? query.OrderBy(d => d.UpdatedAt) : query.OrderByDescending(d => d.UpdatedAt)
        };
    }

    private async Task<Dictionary<string, object>> BuildAggregations(IQueryable<Data.Entities.Document> query, CancellationToken cancellationToken)
    {
        var aggregations = new Dictionary<string, object>();

        try
        {
            // Content type distribution
            var contentTypes = await query
                .GroupBy(d => d.ContentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            aggregations["content_types"] = contentTypes.ToDictionary(ct => ct.Type, ct => (object)ct.Count);

            // Size distribution
            var totalSize = await query.SumAsync(d => d.Size, cancellationToken);
            aggregations["total_size"] = totalSize;

            // Status distribution
            var statuses = await query
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            aggregations["statuses"] = statuses.ToDictionary(s => ((DocumentStatus)s.Status).ToString(), s => (object)s.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building aggregations");
        }

        return aggregations;
    }

    private static DocumentPermissions GetDocumentPermissions(Data.Entities.Document document, Guid? userId)
    {
        // Basic permission logic - can be extended based on business rules
        var isOwner = document.UserId == userId;

        return new DocumentPermissions
        {
            CanView = true,
            CanEdit = isOwner,
            CanDelete = isOwner,
            CanDownload = true,
            CanShare = isOwner,
            CanMove = isOwner,
            CanComment = true,
            CanVersion = isOwner
        };
    }

    private string GenerateQueryHash(DocumentQueryDto query)
    {
        var json = JsonSerializer.Serialize(query);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)).Replace("/", "_").Replace("+", "-");
    }

    private string? GenerateThumbnailUrl(Guid documentId)
    {
        return $"/api/documents/{documentId}/thumbnail";
    }

    private string? GeneratePreviewUrl(Guid documentId)
    {
        return $"/api/documents/{documentId}/preview";
    }

    private string? GenerateDownloadUrl(Guid documentId)
    {
        return $"/api/documents/{documentId}/download";
    }

    // Placeholder implementations for remaining interface methods
    public async Task<UserDocumentDto?> UpdateDocumentMetadataAsync(Guid documentId, Guid userId, DocumentMetadataUpdateDto update)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> MoveDocumentToCollectionAsync(Guid documentId, Guid userId, Guid? targetCollectionId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> ToggleFavoriteAsync(Guid documentId, Guid userId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return false;
    }

    public async Task<PagedDocumentResultDto> GetFavoriteDocumentsAsync(Guid userId, DocumentQueryDto query)
    {
        query.IsFavorite = true;
        query.UserId = userId; // Now both are Guid
        return await SearchDocumentsAsync(query);
    }

    public async Task<List<UserDocumentDto>> GetRecentDocumentsAsync(Guid userId, int limit = 10)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return new List<UserDocumentDto>();
    }

    public async Task<BatchOperationResultDto> ExecuteBatchOperationAsync(BatchOperationRequestDto request)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return new BatchOperationResultDto();
    }

    public async Task<DocumentAnalyticsDto> GetDocumentAnalyticsAsync(Guid documentId, Guid userId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return new DocumentAnalyticsDto();
    }

    public async Task TrackDocumentViewAsync(Guid documentId, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        // Implementation placeholder
        await Task.CompletedTask;
    }

    public async Task TrackDocumentDownloadAsync(Guid documentId, Guid userId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string term, Guid? userId = null, int limit = 10)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return new List<string>();
    }

    public async Task<DocumentSearchSuggestionsDto> GetAdvancedSearchSuggestionsAsync(string term, Guid? userId = null)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return new DocumentSearchSuggestionsDto();
    }

    public async Task<byte[]> ExportDocumentsAsync(DocumentExportRequestDto request, Guid userId)
    {
        // Implementation placeholder
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    // Additional placeholder implementations for remaining interface methods...
    // (These would be implemented in subsequent iterations)

    public async Task<string?> GetDocumentDownloadUrlAsync(Guid documentId, Guid userId, TimeSpan? expiration = null) => await Task.FromResult<string?>(null);
    public async Task<string?> GetDocumentPreviewUrlAsync(Guid documentId, Guid userId, TimeSpan? expiration = null) => await Task.FromResult<string?>(null);
    public async Task<string?> GetDocumentThumbnailUrlAsync(Guid documentId, Guid userId, TimeSpan? expiration = null) => await Task.FromResult<string?>(null);
    public async Task<DocumentPermissions> GetDocumentPermissionsAsync(Guid documentId, Guid userId) => await Task.FromResult(new DocumentPermissions());
    public async Task<List<UserDocumentDto>> GetDocumentVersionsAsync(Guid documentId, Guid userId) => await Task.FromResult(new List<UserDocumentDto>());
    public async Task<UserDocumentDto?> CreateDocumentVersionAsync(Guid documentId, Guid userId, byte[] content, string? versionNote = null) => await Task.FromResult<UserDocumentDto?>(null);
    public async Task<bool> RestoreDocumentVersionAsync(Guid documentId, Guid userId, string version) => await Task.FromResult(false);
    public async Task<byte[]?> GetDocumentContentAsync(Guid documentId, Guid userId) => await Task.FromResult<byte[]?>(null);
    public async Task<string?> GetDocumentTextContentAsync(Guid documentId, Guid userId) => await Task.FromResult<string?>(null);
    public async Task<bool> UpdateDocumentTagsAsync(Guid documentId, Guid userId, List<string> tags) => await Task.FromResult(false);
    public async Task<List<string>> GetAvailableTagsAsync(Guid userId) => await Task.FromResult(new List<string>());
    public async Task<Dictionary<string, object>> GetUserDocumentStatisticsAsync(Guid userId) => await Task.FromResult(new Dictionary<string, object>());
    public async Task<Dictionary<string, object>> GetCollectionDocumentStatisticsAsync(Guid collectionId, Guid userId) => await Task.FromResult(new Dictionary<string, object>());
    public async Task<UserDocumentDto?> DuplicateDocumentAsync(Guid documentId, Guid userId, string? newName = null, Guid? targetCollectionId = null) => await Task.FromResult<UserDocumentDto?>(null);
    public async Task<bool> ArchiveDocumentAsync(Guid documentId, Guid userId) => await Task.FromResult(false);
    public async Task<bool> RestoreDocumentAsync(Guid documentId, Guid userId) => await Task.FromResult(false);
    public async Task<PagedDocumentResultDto> GetArchivedDocumentsAsync(Guid userId, DocumentQueryDto query) => await Task.FromResult(new PagedDocumentResultDto());
    public async Task<bool> PermanentlyDeleteDocumentAsync(Guid documentId, Guid userId) => await Task.FromResult(false);
    public async Task<List<UserDocumentDto>> GetPendingProcessingDocumentsAsync(Guid userId) => await Task.FromResult(new List<UserDocumentDto>());
    public async Task<bool> RetryDocumentProcessingAsync(Guid documentId, Guid userId) => await Task.FromResult(false);
    public async Task<Dictionary<string, object>> GetDocumentProcessingStatusAsync(Guid documentId, Guid userId) => await Task.FromResult(new Dictionary<string, object>());
    public async Task<string> ShareDocumentAsync(Guid documentId, Guid userId, List<string> targetUserIds, DocumentPermissions permissions, TimeSpan? expiration = null) => await Task.FromResult(string.Empty);
    // Additional interface methods implementations
    public async Task<PagedDocumentResultDto> GetSharedDocumentsAsync(Guid userId, DocumentQueryDto query) => await Task.FromResult(new PagedDocumentResultDto());
    public async Task<PagedDocumentResultDto> GetSharedWithMeDocumentsAsync(Guid userId, DocumentQueryDto query) => await Task.FromResult(new PagedDocumentResultDto());
    public async Task<bool> RevokeDocumentSharingAsync(Guid documentId, Guid userId, string? targetUserId = null) => await Task.FromResult(false);
    public async Task<Dictionary<string, object>> GetDocumentSharingInfoAsync(Guid documentId, Guid userId) => await Task.FromResult(new Dictionary<string, object>());
    public async Task<bool> ValidateDocumentAccessAsync(Guid documentId, Guid userId, DocumentAction action) => await Task.FromResult(false);
    public async Task<List<Dictionary<string, object>>> GetDocumentActivityLogAsync(Guid documentId, Guid userId, int limit = 50) => await Task.FromResult(new List<Dictionary<string, object>>());
    public async Task<Dictionary<string, object>> AddDocumentCommentAsync(Guid documentId, Guid userId, string comment) => await Task.FromResult(new Dictionary<string, object>());
    public async Task<List<Dictionary<string, object>>> GetDocumentCommentsAsync(Guid documentId, Guid userId) => await Task.FromResult(new List<Dictionary<string, object>>());
    public async Task<bool> UpdateDocumentCommentAsync(Guid commentId, Guid userId, string comment) => await Task.FromResult(false);
    public async Task<bool> DeleteDocumentCommentAsync(Guid commentId, Guid userId) => await Task.FromResult(false);
}
