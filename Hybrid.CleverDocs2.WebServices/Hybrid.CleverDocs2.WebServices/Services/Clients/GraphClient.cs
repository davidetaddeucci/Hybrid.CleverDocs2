using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Graph;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class GraphClient : IGraphClient
    {
        private readonly HttpClient _httpClient;

        public GraphClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GraphResponse> CreateAsync(GraphRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/graphs", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GraphResponse>()!;
        }

        public async Task<GraphResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/graphs/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GraphResponse>()!;
        }

        public async Task<IEnumerable<GraphResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/graphs");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<GraphResponse>>()!;
        }

        public async Task<GraphResponse> UpdateAsync(string id, GraphRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/graphs/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GraphResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/graphs/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
