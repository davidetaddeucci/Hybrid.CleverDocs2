using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ValidationClient : IValidationClient
    {
        private readonly HttpClient _httpClient;
        public ValidationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<ValidationResponse> CreateAsync(ValidationRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/validations", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ValidationResponse>()!;
        }
        public async Task<ValidationResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/validations/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ValidationResponse>()!;
        }
        public async Task<IEnumerable<ValidationResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/validations");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ValidationResponse>>()!;
        }
        public async Task<ValidationResponse> UpdateAsync(string id, ValidationRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/validations/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ValidationResponse>()!;
        }
        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/validations/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
