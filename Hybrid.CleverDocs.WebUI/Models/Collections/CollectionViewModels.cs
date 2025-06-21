using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Hybrid.CleverDocs.WebUI.Models.Common;
using Hybrid.CleverDocs.WebUI.Models.Documents;

namespace Hybrid.CleverDocs.WebUI.Models.Collections;

/// <summary>
/// ViewModel for displaying collection information
/// </summary>
public class CollectionViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int DocumentCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime? LastAccessedAt { get; set; }
    
    [Required]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    public string Color { get; set; } = "#3B82F6";
    
    [Required]
    [StringLength(50)]
    public string Icon { get; set; } = "folder";
    
    public bool IsShared { get; set; }
    
    public bool IsFavorite { get; set; }
    
    public string? R2RCollectionId { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public string? TenantId { get; set; }
    
    public CollectionStatsViewModel Stats { get; set; } = new();
    
    public List<string> Tags { get; set; } = new();
    
    public CollectionPermissionsViewModel Permissions { get; set; } = new();
    
    // UI-specific properties
    public string FormattedSize => FormatBytes(Stats.TotalSizeBytes);
    public string RelativeCreatedAt => GetRelativeTime(CreatedAt);
    public string RelativeUpdatedAt => GetRelativeTime(UpdatedAt);
    public string CssColorClass => $"collection-color-{Color.Replace("#", "")}";
    
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
    
    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalDays >= 365)
            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) != 1 ? "s" : "")} ago";
        if (timeSpan.TotalDays >= 30)
            return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) != 1 ? "s" : "")} ago";
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
/// ViewModel for creating a new collection
/// </summary>
public class CreateCollectionViewModel
{
    [Required(ErrorMessage = "Collection name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [Display(Name = "Collection Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid color format")]
    [Display(Name = "Color")]
    public string? Color { get; set; }
    
    [StringLength(50, ErrorMessage = "Icon name cannot exceed 50 characters")]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }
    
    [Display(Name = "Tags")]
    public List<string> Tags { get; set; } = new();
    
    [Display(Name = "Set as Favorite")]
    public bool SetAsFavorite { get; set; } = false;
    
    // UI helper properties
    public List<ColorOption> AvailableColors { get; set; } = new();
    public List<IconOption> AvailableIcons { get; set; } = new();
    public string TagsInput
    {
        get => string.Join(", ", Tags);
        set => Tags = value?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList() ?? new List<string>();
    }
}

/// <summary>
/// ViewModel for updating an existing collection
/// </summary>
public class UpdateCollectionViewModel
{
    public Guid Id { get; set; }
    
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [Display(Name = "Collection Name")]
    public string? Name { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid color format")]
    [Display(Name = "Color")]
    public string? Color { get; set; }
    
    [StringLength(50, ErrorMessage = "Icon name cannot exceed 50 characters")]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }
    
    [Display(Name = "Tags")]
    public List<string>? Tags { get; set; }
    
    [Display(Name = "Is Favorite")]
    public bool? IsFavorite { get; set; }
    
    // UI helper properties
    public List<ColorOption> AvailableColors { get; set; } = new();
    public List<IconOption> AvailableIcons { get; set; } = new();
    public string? TagsInput
    {
        get => Tags != null ? string.Join(", ", Tags) : null;
        set => Tags = value?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList();
    }
}

/// <summary>
/// ViewModel for collection statistics
/// </summary>
public class CollectionStatsViewModel
{
    public int TotalDocuments { get; set; }
    public long TotalSizeBytes { get; set; }
    public int DocumentsThisWeek { get; set; }
    public int DocumentsThisMonth { get; set; }
    public DateTime? LastDocumentAdded { get; set; }
    public List<string> TopDocumentTypes { get; set; } = new();
    public double AverageDocumentSize { get; set; }
    public int SharedWithUsers { get; set; }
}

/// <summary>
/// ViewModel for collection permissions
/// </summary>
public class CollectionPermissionsViewModel
{
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanShare { get; set; } = true;
    public bool CanAddDocuments { get; set; } = true;
    public bool CanRemoveDocuments { get; set; } = true;
    public bool IsOwner { get; set; } = true;
}

/// <summary>
/// ViewModel for collection search and filtering
/// </summary>
public class CollectionSearchViewModel
{
    [Display(Name = "Search")]
    public string? SearchTerm { get; set; }
    
    [Display(Name = "Tags")]
    public List<string> Tags { get; set; } = new();
    
    [Display(Name = "Color")]
    public string? Color { get; set; }
    
    [Display(Name = "Icon")]
    public string? Icon { get; set; }
    
    [Display(Name = "Favorites Only")]
    public bool? IsFavorite { get; set; }
    
    [Display(Name = "Shared Only")]
    public bool? IsShared { get; set; }
    
    [Display(Name = "Created After")]
    [DataType(DataType.Date)]
    public DateTime? CreatedAfter { get; set; }
    
    [Display(Name = "Created Before")]
    [DataType(DataType.Date)]
    public DateTime? CreatedBefore { get; set; }
    
    [Display(Name = "Min Documents")]
    [Range(0, int.MaxValue, ErrorMessage = "Minimum documents must be 0 or greater")]
    public int? MinDocuments { get; set; }
    
    [Display(Name = "Max Documents")]
    [Range(0, int.MaxValue, ErrorMessage = "Maximum documents must be 0 or greater")]
    public int? MaxDocuments { get; set; }
    
    [Display(Name = "Sort By")]
    public string SortBy { get; set; } = "UpdatedAt";
    
    [Display(Name = "Sort Direction")]
    public string SortDirection { get; set; } = "DESC";
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // UI helper properties
    public List<SelectListItem> AvailableColors { get; set; } = new();
    public List<SelectListItem> AvailableIcons { get; set; } = new();
    public List<SelectListItem> SortOptions { get; set; } = new();
    public string TagsAsString
    {
        get => string.Join(", ", Tags);
        set => Tags = value?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList() ?? new List<string>();
    }
}

/// <summary>
/// Helper classes for UI options
/// </summary>
public class ColorOption
{
    public string Value { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = string.Empty;
}

public class IconOption
{
    public string Value { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for collection details page
/// </summary>
public class CollectionDetailsViewModel
{
    public CollectionViewModel Collection { get; set; } = new();
    public CollectionAnalyticsViewModel Analytics { get; set; } = new();
    public List<CollectionViewModel> RelatedCollections { get; set; } = new();
    public List<DocumentViewModel> RecentDocuments { get; set; } = new();

    // Permissions
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanShare { get; set; }
    public bool CanAddDocuments { get; set; }

    // Documents pagination
    public PaginationViewModel DocumentsPagination { get; set; } = new();
    public DocumentSearchViewModel DocumentsSearch { get; set; } = new();
}

/// <summary>
/// ViewModel for collection reordering
/// </summary>
public class CollectionOrderViewModel
{
    public Guid CollectionId { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Placeholder DocumentViewModel for related documents
/// </summary>
public class DocumentViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string? ProcessingError { get; set; }
    public double? ProcessingProgress { get; set; }

    public string FileSizeFormatted
    {
        get
        {
            if (Size < 1024) return $"{Size} B";
            if (Size < 1024 * 1024) return $"{Size / 1024:F1} KB";
            if (Size < 1024 * 1024 * 1024) return $"{Size / (1024 * 1024):F1} MB";
            return $"{Size / (1024 * 1024 * 1024):F1} GB";
        }
    }
}
