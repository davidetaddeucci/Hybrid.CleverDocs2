using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password, string? ipAddress = null, string? userAgent = null);
        Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);
        Task<bool> LogoutAsync(Guid userId, string? accessToken = null);
        Task<User> RegisterUserAsync(string email, string password, string firstName, string lastName, 
            Guid companyId, UserRole role = UserRole.User, string? createdBy = null);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> ConfirmPasswordResetAsync(string token, string newPassword);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> ResendEmailVerificationAsync(string email);
        Task<bool> UpdateUserProfileAsync(Guid userId, string? name = null, string? bio = null,
            string? profilePicture = null, string? firstName = null, string? lastName = null);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);
        Task<bool> ValidatePasswordAsync(User user, string password);
        Task<string> HashPasswordAsync(string password);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}