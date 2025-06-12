using Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.WebUI.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthResult> RefreshTokenAsync(string token, string? ipAddress = null);
    Task<bool> RevokeTokenAsync(string token, string? ipAddress = null);
    Task<bool> LogoutAsync(string userId, string? sessionToken = null);
    Task<bool> LogoutAllSessionsAsync(string userId);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> ValidateTokenAsync(string token);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string token, string newPassword);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendEmailVerificationAsync(string email);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(string userId);
    Task<bool> RevokeSessionAsync(string sessionId);
}

public record LoginRequest(string Email, string Password, bool RememberMe = false);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? CompanyId = null,
    string Role = "User"
);

public record AuthResult(
    bool Success,
    string? Message = null,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null,
    UserDto? User = null,
    IEnumerable<string>? Errors = null
);

public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? CompanyId,
    string? CompanyName,
    string? AvatarUrl,
    bool IsActive,
    bool IsEmailVerified,
    DateTime? LastLogin,
    DateTime CreatedAt
);

public record TokenValidationResult(
    bool IsValid,
    string? UserId = null,
    string? Role = null,
    string? CompanyId = null,
    DateTime? ExpiresAt = null
);