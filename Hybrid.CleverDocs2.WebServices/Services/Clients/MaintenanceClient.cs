using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Maintenance;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class MaintenanceClient : IMaintenanceClient
    {
        private readonly HttpClient _httpClient;

        public MaintenanceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MaintenanceResponse> CreateAsync(MaintenanceRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/maintenance", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MaintenanceResponse>()!;
        }

        public async Task<MaintenanceResponse> GetAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/maintenance/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MaintenanceResponse>()!;
        }

        public async Task<IEnumerable<MaintenanceResponse>> ListAsync()
        {
            var response = await _httpClient.GetAsync("/maintenance");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<MaintenanceResponse>>()!;
        }

        public async Task<MaintenanceResponse> UpdateAsync(string id, MaintenanceRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/maintenance/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MaintenanceResponse>()!;
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/maintenance/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
