using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class OrchestrationClient : IOrchestrationClient
    {
        private readonly HttpClient _httpClient;
        public OrchestrationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<OrchestrationResponse> CreateAsync(OrchestrationRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/orchestrations", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrchestrationResponse>()!;
        }
        public async Task<OrchestrationResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/orchestrations/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrchestrationResponse>()!;
        }
        public async Task<IEnumerable<OrchestrationResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/orchestrations");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<OrchestrationResponse>>()!;
        }
        public async Task<OrchestrationResponse> UpdateAsync(string id, OrchestrationRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/orchestrations/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrchestrationResponse>()!;
        }
        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/orchestrations/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
