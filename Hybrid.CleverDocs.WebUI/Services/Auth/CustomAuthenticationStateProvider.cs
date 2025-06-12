using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Services.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;

    public CustomAuthenticationStateProvider(
        ILocalStorageService localStorage,
        IConfiguration configuration)
    {
        _localStorage = localStorage;
        _configuration = configuration;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
            var token = await _localStorage.GetItemAsync<string>(tokenKey);

            if (string.IsNullOrEmpty(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            // Parse JWT token
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Check if token is expired
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync(tokenKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Create claims from token
            var claims = jsonToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task NotifyAuthenticationStateChangedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }
}