using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        public string? UserRole => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting login for user {Email}", request.Email);

                // Use CancellationToken with timeout for better control
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, cts.Token);

                _logger.LogInformation("Login API response status: {StatusCode} for user {Email}",
                    response.StatusCode, request.Email);

                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions, cts.Token);

                    if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        // Store tokens in secure HttpOnly cookies
                        var userJson = loginResponse.User != null ? JsonSerializer.Serialize(loginResponse.User) : "";
                        SetAuthCookies(loginResponse.AccessToken, loginResponse.RefreshToken, userJson);

                        // Create claims from JWT token and user info
                        await CreateUserClaimsAndSignInAsync(loginResponse.AccessToken, loginResponse.User);

                        // Store in HttpContext.Items for immediate use
                        var context = _httpContextAccessor.HttpContext;
                        if (context != null)
                        {
                            context.Items["AccessToken"] = loginResponse.AccessToken;
                            if (loginResponse.User != null)
                            {
                                context.Items["UserInfo"] = loginResponse.User;
                            }
                        }

                        // Set authorization header for future requests in this session
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

                        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                        return loginResponse;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Login failed for {Email}: {StatusCode} - {Error}",
                    request.Email, response.StatusCode, errorContent);

                return new LoginResponse
                {
                    Success = false,
                    Message = "Login failed. Please check your credentials."
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Login timeout for user {Email}", request.Email);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Login request timed out. Please try again."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during login for {Email}", request.Email);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Unable to connect to authentication service. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
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
                        // Store tokens in secure cookies
                        var context = _httpContextAccessor.HttpContext;
                        var existingUserInfo = context?.Request.Cookies["CleverDocs.UserInfo"] ?? "";

                        SetAuthCookies(loginResponse.AccessToken, loginResponse.RefreshToken, existingUserInfo);

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
                    var userJson = context.Request.Cookies["CleverDocs.UserInfo"];
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
                            // Update user info in cookie
                            var httpContext = _httpContextAccessor.HttpContext;
                            if (httpContext != null)
                            {
                                var userJson = JsonSerializer.Serialize(apiResponse.Data);
                                var isProduction = httpContext.Request.IsHttps;
                                var userCookieOptions = new CookieOptions
                                {
                                    HttpOnly = false, // Allow JavaScript access for UI
                                    Secure = isProduction, // Only secure in HTTPS environments
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTime.UtcNow.AddHours(8)
                                };
                                httpContext.Response.Cookies.Append("CleverDocs.UserInfo", userJson, userCookieOptions);
                            }
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
            if (context == null) return null;

            // 1. Try to get token from HttpContext.Items (immediate request)
            var token = context.Items["AccessToken"] as string;
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }

            // 2. Try to get token from secure HttpOnly cookie
            token = context.Request.Cookies["CleverDocs.AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                // Validate token is not expired
                if (IsTokenValid(token))
                {
                    // Cache in HttpContext.Items for this request
                    context.Items["AccessToken"] = token;
                    return token;
                }
                else
                {
                    // Token expired, clear cookies
                    ClearAuthCookies();
                }
            }

            // 3. Try to refresh token if available
            var refreshToken = context.Request.Cookies["CleverDocs.RefreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var newToken = RefreshTokenAsync(refreshToken).Result;
                if (!string.IsNullOrEmpty(newToken))
                {
                    return newToken;
                }
            }

            // 4. No valid token available
            return null;
        }

        private async Task<string> GetFreshTokenAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var loginRequest = new
                {
                    email = "r.antoniucci@microsis.it",
                    password = "Maremmabona1!"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("http://localhost:5253/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = System.Text.Json.JsonSerializer.Deserialize<InternalLoginResponse>(responseContent);
                    return loginResponse?.AccessToken ?? "";
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - fallback to empty token
                Console.WriteLine($"Error getting fresh token: {ex.Message}");
            }

            return "";
        }

        private bool IsTokenValid(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                // Check if token is expired (with 5 minute buffer)
                return jsonToken.ValidTo > DateTime.UtcNow.AddMinutes(5);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                var refreshRequest = new { refreshToken };

                var json = System.Text.Json.JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("http://localhost:5253/api/auth/refresh", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var refreshResponse = System.Text.Json.JsonSerializer.Deserialize<InternalLoginResponse>(responseContent);

                    if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                    {
                        // Save new tokens to secure cookies
                        var context = _httpContextAccessor.HttpContext;
                        var existingUserInfo = context?.Request.Cookies["CleverDocs.UserInfo"] ?? "";

                        SetAuthCookies(refreshResponse.AccessToken, refreshResponse.RefreshToken, existingUserInfo);

                        // Cache in HttpContext.Items for immediate use
                        if (context != null)
                        {
                            context.Items["AccessToken"] = refreshResponse.AccessToken;
                        }

                        return refreshResponse.AccessToken;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing token: {ex.Message}");
            }

            return null;
        }

        private void ClearAuthCookies()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var isProduction = context.Request.IsHttps;
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isProduction, // Only secure in HTTPS environments
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(-1) // Expire immediately
                };

                context.Response.Cookies.Append("CleverDocs.AccessToken", "", cookieOptions);
                context.Response.Cookies.Append("CleverDocs.RefreshToken", "", cookieOptions);
                context.Response.Cookies.Append("CleverDocs.UserInfo", "", cookieOptions);
            }
        }

        private void SetAuthCookies(string accessToken, string refreshToken, string userInfo)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Use Secure=false in development (HTTP), Secure=true in production (HTTPS)
                var isProduction = context.Request.IsHttps;

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isProduction, // Only secure in HTTPS environments
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8) // 8 hours expiry
                };

                context.Response.Cookies.Append("CleverDocs.AccessToken", accessToken, cookieOptions);

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = isProduction, // Only secure in HTTPS environments
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7) // 7 days for refresh token
                    };
                    context.Response.Cookies.Append("CleverDocs.RefreshToken", refreshToken, refreshCookieOptions);
                }

                // User info cookie (can be accessed by JavaScript for UI)
                var userCookieOptions = new CookieOptions
                {
                    HttpOnly = false, // Allow JavaScript access for UI
                    Secure = isProduction, // Only secure in HTTPS environments
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8)
                };
                context.Response.Cookies.Append("CleverDocs.UserInfo", userInfo, userCookieOptions);
            }
        }

        private class InternalLoginResponse
        {
            public string AccessToken { get; set; } = "";
            public string RefreshToken { get; set; } = "";
        }

        public async Task<string?> GetTokenAsync()
        {
            return GetStoredToken();
        }

        public async Task ClearTokensAsync()
        {
            // Clear secure HttpOnly cookies
            ClearAuthCookies();

            // Clear from HttpContext.Items
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Items.Remove("AccessToken");
                context.Items.Remove("RefreshToken");
                context.Items.Remove("UserInfo");
            }
        }

        // Helper methods for token storage
        private string? GetStoredRefreshToken()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Cookies["CleverDocs.RefreshToken"];
        }

        private async Task<string?> GetStoredTokenAsync()
        {
            return GetStoredToken();
        }

        private async Task<string?> GetStoredRefreshTokenAsync()
        {
            return GetStoredRefreshToken();
        }

        /// <summary>
        /// Create claims from JWT token and sign in the user for ASP.NET Core authentication
        /// </summary>
        private async Task CreateUserClaimsAndSignInAsync(string accessToken, UserInfo? userInfo)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return;

                // Parse JWT token to extract claims
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(accessToken);

                var claims = new List<Claim>();

                // Add standard claims from JWT
                foreach (var claim in jsonToken.Claims)
                {
                    switch (claim.Type)
                    {
                        case "sub":
                        case "nameid":
                            claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                            break;
                        case "email":
                            claims.Add(new Claim(ClaimTypes.Email, claim.Value));
                            break;
                        case "name":
                            claims.Add(new Claim(ClaimTypes.Name, claim.Value));
                            break;
                        case "role":
                            claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                            break;
                        default:
                            claims.Add(new Claim(claim.Type, claim.Value));
                            break;
                    }
                }

                // Add user info claims if available
                if (userInfo != null)
                {
                    if (userInfo.Id != Guid.Empty)
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()));

                    if (!string.IsNullOrEmpty(userInfo.Email))
                        claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));

                    if (!string.IsNullOrEmpty(userInfo.FirstName))
                        claims.Add(new Claim(ClaimTypes.GivenName, userInfo.FirstName));

                    if (!string.IsNullOrEmpty(userInfo.LastName))
                        claims.Add(new Claim(ClaimTypes.Surname, userInfo.LastName));

                    if (!string.IsNullOrEmpty(userInfo.Role))
                        claims.Add(new Claim(ClaimTypes.Role, userInfo.Role));

                    if (userInfo.CompanyId.HasValue)
                        claims.Add(new Claim("CompanyId", userInfo.CompanyId.Value.ToString()));

                    if (!string.IsNullOrEmpty(userInfo.CompanyName))
                        claims.Add(new Claim("CompanyName", userInfo.CompanyName));
                }

                // Ensure we have at least a NameIdentifier claim
                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    var userId = userInfo?.Id.ToString() ?? jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                }

                // Create identity and principal
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                // Sign in the user
                await context.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                });

                _logger.LogInformation("User claims created and signed in successfully: {UserId}",
                    claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user claims and signing in");
            }
        }

    }
}