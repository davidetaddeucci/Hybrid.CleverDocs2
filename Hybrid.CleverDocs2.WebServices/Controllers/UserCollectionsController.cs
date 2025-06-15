using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs2.WebServices.Models.Collections;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Services.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

/// <summary>
/// Controller for user collection operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Admin")]
public class UserCollectionsController : ControllerBase
{
    private readonly IUserCollectionService _collectionService;
    private readonly ICollectionAnalyticsService _analyticsService;
    private readonly ILogger<UserCollectionsController> _logger;
    private readonly ICorrelationService _correlationService;

    public UserCollectionsController(
        IUserCollectionService collectionService,
        ICollectionAnalyticsService analyticsService,
        ILogger<UserCollectionsController> logger,
        ICorrelationService correlationService)
    {
        _collectionService = collectionService;
        _analyticsService = analyticsService;
        _logger = logger;
        _correlationService = correlationService;
    }

    /// <summary>
    /// Gets all collections for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserCollections()
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            var collections = await _collectionService.GetUserCollectionsAsync(userId);

            return this.Success(collections, $"Retrieved {collections.Count} collections");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to retrieve collections", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets a specific collection by ID
    /// </summary>
    [HttpGet("{collectionId:guid}")]
    public async Task<IActionResult> GetCollection(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId, userId);

            if (collection == null)
            {
                return this.NotFound<UserCollectionDto>("Collection not found");
            }

            return this.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}",
                collectionId, userId, correlationId);

            return this.Error("Failed to retrieve collection", ex.Message, 500);
        }
    }

    /// <summary>
    /// Searches collections with advanced filtering
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchCollections([FromBody] CollectionSearchDto searchRequest)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var results = await _collectionService.SearchCollectionsAsync(searchRequest, userId);
            return this.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to search collections", ex.Message, 500);
        }
    }

    /// <summary>
    /// Creates a new collection
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCollection([FromBody] CreateUserCollectionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (!ModelState.IsValid)
            {
                return this.Error("Invalid request data",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
            }

            request.UserId = userId;
            var result = await _collectionService.CreateCollectionAsync(request);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetCollection),
                    new { collectionId = result.Collection!.Id },
                    ApiResponse<UserCollectionDto>.SuccessResponse(result.Collection, result.Message));
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to create collection", ex.Message, 500);
        }
    }

    /// <summary>
    /// Updates an existing collection
    /// </summary>
    [HttpPut("{collectionId:guid}")]
    public async Task<IActionResult> UpdateCollection(Guid collectionId, [FromBody] UpdateUserCollectionDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (!ModelState.IsValid)
            {
                return this.Error("Invalid request data",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
            }

            request.CollectionId = collectionId;
            request.UserId = userId;

            var result = await _collectionService.UpdateCollectionAsync(request);

            if (result.Success)
            {
                return this.Success(result.Collection!, result.Message);
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}",
                collectionId, userId, correlationId);

            return this.Error("Failed to update collection", ex.Message, 500);
        }
    }

    /// <summary>
    /// Deletes a collection
    /// </summary>
    [HttpDelete("{collectionId:guid}")]
    public async Task<IActionResult> DeleteCollection(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var result = await _collectionService.DeleteCollectionAsync(collectionId, userId);

            if (result.Success)
            {
                return this.Success(result.Message);
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId} for user {UserId}, CorrelationId: {CorrelationId}",
                collectionId, userId, correlationId);

            return this.Error("Failed to delete collection", ex.Message, 500);
        }
    }

    /// <summary>
    /// Toggles favorite status for a collection
    /// </summary>
    [HttpPost("{collectionId:guid}/toggle-favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var result = await _collectionService.ToggleFavoriteAsync(collectionId, userId);

            if (result.Success)
            {
                return this.Success(result.Collection!, result.Message);
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for collection {CollectionId}, user {UserId}, CorrelationId: {CorrelationId}",
                collectionId, userId, correlationId);

            return this.Error("Failed to toggle favorite", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets favorite collections for the current user
    /// </summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavoriteCollections()
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var favorites = await _collectionService.GetFavoriteCollectionsAsync(userId);
            return this.Success(favorites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to retrieve favorite collections", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets recently accessed collections
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentCollections([FromQuery] int count = 5)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var recent = await _collectionService.GetRecentCollectionsAsync(userId, count);
            return this.Success(recent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to retrieve recent collections", ex.Message, 500);
        }
    }

    /// <summary>
    /// Reorders collections for the user
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderCollections([FromBody] ReorderCollectionsDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            request.UserId = userId;
            var result = await _collectionService.ReorderCollectionsAsync(request);

            if (result.Success)
            {
                return this.Success(result.Message);
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering collections for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to reorder collections", ex.Message, 500);
        }
    }

    /// <summary>
    /// Performs bulk operations on multiple collections
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkOperation([FromBody] BulkCollectionOperationDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            request.UserId = userId;
            var result = await _collectionService.BulkOperationAsync(request);

            if (result.Success)
            {
                return this.Success(result.Message);
            }
            else
            {
                return this.Error(result.Message, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to perform bulk operation", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets collection suggestions for the user
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetCollectionSuggestions([FromQuery] string? context = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var suggestions = await _collectionService.GetCollectionSuggestionsAsync(userId, context);
            return this.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection suggestions for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to get collection suggestions", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets collection analytics
    /// </summary>
    [HttpGet("{collectionId:guid}/analytics")]
    public async Task<IActionResult> GetCollectionAnalytics(Guid collectionId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var analytics = await _collectionService.GetCollectionAnalyticsAsync(collectionId, userId);
            return this.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for collection {CollectionId}, user {UserId}, CorrelationId: {CorrelationId}",
                collectionId, userId, correlationId);

            return this.Error("Failed to get collection analytics", ex.Message, 500);
        }
    }

    /// <summary>
    /// Validates collection name uniqueness
    /// </summary>
    [HttpGet("validate-name")]
    public async Task<IActionResult> ValidateCollectionName(
        [FromQuery, Required] string name,
        [FromQuery] Guid? excludeCollectionId = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var isUnique = await _collectionService.IsCollectionNameUniqueAsync(name, userId, excludeCollectionId);
            return this.Success(isUnique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating collection name for user {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return this.Error("Failed to validate collection name", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets available colors for collections
    /// </summary>
    [HttpGet("colors")]
    public async Task<IActionResult> GetAvailableColors()
    {
        try
        {
            var colors = await _collectionService.GetAvailableColorsAsync();
            return this.Success(colors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available colors");
            return this.Error("Failed to get available colors", ex.Message, 500);
        }
    }

    /// <summary>
    /// Gets available icons for collections
    /// </summary>
    [HttpGet("icons")]
    public async Task<IActionResult> GetAvailableIcons()
    {
        try
        {
            var icons = await _collectionService.GetAvailableIconsAsync();
            return this.Success(icons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available icons");
            return this.Error("Failed to get available icons", ex.Message, 500);
        }
    }

    // Helper methods
    private string GetCurrentUserId()
    {
        return User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "anonymous";
    }
}
