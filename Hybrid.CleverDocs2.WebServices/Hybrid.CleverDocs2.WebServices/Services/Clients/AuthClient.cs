using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
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
            return await response.Content.ReadFromJsonAsync<LoginResponse>()!;
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/refresh", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RefreshTokenResponse>()!;
        }

        public async Task LogoutAsync(LogoutRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/auth/logout", request);
            response.EnsureSuccessStatusCode();
        }

        // User management
        public async Task<UserResponse> CreateUserAsync(UserRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/users", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserResponse>()!;
        }

        public async Task<UserResponse> GetUserAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/users/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserResponse>()!;
        }

        public async Task<IEnumerable<UserResponse>> ListUsersAsync()
        {
            var response = await _httpClient.GetAsync("/users");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<UserResponse>>()!;
        }

        public async Task<UserResponse> UpdateUserAsync(string id, UserRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserResponse>()!;
        }

        public async Task DeleteUserAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/users/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
