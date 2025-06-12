using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<ApiResponse> RegisterAsync(RegisterRequest request);
        Task<ApiResponse> LogoutAsync();
        Task<LoginResponse> RefreshTokenAsync();
        Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request);
        Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<UserInfo?> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
        string? GetStoredToken();
        Task ClearTokensAsync();
        bool IsAuthenticated { get; }
        string? UserRole { get; }
    }
}