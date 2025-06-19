using System.ComponentModel.DataAnnotations;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;
using Hybrid.CleverDocs.WebUI.ViewModels.Companies;

namespace Hybrid.CleverDocs.WebUI.ViewModels.Users
{
    /// <summary>
    /// User DTO for API responses
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsVerified { get; set; }
        public string? R2RUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// User search parameters
    /// </summary>
    public class UserSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public Guid? CompanyId { get; set; }
        public string? Role { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// User list view model
    /// </summary>
    public class UserListViewModel
    {
        public List<UserDto> Users { get; set; } = new();
        public UserSearchViewModel Search { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public bool HasActiveFilters { get; set; }
        public List<CompanyDto> Companies { get; set; } = new();
    }

    /// <summary>
    /// User details view model
    /// </summary>
    public class UserDetailsViewModel
    {
        public UserDto User { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// Create user view model
    /// </summary>
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for profile picture")]
        public string? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        [Required(ErrorMessage = "Company is required")]
        public Guid CompanyId { get; set; }

        // For dropdown
        public List<CompanyDto> Companies { get; set; } = new();
    }

    /// <summary>
    /// Edit user view model
    /// </summary>
    public class EditUserViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for profile picture")]
        public string? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        public Guid CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? R2RUserId { get; set; }

        // For dropdown
        public List<CompanyDto> Companies { get; set; } = new();
    }

    /// <summary>
    /// Create user DTO for API calls
    /// </summary>
    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = "User";
        public Guid CompanyId { get; set; }
    }

    /// <summary>
    /// Update user DTO for API calls
    /// </summary>
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Company user list view model (for company managers)
    /// </summary>
    public class CompanyUserListViewModel
    {
        public List<UserDto> Users { get; set; } = new();
        public UserSearchViewModel Search { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public bool HasActiveFilters { get; set; }
        public CompanyDto Company { get; set; } = new();
    }

    /// <summary>
    /// Create user view model for company managers
    /// </summary>
    public class CreateCompanyUserViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for profile picture")]
        public string? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        // Company is automatically set from logged user
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Edit user view model for company managers
    /// </summary>
    public class EditCompanyUserViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for profile picture")]
        public string? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? R2RUserId { get; set; }
    }

    /// <summary>
    /// Available user roles
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Company = "Company";
        public const string User = "User";

        public static List<string> GetAll()
        {
            return new List<string> { Admin, Company, User };
        }

        public static List<(string Value, string Display)> GetAllWithDisplay()
        {
            return new List<(string, string)>
            {
                (Admin, "Administrator"),
                (Company, "Company Manager"),
                (User, "Standard User")
            };
        }

        public static List<(string Value, string Display)> GetCompanyRoles()
        {
            return new List<(string, string)>
            {
                (Company, "Company Manager"),
                (User, "Standard User")
            };
        }
    }
}
