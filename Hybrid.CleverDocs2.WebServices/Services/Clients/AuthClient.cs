using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class AuthClient : IAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthClient> _logger;

        public AuthClient(HttpClient httpClient, ILogger<AuthClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Authentication operations
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/login", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/refresh_access_token", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> LogoutAsync(LogoutRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/logout", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // User registration and management
        public async Task<UserCreateResponse?> RegisterUserAsync(UserRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/users", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("R2R user registration failed with status {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<UserCreateResponse>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception during R2R user registration");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during R2R user registration");
                return null;
            }
        }

        public async Task<UserResponse?> GetUserAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/users/{userId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/user");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserListResponse?> ListUsersAsync(int offset = 0, int limit = 100)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/users?offset={offset}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> UpdateUserAsync(string userId, UserUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/users/{userId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> UpdateCurrentUserAsync(UserUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/v3/user", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/users/{userId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteCurrentUserAsync()
        {
            var response = await _httpClient.DeleteAsync("/v3/user");
            response.EnsureSuccessStatusCode();
        }

        // Password management
        public async Task<MessageResponse?> RequestPasswordResetAsync(PasswordResetRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/request_password_reset", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/confirm_password_reset", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/change_password", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Email verification
        public async Task<MessageResponse?> VerifyEmailAsync(EmailVerificationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/verify_email", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> ResendVerificationEmailAsync(ResendVerificationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/send_reset_email", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // User status management
        public async Task<UserResponse?> DeactivateUserAsync(string userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/users/{userId}/deactivate", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> ActivateUserAsync(string userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/users/{userId}/activate", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> MakeSuperuserAsync(string userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/users/{userId}/make_superuser", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<UserResponse?> RemoveSuperuserAsync(string userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/users/{userId}/remove_superuser", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Health check
        public async Task<MessageResponse?> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
