using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Prompt;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class PromptClient : IPromptClient
    {
        private readonly HttpClient _httpClient;
        public PromptClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<PromptResponse> CreateAsync(PromptRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/prompts", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PromptResponse>()!;
        }
        public async Task<PromptResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/prompts/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PromptResponse>()!;
        }
        public async Task<IEnumerable<PromptResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/prompts");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<PromptResponse>>()!;
        }
        public async Task<PromptResponse> UpdateAsync(string id, PromptRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/prompts/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PromptResponse>()!;
        }
        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/prompts/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
