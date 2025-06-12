using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

[Table("companies")]
public class Company
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("logo_url")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("address")]
    [MaxLength(500)]
    public string? Address { get; set; }

    [Column("website")]
    [MaxLength(255)]
    public string? Website { get; set; }

    [Column("industry")]
    [MaxLength(100)]
    public string? Industry { get; set; }

    [Column("size")]
    [MaxLength(50)]
    public string? Size { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("subscription_plan")]
    [MaxLength(50)]
    public string SubscriptionPlan { get; set; } = "Free";

    [Column("subscription_expires")]
    public DateTime? SubscriptionExpires { get; set; }

    [Column("max_users")]
    public int MaxUsers { get; set; } = 10;

    [Column("max_documents")]
    public int MaxDocuments { get; set; } = 1000;

    [Column("max_storage_gb")]
    public int MaxStorageGb { get; set; } = 5;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    // Computed properties
    [NotMapped]
    public int UserCount => Users?.Count ?? 0;

    [NotMapped]
    public bool IsSubscriptionActive => SubscriptionExpires == null || SubscriptionExpires > DateTime.UtcNow;

    [NotMapped]
    public bool CanAddUsers => UserCount < MaxUsers;
}