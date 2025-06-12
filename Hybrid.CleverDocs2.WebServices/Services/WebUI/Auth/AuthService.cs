using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.WebUI.Auth;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt failed for email: {Email} - User not found or inactive", request.Email);
                return new AuthResult(false, "Credenziali non valide");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt failed for email: {Email} - Invalid password", request.Email);
                return new AuthResult(false, "Credenziali non valide");
            }

            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt failed for email: {Email} - Email not verified", request.Email);
                return new AuthResult(false, "Email non verificata. Controlla la tua casella di posta.");
            }

            // Update login statistics
            user.LastLogin = DateTime.UtcNow;
            user.LoginCount++;

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 7),
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // Create user session
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = Guid.NewGuid().ToString(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 1),
                LastActivity = DateTime.UtcNow
            };

            _context.UserSessions.Add(session);

            // Clean up old tokens and sessions
            await CleanupExpiredTokensAsync(user.Id);
            await CleanupExpiredSessionsAsync(user.Id);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged in successfully from IP: {IpAddress}", user.Id, ipAddress);

            var userDto = MapToUserDto(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            return new AuthResult(
                Success: true,
                Message: "Login effettuato con successo",
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAt: expiresAt,
                User: userDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return new AuthResult(false, "Errore interno del server");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string token, string? ipAddress = null)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Refresh token not found or inactive: {Token}", token);
                return new AuthResult(false, "Token non valido");
            }

            var user = refreshToken.User;
            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token user is inactive: {UserId}", user.Id);
                return new AuthResult(false, "Utente non attivo");
            }

            // Revoke old token
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = "Replaced by new token";

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };

            refreshToken.ReplacedByToken = newRefreshToken;
            _context.RefreshTokens.Add(newRefreshTokenEntity);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

            var userDto = MapToUserDto(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            return new AuthResult(
                Success: true,
                Message: "Token aggiornato con successo",
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken,
                ExpiresAt: expiresAt,
                User: userDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new AuthResult(false, "Errore interno del server");
        }
    }

    public async Task<bool> RevokeTokenAsync(string token, string? ipAddress = null)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || refreshToken.IsRevoked)
                return false;

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = "Revoked by user";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token revoked: {Token}", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token: {Token}", token);
            return false;
        }
    }

    public async Task<bool> LogoutAsync(string userId, string? sessionToken = null)
    {
        try
        {
            var query = _context.UserSessions.Where(s => s.UserId == userId && s.IsActive);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                query = query.Where(s => s.SessionToken == sessionToken);
            }

            var sessions = await query.ToListAsync();
            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            // Also revoke refresh tokens
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Revoked == null)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.Revoked = DateTime.UtcNow;
                token.ReasonRevoked = "User logout";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged out", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> LogoutAllSessionsAsync(string userId)
    {
        return await LogoutAsync(userId);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new AuthResult(false, "Un utente con questa email esiste già");
            }

            // Validate company if specified
            Company? company = null;
            if (!string.IsNullOrEmpty(request.CompanyId))
            {
                company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.IsActive);

                if (company == null)
                {
                    return new AuthResult(false, "Azienda non trovata o non attiva");
                }

                // Check company user limits
                var userCount = await _context.Users
                    .CountAsync(u => u.CompanyId == request.CompanyId && u.IsActive);

                if (userCount >= company.MaxUsers)
                {
                    return new AuthResult(false, "Limite massimo di utenti raggiunto per questa azienda");
                }
            }

            // Create new user
            var user = new User
            {
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role,
                CompanyId = request.CompanyId,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                IsEmailVerified = false // Require email verification
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {UserId} - {Email}", user.Id, user.Email);

            // TODO: Send email verification email

            return new AuthResult(
                Success: true,
                Message: "Registrazione completata. Controlla la tua email per verificare l'account."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return new AuthResult(false, "Errore interno del server");
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || !user.IsActive)
                return true; // Don't reveal if user exists

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // TODO: Send password reset email

            _logger.LogInformation("Password reset requested for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email: {Email}", email);
            return false;
        }
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token &&
                                         u.PasswordResetExpires > DateTime.UtcNow);

            if (user == null)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset confirmed for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset");
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
                return false;

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email");
            return false;
        }
    }

    public async Task<bool> ResendEmailVerificationAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || user.IsEmailVerified)
                return true; // Don't reveal if user exists

            user.EmailVerificationToken = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            // TODO: Send email verification email

            _logger.LogInformation("Email verification resent for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending email verification");
            return false;
        }
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(string userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && !s.IsExpired)
            .OrderByDescending(s => s.LastActivity)
            .ToListAsync();
    }

    public async Task<bool> RevokeSessionAsync(string sessionId)
    {
        try
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            session.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session: {SessionId}", sessionId);
            return false;
        }
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("user_id", user.Id),
            new("email", user.Email),
            new("role", user.Role),
            new("first_name", user.FirstName),
            new("last_name", user.LastName)
        };

        if (!string.IsNullOrEmpty(user.CompanyId))
        {
            claims.Add(new Claim("company_id", user.CompanyId));
            if (user.Company != null)
            {
                claims.Add(new Claim("company_name", user.Company.Name));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task CleanupExpiredTokensAsync(string userId)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && (rt.IsExpired || rt.IsRevoked))
            .Where(rt => rt.Created < DateTime.UtcNow.AddDays(-30)) // Keep for 30 days for audit
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
    }

    private async Task CleanupExpiredSessionsAsync(string userId)
    {
        var expiredSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && (s.IsExpired || !s.IsActive))
            .Where(s => s.CreatedAt < DateTime.UtcNow.AddDays(-30)) // Keep for 30 days for audit
            .ToListAsync();

        _context.UserSessions.RemoveRange(expiredSessions);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role,
            CompanyId: user.CompanyId,
            CompanyName: user.Company?.Name,
            AvatarUrl: user.AvatarUrl,
            IsActive: user.IsActive,
            IsEmailVerified: user.IsEmailVerified,
            LastLogin: user.LastLogin,
            CreatedAt: user.CreatedAt
        );
    }
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}