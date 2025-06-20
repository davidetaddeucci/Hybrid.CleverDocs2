using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JwtService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public JwtService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;

            var jwtSection = _configuration.GetSection("Jwt");
            _secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            _issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            _audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            _expirationMinutes = jwtSection.GetValue<int>("ExpirationMinutes");
            _refreshTokenExpirationDays = jwtSection.GetValue<int>("RefreshTokenExpirationDays");

            if (_secretKey.Length < 32)
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
        }

        public string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Role, user.Role.ToString()), // This will use enum name (Admin, Company, User)
                new("companyId", user.CompanyId.ToString()),
                new("tenantId", user.CompanyId.ToString()), // For backward compatibility
                new("firstName", user.FirstName),
                new("lastName", user.LastName),
                new("isActive", user.IsActive.ToString()),
                new("isEmailVerified", user.IsEmailVerified.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("Generated access token for user {UserId} with expiry {Expiry}", 
                user.Id, tokenDescriptor.Expires);

            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                if (validatedToken is not JwtSecurityToken jwtToken || 
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            try
            {
                var tokenHash = GetTokenHash(token);
                var blacklistedToken = await _context.TokenBlacklists
                    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.ExpiresAt > DateTime.UtcNow);
                return blacklistedToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token blacklist");
                return false;
            }
        }

        public async Task BlacklistTokenAsync(string token, TimeSpan? expiry = null)
        {
            try
            {
                var tokenHash = GetTokenHash(token);
                var expiresAt = DateTime.UtcNow.AddDays(1); // Default 1 day

                if (expiry.HasValue)
                {
                    expiresAt = DateTime.UtcNow.Add(expiry.Value);
                }
                else
                {
                    // Default to token expiry time
                    var tokenHandler = new JwtSecurityTokenHandler();
                    if (tokenHandler.CanReadToken(token))
                    {
                        var jwtToken = tokenHandler.ReadJwtToken(token);
                        if (jwtToken.ValidTo > DateTime.UtcNow)
                        {
                            expiresAt = jwtToken.ValidTo;
                        }
                    }
                }

                var blacklistEntry = new TokenBlacklist
                {
                    Id = Guid.NewGuid(),
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    Reason = "Manual blacklist"
                };

                _context.TokenBlacklists.Add(blacklistEntry);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Token blacklisted with hash {TokenHash}", tokenHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting token");
            }
        }

        public async Task<string?> GetRefreshTokenAsync(Guid userId)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsActive)
                    .OrderByDescending(rt => rt.CreatedAt)
                    .FirstOrDefaultAsync();

                return refreshToken?.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token for user {UserId}", userId);
                return null;
            }
        }

        public async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry)
        {
            try
            {
                // Revoke existing refresh tokens for this user
                var existingTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsActive)
                    .ToListAsync();

                foreach (var token in existingTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedReason = "New token issued";
                }

                // Create new refresh token
                var newRefreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(expiry),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(newRefreshToken);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Refresh token saved for user {UserId} with expiry {Expiry}", userId, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token for user {UserId}", userId);
            }
        }

        public async Task RevokeRefreshTokenAsync(Guid userId)
        {
            try
            {
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsActive)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedReason = "Manual revocation";
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Refresh token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token for user {UserId}", userId);
            }
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            try
            {
                // Revoke all refresh tokens
                await RevokeRefreshTokenAsync(userId);

                // Add a global blacklist entry for this user (all tokens issued before this time are invalid)
                var blacklistEntry = new TokenBlacklist
                {
                    Id = Guid.NewGuid(),
                    TokenHash = $"user_global_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    Reason = "All tokens revoked"
                };

                _context.TokenBlacklists.Add(blacklistEntry);
                await _context.SaveChangesAsync();

                _logger.LogDebug("All tokens revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            }
        }

        private static string GetTokenHash(string token)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}