using System.ComponentModel.DataAnnotations;

namespace Hybrid.CleverDocs2.WebServices.Models.Companies
{
    /// <summary>
    /// DTO for company display and operations
    /// </summary>
    public class CompanyDto
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(255)]
        public string? ContactEmail { get; set; }
        
        [StringLength(50)]
        public string? ContactPhone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Subscription and limits
        public int MaxUsers { get; set; } = 10;
        public int MaxDocuments { get; set; } = 1000;
        public long MaxStorageBytes { get; set; } = 1073741824; // 1GB default
        public int MaxCollections { get; set; } = 50;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        
        // R2R Integration fields
        public Guid TenantId { get; set; }
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
        public string? R2RTenantId { get; set; }
        
        // Statistics
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int CollectionCount { get; set; }
        public long StorageUsed { get; set; }
        
        public string FormattedStorageUsed => FormatFileSize(StorageUsed);
        public string FormattedMaxStorage => FormatFileSize(MaxStorageBytes);
        
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// DTO for creating a new company
    /// </summary>
    public class CreateCompanyDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(255)]
        public string? ContactEmail { get; set; }
        
        [StringLength(50)]
        public string? ContactPhone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        // Subscription and limits
        public int MaxUsers { get; set; } = 10;
        public int MaxDocuments { get; set; } = 1000;
        public long MaxStorageBytes { get; set; } = 1073741824; // 1GB default
        public int MaxCollections { get; set; } = 50;
        
        // R2R Configuration
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
    }

    /// <summary>
    /// DTO for updating a company
    /// </summary>
    public class UpdateCompanyDto
    {
        [StringLength(255, MinimumLength = 1)]
        public string? Name { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(255)]
        public string? ContactEmail { get; set; }
        
        [StringLength(50)]
        public string? ContactPhone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        public bool? IsActive { get; set; }
        
        // Subscription and limits
        public int? MaxUsers { get; set; }
        public int? MaxDocuments { get; set; }
        public long? MaxStorageBytes { get; set; }
        public int? MaxCollections { get; set; }
        
        // R2R Configuration
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
    }

    /// <summary>
    /// DTO for company statistics
    /// </summary>
    public class CompanyStatsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int CollectionCount { get; set; }
        public long StorageUsed { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
        public string? R2RTenantId { get; set; }
    }
}
