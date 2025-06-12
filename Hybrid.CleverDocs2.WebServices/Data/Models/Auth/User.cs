using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    [MaxLength(50)]
    public string Role { get; set; } = "User";

    [Column("company_id")]
    [MaxLength(255)]
    public string? CompanyId { get; set; }

    [Column("avatar_url")]
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; } = false;

    [Column("email_verification_token")]
    [MaxLength(255)]
    public string? EmailVerificationToken { get; set; }

    [Column("password_reset_token")]
    [MaxLength(255)]
    public string? PasswordResetToken { get; set; }

    [Column("password_reset_expires")]
    public DateTime? PasswordResetExpires { get; set; }

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("login_count")]
    public int LoginCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public bool IsCompanyAdmin => Role.Equals("Company", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public bool IsUser => Role.Equals("User", StringComparison.OrdinalIgnoreCase);
}