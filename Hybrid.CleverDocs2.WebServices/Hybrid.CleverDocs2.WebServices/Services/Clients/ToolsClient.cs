using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Tools;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ToolsClient : IToolsClient
    {
        private readonly HttpClient _httpClient;

        public ToolsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ToolsResponse> CreateAsync(ToolsRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/tools", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ToolsResponse>()!;
        }

        public async Task<ToolsResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/tools/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ToolsResponse>()!;
        }

        public async Task<IEnumerable<ToolsResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/tools");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ToolsResponse>>()!;
        }

        public async Task<ToolsResponse> UpdateAsync(string id, ToolsRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/tools/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ToolsResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/tools/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
