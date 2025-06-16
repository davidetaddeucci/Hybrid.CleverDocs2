using Hybrid.CleverDocs.WebUI.Models.Documents;
using Hybrid.CleverDocs.WebUI.Services;
using System.Text.Json;
using System.Text;

namespace Hybrid.CleverDocs.WebUI.Services.Documents;

// Backend DTO classes for API communication
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
    public Guid UserId { get; set; }
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public BackendDocumentStatus Status { get; set; }
    public string? R2RDocumentId { get; set; }
    public bool IsProcessing { get; set; }
    public double? ProcessingProgress { get; set; }
    public string? ProcessingError { get; set; }
    public bool IsFavorite { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public string? Version { get; set; }
    public bool HasVersions { get; set; }
    public BackendDocumentPermissions Permissions { get; set; } = new();
}

public class PagedDocumentResultDto
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

public class BackendDocumentPermissions
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

public enum BackendDocumentStatus
{
    Draft = 0,
    Processing = 1,
    Ready = 2,
    Error = 3,
    Archived = 4,
    Deleted = 5
}



/// <summary>
/// API client for document management operations
/// </summary>
public class DocumentApiClient : IDocumentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentApiClient> _logger;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentApiClient(HttpClient httpClient, ILogger<DocumentApiClient> logger, IAuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<PagedResult<DocumentViewModel>> SearchDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default)
    {
        try
        {
            // Set authorization header
            await SetAuthorizationHeaderAsync();

            // Map frontend DocumentSearchViewModel to backend DocumentQueryDto
            var query = new
            {
                page = search.Page,
                pageSize = search.PageSize,
                searchTerm = search.SearchTerm,
                sortBy = search.SortBy,
                sortDirection = (int)search.SortDirection, // Convert enum to int
                collectionId = search.CollectionId,
                tags = search.SelectedTags,
                contentTypes = search.SelectedContentTypes,
                createdAfter = search.CreatedAfter,
                createdBefore = search.CreatedBefore,
                minSize = search.MinSize,
                maxSize = search.MaxSize,
                isFavorite = search.IsFavorite,
                includeMetadata = true,
                includePermissions = true
            };

            var json = JsonSerializer.Serialize(query, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling POST /api/UserDocuments/search with query: {Query}", json);

            var response = await _httpClient.PostAsync("api/UserDocuments/search", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Search API response: {Response}", responseContent);

                // Backend returns ApiResponse<PagedDocumentResultDto>
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedDocumentResultDto>>(responseContent, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Map backend PagedDocumentResultDto to frontend PagedResult<DocumentViewModel>
                    var result = new PagedResult<DocumentViewModel>
                    {
                        Items = apiResponse.Data.Items.Select(MapToDocumentViewModel).ToList(),
                        Page = apiResponse.Data.Page,
                        PageSize = apiResponse.Data.PageSize,
                        TotalCount = apiResponse.Data.TotalCount,
                        TotalPages = apiResponse.Data.TotalPages
                    };

                    return result;
                }
            }

            _logger.LogWarning("Search documents API call failed with status: {StatusCode}", response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Error response: {ErrorContent}", errorContent);

            return new PagedResult<DocumentViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return new PagedResult<DocumentViewModel>();
        }
    }

    public async Task<PagedResult<DocumentViewModel>> GetCollectionDocumentsAsync(Guid collectionId, DocumentSearchViewModel search, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new PagedResult<DocumentViewModel>();
    }

    public async Task<DocumentViewModel?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Set authorization header
            await SetAuthorizationHeaderAsync();

            _logger.LogInformation("Calling GET /api/UserDocuments/{DocumentId}", documentId);

            var response = await _httpClient.GetAsync($"api/UserDocuments/{documentId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Get document API response: {Response}", responseContent);

                // Backend returns ApiResponse<UserDocumentDto>
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserDocumentDto>>(responseContent, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return MapToDocumentViewModel(apiResponse.Data);
                }
            }

            _logger.LogWarning("Get document API call failed with status: {StatusCode}", response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Error response: {ErrorContent}", errorContent);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<List<DocumentViewModel>> GetDocumentsByIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new List<DocumentViewModel>();
    }

    public async Task<DocumentViewModel?> UpdateDocumentMetadataAsync(Guid documentId, DocumentEditViewModel model, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> MoveDocumentAsync(Guid documentId, Guid? targetCollectionId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> ToggleFavoriteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }

    public async Task<PagedResult<DocumentViewModel>> GetFavoriteDocumentsAsync(DocumentSearchViewModel search, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new PagedResult<DocumentViewModel>();
    }

    public async Task<List<DocumentViewModel>> GetRecentDocumentsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new List<DocumentViewModel>();
    }

    public async Task<BatchOperationResult> ExecuteBatchOperationAsync(BatchOperationViewModel operation, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new BatchOperationResult { Success = false };
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return new List<string>();
    }

    public async Task<bool> TrackDocumentViewAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }

    // Helper method to map backend DTO to frontend ViewModel
    private static DocumentViewModel MapToDocumentViewModel(UserDocumentDto dto)
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
            UserId = dto.UserId.ToString(),
            CollectionId = dto.CollectionId,
            CollectionName = dto.CollectionName,
            Tags = dto.Tags,
            Metadata = dto.Metadata,
            Status = (Models.Documents.DocumentStatus)dto.Status,
            R2RDocumentId = dto.R2RDocumentId,
            IsProcessing = dto.IsProcessing,
            ProcessingProgress = dto.ProcessingProgress,
            ProcessingError = dto.ProcessingError,
            IsFavorite = dto.IsFavorite,
            ViewCount = dto.ViewCount,
            LastViewedAt = dto.LastViewedAt,
            Version = dto.Version,
            HasVersions = dto.HasVersions,
            Permissions = new Models.Documents.DocumentPermissions
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

    /// <summary>
    /// Sets the Authorization header with JWT token from authentication service
    /// </summary>
    private async Task SetAuthorizationHeaderAsync()
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Authorization header set with JWT token");
            }
            else
            {
                _logger.LogWarning("No JWT token available for API call");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting authorization header");
        }
    }
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


