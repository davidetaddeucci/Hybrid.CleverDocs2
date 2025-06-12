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
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

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
        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

        [MaxLength(500)]
        public string? StatusMessage { get; set; }

        // R2R Integration
        public string? R2RDocumentId { get; set; }
        public string? R2RIngestionJobId { get; set; }
        public DateTime? R2RProcessedAt { get; set; }

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
        Uploaded = 1,
        Processing = 2,
        Processed = 3,
        Failed = 4,
        Deleted = 5
    }
}