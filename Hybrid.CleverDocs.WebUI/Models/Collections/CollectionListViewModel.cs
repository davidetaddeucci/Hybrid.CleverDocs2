using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs.WebUI.Models.Collections;

/// <summary>
/// ViewModel for the Collections Index page
/// </summary>
public class CollectionListViewModel
{
    public List<CollectionViewModel> Collections { get; set; } = new();
    public List<CollectionViewModel> FavoriteCollections { get; set; } = new();
    public List<CollectionViewModel> RecentCollections { get; set; } = new();
    public CollectionSearchViewModel SearchFilters { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public CollectionStatsOverviewViewModel StatsOverview { get; set; } = new();
    
    // UI State
    public string ViewMode { get; set; } = "grid"; // "grid" or "list"
    public bool ShowFilters { get; set; } = false;
    public string? SelectedCollectionId { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Quick Actions
    public bool CanCreateCollection { get; set; } = true;
    public bool CanBulkOperations { get; set; } = true;
    public List<string> SelectedCollectionIds { get; set; } = new();
    
    // Available Options for Filters
    public List<ColorOption> AvailableColors { get; set; } = new();
    public List<IconOption> AvailableIcons { get; set; } = new();
    public List<string> AvailableTags { get; set; } = new();
    public List<SortOption> SortOptions { get; set; } = new()
    {
        new() { Value = "Name", Text = "Name" },
        new() { Value = "UpdatedAt", Text = "Last Updated" },
        new() { Value = "CreatedAt", Text = "Created Date" },
        new() { Value = "DocumentCount", Text = "Document Count" },
        new() { Value = "TotalSizeBytes", Text = "Size" }
    };
    
    // Helper Properties
    public int TotalCollections => Collections.Count;
    public int FavoriteCount => FavoriteCollections.Count;
    public bool HasCollections => Collections.Any();
    public bool HasFavorites => FavoriteCollections.Any();
    public bool HasRecent => RecentCollections.Any();
    public bool HasActiveFilters => !string.IsNullOrEmpty(SearchFilters.SearchTerm) ||
                             SearchFilters.Tags.Any() ||
                             !string.IsNullOrEmpty(SearchFilters.Color) ||
                             !string.IsNullOrEmpty(SearchFilters.Icon) ||
                             SearchFilters.IsFavorite.HasValue ||
                             SearchFilters.IsShared.HasValue ||
                             SearchFilters.CreatedAfter.HasValue ||
                             SearchFilters.CreatedBefore.HasValue ||
                             SearchFilters.MinDocuments.HasValue ||
                             SearchFilters.MaxDocuments.HasValue;
    public bool IsFiltered => HasActiveFilters;
}

/// <summary>
/// ViewModel for pagination controls
/// </summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; } = 0;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int StartItem => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
    
    public List<int> GetPageNumbers()
    {
        var pages = new List<int>();
        var start = Math.Max(1, CurrentPage - 2);
        var end = Math.Min(TotalPages, CurrentPage + 2);
        
        for (int i = start; i <= end; i++)
        {
            pages.Add(i);
        }
        
        return pages;
    }
}

/// <summary>
/// ViewModel for collection statistics overview
/// </summary>
public class CollectionStatsOverviewViewModel
{
    public int TotalCollections { get; set; }
    public int TotalDocuments { get; set; }
    public long TotalSizeBytes { get; set; }
    public int FavoriteCollections { get; set; }
    public int SharedCollections { get; set; }
    public int CollectionsCreatedThisWeek { get; set; }
    public int CollectionsCreatedThisMonth { get; set; }
    public int CollectionsThisWeek { get; set; }
    public int CollectionsThisMonth { get; set; }
    public DateTime? LastActivity { get; set; }
    
    // Formatted Properties
    public string FormattedTotalSize => FormatBytes(TotalSizeBytes);
    public double AverageDocumentsPerCollection => TotalCollections > 0 ? (double)TotalDocuments / TotalCollections : 0;
    public string FormattedAverageDocuments => AverageDocumentsPerCollection.ToString("F1");
    
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// ViewModel for bulk operations
/// </summary>
public class BulkCollectionOperationViewModel
{
    public List<Guid> CollectionIds { get; set; } = new();
    
    [Required(ErrorMessage = "Operation is required")]
    public string Operation { get; set; } = string.Empty; // "delete", "favorite", "unfavorite", "tag", "untag"
    
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    // UI Properties
    public string OperationDisplayName => Operation switch
    {
        "delete" => "Delete Collections",
        "favorite" => "Add to Favorites",
        "unfavorite" => "Remove from Favorites",
        "tag" => "Add Tags",
        "untag" => "Remove Tags",
        _ => "Unknown Operation"
    };
    
    public string ConfirmationMessage => Operation switch
    {
        "delete" => $"Are you sure you want to delete {CollectionIds.Count} collection(s)? This action cannot be undone.",
        "favorite" => $"Add {CollectionIds.Count} collection(s) to favorites?",
        "unfavorite" => $"Remove {CollectionIds.Count} collection(s) from favorites?",
        "tag" => $"Add tags to {CollectionIds.Count} collection(s)?",
        "untag" => $"Remove tags from {CollectionIds.Count} collection(s)?",
        _ => $"Perform operation on {CollectionIds.Count} collection(s)?"
    };
}

/// <summary>
/// ViewModel for collection suggestions
/// </summary>
public class CollectionSuggestionViewModel
{
    public string SuggestedName { get; set; } = string.Empty;
    public string? SuggestedDescription { get; set; }
    public string SuggestedColor { get; set; } = string.Empty;
    public string SuggestedIcon { get; set; } = string.Empty;
    public List<string> SuggestedTags { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    
    // UI Properties
    public string ConfidencePercentage => $"{Confidence * 100:F0}%";
    public string ConfidenceClass => Confidence switch
    {
        >= 0.8 => "high-confidence",
        >= 0.6 => "medium-confidence",
        _ => "low-confidence"
    };
}

/// <summary>
/// Helper class for sort options
/// </summary>
public class SortOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for collection analytics
/// </summary>
public class CollectionAnalyticsViewModel
{
    public Guid CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int ViewsThisWeek { get; set; }
    public int ViewsThisMonth { get; set; }
    public int DocumentAddCount { get; set; }
    public int DocumentsAddedThisWeek { get; set; }
    public int DocumentsAddedThisMonth { get; set; }
    public int DocumentRemoveCount { get; set; }
    public int ShareCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<CollectionActivityViewModel> RecentActivities { get; set; } = new();
    public List<string> TopTags { get; set; } = new();
    public List<string> RecentActivity { get; set; } = new();
    public Dictionary<string, int> ActivityByDay { get; set; } = new();
    public Dictionary<string, int> DocumentTypeDistribution { get; set; } = new();
    
    // UI Properties
    public string FormattedLastActivity => GetRelativeTime(LastActivity);
    public bool HasRecentActivity => RecentActivities.Any();
    public bool HasActivityData => ActivityByDay.Any();
    public bool HasDocumentTypes => DocumentTypeDistribution.Any();
    
    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
        
        return "Just now";
    }
}

/// <summary>
/// ViewModel for collection activity
/// </summary>
public class CollectionActivityViewModel
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // UI Properties
    public string FormattedTimestamp => GetRelativeTime(Timestamp);
    public string ActivityIcon => ActivityType switch
    {
        "CREATE" => "add_circle",
        "UPDATE" => "edit",
        "DELETE" => "delete",
        "VIEW" => "visibility",
        "SHARE" => "share",
        "DOCUMENT_ADD" => "note_add",
        "DOCUMENT_REMOVE" => "note_remove",
        _ => "info"
    };
    
    public string ActivityColor => ActivityType switch
    {
        "CREATE" => "success",
        "UPDATE" => "info",
        "DELETE" => "danger",
        "VIEW" => "secondary",
        "SHARE" => "primary",
        "DOCUMENT_ADD" => "success",
        "DOCUMENT_REMOVE" => "warning",
        _ => "secondary"
    };
    
    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        
        return "now";
    }
}
