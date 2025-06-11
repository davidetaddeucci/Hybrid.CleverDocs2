using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ConversationClient : IConversationClient
    {
        private readonly HttpClient _httpClient;

        public ConversationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ConversationResponse> CreateAsync(ConversationRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/conversations", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConversationResponse>()!;
        }

        public async Task<ConversationResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/conversations/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConversationResponse>()!;
        }

        public async Task<IEnumerable<ConversationResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/conversations");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ConversationResponse>>()!;
        }

        public async Task<ConversationResponse> UpdateAsync(string id, ConversationRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/conversations/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConversationResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/conversations/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
