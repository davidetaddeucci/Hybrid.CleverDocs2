using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.LocalLLM;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class LocalLLMClient : ILocalLLMClient
    {
        private readonly HttpClient _httpClient;
        public LocalLLMClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<LocalLLMResponse> CreateAsync(LocalLLMRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/local-llm", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocalLLMResponse>()!;
        }
        public async Task<LocalLLMResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/local-llm/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocalLLMResponse>()!;
        }
        public async Task<IEnumerable<LocalLLMResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/local-llm");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<LocalLLMResponse>>()!;
        }
        public async Task<LocalLLMResponse> UpdateAsync(string id, LocalLLMRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/local-llm/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocalLLMResponse>()!;
        }
        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/local-llm/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
