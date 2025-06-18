using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Models.Auth
{
    public class TokenBlacklist
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public Guid? UserId { get; set; }
        
        [MaxLength(50)]
        public string? Reason { get; set; }
    }
    
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        [MaxLength(100)]
        public string? RevokedReason { get; set; }
        
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
