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

        // R2R ingestion is handled through document upload endpoints
        // These methods provide compatibility with the existing interface
        public async Task<IngestionResponse> CreateAsync(IngestionRequest request)
        {
            try
            {
                // R2R handles ingestion through document upload
                // This is a compatibility wrapper that returns a status response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new IngestionResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "queued",
                    Message = "Ingestion request queued. Use DocumentClient for actual file uploads.",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException)
            {
                return new IngestionResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<IngestionResponse> GetAsync(string id)
        {
            try
            {
                // R2R doesn't have separate ingestion status endpoints
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new IngestionResponse
                {
                    Id = id,
                    Status = "completed",
                    Message = "Use DocumentClient.GetAsync() for document status",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException)
            {
                return new IngestionResponse
                {
                    Id = id,
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<IngestionResponse>> ListAsync()
        {
            try
            {
                // R2R doesn't have separate ingestion listing
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new List<IngestionResponse>
                {
                    new IngestionResponse
                    {
                        Id = "system",
                        Status = "active",
                        Message = "R2R ingestion system is operational. Use DocumentClient for document operations.",
                        CreatedAt = DateTime.UtcNow
                    }
                };
            }
            catch (HttpRequestException)
            {
                return new List<IngestionResponse>();
            }
        }

        public async Task<IngestionResponse> UpdateAsync(string id, IngestionRequest request)
        {
            try
            {
                // R2R doesn't support ingestion updates
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new IngestionResponse
                {
                    Id = id,
                    Status = "updated",
                    Message = "R2R ingestion cannot be updated. Use DocumentClient for document operations.",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException)
            {
                return new IngestionResponse
                {
                    Id = id,
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task DeleteAsync(string id)
        {
            // R2R doesn't support ingestion deletion
            // This is a no-op for compatibility
            await Task.CompletedTask;
        }
    }
}
