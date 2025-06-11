using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.McpTuning;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class McpTuningClient : IMcpTuningClient
    {
        private readonly HttpClient _httpClient;
        public McpTuningClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<McpTuningResponse> CreateAsync(McpTuningRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/mcp-tuning", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpTuningResponse>()!;
        }
        public async Task<McpTuningResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/mcp-tuning/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpTuningResponse>()!;
        }
        public async Task<IEnumerable<McpTuningResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/mcp-tuning");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<McpTuningResponse>>()!;
        }
        public async Task<McpTuningResponse> UpdateAsync(string id, McpTuningRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/mcp-tuning/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpTuningResponse>()!;
        }
        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/mcp-tuning/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
