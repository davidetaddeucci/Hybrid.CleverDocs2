using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthClient _client;
        public AuthController(IAuthClient client) => _client = client;

        // Authentication operations
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request) => Ok(await _client.LoginAsync(request));

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request) => Ok(await _client.RefreshTokenAsync(request));

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request) => Ok(await _client.LogoutAsync(request));

        // User registration and management
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRequest request) => Ok(await _client.RegisterUserAsync(request));

        [HttpGet("user")]
        public async Task<IActionResult> GetCurrentUser() => Ok(await _client.GetCurrentUserAsync());

        [HttpPut("user")]
        public async Task<IActionResult> UpdateCurrentUser(UserUpdateRequest request) => Ok(await _client.UpdateCurrentUserAsync(request));

        [HttpDelete("user")]
        public async Task<IActionResult> DeleteCurrentUser()
        {
            await _client.DeleteCurrentUserAsync();
            return NoContent();
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUser(string userId) => Ok(await _client.GetUserAsync(userId));

        [HttpGet("users")]
        public async Task<IActionResult> ListUsers([FromQuery] int offset = 0, [FromQuery] int limit = 100) => Ok(await _client.ListUsersAsync(offset, limit));

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, UserUpdateRequest request) => Ok(await _client.UpdateUserAsync(userId, request));

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            await _client.DeleteUserAsync(userId);
            return NoContent();
        }

        // Password management
        [HttpPost("password/reset/request")]
        public async Task<IActionResult> RequestPasswordReset(PasswordResetRequest request) => Ok(await _client.RequestPasswordResetAsync(request));

        [HttpPost("password/reset/confirm")]
        public async Task<IActionResult> ConfirmPasswordReset(PasswordResetConfirmRequest request) => Ok(await _client.ConfirmPasswordResetAsync(request));

        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request) => Ok(await _client.ChangePasswordAsync(request));

        // Email verification
        [HttpPost("email/verify")]
        public async Task<IActionResult> VerifyEmail(EmailVerificationRequest request) => Ok(await _client.VerifyEmailAsync(request));

        [HttpPost("email/resend")]
        public async Task<IActionResult> ResendVerificationEmail(ResendVerificationRequest request) => Ok(await _client.ResendVerificationEmailAsync(request));

        // User status management
        [HttpPost("users/{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string userId) => Ok(await _client.DeactivateUserAsync(userId));

        [HttpPost("users/{userId}/activate")]
        public async Task<IActionResult> ActivateUser(string userId) => Ok(await _client.ActivateUserAsync(userId));

        [HttpPost("users/{userId}/make-superuser")]
        public async Task<IActionResult> MakeSuperuser(string userId) => Ok(await _client.MakeSuperuserAsync(userId));

        [HttpPost("users/{userId}/remove-superuser")]
        public async Task<IActionResult> RemoveSuperuser(string userId) => Ok(await _client.RemoveSuperuserAsync(userId));

        // Health check
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck() => Ok(await _client.HealthCheckAsync());
    }
}
