using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

[Table("user_sessions")]
public class UserSession
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("user_id")]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("session_token")]
    [MaxLength(255)]
    public string SessionToken { get; set; } = string.Empty;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Column("device_info")]
    [MaxLength(255)]
    public string? DeviceInfo { get; set; }

    [Column("location")]
    [MaxLength(255)]
    public string? Location { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public TimeSpan TimeSinceLastActivity => DateTime.UtcNow - LastActivity;

    [NotMapped]
    public bool IsStale => TimeSinceLastActivity.TotalHours > 24;
}