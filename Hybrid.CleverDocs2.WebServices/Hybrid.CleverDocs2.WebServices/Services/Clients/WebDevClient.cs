using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.WebDev;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class WebDevClient : IWebDevClient
    {
        private readonly HttpClient _httpClient;
        public WebDevClient(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<WebDevResponse> CreateAsync(WebDevRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/webdev", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WebDevResponse>()!;
        }

        public async Task<WebDevResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/webdev/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WebDevResponse>()!;
        }

        public async Task<IEnumerable<WebDevResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/webdev");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<WebDevResponse>>()!;
        }

        public async Task<WebDevResponse> UpdateAsync(string id, WebDevRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/webdev/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WebDevResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/webdev/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
