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

        [Required]
        public long Size { get; set; }

        [Required]
        public long SizeBytes { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? FileHash { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int Status { get; set; } = (int)DocumentStatus.Draft;

        [MaxLength(500)]
        public string? StatusMessage { get; set; }

        // Document Management Features
        public bool IsFavorite { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public DateTime? LastViewedAt { get; set; }
        public string Version { get; set; } = "1.0";
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