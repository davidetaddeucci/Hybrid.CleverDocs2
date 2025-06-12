using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Hybrid.CleverDocs2.WebServices.Data.Entities;

namespace Hybrid.CleverDocs2.WebServices.Services.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly ILogger<JwtService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public JwtService(
            IConfiguration configuration,
            IDistributedCache cache,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _cache = cache;
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
                new(ClaimTypes.Role, user.Role.ToString()),
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
                var key = $"blacklist:token:{GetTokenHash(token)}";
                var result = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(result);
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
                var key = $"blacklist:token:{GetTokenHash(token)}";
                var options = new DistributedCacheEntryOptions();
                
                if (expiry.HasValue)
                {
                    options.SetAbsoluteExpiration(expiry.Value);
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
                            options.SetAbsoluteExpiration(jwtToken.ValidTo);
                        }
                    }
                }

                await _cache.SetStringAsync(key, "blacklisted", options);
                _logger.LogDebug("Token blacklisted with key {Key}", key);
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
                var key = $"refresh:token:{userId}";
                return await _cache.GetStringAsync(key);
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
                var key = $"refresh:token:{userId}";
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                };

                await _cache.SetStringAsync(key, refreshToken, options);
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
                var key = $"refresh:token:{userId}";
                await _cache.RemoveAsync(key);
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
                // Revoke refresh token
                await RevokeRefreshTokenAsync(userId);

                // Add user to global blacklist (all tokens issued before this time are invalid)
                var key = $"user:blacklist:{userId}";
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_refreshTokenExpirationDays)
                };

                await _cache.SetStringAsync(key, DateTime.UtcNow.ToString("O"), options);
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