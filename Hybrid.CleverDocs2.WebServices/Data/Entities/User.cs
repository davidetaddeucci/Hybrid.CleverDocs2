using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Full name for R2R compatibility. Combines FirstName and LastName.
        /// </summary>
        [MaxLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// User bio/description for R2R compatibility.
        /// </summary>
        [MaxLength(1000)]
        public string? Bio { get; set; }

        /// <summary>
        /// Profile picture URL for R2R compatibility.
        /// </summary>
        [MaxLength(500)]
        public string? ProfilePicture { get; set; }

        /// <summary>
        /// Email verification status for R2R compatibility.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// R2R User ID for direct reference to R2R user entity
        /// </summary>
        [MaxLength(255)]
        public string? R2RUserId { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; } = null!;

        public bool IsActive { get; set; } = true;
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Computed properties for R2R compatibility
        [NotMapped]
        public string FullName => Name ?? $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public bool IsEmailVerified => IsVerified;

        // Navigation properties
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<Collection> Collections { get; set; } = new List<Collection>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public enum UserRole
    {
        Admin = 1,
        Company = 2,
        User = 3
    }
}