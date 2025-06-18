using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// TenantId for multi-tenant architecture. Each company is a separate tenant.
        /// This should match the Company.Id for consistency.
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Subscription and limits
        public int MaxUsers { get; set; } = 10;
        public int MaxDocuments { get; set; } = 1000;
        public long MaxStorageBytes { get; set; } = 1073741824; // 1GB default
        public int MaxCollections { get; set; } = 50;

        // R2R Configuration
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }

        /// <summary>
        /// R2R Tenant ID for direct reference to R2R tenant entity
        /// </summary>
        [MaxLength(255)]
        public string? R2RTenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<Collection> Collections { get; set; } = new List<Collection>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}