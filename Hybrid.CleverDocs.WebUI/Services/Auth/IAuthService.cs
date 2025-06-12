using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<bool> LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task<UserProfile?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> HasRoleAsync(UserRole role);
    Task<bool> HasPermissionAsync(string permission);
    event Action<UserProfile?> AuthenticationStateChanged;
}