using System.Net.Http.Json;
using System.Text.Json;
using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private const string TokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";
        private const string UserKey = "user_info";

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(GetStoredToken());
        public string? UserRole => GetCurrentUserAsync().Result?.Role;

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);
                    
                    if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        // Store tokens and user info in cookies
                        StoreTokenInCookie(loginResponse.AccessToken);
                        StoreRefreshTokenInCookie(loginResponse.RefreshToken);
                        
                        if (loginResponse.User != null)
                        {
                            StoreUserInfoInCookie(loginResponse.User);
                        }

                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

                        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                        return loginResponse;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for {Email}: {Error}", request.Email, errorContent);
                
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = "Login failed. Please check your credentials." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = "An error occurred during login. Please try again." 
                };
            }
        }

        public async Task<ApiResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>(jsonOptions);
                    _logger.LogInformation("User registered successfully: {Email}", request.Email);
                    return result ?? new ApiResponse { Success = true, Message = "Registration successful" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Registration failed for {Email}: {Error}", request.Email, errorContent);
                
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "Registration failed. Please try again." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", request.Email);
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "An error occurred during registration. Please try again." 
                };
            }
        }

        public async Task<ApiResponse> LogoutAsync()
        {
            try
            {
                var token = await GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    // Call backend logout to blacklist token
                    await _httpClient.PostAsync("/api/auth/logout", null);
                }

                // Clear local storage
                await ClearTokensAsync();
                
                // Clear authorization header
                _httpClient.DefaultRequestHeaders.Authorization = null;

                _logger.LogInformation("User logged out successfully");
                return new ApiResponse { Success = true, Message = "Logged out successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                // Still clear local tokens even if backend call fails
                await ClearTokensAsync();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "An error occurred during logout." 
                };
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await GetStoredRefreshTokenAsync();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new LoginResponse { Success = false, Message = "No refresh token available" };
                }

                var request = new RefreshTokenRequest { RefreshToken = refreshToken };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);
                    
                    if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        StoreTokenInCookie(loginResponse.AccessToken);
                        StoreRefreshTokenInCookie(loginResponse.RefreshToken);
                        
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

                        _logger.LogInformation("Token refreshed successfully");
                        return loginResponse;
                    }
                }

                // Refresh failed, clear tokens
                await ClearTokensAsync();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                return new LoginResponse { Success = false, Message = "Token refresh failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                await ClearTokensAsync();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                return new LoginResponse { Success = false, Message = "Token refresh failed" };
            }
        }

        public async Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    _logger.LogInformation("Password changed successfully");
                    return result ?? new ApiResponse { Success = true, Message = "Password changed successfully" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Password change failed: {Error}", errorContent);
                
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "Password change failed. Please try again." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "An error occurred during password change. Please try again." 
                };
            }
        }

        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/forgot-password", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                    return result ?? new ApiResponse { Success = true, Message = "Password reset email sent" };
                }

                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "Failed to send password reset email. Please try again." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "An error occurred. Please try again." 
                };
            }
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/reset-password", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    _logger.LogInformation("Password reset successfully for {Email}", request.Email);
                    return result ?? new ApiResponse { Success = true, Message = "Password reset successfully" };
                }

                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "Password reset failed. Please try again." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = "An error occurred during password reset. Please try again." 
                };
            }
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                // First try to get from cookie
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    var userJson = context.Request.Cookies[UserKey];
                    if (!string.IsNullOrEmpty(userJson))
                    {
                        var user = JsonSerializer.Deserialize<UserInfo>(userJson);
                        if (user != null)
                        {
                            return user;
                        }
                    }
                }

                // If not in cookie, try to get from API
                var token = await GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    var response = await _httpClient.GetAsync("/api/auth/me");
                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserInfo>>();
                        if (apiResponse?.Success == true && apiResponse.Data != null)
                        {
                            StoreUserInfoInCookie(apiResponse.Data);
                            return apiResponse.Data;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetStoredTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public string? GetStoredToken()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Cookies[TokenKey];
        }

        public async Task<string?> GetTokenAsync()
        {
            return GetStoredToken();
        }

        public async Task ClearTokensAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Delete(TokenKey);
                context.Response.Cookies.Delete(RefreshTokenKey);
                context.Response.Cookies.Delete(UserKey);
            }
        }

        // Private helper methods
        private void StoreTokenInCookie(string token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Append(TokenKey, token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8)
                });
            }
        }

        private void StoreRefreshTokenInCookie(string refreshToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Append(RefreshTokenKey, refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
            }
        }

        private void StoreUserInfoInCookie(UserInfo user)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var userJson = JsonSerializer.Serialize(user);
                context.Response.Cookies.Append(UserKey, userJson, new CookieOptions
                {
                    HttpOnly = false, // Allow JS access for user info
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8)
                });
            }
        }



        private string? GetStoredRefreshToken()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Cookies[RefreshTokenKey];
        }

        private async Task<string?> GetStoredTokenAsync()
        {
            return GetStoredToken();
        }

        private async Task<string?> GetStoredRefreshTokenAsync()
        {
            return GetStoredRefreshToken();
        }
    }
}