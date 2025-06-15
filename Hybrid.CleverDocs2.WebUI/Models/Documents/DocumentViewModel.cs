using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebUI.Models.Documents;

/// <summary>
/// View model for document display and management
/// </summary>
public class DocumentViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string FormattedSize => FormatFileSize(Size);
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
    public DocumentStatus Status { get; set; }
    public string? R2RDocumentId { get; set; }
    public bool IsProcessing { get; set; }
    public double? ProcessingProgress { get; set; }
    public string? ProcessingError { get; set; }
    public bool IsFavorite { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public string? Version { get; set; }
    public bool HasVersions { get; set; }
    public DocumentPermissions Permissions { get; set; } = new();

    // UI Helper Properties
    public string FileTypeIcon => GetFileTypeIcon(ContentType);
    public string StatusBadgeClass => GetStatusBadgeClass(Status);
    public string StatusDisplayName => GetStatusDisplayName(Status);
    public bool CanPreview => CanPreviewFile(ContentType);
    public string RelativeCreatedTime => GetRelativeTime(CreatedAt);
    public string RelativeUpdatedTime => GetRelativeTime(UpdatedAt);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string GetFileTypeIcon(string contentType)
    {
        return contentType.ToLower() switch
        {
            "application/pdf" => "icon-file-pdf",
            "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "icon-file-word",
            "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "icon-file-excel",
            "application/vnd.ms-powerpoint" or "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "icon-file-powerpoint",
            "text/plain" => "icon-file-text",
            var ct when ct.StartsWith("image/") => "icon-file-image",
            var ct when ct.StartsWith("video/") => "icon-file-video",
            var ct when ct.StartsWith("audio/") => "icon-file-audio",
            _ => "icon-file"
        };
    }

    private static string GetStatusBadgeClass(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Draft => "badge-secondary",
            DocumentStatus.Processing => "badge-warning",
            DocumentStatus.Ready => "badge-success",
            DocumentStatus.Error => "badge-danger",
            DocumentStatus.Archived => "badge-info",
            DocumentStatus.Deleted => "badge-dark",
            _ => "badge-secondary"
        };
    }

    private static string GetStatusDisplayName(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Draft => "Draft",
            DocumentStatus.Processing => "Processing",
            DocumentStatus.Ready => "Ready",
            DocumentStatus.Error => "Error",
            DocumentStatus.Archived => "Archived",
            DocumentStatus.Deleted => "Deleted",
            _ => "Unknown"
        };
    }

    private static bool CanPreviewFile(string contentType)
    {
        return contentType.ToLower() switch
        {
            "application/pdf" => true,
            "text/plain" => true,
            var ct when ct.StartsWith("image/") => true,
            _ => false
        };
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        return timeSpan.TotalDays switch
        {
            < 1 when timeSpan.TotalHours < 1 => $"{(int)timeSpan.TotalMinutes} minutes ago",
            < 1 => $"{(int)timeSpan.TotalHours} hours ago",
            < 7 => $"{(int)timeSpan.TotalDays} days ago",
            < 30 => $"{(int)(timeSpan.TotalDays / 7)} weeks ago",
            < 365 => $"{(int)(timeSpan.TotalDays / 30)} months ago",
            _ => $"{(int)(timeSpan.TotalDays / 365)} years ago"
        };
    }
}

/// <summary>
/// View model for document search and filtering
/// </summary>
public class DocumentSearchViewModel
{
    public string? SearchTerm { get; set; }
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public List<string> SelectedContentTypes { get; set; } = new();
    public List<string> SelectedTags { get; set; } = new();
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public bool? IsFavorite { get; set; }
    public string SortBy { get; set; } = "updated_at";
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public ViewMode ViewMode { get; set; } = ViewMode.Grid;

    // Available options for filters
    public List<SelectListItem> AvailableContentTypes { get; set; } = new();
    public List<SelectListItem> AvailableTags { get; set; } = new();
    public List<SelectListItem> AvailableCollections { get; set; } = new();
    public List<SelectListItem> SortOptions { get; set; } = new();
}

/// <summary>
/// View model for document list page
/// </summary>
public class DocumentListViewModel
{
    public List<DocumentViewModel> Documents { get; set; } = new();
    public DocumentSearchViewModel Search { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public DocumentStatisticsViewModel Statistics { get; set; } = new();
    public List<DocumentViewModel> SelectedDocuments { get; set; } = new();
    public bool HasActiveFilters { get; set; }
    public Dictionary<string, object> Aggregations { get; set; } = new();
}

/// <summary>
/// View model for document details page
/// </summary>
public class DocumentDetailsViewModel
{
    public DocumentViewModel Document { get; set; } = new();
    public List<DocumentViewModel> RelatedDocuments { get; set; } = new();
    public List<DocumentVersionViewModel> Versions { get; set; } = new();
    public List<DocumentCommentViewModel> Comments { get; set; } = new();
    public DocumentAnalyticsViewModel Analytics { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanShare { get; set; }
}

/// <summary>
/// View model for document metadata editing
/// </summary>
public class DocumentEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public Guid? CollectionId { get; set; }
    
    [Display(Name = "Tags (comma-separated)")]
    public string TagsInput { get; set; } = string.Empty;
    
    public List<string> Tags => TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    
    public bool IsFavorite { get; set; }
    
    // Available collections for dropdown
    public List<SelectListItem> AvailableCollections { get; set; } = new();
    
    // Suggested tags
    public List<string> SuggestedTags { get; set; } = new();
}

/// <summary>
/// View model for batch operations
/// </summary>
public class BatchOperationViewModel
{
    public List<Guid> DocumentIds { get; set; } = new();
    public BatchOperationType Operation { get; set; }
    public Guid? TargetCollectionId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Reason { get; set; }
    
    // UI Properties
    public List<DocumentViewModel> SelectedDocuments { get; set; } = new();
    public List<SelectListItem> AvailableCollections { get; set; } = new();
    public string OperationDisplayName => GetOperationDisplayName(Operation);
    public string ConfirmationMessage => GetConfirmationMessage(Operation, DocumentIds.Count);

    private static string GetOperationDisplayName(BatchOperationType operation)
    {
        return operation switch
        {
            BatchOperationType.Move => "Move Documents",
            BatchOperationType.Delete => "Delete Documents",
            BatchOperationType.Tag => "Add Tags",
            BatchOperationType.Archive => "Archive Documents",
            BatchOperationType.Restore => "Restore Documents",
            BatchOperationType.Download => "Download Documents",
            BatchOperationType.UpdateMetadata => "Update Metadata",
            BatchOperationType.ChangeCollection => "Change Collection",
            _ => "Batch Operation"
        };
    }

    private static string GetConfirmationMessage(BatchOperationType operation, int count)
    {
        return operation switch
        {
            BatchOperationType.Delete => $"Are you sure you want to delete {count} document(s)? This action cannot be undone.",
            BatchOperationType.Archive => $"Archive {count} document(s)?",
            BatchOperationType.Move => $"Move {count} document(s) to the selected collection?",
            _ => $"Perform this operation on {count} document(s)?"
        };
    }
}

/// <summary>
/// Supporting view models
/// </summary>
public class DocumentStatisticsViewModel
{
    public int TotalDocuments { get; set; }
    public long TotalSize { get; set; }
    public string FormattedTotalSize => FormatFileSize(TotalSize);
    public int FavoriteCount { get; set; }
    public int ProcessingCount { get; set; }
    public Dictionary<string, int> ContentTypeDistribution { get; set; } = new();
    public Dictionary<string, int> StatusDistribution { get; set; } = new();

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class DocumentVersionViewModel
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
    public long Size { get; set; }
    public bool IsCurrent { get; set; }
}

public class DocumentCommentViewModel
{
    public Guid Id { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class DocumentAnalyticsViewModel
{
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public int ShareCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public List<DocumentViewEvent> RecentViews { get; set; } = new();
    public Dictionary<string, int> ViewsByDay { get; set; } = new();
}

public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int StartItem => (CurrentPage - 1) * PageSize + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
}

/// <summary>
/// Enums for view models
/// </summary>
public enum ViewMode
{
    Grid = 0,
    List = 1
}

public enum DocumentStatus
{
    Draft = 0,
    Processing = 1,
    Ready = 2,
    Error = 3,
    Archived = 4,
    Deleted = 5
}

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}

public enum BatchOperationType
{
    Move = 0,
    Delete = 1,
    Tag = 2,
    Archive = 3,
    Restore = 4,
    Download = 5,
    UpdateMetadata = 6,
    ChangeCollection = 7
}

public class DocumentPermissions
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

public class DocumentViewEvent
{
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public TimeSpan? Duration { get; set; }
}

public class SelectListItem
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
