using Hybrid.CleverDocs2.WebUI.Models.Documents;
using Hybrid.CleverDocs2.WebUI.Services.Base;
using System.Text.Json;
using System.Text;

namespace Hybrid.CleverDocs2.WebUI.Services.Documents;

/// <summary>
/// API client for document management operations
/// </summary>
public class DocumentApiClient : BaseApiClient, IDocumentApiClient
{
    private readonly ILogger<DocumentApiClient> _logger;

    public DocumentApiClient(
        HttpClient httpClient, 
        ILogger<DocumentApiClient> logger,
        IConfiguration configuration) : base(httpClient, configuration)
    {
        _logger = logger;
    }

    public async Task<PagedResult<DocumentViewModel>> SearchDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new
            {
                Page = search.Page,
                PageSize = search.PageSize,
                SearchTerm = search.SearchTerm,
                SortBy = search.SortBy,
                SortDirection = search.SortDirection,
                CollectionId = search.CollectionId,
                ContentTypes = search.SelectedContentTypes,
                Tags = search.SelectedTags,
                CreatedAfter = search.CreatedAfter,
                CreatedBefore = search.CreatedBefore,
                MinSize = search.MinSize,
                MaxSize = search.MaxSize,
                IsFavorite = search.IsFavorite,
                Filters = new Dictionary<string, object>()
            };

            var response = await PostAsync<ApiResponse<PagedDocumentResult>>(
                "api/userdocuments/search", 
                searchRequest, 
                cancellationToken);

            if (response?.Data == null)
            {
                return new PagedResult<DocumentViewModel>();
            }

            return new PagedResult<DocumentViewModel>
            {
                Items = response.Data.Items.Select(MapToViewModel).ToList(),
                TotalCount = response.Data.TotalCount,
                Page = response.Data.Page,
                PageSize = response.Data.PageSize,
                TotalPages = response.Data.TotalPages,
                HasNextPage = response.Data.HasNextPage,
                HasPreviousPage = response.Data.HasPreviousPage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return new PagedResult<DocumentViewModel>();
        }
    }

    public async Task<PagedResult<DocumentViewModel>> GetCollectionDocumentsAsync(
        Guid collectionId, 
        DocumentSearchViewModel search, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["page"] = search.Page.ToString(),
                ["pageSize"] = search.PageSize.ToString(),
                ["sortBy"] = search.SortBy,
                ["sortDirection"] = search.SortDirection.ToString()
            };

            if (!string.IsNullOrEmpty(search.SearchTerm))
                queryParams["searchTerm"] = search.SearchTerm;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var url = $"api/userdocuments/collections/{collectionId}?{queryString}";

            var response = await GetAsync<ApiResponse<PagedDocumentResult>>(url, cancellationToken);

            if (response?.Data == null)
            {
                return new PagedResult<DocumentViewModel>();
            }

            return new PagedResult<DocumentViewModel>
            {
                Items = response.Data.Items.Select(MapToViewModel).ToList(),
                TotalCount = response.Data.TotalCount,
                Page = response.Data.Page,
                PageSize = response.Data.PageSize,
                TotalPages = response.Data.TotalPages,
                HasNextPage = response.Data.HasNextPage,
                HasPreviousPage = response.Data.HasPreviousPage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection documents for collection {CollectionId}", collectionId);
            return new PagedResult<DocumentViewModel>();
        }
    }

    public async Task<DocumentViewModel?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetAsync<ApiResponse<UserDocumentDto>>($"api/userdocuments/{documentId}", cancellationToken);
            return response?.Data != null ? MapToViewModel(response.Data) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<List<DocumentViewModel>> GetDocumentsByIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await PostAsync<ApiResponse<List<UserDocumentDto>>>(
                "api/userdocuments/batch", 
                documentIds, 
                cancellationToken);

            return response?.Data?.Select(MapToViewModel).ToList() ?? new List<DocumentViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by IDs");
            return new List<DocumentViewModel>();
        }
    }

    public async Task<DocumentViewModel?> UpdateDocumentMetadataAsync(Guid documentId, DocumentEditViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateRequest = new
            {
                Name = model.Name,
                Description = model.Description,
                Tags = model.Tags,
                CollectionId = model.CollectionId,
                IsFavorite = model.IsFavorite,
                Metadata = new Dictionary<string, object>()
            };

            var response = await PutAsync<ApiResponse<UserDocumentDto>>(
                $"api/userdocuments/{documentId}/metadata", 
                updateRequest, 
                cancellationToken);

            return response?.Data != null ? MapToViewModel(response.Data) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document metadata for {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<bool> MoveDocumentAsync(Guid documentId, Guid? targetCollectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var moveRequest = new { TargetCollectionId = targetCollectionId };
            var response = await PutAsync<ApiResponse<object>>(
                $"api/userdocuments/{documentId}/move", 
                moveRequest, 
                cancellationToken);

            return response?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/userdocuments/{documentId}", cancellationToken);
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await PostAsync<ApiResponse<object>>(
                $"api/userdocuments/{documentId}/toggle-favorite", 
                null, 
                cancellationToken);

            return response?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<PagedResult<DocumentViewModel>> GetFavoriteDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["page"] = search.Page.ToString(),
                ["pageSize"] = search.PageSize.ToString(),
                ["sortBy"] = search.SortBy,
                ["sortDirection"] = search.SortDirection.ToString()
            };

            if (!string.IsNullOrEmpty(search.SearchTerm))
                queryParams["searchTerm"] = search.SearchTerm;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var url = $"api/userdocuments/favorites?{queryString}";

            var response = await GetAsync<ApiResponse<PagedDocumentResult>>(url, cancellationToken);

            if (response?.Data == null)
            {
                return new PagedResult<DocumentViewModel>();
            }

            return new PagedResult<DocumentViewModel>
            {
                Items = response.Data.Items.Select(MapToViewModel).ToList(),
                TotalCount = response.Data.TotalCount,
                Page = response.Data.Page,
                PageSize = response.Data.PageSize,
                TotalPages = response.Data.TotalPages,
                HasNextPage = response.Data.HasNextPage,
                HasPreviousPage = response.Data.HasPreviousPage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite documents");
            return new PagedResult<DocumentViewModel>();
        }
    }

    public async Task<List<DocumentViewModel>> GetRecentDocumentsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetAsync<ApiResponse<List<UserDocumentDto>>>(
                $"api/userdocuments/recent?limit={limit}", 
                cancellationToken);

            return response?.Data?.Select(MapToViewModel).ToList() ?? new List<DocumentViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent documents");
            return new List<DocumentViewModel>();
        }
    }

    public async Task<BatchOperationResult> ExecuteBatchOperationAsync(BatchOperationViewModel operation, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                Operation = operation.Operation,
                DocumentIds = operation.DocumentIds,
                TargetCollectionId = operation.TargetCollectionId,
                Tags = operation.Tags,
                Reason = operation.Reason,
                Metadata = new Dictionary<string, object>()
            };

            var response = await PostAsync<ApiResponse<BatchOperationResultDto>>(
                "api/userdocuments/batch-operation", 
                request, 
                cancellationToken);

            return new BatchOperationResult
            {
                Success = response?.Success == true,
                SuccessCount = response?.Data?.SuccessCount ?? 0,
                FailureCount = response?.Data?.FailureCount ?? 0,
                Message = response?.Message ?? "Operation completed",
                Results = response?.Data?.Results?.Select(r => new BatchOperationItemResult
                {
                    DocumentId = r.DocumentId,
                    Success = r.Success,
                    Error = r.Error
                }).ToList() ?? new List<BatchOperationItemResult>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing batch operation");
            return new BatchOperationResult
            {
                Success = false,
                Message = "Failed to execute batch operation"
            };
        }
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetAsync<ApiResponse<List<string>>>(
                $"api/userdocuments/search-suggestions?term={Uri.EscapeDataString(term)}&limit={limit}", 
                cancellationToken);

            return response?.Data ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
            return new List<string>();
        }
    }

    public async Task<bool> TrackDocumentViewAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await PostAsync<ApiResponse<object>>(
                $"api/userdocuments/{documentId}/track-view", 
                null, 
                cancellationToken);

            return response?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking document view for {DocumentId}", documentId);
            return false;
        }
    }

    // Helper method to map DTO to ViewModel
    private static DocumentViewModel MapToViewModel(UserDocumentDto dto)
    {
        return new DocumentViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            ContentType = dto.ContentType,
            Size = dto.Size,
            ThumbnailUrl = dto.ThumbnailUrl,
            PreviewUrl = dto.PreviewUrl,
            DownloadUrl = dto.DownloadUrl,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            UserId = dto.UserId,
            CollectionId = dto.CollectionId,
            CollectionName = dto.CollectionName,
            Tags = dto.Tags,
            Metadata = dto.Metadata,
            Status = (DocumentStatus)dto.Status,
            R2RDocumentId = dto.R2RDocumentId,
            IsProcessing = dto.IsProcessing,
            ProcessingProgress = dto.ProcessingProgress,
            ProcessingError = dto.ProcessingError,
            IsFavorite = dto.IsFavorite,
            ViewCount = dto.ViewCount,
            LastViewedAt = dto.LastViewedAt,
            Version = dto.Version,
            HasVersions = dto.HasVersions,
            Permissions = new DocumentPermissions
            {
                CanView = dto.Permissions.CanView,
                CanEdit = dto.Permissions.CanEdit,
                CanDelete = dto.Permissions.CanDelete,
                CanDownload = dto.Permissions.CanDownload,
                CanShare = dto.Permissions.CanShare,
                CanMove = dto.Permissions.CanMove,
                CanComment = dto.Permissions.CanComment,
                CanVersion = dto.Permissions.CanVersion
            }
        };
    }
}

// Supporting classes for API responses
public class PagedDocumentResult
{
    public List<UserDocumentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public Dictionary<string, object> Aggregations { get; set; } = new();
}

public class UserDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int Status { get; set; }
    public string? R2RDocumentId { get; set; }
    public bool IsProcessing { get; set; }
    public double? ProcessingProgress { get; set; }
    public string? ProcessingError { get; set; }
    public bool IsFavorite { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public string? Version { get; set; }
    public bool HasVersions { get; set; }
    public DocumentPermissionsDto Permissions { get; set; } = new();
}

public class DocumentPermissionsDto
{
    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanDownload { get; set; } = true;
    public bool CanShare { get; set; } = true;
    public bool CanMove { get; set; } = true;
    public bool CanComment { get; set; } = true;
    public bool CanVersion { get; set; } = true;
}

public class BatchOperationResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationItemResultDto> Results { get; set; } = new();
    public string? Message { get; set; }
    public Dictionary<string, object> Summary { get; set; } = new();
}

public class BatchOperationItemResultDto
{
    public Guid DocumentId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class BatchOperationResult
{
    public bool Success { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BatchOperationItemResult> Results { get; set; } = new();
}

public class BatchOperationItemResult
{
    public Guid DocumentId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
