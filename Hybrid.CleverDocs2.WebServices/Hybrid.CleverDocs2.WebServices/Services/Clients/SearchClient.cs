using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Search;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class SearchClient : ISearchClient
    {
        private readonly HttpClient _httpClient;

        public SearchClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SearchResponse> CreateAsync(SearchRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/search", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SearchResponse>()!;
        }

        public async Task<SearchResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/search/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SearchResponse>()!;
        }

        public async Task<IEnumerable<SearchResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/search");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<SearchResponse>>()!;
        }

        public async Task<SearchResponse> UpdateAsync(string id, SearchRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/search/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SearchResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/search/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
