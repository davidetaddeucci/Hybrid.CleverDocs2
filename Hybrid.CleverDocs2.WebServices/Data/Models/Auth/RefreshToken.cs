using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("token")]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Column("expires")]
    public DateTime Expires { get; set; }

    [Column("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [Column("created_by_ip")]
    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    [Column("revoked")]
    public DateTime? Revoked { get; set; }

    [Column("revoked_by_ip")]
    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    [Column("replaced_by_token")]
    [MaxLength(255)]
    public string? ReplacedByToken { get; set; }

    [Column("reason_revoked")]
    [MaxLength(255)]
    public string? ReasonRevoked { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    [NotMapped]
    public bool IsRevoked => Revoked != null;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}