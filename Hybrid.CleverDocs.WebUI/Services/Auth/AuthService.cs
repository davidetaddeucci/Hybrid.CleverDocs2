using Blazored.LocalStorage;
using Hybrid.CleverDocs.WebUI.Models;
using Hybrid.CleverDocs.WebUI.Services.Api;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IConfiguration _configuration;
    private UserProfile? _currentUser;

    public event Action<UserProfile?>? AuthenticationStateChanged;

    public AuthService(
        IApiClient apiClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        IConfiguration configuration)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _configuration = configuration;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<LoginResponse>("webui/auth/login", request);
            
            if (response.Success && response.Data != null)
            {
                var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
                
                await _localStorage.SetItemAsync(tokenKey, response.Data.AccessToken);
                
                _currentUser = response.Data.User;
                AuthenticationStateChanged?.Invoke(_currentUser);
                
                // Notify authentication state provider
                if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
                {
                    await customProvider.NotifyAuthenticationStateChangedAsync();
                }
            }
            
            return response;
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoginResponse>(false, Message: $"Errore durante il login: {ex.Message}");
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            // Call backend logout endpoint
            await _apiClient.PostAsync("webui/auth/logout", new { });
            
            var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
            
            await _localStorage.RemoveItemAsync(tokenKey);
            
            _currentUser = null;
            AuthenticationStateChanged?.Invoke(null);
            
            // Notify authentication state provider
            if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                await customProvider.NotifyAuthenticationStateChangedAsync();
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var response = await _apiClient.PostAsync<LoginResponse>("webui/auth/refresh", new { });
            
            if (response.Success && response.Data != null)
            {
                var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
                
                await _localStorage.SetItemAsync(tokenKey, response.Data.AccessToken);
                
                _currentUser = response.Data.User;
                AuthenticationStateChanged?.Invoke(_currentUser);
                
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        try
        {
            var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
            var token = await _localStorage.GetItemAsync<string>(tokenKey);
            
            if (string.IsNullOrEmpty(token))
                return null;

            // Parse JWT token to get user info
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            // Check if token is expired
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                // Try to refresh token
                if (!await RefreshTokenAsync())
                    return null;
                
                return _currentUser;
            }

            // Extract user info from token
            var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value;
            var email = jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            var firstName = jsonToken.Claims.FirstOrDefault(x => x.Type == "first_name")?.Value;
            var lastName = jsonToken.Claims.FirstOrDefault(x => x.Type == "last_name")?.Value;
            var roleString = jsonToken.Claims.FirstOrDefault(x => x.Type == "role")?.Value;
            var companyId = jsonToken.Claims.FirstOrDefault(x => x.Type == "company_id")?.Value;
            var companyName = jsonToken.Claims.FirstOrDefault(x => x.Type == "company_name")?.Value;

            if (userId != null && email != null && firstName != null && lastName != null && roleString != null)
            {
                if (Enum.TryParse<UserRole>(roleString, out var role))
                {
                    _currentUser = new UserProfile(
                        userId,
                        email,
                        firstName,
                        lastName,
                        role,
                        companyId,
                        companyName
                    );
                    
                    return _currentUser;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<bool> HasRoleAsync(UserRole role)
    {
        var user = await GetCurrentUserAsync();
        return user?.Role == role;
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;

        // Define permissions based on roles
        return user.Role switch
        {
            UserRole.Admin => true, // Admin has all permissions
            UserRole.Company => permission switch
            {
                "manage_users" => true,
                "view_all_collections" => true,
                "view_analytics" => true,
                "manage_company_settings" => true,
                _ => false
            },
            UserRole.User => permission switch
            {
                "manage_collections" => true,
                "upload_documents" => true,
                "use_chat" => true,
                "view_profile" => true,
                _ => false
            },
            _ => false
        };
    }
}