using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

[ApiController]
[Route("api/local-auth")]
public class LocalAuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalAuthController> _logger;

    public LocalAuthController(AuthDbContext context, IConfiguration configuration, ILogger<LocalAuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LocalLoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // Find user by email
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Update login statistics
            user.LastLogin = DateTime.UtcNow;
            user.LoginCount++;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            // Create user session
            var session = new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                SessionToken = Guid.NewGuid().ToString(),
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successful login for user: {Email}", request.Email);

            return Ok(new LocalLoginResponse
            {
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    Company = user.Company != null ? new CompanyInfo
                    {
                        Id = user.Company.Id,
                        Name = user.Company.Name,
                        SubscriptionPlan = user.Company.SubscriptionPlan
                    } : null
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LocalLogoutRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.SessionToken))
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken);

                if (session != null)
                {
                    session.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Company = user.Company != null ? new CompanyInfo
                {
                    Id = user.Company.Id,
                    Name = user.Company.Name,
                    SubscriptionPlan = user.Company.SubscriptionPlan
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = "CleverDocs2-Super-Secret-JWT-Key-For-Authentication-2024-Very-Long-And-Secure";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("company_id", user.CompanyId ?? ""),
            new Claim("user_id", user.Id)
        };

        var token = new JwtSecurityToken(
            issuer: "CleverDocs2",
            audience: "CleverDocs2-Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string? GetUserIdFromToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var jwtKey = "CleverDocs2-Super-Secret-JWT-Key-For-Authentication-2024-Very-Long-And-Secure";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = "CleverDocs2",
                ValidateAudience = true,
                ValidAudience = "CleverDocs2-Users",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return jwtToken.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private string GetClientIpAddress()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress ?? "Unknown";
    }
}

// DTOs
public class LocalLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LocalLogoutRequest
{
    public string? SessionToken { get; set; }
}

public class LocalLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public CompanyInfo? Company { get; set; }
}

public class CompanyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = string.Empty;
}