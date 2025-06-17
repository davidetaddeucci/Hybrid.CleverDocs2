using System.Text;
using System.Text.Json;
using Hybrid.CleverDocs.WebUI.Models.Collections;
using Hybrid.CleverDocs.WebUI.Models.Common;

namespace Hybrid.CleverDocs.WebUI.Services.Collections;

// DTOs for API communication - aligned with WebServices UserCollectionDto
public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "folder";
    public bool IsShared { get; set; }
    public bool IsFavorite { get; set; }
    public string? R2RCollectionId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public List<string> Tags { get; set; } = new();
    public CollectionStatsDto? Stats { get; set; }
    public CollectionPermissionsDto? Permissions { get; set; }
}

public class CollectionStatsDto
{
    public int TotalDocuments { get; set; }
    public long TotalSizeBytes { get; set; }
    public int DocumentsThisWeek { get; set; }
    public int DocumentsThisMonth { get; set; }
}

public class CollectionPermissionsDto
{
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanShare { get; set; }
    public bool CanAddDocuments { get; set; }
}

public class CollectionStatsOverviewDto
{
    public int TotalCollections { get; set; }
    public int FavoriteCollections { get; set; }
    public int SharedCollections { get; set; }
    public int TotalDocuments { get; set; }
    public int CollectionsThisWeek { get; set; }
    public int CollectionsThisMonth { get; set; }
}

public class CollectionAnalyticsDto
{
    public Guid CollectionId { get; set; }
    public int ViewsThisWeek { get; set; }
    public int ViewsThisMonth { get; set; }
    public int DocumentsAddedThisWeek { get; set; }
    public int DocumentsAddedThisMonth { get; set; }
    public List<string> TopTags { get; set; } = new();
    public List<string> RecentActivity { get; set; } = new();
}

/// <summary>
/// Enterprise-grade Collections API client with Redis caching, JWT auth, and resilience patterns
/// </summary>
public class CollectionsApiClient : ICollectionsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CollectionsApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CollectionsApiClient(
        HttpClient httpClient,
        IAuthService authService,
        ICacheService cacheService,
        ILogger<CollectionsApiClient> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _cacheService = cacheService;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region Core CRUD Operations

    public async Task<PagedResult<CollectionViewModel>> SearchCollectionsAsync(CollectionSearchViewModel search)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var cacheKey = $"collections:search:{GetSearchCacheKey(search)}";
            var cached = await _cacheService.GetAsync<PagedResult<CollectionViewModel>>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Collections search cache hit for key: {CacheKey}", cacheKey);
                return cached;
            }

            var requestBody = JsonSerializer.Serialize(new
            {
                searchTerm = search.SearchTerm,
                tags = search.Tags,
                color = search.Color,
                icon = search.Icon,
                isFavorite = search.IsFavorite,
                isShared = search.IsShared,
                createdAfter = search.CreatedAfter,
                createdBefore = search.CreatedBefore,
                minDocuments = search.MinDocuments,
                maxDocuments = search.MaxDocuments,
                sortBy = search.SortBy,
                sortDirection = search.SortDirection,
                page = search.Page,
                pageSize = search.PageSize
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/UserCollections/search", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<CollectionDto>>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = MapToPagedResult(apiResponse.Data);

                    // Cache for 5 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                    return result;
                }
            }

            _logger.LogWarning("Collections search failed with status: {StatusCode}", response.StatusCode);
            return new PagedResult<CollectionViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections");
            return new PagedResult<CollectionViewModel>();
        }
    }

    public async Task<CollectionViewModel?> GetCollectionAsync(Guid collectionId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var cacheKey = $"collection:details:{collectionId}";
            var cached = await _cacheService.GetAsync<CollectionViewModel>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Collection details cache hit for ID: {CollectionId}", collectionId);
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/{collectionId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CollectionDto>>(jsonResponse, _jsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = MapToViewModel(apiResponse.Data);
                    
                    // Cache for 10 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
                    
                    return result;
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Get collection failed with status: {StatusCode} for ID: {CollectionId}", 
                response.StatusCode, collectionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection {CollectionId}", collectionId);
            return null;
        }
    }

    public async Task<CollectionViewModel?> CreateCollectionAsync(CreateCollectionViewModel collection)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var requestBody = JsonSerializer.Serialize(new
            {
                name = collection.Name,
                description = collection.Description,
                color = collection.Color,
                icon = collection.Icon,
                tags = collection.Tags,
                setAsFavorite = collection.SetAsFavorite
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/UserCollections", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CollectionDto>>(jsonResponse, _jsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = MapToViewModel(apiResponse.Data);
                    
                    // Invalidate search cache
                    await InvalidateSearchCache();
                    
                    return result;
                }
            }

            _logger.LogWarning("Create collection failed with status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            return null;
        }
    }

    public async Task<CollectionViewModel?> UpdateCollectionAsync(Guid collectionId, UpdateCollectionViewModel collection)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var requestBody = JsonSerializer.Serialize(new
            {
                name = collection.Name,
                description = collection.Description,
                color = collection.Color,
                icon = collection.Icon,
                tags = collection.Tags
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/UserCollections/{collectionId}", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CollectionDto>>(jsonResponse, _jsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = MapToViewModel(apiResponse.Data);
                    
                    // Invalidate caches
                    await InvalidateCollectionCache(collectionId);
                    await InvalidateSearchCache();
                    
                    return result;
                }
            }

            _logger.LogWarning("Update collection failed with status: {StatusCode} for ID: {CollectionId}", 
                response.StatusCode, collectionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId}", collectionId);
            return null;
        }
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var response = await _httpClient.DeleteAsync($"/api/UserCollections/{collectionId}");
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate caches
                await InvalidateCollectionCache(collectionId);
                await InvalidateSearchCache();
                
                return true;
            }

            _logger.LogWarning("Delete collection failed with status: {StatusCode} for ID: {CollectionId}", 
                response.StatusCode, collectionId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId}", collectionId);
            return false;
        }
    }

    #endregion

    #region Favorites and Quick Actions

    public async Task<PagedResult<CollectionViewModel>> GetFavoriteCollectionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var cacheKey = $"collections:favorites:{page}:{pageSize}";
            var cached = await _cacheService.GetAsync<PagedResult<CollectionViewModel>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/favorites?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CollectionDto>>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Convert List to PagedResult for consistency
                    var result = new PagedResult<CollectionViewModel>
                    {
                        Items = apiResponse.Data.Select(MapToViewModel).ToList(),
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = apiResponse.Data.Count,
                        TotalPages = 1
                    };

                    // Cache for 3 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));

                    return result;
                }
            }

            return new PagedResult<CollectionViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite collections");
            return new PagedResult<CollectionViewModel>();
        }
    }

    public async Task<List<CollectionViewModel>> GetRecentCollectionsAsync(int limit = 10)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var cacheKey = $"collections:recent:{limit}";
            var cached = await _cacheService.GetAsync<List<CollectionViewModel>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/recent?limit={limit}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CollectionDto>>>(jsonResponse, _jsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = apiResponse.Data.Select(MapToViewModel).ToList();
                    
                    // Cache for 2 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
                    
                    return result;
                }
            }

            return new List<CollectionViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent collections");
            return new List<CollectionViewModel>();
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Guid collectionId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            
            var response = await _httpClient.PostAsync($"/api/UserCollections/{collectionId}/toggle-favorite", null);
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate relevant caches
                await InvalidateCollectionCache(collectionId);
                await InvalidateFavoritesCache();
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for collection {CollectionId}", collectionId);
            return false;
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<bool> BulkOperationAsync(BulkCollectionOperationViewModel operation)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var requestBody = JsonSerializer.Serialize(new
            {
                collectionIds = operation.CollectionIds,
                operation = operation.Operation,
                parameters = operation.Parameters
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/UserCollections/bulk-operation", content);

            if (response.IsSuccessStatusCode)
            {
                // Invalidate all relevant caches
                await InvalidateSearchCache();
                await InvalidateFavoritesCache();

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation");
            return false;
        }
    }

    public async Task<bool> ReorderCollectionsAsync(List<CollectionOrderViewModel> collections)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var requestBody = JsonSerializer.Serialize(new
            {
                collections = collections.Select(c => new { collectionId = c.CollectionId, order = c.Order })
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/UserCollections/reorder", content);

            if (response.IsSuccessStatusCode)
            {
                await InvalidateSearchCache();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering collections");
            return false;
        }
    }

    #endregion

    #region Search and Suggestions

    public async Task<List<string>> GetSearchSuggestionsAsync(string term, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return new List<string>();
            }

            await SetAuthorizationHeaderAsync();

            var cacheKey = $"collections:suggestions:{term}:{limit}";
            var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/search-suggestions?term={Uri.EscapeDataString(term)}&limit={limit}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<string>>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Cache for 5 minutes
                    await _cacheService.SetAsync(cacheKey, apiResponse.Data, TimeSpan.FromMinutes(5));

                    return apiResponse.Data;
                }
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetTagSuggestionsAsync(string term, int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return new List<string>();
            }

            await SetAuthorizationHeaderAsync();

            var cacheKey = $"collections:tag-suggestions:{term}:{limit}";
            var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/tag-suggestions?term={Uri.EscapeDataString(term)}&limit={limit}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<string>>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Cache for 10 minutes
                    await _cacheService.SetAsync(cacheKey, apiResponse.Data, TimeSpan.FromMinutes(10));

                    return apiResponse.Data;
                }
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for term: {Term}", term);
            return new List<string>();
        }
    }

    #endregion

    #region Analytics and Statistics

    public async Task<CollectionStatsOverviewViewModel> GetStatsOverviewAsync()
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var cacheKey = "collections:stats-overview";
            var cached = await _cacheService.GetAsync<CollectionStatsOverviewViewModel>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync("/api/UserCollections/stats-overview");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CollectionStatsOverviewDto>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = new CollectionStatsOverviewViewModel
                    {
                        TotalCollections = apiResponse.Data.TotalCollections,
                        FavoriteCollections = apiResponse.Data.FavoriteCollections,
                        SharedCollections = apiResponse.Data.SharedCollections,
                        TotalDocuments = apiResponse.Data.TotalDocuments,
                        CollectionsThisWeek = apiResponse.Data.CollectionsThisWeek,
                        CollectionsThisMonth = apiResponse.Data.CollectionsThisMonth
                    };

                    // Cache for 5 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                    return result;
                }
            }

            return new CollectionStatsOverviewViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections stats overview");
            return new CollectionStatsOverviewViewModel();
        }
    }

    public async Task<CollectionAnalyticsViewModel> GetCollectionAnalyticsAsync(Guid collectionId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var cacheKey = $"collection:analytics:{collectionId}";
            var cached = await _cacheService.GetAsync<CollectionAnalyticsViewModel>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync($"/api/UserCollections/{collectionId}/analytics");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<CollectionAnalyticsDto>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = new CollectionAnalyticsViewModel
                    {
                        CollectionId = apiResponse.Data.CollectionId,
                        ViewsThisWeek = apiResponse.Data.ViewsThisWeek,
                        ViewsThisMonth = apiResponse.Data.ViewsThisMonth,
                        DocumentsAddedThisWeek = apiResponse.Data.DocumentsAddedThisWeek,
                        DocumentsAddedThisMonth = apiResponse.Data.DocumentsAddedThisMonth,
                        TopTags = apiResponse.Data.TopTags,
                        RecentActivity = apiResponse.Data.RecentActivity
                    };

                    // Cache for 15 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

                    return result;
                }
            }

            return new CollectionAnalyticsViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection analytics for {CollectionId}", collectionId);
            return new CollectionAnalyticsViewModel();
        }
    }

    #endregion

    #region Import/Export and Real-time Updates

    public async Task<bool> ExportCollectionsAsync(List<Guid> collectionIds, string format = "json")
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var requestBody = JsonSerializer.Serialize(new
            {
                collectionIds = collectionIds,
                format = format
            }, _jsonOptions);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/UserCollections/export", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting collections");
            return false;
        }
    }

    public async Task<bool> ImportCollectionsAsync(byte[] fileData, string format = "json")
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(fileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", $"collections.{format}");
            content.Add(new StringContent(format), "format");

            var response = await _httpClient.PostAsync("/api/UserCollections/import", content);

            if (response.IsSuccessStatusCode)
            {
                // Invalidate all caches after import
                await InvalidateSearchCache();
                await InvalidateFavoritesCache();

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing collections");
            return false;
        }
    }

    public async Task TrackCollectionViewAsync(Guid collectionId)
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            // Fire and forget - don't wait for response
            _ = Task.Run(async () =>
            {
                try
                {
                    await _httpClient.PostAsync($"/api/UserCollections/{collectionId}/track-view", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to track collection view for {CollectionId}", collectionId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error initiating collection view tracking for {CollectionId}", collectionId);
        }
    }

    public async Task<List<CollectionViewModel>> GetSharedCollectionsAsync()
    {
        try
        {
            await SetAuthorizationHeaderAsync();

            var cacheKey = "collections:shared";
            var cached = await _cacheService.GetAsync<List<CollectionViewModel>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var response = await _httpClient.GetAsync("/api/UserCollections/shared");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CollectionDto>>>(jsonResponse, _jsonOptions);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var result = apiResponse.Data.Select(MapToViewModel).ToList();

                    // Cache for 5 minutes
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                    return result;
                }
            }

            return new List<CollectionViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared collections");
            return new List<CollectionViewModel>();
        }
    }

    #endregion

    #region Helper Methods

    private async Task SetAuthorizationHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private string GetSearchCacheKey(CollectionSearchViewModel search)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append($"{search.SearchTerm}:");
        keyBuilder.Append($"{string.Join(",", search.Tags)}:");
        keyBuilder.Append($"{search.Color}:");
        keyBuilder.Append($"{search.Icon}:");
        keyBuilder.Append($"{search.IsFavorite}:");
        keyBuilder.Append($"{search.IsShared}:");
        keyBuilder.Append($"{search.SortBy}:");
        keyBuilder.Append($"{search.SortDirection}:");
        keyBuilder.Append($"{search.Page}:");
        keyBuilder.Append($"{search.PageSize}");
        
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
    }

    private CollectionViewModel MapToViewModel(CollectionDto dto)
    {
        return new CollectionViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            DocumentCount = dto.DocumentCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            LastAccessedAt = dto.LastAccessedAt,
            Color = dto.Color,
            Icon = dto.Icon,
            IsShared = dto.IsShared,
            IsFavorite = dto.IsFavorite,
            R2RCollectionId = dto.R2RCollectionId,
            CreatedBy = dto.CreatedBy,
            TenantId = dto.TenantId,
            Tags = dto.Tags,
            Stats = new CollectionStatsViewModel
            {
                TotalDocuments = dto.Stats?.TotalDocuments ?? 0,
                TotalSizeBytes = dto.Stats?.TotalSizeBytes ?? 0,
                DocumentsThisWeek = dto.Stats?.DocumentsThisWeek ?? 0,
                DocumentsThisMonth = dto.Stats?.DocumentsThisMonth ?? 0
            },
            Permissions = new CollectionPermissionsViewModel
            {
                IsOwner = dto.Permissions?.IsOwner ?? false,
                CanEdit = dto.Permissions?.CanEdit ?? false,
                CanDelete = dto.Permissions?.CanDelete ?? false,
                CanShare = dto.Permissions?.CanShare ?? false,
                CanAddDocuments = dto.Permissions?.CanAddDocuments ?? false
            }
        };
    }

    private PagedResult<CollectionViewModel> MapToPagedResult(PagedResult<CollectionDto> dto)
    {
        return new PagedResult<CollectionViewModel>
        {
            Items = dto.Items.Select(MapToViewModel).ToList(),
            Page = dto.Page,
            PageSize = dto.PageSize,
            TotalCount = dto.TotalCount,
            TotalPages = dto.TotalPages
        };
    }

    private async Task InvalidateCollectionCache(Guid collectionId)
    {
        await _cacheService.RemoveAsync($"collection:details:{collectionId}");
    }

    private async Task InvalidateSearchCache()
    {
        await _cacheService.RemoveByPatternAsync("collections:search:*");
    }

    private async Task InvalidateFavoritesCache()
    {
        await _cacheService.RemoveByPatternAsync("collections:favorites:*");
    }

    #endregion
}
