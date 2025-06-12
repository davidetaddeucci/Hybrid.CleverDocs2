using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.UI.DTOs.User;

namespace Hybrid.CleverDocs2.WebServices.Services.UI.Services
{
    /// <summary>
    /// Service interface for WebUI user management operations
    /// Handles multitenant user operations for the WebUI layer
    /// </summary>
    public interface IUserService
    {
        // User management for WebUI
        Task<UserDto?> GetUserAsync(int userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<List<UserDto>?> GetUsersByCompanyAsync(int companyId);
        Task<UserDto?> CreateUserAsync(CreateUserRequest request);
        Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int userId);
        
        // Authentication for WebUI
        Task<AuthenticationResult?> AuthenticateAsync(string email, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email);
        
        // User preferences and settings
        Task<UserPreferencesDto?> GetUserPreferencesAsync(int userId);
        Task<bool> UpdateUserPreferencesAsync(int userId, UserPreferencesDto preferences);
        
        // User activity and audit
        Task<List<UserActivityDto>?> GetUserActivityAsync(int userId, int days = 30);
        Task LogUserActivityAsync(int userId, string action, string details);
    }
}