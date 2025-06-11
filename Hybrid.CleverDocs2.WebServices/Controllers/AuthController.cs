using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthClient _client;
        public AuthController(IAuthClient client) => _client = client;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request) => Ok(await _client.LoginAsync(request));

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request) => Ok(await _client.RefreshTokenAsync(request));

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            await _client.LogoutAsync(request);
            return NoContent();
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser(UserRequest request) => Ok(await _client.CreateUserAsync(request));

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id) => Ok(await _client.GetUserAsync(id));

        [HttpGet("users")]
        public async Task<IActionResult> ListUsers() => Ok(await _client.ListUsersAsync());

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UserRequest request) => Ok(await _client.UpdateUserAsync(id, request));

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _client.DeleteUserAsync(id);
            return NoContent();
        }
    }
}
