using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class DocumentClient : IDocumentClient
    {
        private readonly HttpClient _httpClient;

        public DocumentClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DocumentResponse> CreateAsync(DocumentRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/documents", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentResponse>()!;
        }

        public async Task<DocumentResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/documents/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentResponse>()!;
        }

        public async Task<IEnumerable<DocumentResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/documents");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<DocumentResponse>>()!;
        }

        public async Task<DocumentResponse> UpdateAsync(string id, DocumentRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/documents/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DocumentResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/documents/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
