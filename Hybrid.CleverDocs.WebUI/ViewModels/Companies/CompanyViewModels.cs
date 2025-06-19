using System.ComponentModel.DataAnnotations;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;

namespace Hybrid.CleverDocs.WebUI.ViewModels.Companies
{
    /// <summary>
    /// Company DTO for API responses
    /// </summary>
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDocuments { get; set; }
        public long MaxStorageBytes { get; set; }
        public int MaxCollections { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public Guid TenantId { get; set; }
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
        public string? R2RTenantId { get; set; }
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int CollectionCount { get; set; }
        public long StorageUsed { get; set; }
    }

    /// <summary>
    /// Company search parameters
    /// </summary>
    public class CompanySearchViewModel
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Company list view model
    /// </summary>
    public class CompanyListViewModel
    {
        public List<CompanyDto> Companies { get; set; } = new();
        public CompanySearchViewModel Search { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public bool HasActiveFilters { get; set; }
    }

    /// <summary>
    /// Company details view model
    /// </summary>
    public class CompanyDetailsViewModel
    {
        public CompanyDto Company { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// Create company view model
    /// </summary>
    public class CreateCompanyViewModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "Please enter a valid website URL")]
        public string? Website { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? ContactPhone { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [Range(1, 10000, ErrorMessage = "Max users must be between 1 and 10,000")]
        public int MaxUsers { get; set; } = 50;

        [Range(1, 1000000, ErrorMessage = "Max documents must be between 1 and 1,000,000")]
        public int MaxDocuments { get; set; } = 10000;

        [Range(1, long.MaxValue, ErrorMessage = "Max storage must be greater than 0")]
        public long MaxStorageBytes { get; set; } = 10737418240; // 10GB

        [Range(1, 10000, ErrorMessage = "Max collections must be between 1 and 10,000")]
        public int MaxCollections { get; set; } = 500;

        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
    }

    /// <summary>
    /// Edit company view model
    /// </summary>
    public class EditCompanyViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "Please enter a valid website URL")]
        public string? Website { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? ContactPhone { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 10000, ErrorMessage = "Max users must be between 1 and 10,000")]
        public int MaxUsers { get; set; }

        [Range(1, 1000000, ErrorMessage = "Max documents must be between 1 and 1,000,000")]
        public int MaxDocuments { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Max storage must be greater than 0")]
        public long MaxStorageBytes { get; set; }

        [Range(1, 10000, ErrorMessage = "Max collections must be between 1 and 10,000")]
        public int MaxCollections { get; set; }

        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
        public string? R2RTenantId { get; set; }
    }

    /// <summary>
    /// Create company DTO for API calls
    /// </summary>
    public class CreateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDocuments { get; set; }
        public long MaxStorageBytes { get; set; }
        public int MaxCollections { get; set; }
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
    }

    /// <summary>
    /// Update company DTO for API calls
    /// </summary>
    public class UpdateCompanyDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxDocuments { get; set; }
        public long? MaxStorageBytes { get; set; }
        public int? MaxCollections { get; set; }
        public string? R2RApiKey { get; set; }
        public string? R2RConfiguration { get; set; }
    }


}
