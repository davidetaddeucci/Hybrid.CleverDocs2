using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IAuthClient
    {
        // Authentication operations
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);
        Task<MessageResponse?> LogoutAsync(LogoutRequest request);

        // User registration and management
        Task<UserCreateResponse?> RegisterUserAsync(UserRequest request);
        Task<UserResponse?> GetUserAsync(string userId);
        Task<UserResponse?> GetCurrentUserAsync();
        Task<UserListResponse?> ListUsersAsync(int offset = 0, int limit = 100);
        Task<UserResponse?> UpdateUserAsync(string userId, UserUpdateRequest request);
        Task<UserResponse?> UpdateCurrentUserAsync(UserUpdateRequest request);
        Task DeleteUserAsync(string userId);
        Task DeleteCurrentUserAsync();

        // Password management
        Task<MessageResponse?> RequestPasswordResetAsync(PasswordResetRequest request);
        Task<MessageResponse?> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request);
        Task<MessageResponse?> ChangePasswordAsync(ChangePasswordRequest request);

        // Email verification
        Task<MessageResponse?> VerifyEmailAsync(EmailVerificationRequest request);
        Task<MessageResponse?> ResendVerificationEmailAsync(ResendVerificationRequest request);

        // User status management
        Task<UserResponse?> DeactivateUserAsync(string userId);
        Task<UserResponse?> ActivateUserAsync(string userId);
        Task<UserResponse?> MakeSuperuserAsync(string userId);
        Task<UserResponse?> RemoveSuperuserAsync(string userId);

        // Health check
        Task<MessageResponse?> HealthCheckAsync();
    }
}
