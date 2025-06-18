using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;
using Hybrid.CleverDocs2.WebServices.Services.Users;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Hybrid.CleverDocs2.WebServices.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IUserSyncService _userSyncService;
        private readonly IHubContext<CollectionHub> _hubContext;
        private readonly int _refreshTokenExpirationDays;

        public AuthService(
            ApplicationDbContext context,
            IJwtService jwtService,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IUserSyncService userSyncService,
            IHubContext<CollectionHub> hubContext)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
            _logger = logger;
            _userSyncService = userSyncService;
            _hubContext = hubContext;
            _refreshTokenExpirationDays = _configuration.GetSection("Jwt").GetValue<int>("RefreshTokenExpirationDays");
        }

        public async Task<AuthResult> LoginAsync(string email, string password, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                    return new AuthResult { Success = false, ErrorMessage = "Account is inactive" };
                }

                if (!await ValidatePasswordAsync(user, password))
                {
                    _logger.LogWarning("Invalid password for user: {UserId}", user.Id);
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };
                }

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetSection("Jwt").GetValue<int>("ExpirationMinutes"));

                // Save refresh token
                await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken, TimeSpan.FromDays(_refreshTokenExpirationDays));

                // Log successful login
                await LogAuditAsync(user.CompanyId, user.Id, "Login", "User", user.Id, 
                    null, $"Successful login from {ipAddress}", ipAddress, userAgent);

                _logger.LogInformation("User {UserId} logged in successfully", user.Id);

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                return new AuthResult { Success = false, ErrorMessage = "An error occurred during login" };
            }
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // Find user by refresh token (we need to search through cache or implement a different approach)
                // For now, we'll implement a simple approach - in production, consider storing refresh tokens in database
                
                // This is a simplified implementation - in production, you'd want to store refresh tokens with user associations
                _logger.LogWarning("RefreshTokenAsync needs proper implementation with user-token association");
                return new AuthResult { Success = false, ErrorMessage = "Refresh token functionality needs implementation" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthResult { Success = false, ErrorMessage = "An error occurred during token refresh" };
            }
        }

        public async Task<bool> LogoutAsync(Guid userId, string? accessToken = null)
        {
            try
            {
                // Revoke refresh token
                await _jwtService.RevokeRefreshTokenAsync(userId);

                // Blacklist access token if provided
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await _jwtService.BlacklistTokenAsync(accessToken);
                }

                // Log logout
                await LogAuditAsync(await GetUserCompanyIdAsync(userId), userId, "Logout", "User", userId, 
                    null, "User logged out");

                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<User> RegisterUserAsync(string email, string password, string firstName, string lastName, 
            Guid companyId, UserRole role = UserRole.User, string? createdBy = null)
        {
            try
            {
                if (!await IsEmailAvailableAsync(email))
                {
                    throw new InvalidOperationException("Email is already in use");
                }

                var passwordHash = await HashPasswordAsync(password);
                var emailVerificationToken = GenerateSecureToken();

                var user = new User
                {
                    Email = email.ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    FirstName = firstName,
                    LastName = lastName,
                    Name = $"{firstName} {lastName}".Trim(), // R2R compatibility
                    CompanyId = companyId,
                    Role = role,
                    IsActive = true,
                    IsVerified = false,
                    EmailVerificationToken = emailVerificationToken,
                    EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(7),
                    CreatedBy = createdBy
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var r2rUserId = await _userSyncService.CreateR2RUserAsync(user);
                        if (!string.IsNullOrEmpty(r2rUserId))
                        {
                            // Update database with R2R user ID
                            user.R2RUserId = r2rUserId;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("R2R user ID {R2RUserId} saved to database for user {UserId}",
                                r2rUserId, user.Id);

                            // Notify via SignalR
                            await _hubContext.Clients.All.SendAsync("UserCreated", new {
                                UserId = user.Id,
                                R2RUserId = r2rUserId,
                                Email = user.Email,
                                Name = user.Name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create R2R user for {UserId}", user.Id);
                    }
                });

                // Log user creation
                await LogAuditAsync(companyId, null, "Create", "User", user.Id,
                    null, $"User registered: {email}", null, null, createdBy);

                _logger.LogInformation("User registered successfully: {UserId} - {Email}", user.Id, email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Register user with R2R compatibility fields
        /// </summary>
        public async Task<User> RegisterUserAsync(string email, string password, string firstName, string lastName,
            Guid companyId, UserRole role = UserRole.User, string? name = null, string? bio = null,
            string? profilePicture = null, string? createdBy = null)
        {
            try
            {
                if (!await IsEmailAvailableAsync(email))
                {
                    throw new InvalidOperationException("Email is already in use");
                }

                var passwordHash = await HashPasswordAsync(password);
                var emailVerificationToken = GenerateSecureToken();

                var user = new User
                {
                    Email = email.ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    FirstName = firstName,
                    LastName = lastName,
                    Name = name ?? $"{firstName} {lastName}".Trim(), // R2R compatibility
                    Bio = bio, // R2R compatibility
                    ProfilePicture = profilePicture, // R2R compatibility
                    CompanyId = companyId,
                    Role = role,
                    IsActive = true,
                    IsVerified = false,
                    EmailVerificationToken = emailVerificationToken,
                    EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(7),
                    CreatedBy = createdBy
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var r2rUserId = await _userSyncService.CreateR2RUserAsync(user);
                        if (!string.IsNullOrEmpty(r2rUserId))
                        {
                            // Update database with R2R user ID
                            user.R2RUserId = r2rUserId;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("R2R user ID {R2RUserId} saved to database for user {UserId}",
                                r2rUserId, user.Id);

                            // Notify via SignalR
                            await _hubContext.Clients.All.SendAsync("UserCreated", new {
                                UserId = user.Id,
                                R2RUserId = r2rUserId,
                                Email = user.Email,
                                Name = user.Name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create R2R user for {UserId}", user.Id);
                    }
                });

                // Log user creation
                await LogAuditAsync(companyId, null, "Create", "User", user.Id,
                    null, $"User registered: {email}", null, null, createdBy);

                _logger.LogInformation("User registered successfully: {UserId} - {Email}", user.Id, email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                if (!await ValidatePasswordAsync(user, currentPassword))
                {
                    _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                    return false;
                }

                user.PasswordHash = await HashPasswordAsync(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = userId.ToString();

                await _context.SaveChangesAsync();

                // Revoke all existing tokens to force re-login
                await _jwtService.RevokeAllUserTokensAsync(userId);

                // Log password change
                await LogAuditAsync(user.CompanyId, userId, "PasswordChange", "User", userId, 
                    null, "Password changed");

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
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal if email exists
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
                    return true;
                }

                user.PasswordResetToken = GenerateSecureToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Send password reset email
                _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);
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
                                            u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    _logger.LogWarning("Invalid or expired password reset token: {Token}", token);
                    return false;
                }

                user.PasswordHash = await HashPasswordAsync(newPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Revoke all existing tokens
                await _jwtService.RevokeAllUserTokensAsync(user.Id);

                // Log password reset
                await LogAuditAsync(user.CompanyId, user.Id, "PasswordReset", "User", user.Id, 
                    null, "Password reset completed");

                _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);
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
                    .FirstOrDefaultAsync(u => u.EmailVerificationToken == token && 
                                            u.EmailVerificationTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    _logger.LogWarning("Invalid or expired email verification token: {Token}", token);
                    return false;
                }

                user.IsVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log email verification
                await LogAuditAsync(user.CompanyId, user.Id, "EmailVerification", "User", user.Id, 
                    null, "Email verified");

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
                var user = await GetUserByEmailAsync(email);
                if (user == null || user.IsEmailVerified)
                {
                    return false;
                }

                user.EmailVerificationToken = GenerateSecureToken();
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(7);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Send verification email
                _logger.LogInformation("Email verification token regenerated for user: {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email verification for: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Update user profile with R2R compatibility fields
        /// </summary>
        public async Task<bool> UpdateUserProfileAsync(Guid userId, string? name = null, string? bio = null,
            string? profilePicture = null, string? firstName = null, string? lastName = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Update fields if provided
                if (firstName != null) user.FirstName = firstName;
                if (lastName != null) user.LastName = lastName;
                if (name != null) user.Name = name;
                if (bio != null) user.Bio = bio;
                if (profilePicture != null) user.ProfilePicture = profilePicture;

                // Auto-update Name if FirstName or LastName changed but Name wasn't explicitly set
                if ((firstName != null || lastName != null) && name == null)
                {
                    user.Name = $"{user.FirstName} {user.LastName}".Trim();
                }

                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = userId.ToString();

                await _context.SaveChangesAsync();

                // Update in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _userSyncService.UpdateR2RUserAsync(user);

                        // Notify via SignalR
                        await _hubContext.Clients.All.SendAsync("UserUpdated", new {
                            UserId = user.Id,
                            R2RUserId = user.R2RUserId,
                            Email = user.Email,
                            Name = user.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update R2R user for {UserId}", user.Id);
                    }
                });

                // Log profile update
                await LogAuditAsync(user.CompanyId, userId, "ProfileUpdate", "User", userId,
                    null, "User profile updated");

                _logger.LogInformation("Profile updated for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }

        public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email.ToLowerInvariant());
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> ValidatePasswordAsync(User user, string password)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
        }

        public async Task<string> HashPasswordAsync(string password)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)));
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private async Task<Guid> GetUserCompanyIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.CompanyId ?? Guid.Empty;
        }

        private async Task LogAuditAsync(Guid companyId, Guid? userId, string action, string entityType, 
            Guid? entityId, string? oldValues, string? details, string? ipAddress = null, 
            string? userAgent = null, string? performedBy = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = oldValues,
                    Details = details,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry");
            }
        }
    }
}