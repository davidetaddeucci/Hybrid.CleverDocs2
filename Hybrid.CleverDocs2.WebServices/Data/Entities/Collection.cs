using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public class Collection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = false;

        // UI Properties
        [MaxLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        public string Color { get; set; } = "#3B82F6";

        [MaxLength(50)]
        public string Icon { get; set; } = "folder";

        // Tags stored as JSON array
        public string? TagsJson { get; set; }

        public bool IsFavorite { get; set; } = false;

        // R2R Integration
        public string? R2RCollectionId { get; set; }

        // ✅ COLLECTIONS SYNC - Sync tracking
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// R2R Graph cluster status for knowledge graph integration
        /// </summary>
        [MaxLength(50)]
        public string? GraphClusterStatus { get; set; }

        /// <summary>
        /// R2R Graph sync status for knowledge graph synchronization
        /// </summary>
        [MaxLength(50)]
        public string? GraphSyncStatus { get; set; }

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

        // Helper property for Tags (not mapped to database)
        [NotMapped]
        public List<string> Tags
        {
            get => string.IsNullOrEmpty(TagsJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
            set => TagsJson = value?.Any() == true
                ? System.Text.Json.JsonSerializer.Serialize(value)
                : null;
        }

        // Navigation properties
        public ICollection<CollectionDocument> CollectionDocuments { get; set; } = new List<CollectionDocument>();

        // ✅ COLLECTIONS SYNC - Helper navigation property for documents
        [NotMapped]
        public ICollection<Document> Documents => CollectionDocuments.Select(cd => cd.Document).ToList();

        // R2R Compatibility properties
        [NotMapped]
        public Guid OwnerId => UserId;

        [NotMapped]
        public int DocumentCount => CollectionDocuments.Count;

        [NotMapped]
        public List<Guid> UserIds => new() { UserId }; // In our system, each collection has one owner
    }

    public class CollectionDocument
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public Collection Collection { get; set; } = null!;

        [Required]
        public Guid DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document Document { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public string? AddedBy { get; set; }
    }
}