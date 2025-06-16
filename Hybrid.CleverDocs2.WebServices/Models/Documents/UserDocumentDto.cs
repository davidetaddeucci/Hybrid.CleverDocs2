using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Models.Documents;

/// <summary>
/// DTO for user document information
/// </summary>
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
}

/// <summary>
/// Document query parameters for search and filtering
/// </summary>
public class DocumentQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "updated_at";
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public Guid? CollectionId { get; set; }
    public Guid? UserId { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Filters { get; set; } = new();
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public List<string> ContentTypes { get; set; } = new();
    public List<DocumentStatus> Statuses { get; set; } = new();
    public bool? IsFavorite { get; set; }
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludePermissions { get; set; } = true;
}

/// <summary>
/// Paginated result for document queries
/// </summary>
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

/// <summary>
/// Document action event arguments
/// </summary>
public class DocumentActionEventArgs
{
    public UserDocumentDto Document { get; set; } = new();
    public DocumentAction Action { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Batch operation request
/// </summary>
public class BatchOperationRequestDto
{
    [Required]
    public BatchOperationType Operation { get; set; }
    
    [Required]
    public List<Guid> DocumentIds { get; set; } = new();
    
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? TargetCollectionId { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Reason { get; set; }
}

/// <summary>
/// Batch operation result
/// </summary>
public class BatchOperationResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationItemResult> Results { get; set; } = new();
    public string? Message { get; set; }
    public Dictionary<string, object> Summary { get; set; } = new();
}

/// <summary>
/// Individual batch operation result
/// </summary>
public class BatchOperationItemResult
{
    public Guid DocumentId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Document metadata update request
/// </summary>
public class DocumentMetadataUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Guid? CollectionId { get; set; }
    public bool? IsFavorite { get; set; }
}

/// <summary>
/// Document permissions
/// </summary>
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



/// <summary>
/// Document view event
/// </summary>
public class DocumentViewEvent
{
    public DateTime Timestamp { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Document search suggestions
/// </summary>
public class DocumentSearchSuggestionsDto
{
    public List<string> Terms { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Collections { get; set; } = new();
    public List<string> ContentTypes { get; set; } = new();
}

/// <summary>
/// Document export request
/// </summary>
public class DocumentExportRequestDto
{
    public List<Guid> DocumentIds { get; set; } = new();
    public DocumentExportFormat Format { get; set; } = DocumentExportFormat.Csv;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeContent { get; set; } = false;
    public string? FileName { get; set; }
}

/// <summary>
/// Document enums
/// </summary>
public enum DocumentStatus
{
    Draft = 0,
    Processing = 1,
    Ready = 2,
    Error = 3,
    Archived = 4,
    Deleted = 5
}

public enum DocumentAction
{
    View = 0,
    Download = 1,
    Edit = 2,
    Delete = 3,
    Share = 4,
    Move = 5,
    Copy = 6,
    Archive = 7,
    Restore = 8,
    Version = 9
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

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}

public enum DocumentExportFormat
{
    Csv = 0,
    Json = 1,
    Excel = 2,
    Pdf = 3
}

/// <summary>
/// Context menu event arguments
/// </summary>
public class ContextMenuEventArgs
{
    public UserDocumentDto Document { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Context menu item
/// </summary>
public class ContextMenuItem
{
    public string Icon { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsDestructive { get; set; }
    public bool IsSeparator { get; set; }
    public bool IsDisabled { get; set; }
    public string? Shortcut { get; set; }
}
