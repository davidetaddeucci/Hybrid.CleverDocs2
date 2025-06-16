using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs.WebUI.Models.Collections;
using Hybrid.CleverDocs.WebUI.Models.Common;
using Hybrid.CleverDocs.WebUI.Services.Collections;
using Hybrid.CleverDocs.WebUI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hybrid.CleverDocs.WebUI.Controllers;

/// <summary>
/// Enterprise-grade MVC Controller for Collections management with Redis caching and RabbitMQ integration
/// </summary>
[Authorize]
[Route("collections")]
public class CollectionsController : Controller
{
    private readonly ICollectionsApiClient _collectionsApiClient;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CollectionsController> _logger;

    public CollectionsController(
        ICollectionsApiClient collectionsApiClient,
        ICacheService cacheService,
        ILogger<CollectionsController> logger)
    {
        _collectionsApiClient = collectionsApiClient;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Main collections page with advanced search, filtering, and caching
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CollectionSearchViewModel search)
    {
        _logger.LogInformation("CollectionsController.Index called with search: {@Search}", search);
        
        try
        {
            // Set defaults if not provided
            search.Page = search.Page <= 0 ? 1 : search.Page;
            search.PageSize = search.PageSize <= 0 ? 20 : search.PageSize;
            search.SortBy = string.IsNullOrEmpty(search.SortBy) ? "UpdatedAt" : search.SortBy;
            search.SortDirection = string.IsNullOrEmpty(search.SortDirection) ? "DESC" : search.SortDirection;

            // Get collections with caching
            var collectionsResult = await _collectionsApiClient.SearchCollectionsAsync(search);

            // Get additional data for the view
            var favoriteCollections = await _collectionsApiClient.GetFavoriteCollectionsAsync(1, 5);
            var recentCollections = await _collectionsApiClient.GetRecentCollectionsAsync(5);
            var statsOverview = await _collectionsApiClient.GetStatsOverviewAsync();

            // Build comprehensive view model
            var viewModel = new CollectionListViewModel
            {
                Collections = collectionsResult.Items,
                FavoriteCollections = favoriteCollections.Items,
                RecentCollections = recentCollections,
                SearchFilters = search,
                Pagination = new Models.Collections.PaginationViewModel
                {
                    CurrentPage = collectionsResult.Page,
                    TotalPages = collectionsResult.TotalPages,
                    TotalItems = collectionsResult.TotalCount,
                    PageSize = collectionsResult.PageSize
                },
                StatsOverview = statsOverview,
                ViewMode = GetViewModeFromCookie(),
                ShowFilters = Request.Query.ContainsKey("showFilters")
            };

            // Populate filter options for dropdowns
            await PopulateFilterOptions(viewModel.SearchFilters);

            // Set page metadata
            ViewBag.PageTitle = "Collections";
            ViewBag.PageDescription = $"Manage your {statsOverview.TotalCollections} collections";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading collections index page");
            TempData["ErrorMessage"] = "Failed to load collections. Please try again.";
            return View(new CollectionListViewModel());
        }
    }

    /// <summary>
    /// Collection details page with analytics and document preview
    /// </summary>
    [HttpGet("{collectionId:guid}")]
    public async Task<IActionResult> Details(Guid collectionId)
    {
        try
        {
            var collection = await _collectionsApiClient.GetCollectionAsync(collectionId);
            if (collection == null)
            {
                TempData["ErrorMessage"] = "Collection not found.";
                return RedirectToAction("Index");
            }

            // Track view asynchronously
            _ = Task.Run(async () => await _collectionsApiClient.TrackCollectionViewAsync(collectionId));

            // Get analytics data
            var analytics = await _collectionsApiClient.GetCollectionAnalyticsAsync(collectionId);

            var viewModel = new CollectionDetailsViewModel
            {
                Collection = collection,
                Analytics = analytics,
                CanEdit = collection.Permissions.CanEdit,
                CanDelete = collection.Permissions.CanDelete,
                CanShare = collection.Permissions.CanShare,
                CanAddDocuments = collection.Permissions.CanAddDocuments,
                RelatedCollections = new List<CollectionViewModel>(),
                RecentDocuments = new List<DocumentViewModel>()
            };

            ViewBag.PageTitle = collection.Name;
            ViewBag.PageDescription = collection.Description ?? $"Collection with {collection.DocumentCount} documents";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading collection details for {CollectionId}", collectionId);
            TempData["ErrorMessage"] = "Failed to load collection details. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Create new collection page
    /// </summary>
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var viewModel = new CreateCollectionViewModel
            {
                Color = "#3B82F6", // Default blue
                Icon = "folder",   // Default icon
                SetAsFavorite = false
            };

            // Populate available options
            await PopulateCreateOptions(viewModel);

            ViewBag.PageTitle = "Create Collection";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create collection page");
            TempData["ErrorMessage"] = "Failed to load create collection page. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Create new collection - POST action
    /// </summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCollectionViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await PopulateCreateOptions(model);
                return View(model);
            }

            var result = await _collectionsApiClient.CreateCollectionAsync(model);
            if (result != null)
            {
                TempData["SuccessMessage"] = $"Collection '{result.Name}' created successfully.";
                return RedirectToAction("Details", new { collectionId = result.Id });
            }
            else
            {
                ModelState.AddModelError("", "Failed to create collection. Please try again.");
                await PopulateCreateOptions(model);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            ModelState.AddModelError("", "An error occurred while creating the collection. Please try again.");
            await PopulateCreateOptions(model);
            return View(model);
        }
    }

    /// <summary>
    /// Edit collection page
    /// </summary>
    [HttpGet("{collectionId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid collectionId)
    {
        try
        {
            var collection = await _collectionsApiClient.GetCollectionAsync(collectionId);
            if (collection == null || !collection.Permissions.CanEdit)
            {
                TempData["ErrorMessage"] = "Collection not found or you don't have permission to edit it.";
                return RedirectToAction("Index");
            }

            var viewModel = new UpdateCollectionViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Description = collection.Description,
                Color = collection.Color,
                Icon = collection.Icon,
                TagsInput = string.Join(", ", collection.Tags)
            };

            // Populate available options
            await PopulateEditOptions(viewModel);

            ViewBag.PageTitle = $"Edit {collection.Name}";
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit collection page for {CollectionId}", collectionId);
            TempData["ErrorMessage"] = "Failed to load collection for editing. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Edit collection - POST action
    /// </summary>
    [HttpPost("{collectionId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid collectionId, UpdateCollectionViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await PopulateEditOptions(model);
                return View(model);
            }

            var result = await _collectionsApiClient.UpdateCollectionAsync(collectionId, model);
            if (result != null)
            {
                TempData["SuccessMessage"] = $"Collection '{result.Name}' updated successfully.";
                return RedirectToAction("Details", new { collectionId = result.Id });
            }
            else
            {
                ModelState.AddModelError("", "Failed to update collection. Please try again.");
                await PopulateEditOptions(model);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId}", collectionId);
            ModelState.AddModelError("", "An error occurred while updating the collection. Please try again.");
            await PopulateEditOptions(model);
            return View(model);
        }
    }

    /// <summary>
    /// Delete collection - POST action
    /// </summary>
    [HttpPost("{collectionId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid collectionId)
    {
        try
        {
            var collection = await _collectionsApiClient.GetCollectionAsync(collectionId);
            if (collection == null || !collection.Permissions.CanDelete)
            {
                TempData["ErrorMessage"] = "Collection not found or you don't have permission to delete it.";
                return RedirectToAction("Index");
            }

            var success = await _collectionsApiClient.DeleteCollectionAsync(collectionId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Collection '{collection.Name}' deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete collection. Please try again.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId}", collectionId);
            TempData["ErrorMessage"] = "An error occurred while deleting the collection. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Favorites page
    /// </summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> Favorites(int page = 1, int pageSize = 20)
    {
        try
        {
            var favoritesResult = await _collectionsApiClient.GetFavoriteCollectionsAsync(page, pageSize);
            var statsOverview = await _collectionsApiClient.GetStatsOverviewAsync();

            var viewModel = new CollectionListViewModel
            {
                Collections = favoritesResult.Items,
                FavoriteCollections = favoritesResult.Items,
                RecentCollections = new List<CollectionViewModel>(),
                SearchFilters = new CollectionSearchViewModel { IsFavorite = true },
                Pagination = new Models.Collections.PaginationViewModel
                {
                    CurrentPage = favoritesResult.Page,
                    TotalPages = favoritesResult.TotalPages,
                    TotalItems = favoritesResult.TotalCount,
                    PageSize = favoritesResult.PageSize
                },
                StatsOverview = statsOverview,
                ViewMode = GetViewModeFromCookie()
            };

            ViewBag.PageTitle = "Favorite Collections";
            ViewBag.PageDescription = $"Your {favoritesResult.TotalCount} favorite collections";

            return View("Index", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading favorite collections");
            TempData["ErrorMessage"] = "Failed to load favorite collections. Please try again.";
            return View("Index", new CollectionListViewModel());
        }
    }

    /// <summary>
    /// AJAX endpoint for search suggestions
    /// </summary>
    [HttpGet("search-suggestions")]
    public async Task<IActionResult> GetSearchSuggestions(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            var suggestions = await _collectionsApiClient.GetSearchSuggestionsAsync(term, 10);
            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// AJAX endpoint for tag suggestions
    /// </summary>
    [HttpGet("tag-suggestions")]
    public async Task<IActionResult> GetTagSuggestions(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return Json(new List<string>());
            }

            var suggestions = await _collectionsApiClient.GetTagSuggestionsAsync(term, 10);
            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// Toggle favorite status - AJAX endpoint
    /// </summary>
    [HttpPost("{collectionId:guid}/toggle-favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid collectionId)
    {
        try
        {
            var success = await _collectionsApiClient.ToggleFavoriteAsync(collectionId);
            return Json(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for collection {CollectionId}", collectionId);
            return Json(new { success = false, error = "Failed to toggle favorite status" });
        }
    }

    /// <summary>
    /// Bulk operations - AJAX endpoint
    /// </summary>
    [HttpPost("bulk-operation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkOperation([FromBody] BulkCollectionOperationViewModel operation)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, error = "Invalid operation data" });
            }

            var success = await _collectionsApiClient.BulkOperationAsync(operation);
            return Json(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation");
            return Json(new { success = false, error = "Failed to perform bulk operation" });
        }
    }

    /// <summary>
    /// Reorder collections - AJAX endpoint
    /// </summary>
    [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] List<CollectionOrderViewModel> collections)
    {
        try
        {
            var success = await _collectionsApiClient.ReorderCollectionsAsync(collections);
            return Json(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering collections");
            return Json(new { success = false, error = "Failed to reorder collections" });
        }
    }

    #region Helper Methods

    private bool HasActiveFilters(CollectionSearchViewModel search)
    {
        return !string.IsNullOrEmpty(search.SearchTerm) ||
               search.Tags.Any() ||
               !string.IsNullOrEmpty(search.Color) ||
               !string.IsNullOrEmpty(search.Icon) ||
               search.IsFavorite.HasValue ||
               search.IsShared.HasValue ||
               search.CreatedAfter.HasValue ||
               search.CreatedBefore.HasValue ||
               search.MinDocuments.HasValue ||
               search.MaxDocuments.HasValue;
    }

    private string GetViewModeFromCookie()
    {
        return Request.Cookies["CollectionViewMode"] ?? "grid";
    }

    private async Task PopulateFilterOptions(CollectionSearchViewModel search)
    {
        try
        {
            // Color options
            search.AvailableColors = new List<SelectListItem>
            {
                new() { Value = "", Text = "All Colors" },
                new() { Value = "#3B82F6", Text = "Blue" },
                new() { Value = "#10B981", Text = "Green" },
                new() { Value = "#F59E0B", Text = "Yellow" },
                new() { Value = "#EF4444", Text = "Red" },
                new() { Value = "#8B5CF6", Text = "Purple" },
                new() { Value = "#06B6D4", Text = "Cyan" }
            };

            // Icon options
            search.AvailableIcons = new List<SelectListItem>
            {
                new() { Value = "", Text = "All Icons" },
                new() { Value = "folder", Text = "Folder" },
                new() { Value = "document", Text = "Document" },
                new() { Value = "archive", Text = "Archive" },
                new() { Value = "star", Text = "Star" },
                new() { Value = "bookmark", Text = "Bookmark" },
                new() { Value = "tag", Text = "Tag" }
            };

            // Sort options
            search.SortOptions = new List<SelectListItem>
            {
                new() { Value = "UpdatedAt", Text = "Date Modified" },
                new() { Value = "CreatedAt", Text = "Date Created" },
                new() { Value = "Name", Text = "Name" },
                new() { Value = "DocumentCount", Text = "Document Count" }
            };

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating filter options");
        }
    }

    private async Task PopulateCreateOptions(CreateCollectionViewModel model)
    {
        try
        {
            // Color options
            model.AvailableColors = new List<ColorOption>
            {
                new() { Value = "#3B82F6", Name = "Blue", HexCode = "#3B82F6" },
                new() { Value = "#10B981", Name = "Green", HexCode = "#10B981" },
                new() { Value = "#F59E0B", Name = "Yellow", HexCode = "#F59E0B" },
                new() { Value = "#EF4444", Name = "Red", HexCode = "#EF4444" },
                new() { Value = "#8B5CF6", Name = "Purple", HexCode = "#8B5CF6" },
                new() { Value = "#06B6D4", Name = "Cyan", HexCode = "#06B6D4" }
            };

            // Icon options
            model.AvailableIcons = new List<IconOption>
            {
                new() { Value = "folder", Name = "Folder", CssClass = "fas fa-folder" },
                new() { Value = "document", Name = "Document", CssClass = "fas fa-file" },
                new() { Value = "archive", Name = "Archive", CssClass = "fas fa-archive" },
                new() { Value = "star", Name = "Star", CssClass = "fas fa-star" },
                new() { Value = "bookmark", Name = "Bookmark", CssClass = "fas fa-bookmark" },
                new() { Value = "tag", Name = "Tag", CssClass = "fas fa-tag" }
            };

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating create options");
        }
    }

    private async Task PopulateEditOptions(UpdateCollectionViewModel model)
    {
        try
        {
            // Color options
            model.AvailableColors = new List<ColorOption>
            {
                new() { Value = "#3B82F6", Name = "Blue", HexCode = "#3B82F6" },
                new() { Value = "#10B981", Name = "Green", HexCode = "#10B981" },
                new() { Value = "#F59E0B", Name = "Yellow", HexCode = "#F59E0B" },
                new() { Value = "#EF4444", Name = "Red", HexCode = "#EF4444" },
                new() { Value = "#8B5CF6", Name = "Purple", HexCode = "#8B5CF6" },
                new() { Value = "#06B6D4", Name = "Cyan", HexCode = "#06B6D4" }
            };

            // Icon options
            model.AvailableIcons = new List<IconOption>
            {
                new() { Value = "folder", Name = "Folder", CssClass = "fas fa-folder" },
                new() { Value = "document", Name = "Document", CssClass = "fas fa-file" },
                new() { Value = "archive", Name = "Archive", CssClass = "fas fa-archive" },
                new() { Value = "star", Name = "Star", CssClass = "fas fa-star" },
                new() { Value = "bookmark", Name = "Bookmark", CssClass = "fas fa-bookmark" },
                new() { Value = "tag", Name = "Tag", CssClass = "fas fa-tag" }
            };

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating edit options");
        }
    }

    #endregion
}
