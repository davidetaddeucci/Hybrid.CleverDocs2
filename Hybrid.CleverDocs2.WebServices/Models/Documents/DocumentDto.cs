using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Models.Documents;

/// <summary>
/// DTO for document display and operations
/// </summary>
public class DocumentDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public long SizeBytes { get; set; }
    
    public string FormattedSize => FormatFileSize(SizeBytes);
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime? LastAccessedAt { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public string? TenantId { get; set; }
    
    public Guid? CollectionId { get; set; }
    
    public string? CollectionName { get; set; }
    
    public DocumentStatusDto Status { get; set; } = DocumentStatusDto.Uploaded;
    
    public DocumentMetadataDto Metadata { get; set; } = new();
    
    public List<string> Tags { get; set; } = new();
    
    public DocumentPermissionsDto Permissions { get; set; } = new();
    
    public DocumentVersionInfoDto VersionInfo { get; set; } = new();
    
    public DocumentProcessingInfoDto ProcessingInfo { get; set; } = new();
    
    public string? R2RDocumentId { get; set; }
    
    public string? PreviewUrl { get; set; }
    
    public string? DownloadUrl { get; set; }
    
    public bool IsProcessing => Status == DocumentStatusDto.Processing || Status == DocumentStatusDto.Extracting;
    
    public bool IsReady => Status == DocumentStatusDto.Ready;
    
    public bool HasError => Status == DocumentStatusDto.Error;
    
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

/// <summary>
/// DTO for creating a new document
/// </summary>
public class CreateDocumentDto
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public Guid? CollectionId { get; set; }
    
    public List<string> Tags { get; set; } = new();
    
    public string UserId { get; set; } = string.Empty;
    
    public bool ExtractMetadata { get; set; } = true;
    
    public bool PerformOCR { get; set; } = true;
    
    public bool AutoDetectLanguage { get; set; } = true;
    
    public string? Language { get; set; }
    
    public DocumentProcessingOptionsDto ProcessingOptions { get; set; } = new();
}

/// <summary>
/// DTO for updating an existing document
/// </summary>
public class UpdateDocumentDto
{
    [Required]
    public Guid DocumentId { get; set; }
    
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public Guid? CollectionId { get; set; }
    
    public List<string>? Tags { get; set; }
    
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO for document upload
/// </summary>
public class DocumentUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    [StringLength(255)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public Guid? CollectionId { get; set; }
    
    public List<string> Tags { get; set; } = new();
    
    public bool ExtractMetadata { get; set; } = true;
    
    public bool PerformOCR { get; set; } = true;
    
    public bool AutoDetectLanguage { get; set; } = true;
    
    public string? Language { get; set; }
    
    public DocumentProcessingOptionsDto ProcessingOptions { get; set; } = new();
}

/// <summary>
/// DTO for document metadata
/// </summary>
public class DocumentMetadataDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public string? Creator { get; set; }
    public string? Producer { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }
    public string? Language { get; set; }
    public int? PageCount { get; set; }
    public int? WordCount { get; set; }
    public int? CharacterCount { get; set; }
    public string? Format { get; set; }
    public int Version { get; set; } = 1;
    public Dictionary<string, object> CustomProperties { get; set; } = new();
    public DocumentContentInfoDto ContentInfo { get; set; } = new();
}

/// <summary>
/// DTO for document content information
/// </summary>
public class DocumentContentInfoDto
{
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public List<string> DetectedLanguages { get; set; } = new();
    public double? ConfidenceScore { get; set; }
    public List<DocumentEntityDto> Entities { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public DocumentSentimentDto? Sentiment { get; set; }
}

/// <summary>
/// DTO for document entities
/// </summary>
public class DocumentEntityDto
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}

/// <summary>
/// DTO for document sentiment analysis
/// </summary>
public class DocumentSentimentDto
{
    public string Sentiment { get; set; } = string.Empty; // Positive, Negative, Neutral
    public double Score { get; set; }
    public double Confidence { get; set; }
}

/// <summary>
/// DTO for document permissions
/// </summary>
public class DocumentPermissionsDto
{
    public bool CanRead { get; set; } = true;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanShare { get; set; } = true;
    public bool CanDownload { get; set; } = true;
    public bool CanPrint { get; set; } = true;
    public bool IsOwner { get; set; } = true;
}

/// <summary>
/// DTO for document version information
/// </summary>
public class DocumentVersionInfoDto
{
    public int CurrentVersion { get; set; } = 1;
    public int TotalVersions { get; set; } = 1;
    public DateTime LastVersionDate { get; set; } = DateTime.UtcNow;
    public string LastVersionBy { get; set; } = string.Empty;
    public List<DocumentVersionDto> Versions { get; set; } = new();
}

/// <summary>
/// DTO for individual document version
/// </summary>
public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// DTO for document processing information
/// </summary>
public class DocumentProcessingInfoDto
{
    public DateTime? ProcessingStarted { get; set; }
    public DateTime? ProcessingCompleted { get; set; }
    public TimeSpan? ProcessingDuration => ProcessingCompleted - ProcessingStarted;
    public string? ProcessingEngine { get; set; }
    public List<DocumentProcessingStepDto> Steps { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// DTO for document processing steps
/// </summary>
public class DocumentProcessingStepDto
{
    public string Name { get; set; } = string.Empty;
    public DocumentStepStatusDto Status { get; set; } = DocumentStepStatusDto.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DTO for document processing options
/// </summary>
public class DocumentProcessingOptionsDto
{
    public bool ExtractText { get; set; } = true;
    public bool ExtractMetadata { get; set; } = true;
    public bool PerformOCR { get; set; } = true;
    public bool DetectLanguage { get; set; } = true;
    public bool ExtractEntities { get; set; } = true;
    public bool AnalyzeSentiment { get; set; } = false;
    public bool GenerateSummary { get; set; } = false;
    public bool ExtractKeywords { get; set; } = true;
    public string? PreferredLanguage { get; set; }
    public int? MaxSummaryLength { get; set; } = 500;
    public double? ConfidenceThreshold { get; set; } = 0.7;
}

/// <summary>
/// Document status enumeration
/// </summary>
public enum DocumentStatusDto
{
    Uploaded = 0,
    Processing = 1,
    Extracting = 2,
    Ready = 3,
    Error = 4,
    Archived = 5,
    Deleted = 6
}

/// <summary>
/// Document processing step status enumeration
/// </summary>
public enum DocumentStepStatusDto
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}

/// <summary>
/// DTO for document search
/// </summary>
public class DocumentSearchDto
{
    public string? SearchTerm { get; set; }
    public List<Guid> CollectionIds { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> ContentTypes { get; set; } = new();
    public DocumentStatusDto? Status { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? ModifiedAfter { get; set; }
    public DateTime? ModifiedBefore { get; set; }
    public long? MinSizeBytes { get; set; }
    public long? MaxSizeBytes { get; set; }
    public string? CreatedBy { get; set; }
    public bool? HasMetadata { get; set; }
    public string SortBy { get; set; } = "UpdatedAt";
    public string SortDirection { get; set; } = "DESC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeContent { get; set; } = false;
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// DTO for bulk document operations
/// </summary>
public class BulkDocumentOperationDto
{
    public List<Guid> DocumentIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "delete", "move", "tag", "untag", "archive"
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Guid UserId { get; set; }
}

/// <summary>
/// Response DTO for document operations
/// </summary>
public class DocumentOperationResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DocumentDto? Document { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DTO for document analytics
/// </summary>
public class DocumentAnalyticsDto
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public int ShareCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<DocumentActivityDto> RecentActivities { get; set; } = new();
    public Dictionary<string, int> ActivityByDay { get; set; } = new();
    public Dictionary<string, int> AccessByUser { get; set; } = new();
}

/// <summary>
/// DTO for document activity
/// </summary>
public class DocumentActivityDto
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
