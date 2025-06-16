using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Models.Documents;
using Hybrid.CleverDocs.WebUI.Services.Documents;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Controllers;

/// <summary>
/// Controller for advanced document search functionality
/// </summary>
[Authorize]
[Route("api/documents/search")]
public class DocumentSearchController : Controller
{
    private readonly IDocumentApiClient _documentApiClient;
    private readonly ILogger<DocumentSearchController> _logger;

    public DocumentSearchController(
        IDocumentApiClient documentApiClient,
        ILogger<DocumentSearchController> logger)
    {
        _documentApiClient = documentApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Get search suggestions based on partial input
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSearchSuggestions([FromQuery] string term, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            // Get suggestions from various sources
            var suggestions = new List<string>();

            // Document names
            var nameResults = await _documentApiClient.GetDocumentNameSuggestionsAsync(term, limit / 2);
            suggestions.AddRange(nameResults);

            // Content suggestions (if available)
            var contentResults = await _documentApiClient.GetContentSuggestionsAsync(term, limit / 2);
            suggestions.AddRange(contentResults);

            // Remove duplicates and limit results
            var uniqueSuggestions = suggestions.Distinct().Take(limit).ToList();

            return Json(uniqueSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// Get tag suggestions for autocomplete
    /// </summary>
    [HttpGet("tag-suggestions")]
    public async Task<IActionResult> GetTagSuggestions([FromQuery] string term, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return Json(new List<string>());
            }

            var suggestions = await _documentApiClient.GetTagSuggestionsAsync(term, limit);
            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// Get author suggestions for autocomplete
    /// </summary>
    [HttpGet("author-suggestions")]
    public async Task<IActionResult> GetAuthorSuggestions([FromQuery] string term, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return Json(new List<string>());
            }

            var suggestions = await _documentApiClient.GetAuthorSuggestionsAsync(term, limit);
            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting author suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// Save current search for future use
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> SaveSearch([FromBody] SaveSearchRequest request)
    {
        try
        {
            var savedSearch = new SavedSearchItem
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SearchTerm = request.SearchTerm ?? string.Empty,
                Filters = JsonSerializer.Serialize(request.Filters),
                CreatedAt = DateTime.UtcNow,
                UserId = User.Identity?.Name ?? string.Empty,
                IsPublic = request.IsPublic,
                IsFavorite = request.IsFavorite
            };

            var success = await _documentApiClient.SaveSearchAsync(savedSearch);
            
            if (success)
            {
                return Json(new { success = true, id = savedSearch.Id, message = "Search saved successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to save search" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving search: {Name}", request.Name);
            return Json(new { success = false, message = "An error occurred while saving the search" });
        }
    }

    /// <summary>
    /// Get user's saved searches
    /// </summary>
    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedSearches()
    {
        try
        {
            var savedSearches = await _documentApiClient.GetSavedSearchesAsync();
            return Json(savedSearches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved searches");
            return Json(new List<SavedSearchItem>());
        }
    }

    /// <summary>
    /// Load a saved search
    /// </summary>
    [HttpGet("saved/{id}")]
    public async Task<IActionResult> LoadSavedSearch(Guid id)
    {
        try
        {
            var savedSearch = await _documentApiClient.GetSavedSearchAsync(id);
            if (savedSearch == null)
            {
                return NotFound();
            }

            // Update last used timestamp
            await _documentApiClient.UpdateSavedSearchUsageAsync(id);

            return Json(savedSearch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading saved search: {Id}", id);
            return NotFound();
        }
    }

    /// <summary>
    /// Delete a saved search
    /// </summary>
    [HttpDelete("saved/{id}")]
    public async Task<IActionResult> DeleteSavedSearch(Guid id)
    {
        try
        {
            var success = await _documentApiClient.DeleteSavedSearchAsync(id);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved search: {Id}", id);
            return Json(new { success = false });
        }
    }

    /// <summary>
    /// Get search history for the current user
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetSearchHistory([FromQuery] int limit = 20)
    {
        try
        {
            var history = await _documentApiClient.GetSearchHistoryAsync(limit);
            return Json(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history");
            return Json(new List<SearchHistoryItem>());
        }
    }

    /// <summary>
    /// Clear search history
    /// </summary>
    [HttpDelete("history")]
    public async Task<IActionResult> ClearSearchHistory()
    {
        try
        {
            var success = await _documentApiClient.ClearSearchHistoryAsync();
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history");
            return Json(new { success = false });
        }
    }

    /// <summary>
    /// Record a search in history
    /// </summary>
    [HttpPost("history")]
    public async Task<IActionResult> RecordSearch([FromBody] RecordSearchRequest request)
    {
        try
        {
            var historyItem = new SearchHistoryItem
            {
                Id = Guid.NewGuid(),
                SearchTerm = request.SearchTerm ?? string.Empty,
                Filters = JsonSerializer.Serialize(request.Filters),
                SearchedAt = DateTime.UtcNow,
                ResultCount = request.ResultCount,
                UserId = User.Identity?.Name ?? string.Empty
            };

            await _documentApiClient.RecordSearchAsync(historyItem);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search");
            return Json(new { success = false });
        }
    }
}

/// <summary>
/// Request models for search operations
/// </summary>
public class SaveSearchRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SearchTerm { get; set; }
    public object? Filters { get; set; }
    public bool IsPublic { get; set; }
    public bool IsFavorite { get; set; }
}

public class RecordSearchRequest
{
    public string? SearchTerm { get; set; }
    public object? Filters { get; set; }
    public int ResultCount { get; set; }
}
