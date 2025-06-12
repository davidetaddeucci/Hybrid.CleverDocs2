using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Middleware;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IJwtService jwtService,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtService = jwtService;
            _context = context;
            _logger = logger;
        }

        // Authentication operations
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _authService.LoginAsync(request.Email, request.Password, 
                    HttpContext.Connection.RemoteIpAddress?.ToString(), 
                    HttpContext.Request.Headers.UserAgent.ToString());
                
                if (result.Success && result.User != null)
                {
                    // Update last login
                    result.User.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    var userInfo = new UserInfoDto
                    {
                        Id = result.User.Id,
                        Email = result.User.Email,
                        FirstName = result.User.FirstName,
                        LastName = result.User.LastName,
                        Role = result.User.Role.ToString(),
                        CompanyId = result.User.CompanyId,
                        CompanyName = result.User.Company?.Name,
                        IsEmailVerified = result.User.IsEmailVerified,
                        CreatedAt = result.User.CreatedAt,
                        LastLoginAt = result.User.LastLoginAt
                    };

                    return Ok(new LoginResponseDto
                    {
                        Success = true,
                        Message = "Login successful",
                        AccessToken = result.AccessToken!,
                        RefreshToken = result.RefreshToken!,
                        User = userInfo
                    });
                }

                return Unauthorized(new ApiResponseDto 
                { 
                    Success = false, 
                    Message = result.ErrorMessage ?? "Login failed" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Refresh token is required" 
                    });
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken, 
                    HttpContext.Connection.RemoteIpAddress?.ToString(), 
                    HttpContext.Request.Headers.UserAgent.ToString());
                
                if (result.Success)
                {
                    return Ok(new LoginResponseDto
                    {
                        Success = true,
                        Message = "Token refreshed successfully",
                        AccessToken = result.AccessToken!,
                        RefreshToken = result.RefreshToken!
                    });
                }

                return Unauthorized(new ApiResponseDto 
                { 
                    Success = false, 
                    Message = result.ErrorMessage ?? "Token refresh failed" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                var token = HttpContext.Items["AccessToken"] as string;
                
                if (userId.HasValue)
                {
                    await _authService.LogoutAsync(userId.Value, token);
                }

                return Ok(new ApiResponseDto 
                { 
                    Success = true, 
                    Message = "Logged out successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        // User registration and management
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Passwords do not match" 
                    });
                }

                // Check if email is available
                if (!await _authService.IsEmailAvailableAsync(request.Email))
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Email is already registered" 
                    });
                }

                // For now, create a default company if none provided
                Guid companyId;
                if (!string.IsNullOrEmpty(request.CompanyName))
                {
                    // Create new company
                    var company = new Company
                    {
                        Id = Guid.NewGuid(),
                        Name = request.CompanyName,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                    companyId = company.Id;
                }
                else
                {
                    // Use default company or create one
                    var defaultCompany = await _context.Companies.FirstOrDefaultAsync(c => c.Name == "Default Company");
                    if (defaultCompany == null)
                    {
                        defaultCompany = new Company
                        {
                            Id = Guid.NewGuid(),
                            Name = "Default Company",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.Companies.Add(defaultCompany);
                        await _context.SaveChangesAsync();
                    }
                    companyId = defaultCompany.Id;
                }

                var user = await _authService.RegisterUserAsync(
                    request.Email, 
                    request.Password, 
                    request.FirstName, 
                    request.LastName, 
                    companyId);

                return Ok(new ApiResponseDto 
                { 
                    Success = true, 
                    Message = "Registration successful. Please check your email for verification." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", request.Email);
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = HttpContext.GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "User not found" 
                    });
                }

                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                {
                    return NotFound(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "User not found" 
                    });
                }

                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.Name,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Ok(new ApiResponseDto<UserInfoDto> 
                { 
                    Success = true, 
                    Data = userInfo 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "New passwords do not match" 
                    });
                }

                var userId = HttpContext.GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
                
                if (result)
                {
                    return Ok(new ApiResponseDto 
                    { 
                        Success = true, 
                        Message = "Password changed successfully" 
                    });
                }

                return BadRequest(new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "Failed to change password. Please check your current password." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new ApiResponseDto 
                    { 
                        Success = false, 
                        Message = "Email is required" 
                    });
                }

                await _authService.ResetPasswordAsync(request.Email);
                
                // Always return success for security reasons (don't reveal if email exists)
                return Ok(new ApiResponseDto 
                { 
                    Success = true, 
                    Message = "If the email exists, a password reset link has been sent." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
                return StatusCode(500, new ApiResponseDto 
                { 
                    Success = false, 
                    Message = "An internal error occurred" 
                });
            }
        }

        // Health check
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
    }

    // DTOs for API requests and responses
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserInfoDto? User { get; set; }
    }

    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
    }

    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool IsAdmin => Role == "Admin";
        public bool IsCompanyUser => Role == "Company";
        public bool IsRegularUser => Role == "User";
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ApiResponseDto : ApiResponseDto<object>
    {
    }
}
