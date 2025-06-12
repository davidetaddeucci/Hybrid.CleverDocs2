using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.WebUI.Auth;
using Hybrid.CleverDocs2.WebServices.Data.Models.Auth;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Controllers.WebUI;

[ApiController]
[Route("api/webui/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and return JWT tokens
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResult(false, "Dati non validi", Errors: ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)));
        }

        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        // Set refresh token in HTTP-only cookie
        SetRefreshTokenCookie(result.RefreshToken!);

        // Don't return refresh token in response body for security
        return Ok(result with { RefreshToken = null });
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResult(false, "Dati non validi", Errors: ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)));
        }

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new AuthResult(false, "Token di refresh non trovato"));
        }

        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        // Set new refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken!);

        // Don't return refresh token in response body
        return Ok(result with { RefreshToken = null });
    }

    /// <summary>
    /// Logout user and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userId = User.FindFirst("user_id")?.Value;
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(userId))
        {
            await _authService.LogoutAsync(userId);
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(refreshToken, ipAddress);
        }

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Logout effettuato con successo" });
    }

    /// <summary>
    /// Logout from all sessions
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<ActionResult> LogoutAll()
    {
        var userId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAllSessionsAsync(userId);

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Logout da tutte le sessioni effettuato con successo" });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        var userDto = new UserDto(
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

        return Ok(userDto);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Password corrente non valida" });
        }

        return Ok(new { message = "Password cambiata con successo" });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _authService.ResetPasswordAsync(request.Email);

        // Always return success to prevent email enumeration
        return Ok(new { message = "Se l'email esiste, riceverai le istruzioni per il reset della password" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.ConfirmPasswordResetAsync(request.Token, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Token non valido o scaduto" });
        }

        return Ok(new { message = "Password reimpostata con successo" });
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.VerifyEmailAsync(request.Token);

        if (!success)
        {
            return BadRequest(new { message = "Token di verifica non valido" });
        }

        return Ok(new { message = "Email verificata con successo" });
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<ActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _authService.ResendEmailVerificationAsync(request.Email);

        // Always return success to prevent email enumeration
        return Ok(new { message = "Se l'email esiste, riceverai un nuovo link di verifica" });
    }

    /// <summary>
    /// Get active user sessions
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserSession>>> GetActiveSessions()
    {
        var userId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetActiveSessionsAsync(userId);
        return Ok(sessions);
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    public async Task<ActionResult> RevokeSession(string sessionId)
    {
        var success = await _authService.RevokeSessionAsync(sessionId);

        if (!success)
        {
            return NotFound(new { message = "Sessione non trovata" });
        }

        return Ok(new { message = "Sessione revocata con successo" });
    }

    /// <summary>
    /// Validate current token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public ActionResult ValidateToken()
    {
        var userId = User.FindFirst("user_id")?.Value;
        var role = User.FindFirst("role")?.Value;
        var companyId = User.FindFirst("company_id")?.Value;

        return Ok(new
        {
            isValid = true,
            userId,
            role,
            companyId,
            expiresAt = User.FindFirst("exp")?.Value
        });
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

// Request DTOs
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record VerifyEmailRequest(string Token);
public record ResendVerificationRequest(string Email);