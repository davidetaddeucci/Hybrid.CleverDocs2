using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class IngestionClient : IIngestionClient
    {
        private readonly HttpClient _httpClient;

        public IngestionClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IngestionResponse> CreateAsync(IngestionRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/ingestions", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IngestionResponse>()!;
        }

        public async Task<IngestionResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/ingestions/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IngestionResponse>()!;
        }

        public async Task<IEnumerable<IngestionResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/ingestions");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<IngestionResponse>>()!;
        }

        public async Task<IngestionResponse> UpdateAsync(string id, IngestionRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/ingestions/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IngestionResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/ingestions/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
