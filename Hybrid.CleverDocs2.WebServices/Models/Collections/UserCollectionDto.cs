using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Models.Collections;

/// <summary>
/// DTO for user collection display and operations
/// </summary>
public class UserCollectionDto
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
    
    public CollectionStatsDto Stats { get; set; } = new();
    
    public List<string> Tags { get; set; } = new();
    
    public CollectionPermissionsDto Permissions { get; set; } = new();
}

/// <summary>
/// DTO for creating a new collection
/// </summary>
public class CreateUserCollectionDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    public string? Color { get; set; }
    
    [StringLength(50)]
    public string? Icon { get; set; }
    
    public List<string> Tags { get; set; } = new();
    
    public string UserId { get; set; } = string.Empty;
    
    public bool SetAsFavorite { get; set; } = false;
}

/// <summary>
/// DTO for updating an existing collection
/// </summary>
public class UpdateUserCollectionDto
{
    [Required]
    public Guid CollectionId { get; set; }
    
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    public string? Color { get; set; }
    
    [StringLength(50)]
    public string? Icon { get; set; }
    
    public List<string>? Tags { get; set; }
    
    public bool? IsFavorite { get; set; }
    
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for collection statistics
/// </summary>
public class CollectionStatsDto
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
/// DTO for collection permissions
/// </summary>
public class CollectionPermissionsDto
{
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanShare { get; set; } = true;
    public bool CanAddDocuments { get; set; } = true;
    public bool CanRemoveDocuments { get; set; } = true;
    public bool IsOwner { get; set; } = true;
}

/// <summary>
/// DTO for collection search and filtering
/// </summary>
public class CollectionSearchDto
{
    public string? SearchTerm { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? IsShared { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int? MinDocuments { get; set; }
    public int? MaxDocuments { get; set; }
    public string SortBy { get; set; } = "UpdatedAt";
    public string SortDirection { get; set; } = "DESC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO for collection suggestions
/// </summary>
public class CollectionSuggestionDto
{
    public string SuggestedName { get; set; } = string.Empty;
    public string? SuggestedDescription { get; set; }
    public string SuggestedColor { get; set; } = string.Empty;
    public string SuggestedIcon { get; set; } = string.Empty;
    public List<string> SuggestedTags { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// DTO for bulk collection operations
/// </summary>
public class BulkCollectionOperationDto
{
    public List<Guid> CollectionIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "delete", "favorite", "unfavorite", "tag", "untag"
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for collection reordering
/// </summary>
public class ReorderCollectionsDto
{
    public List<CollectionOrderDto> Collections { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
}

public class CollectionOrderDto
{
    public Guid CollectionId { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// DTO for R2R collection synchronization
/// </summary>
public class R2RCollectionSyncDto
{
    public Guid CollectionId { get; set; }
    public string? R2RCollectionId { get; set; }
    public string Operation { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"
    public Dictionary<string, object> R2RData { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response DTO for collection operations
/// </summary>
public class CollectionOperationResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserCollectionDto? Collection { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DTO for collection analytics
/// </summary>
public class CollectionAnalyticsDto
{
    public Guid CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int DocumentAddCount { get; set; }
    public int DocumentRemoveCount { get; set; }
    public int ShareCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<CollectionActivityDto> RecentActivities { get; set; } = new();
    public Dictionary<string, int> ActivityByDay { get; set; } = new();
    public Dictionary<string, int> DocumentTypeDistribution { get; set; } = new();
}

public class CollectionActivityDto
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
