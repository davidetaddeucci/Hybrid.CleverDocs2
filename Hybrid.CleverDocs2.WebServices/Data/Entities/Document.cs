using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public class Document
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes - R2R compatible field
        /// </summary>
        [Required]
        public long SizeInBytes { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string FileHash { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int Status { get; set; } = (int)DocumentStatus.Draft;

        [MaxLength(500)]
        public string? StatusMessage { get; set; }

        /// <summary>
        /// R2R compatible document type (file extension based)
        /// </summary>
        [MaxLength(10)]
        public string? DocumentType { get; set; }

        /// <summary>
        /// R2R compatible ingestion status
        /// </summary>
        [MaxLength(50)]
        public string IngestionStatus { get; set; } = "pending";

        /// <summary>
        /// R2R compatible extraction status for knowledge graphs
        /// </summary>
        [MaxLength(50)]
        public string ExtractionStatus { get; set; } = "pending";

        // Document Management Features
        public bool IsFavorite { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public DateTime? LastViewedAt { get; set; }
        public int Version { get; set; } = 1;
        public bool HasVersions { get; set; } = false;
        public bool HasThumbnail { get; set; } = false;
        public bool IsProcessing { get; set; } = false;
        public double? ProcessingProgress { get; set; }
        public string? ProcessingError { get; set; }

        // Tags and Metadata
        [Column(TypeName = "jsonb")]
        public List<string> Tags { get; set; } = new();

        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        // R2R Integration
        public string? R2RDocumentId { get; set; }
        public string? R2RIngestionJobId { get; set; }
        public string? R2RTaskId { get; set; } // R2R API Task ID for progress tracking
        public DateTime? R2RProcessedAt { get; set; }

        // Collection Association
        public Guid? CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public Collection? Collection { get; set; }

        // Tenant isolation
        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // R2R Compatibility properties
        [NotMapped]
        public Guid OwnerId => UserId;

        [NotMapped]
        public List<Guid> CollectionIds => CollectionDocuments.Select(cd => cd.CollectionId).ToList();

        [NotMapped]
        public string Title => Name;

        [NotMapped]
        public long Size => SizeInBytes; // Backward compatibility

        // Navigation properties
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
        public ICollection<CollectionDocument> CollectionDocuments { get; set; } = new List<CollectionDocument>();
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
}