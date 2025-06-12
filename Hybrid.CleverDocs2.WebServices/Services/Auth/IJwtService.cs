using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Data.Entities;

namespace Hybrid.CleverDocs2.WebServices.Services.Auth
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task BlacklistTokenAsync(string token, TimeSpan? expiry = null);
        Task<string?> GetRefreshTokenAsync(Guid userId);
        Task SaveRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry);
        Task RevokeRefreshTokenAsync(Guid userId);
        Task RevokeAllUserTokensAsync(Guid userId);
    }
}