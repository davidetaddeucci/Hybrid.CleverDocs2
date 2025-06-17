using Hybrid.CleverDocs.WebUI.Models.Documents;
using Hybrid.CleverDocs.WebUI.Services;
using System.Text.Json;
using System.Text;
using Hybrid.CleverDocs.WebUI.Models.Shared;

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
        try
        {
            await SetAuthorizationHeaderAsync();

            var queryParams = new Dictionary<string, string>
            {
                ["page"] = search.Page.ToString(),
                ["pageSize"] = search.PageSize.ToString(),
                ["sortBy"] = search.SortBy ?? "updated_at",
                ["sortDirection"] = search.SortDirection.ToString()
            };

            if (!string.IsNullOrEmpty(search.SearchTerm))
                queryParams["searchTerm"] = search.SearchTerm;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var url = $"api/UserDocuments/collections/{collectionId}?{queryString}";

            _logger.LogInformation("Calling collection documents API: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Collection documents API returned {StatusCode}: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return new PagedResult<DocumentViewModel>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedDocumentResultDto>>(content, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("Collection documents API returned null data");
                return new PagedResult<DocumentViewModel>();
            }

            var result = new PagedResult<DocumentViewModel>
            {
                Items = apiResponse.Data.Items.Select(MapToDocumentViewModel).ToList(),
                TotalCount = apiResponse.Data.TotalCount,
                Page = apiResponse.Data.Page,
                PageSize = apiResponse.Data.PageSize,
                TotalPages = apiResponse.Data.TotalPages,
                HasNextPage = apiResponse.Data.HasNextPage,
                HasPreviousPage = apiResponse.Data.HasPreviousPage
            };

            _logger.LogInformation("Retrieved {Count} documents for collection {CollectionId}",
                result.Items.Count, collectionId);

            return result;
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
        try
        {
            await SetAuthorizationHeaderAsync();

            _logger.LogInformation("Calling delete document API for document {DocumentId}", documentId);

            var response = await _httpClient.DeleteAsync($"api/UserDocuments/{documentId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Document {DocumentId} deleted successfully via API", documentId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete document {DocumentId}. Status: {StatusCode}, Content: {Content}",
                    documentId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }

    public async Task<DocumentViewModel?> UploadDocumentAsync(DocumentUploadViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            _logger.LogInformation("Starting enterprise upload for file: {FileName}", model.File?.FileName);

            // Step 1: Initialize upload session
            var initRequest = new InitializeUploadSessionDto
            {
                Files = new List<FileInfoDto>
                {
                    new FileInfoDto
                    {
                        FileName = model.File!.FileName,
                        Size = model.File.Length,
                        ContentType = model.File.ContentType,
                        LastModified = DateTime.UtcNow
                    }
                },
                CollectionId = model.CollectionId,
                Tags = model.Tags,
                Options = new UploadOptionsDto
                {
                    ExtractMetadata = true,
                    PerformOCR = true,
                    AutoDetectLanguage = true,
                    GenerateThumbnails = true,
                    EnableVersioning = true
                }
            };

            var initResponse = await _httpClient.PostAsJsonAsync("/api/documentupload/initialize", initRequest, cancellationToken);

            if (!initResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to initialize upload session. Status: {StatusCode}", initResponse.StatusCode);
                return null;
            }

            var initContent = await initResponse.Content.ReadAsStringAsync(cancellationToken);
            var initResult = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<DocumentUploadSessionDto>>(initContent, _jsonOptions);

            if (initResult?.Success != true || initResult.Data?.SessionId == null)
            {
                _logger.LogError("Invalid response from upload session initialization");
                return null;
            }

            var sessionId = initResult.Data.SessionId;
            _logger.LogInformation("Upload session initialized: {SessionId}", sessionId);

            // Step 2: Upload file
            using var uploadContent = new MultipartFormDataContent();

            using var fileStream = model.File.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.File.ContentType);
            uploadContent.Add(streamContent, "file", model.File.FileName);
            uploadContent.Add(new StringContent(sessionId.ToString()), "sessionId");

            var uploadResponse = await _httpClient.PostAsync("/api/documentupload/file", uploadContent, cancellationToken);

            if (uploadResponse.IsSuccessStatusCode)
            {
                var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                var uploadResult = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UploadResponseDto>>(uploadResponseContent, _jsonOptions);

                if (uploadResult?.Success == true && uploadResult.Data != null)
                {
                    _logger.LogInformation("File uploaded successfully via enterprise system: {FileName}", model.File.FileName);

                    // Create DocumentViewModel from upload result
                    return CreateDocumentViewModelFromUpload(uploadResult.Data, model);
                }
            }

            _logger.LogWarning("Enterprise upload failed with status: {StatusCode}", uploadResponse.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in enterprise upload for file: {FileName}", model.File?.FileName);
            return null;
        }
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

    public async Task<string?> GetDocumentDownloadUrlAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            _logger.LogInformation("Getting download URL for document {DocumentId}", documentId);

            var response = await _httpClient.GetAsync($"api/UserDocuments/{documentId}/download-url", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, _jsonOptions);

                if (apiResponse?.Success == true && !string.IsNullOrEmpty(apiResponse.Data))
                {
                    return apiResponse.Data;
                }
            }

            _logger.LogWarning("Failed to get download URL for document {DocumentId}: {StatusCode}",
                documentId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URL for document {DocumentId}", documentId);
            return null;
        }
    }

    // Advanced Search Methods Implementation

    public async Task<List<string>> GetDocumentNameSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/suggestions/names?term={Uri.EscapeDataString(term)}&limit={limit}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document name suggestions for term: {Term}", term);
        }
        return new List<string>();
    }

    public async Task<List<string>> GetContentSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/suggestions/content?term={Uri.EscapeDataString(term)}&limit={limit}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content suggestions for term: {Term}", term);
        }
        return new List<string>();
    }

    public async Task<List<string>> GetTagSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/suggestions/tags?term={Uri.EscapeDataString(term)}&limit={limit}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for term: {Term}", term);
        }
        return new List<string>();
    }

    public async Task<List<string>> GetAuthorSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/suggestions/authors?term={Uri.EscapeDataString(term)}&limit={limit}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting author suggestions for term: {Term}", term);
        }
        return new List<string>();
    }

    // Saved Search Methods Implementation

    public async Task<bool> SaveSearchAsync(SavedSearchItem savedSearch, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(savedSearch, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/documents/saved-searches", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving search: {SearchName}", savedSearch.Name);
            return false;
        }
    }

    public async Task<List<SavedSearchItem>> GetSavedSearchesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/documents/saved-searches", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<SavedSearchItem>>(content, _jsonOptions) ?? new List<SavedSearchItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved searches");
        }
        return new List<SavedSearchItem>();
    }

    public async Task<SavedSearchItem?> GetSavedSearchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/saved-searches/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<SavedSearchItem>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved search: {Id}", id);
        }
        return null;
    }

    public async Task<bool> UpdateSavedSearchUsageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/documents/saved-searches/{id}/usage", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saved search usage: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteSavedSearchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/documents/saved-searches/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved search: {Id}", id);
            return false;
        }
    }

    // Search History Methods Implementation

    public async Task<List<SearchHistoryItem>> GetSearchHistoryAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/documents/search-history?limit={limit}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<SearchHistoryItem>>(content, _jsonOptions) ?? new List<SearchHistoryItem>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history");
        }
        return new List<SearchHistoryItem>();
    }

    public async Task<bool> RecordSearchAsync(SearchHistoryItem historyItem, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(historyItem, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/documents/search-history", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search history");
            return false;
        }
    }

    public async Task<bool> ClearSearchHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/documents/search-history", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history");
            return false;
        }
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
            Status = MapDocumentStatus(dto.Status.ToString()),
            CollectionId = dto.CollectionId,
            Tags = dto.Tags ?? new List<string>(),
            IsFavorite = dto.IsFavorite,
            IsProcessing = dto.Status.ToString().ToLower() == "processing",
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            R2RDocumentId = dto.R2RDocumentId,
            Permissions = new DocumentPermissions
            {
                CanEdit = true, // TODO: Map from actual permissions
                CanDelete = true,
                CanShare = true,
                CanDownload = true
            }
        };
    }

    private static DocumentStatus MapDocumentStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "uploaded" => DocumentStatus.Processing,
            "processing" => DocumentStatus.Processing,
            "processed" => DocumentStatus.Ready,
            "failed" => DocumentStatus.Error,
            "ready" => DocumentStatus.Ready,
            "error" => DocumentStatus.Error,
            "draft" => DocumentStatus.Draft,
            "archived" => DocumentStatus.Archived,
            "deleted" => DocumentStatus.Deleted,
            _ => DocumentStatus.Draft
        };
    }

    // Helper method to create DocumentViewModel from enterprise upload result
    private static DocumentViewModel CreateDocumentViewModelFromUpload(UploadResponseDto uploadResult, DocumentUploadViewModel originalModel)
    {
        var fileInfo = uploadResult.Files.FirstOrDefault();

        return new DocumentViewModel
        {
            Id = fileInfo?.FileId ?? Guid.NewGuid(),
            Name = originalModel.Title ?? Path.GetFileNameWithoutExtension(originalModel.File!.FileName),
            Description = originalModel.Description,
            ContentType = originalModel.File.ContentType,
            Size = originalModel.File.Length,
            Status = MapFileUploadStatusToDocumentStatus(fileInfo?.Status ?? FileUploadStatusDto.Completed),
            CollectionId = originalModel.CollectionId,
            Tags = originalModel.Tags,
            IsFavorite = originalModel.IsFavorite,
            IsProcessing = fileInfo?.Status == FileUploadStatusDto.Processing,
            CreatedAt = fileInfo?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Permissions = new DocumentPermissions
            {
                CanEdit = true,
                CanDelete = true,
                CanShare = true,
                CanDownload = true
            },
            // Store session ID for tracking
            Metadata = new Dictionary<string, object>
            {
                ["SessionId"] = uploadResult.SessionId?.ToString() ?? "",
                ["UploadMethod"] = "Enterprise"
            }
        };
    }

    // Helper method to map FileUploadStatusDto to DocumentStatus
    private static DocumentStatus MapFileUploadStatusToDocumentStatus(FileUploadStatusDto status)
    {
        return status switch
        {
            FileUploadStatusDto.Pending => DocumentStatus.Draft,
            FileUploadStatusDto.Uploading => DocumentStatus.Processing,
            FileUploadStatusDto.Uploaded => DocumentStatus.Processing,
            FileUploadStatusDto.Processing => DocumentStatus.Processing,
            FileUploadStatusDto.Completed => DocumentStatus.Ready,
            FileUploadStatusDto.Failed => DocumentStatus.Error,
            FileUploadStatusDto.Cancelled => DocumentStatus.Archived,
            _ => DocumentStatus.Draft
        };
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

// Note: Upload DTOs are now imported from Hybrid.CleverDocs2.WebServices.Models.Documents


