using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class AuthClient : IAuthClient
    {
        private readonly HttpClient _httpClient;

        public AuthClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/login", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result!;
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/refresh", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            return result!;
        }

        public async Task LogoutAsync(LogoutRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/logout", request);
            response.EnsureSuccessStatusCode();
        }
    }
}