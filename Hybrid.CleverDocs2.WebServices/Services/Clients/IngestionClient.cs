using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
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

        // Ingestion CRUD operations
        public async Task<IngestionCreateResponse?> CreateIngestionAsync(IngestionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/ingestion", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionCreateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionResponse?> GetIngestionAsync(string ingestionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/ingestion/{ingestionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionListResponse?> ListIngestionsAsync(IngestionListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.DocumentIds?.Any() == true)
                        queryParams.Add($"document_ids={string.Join(",", request.DocumentIds)}");
                    
                    if (request.IngestionIds?.Any() == true)
                        queryParams.Add($"ingestion_ids={string.Join(",", request.IngestionIds)}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/ingestion{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionResponse?> UpdateIngestionAsync(string ingestionId, IngestionUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/ingestion/{ingestionId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteIngestionAsync(string ingestionId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/ingestion/{ingestionId}");
            response.EnsureSuccessStatusCode();
        }

        // Ingestion status and monitoring
        public async Task<IngestionStatusResponse?> GetIngestionStatusAsync(IngestionStatusRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/ingestion/status", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionStatusResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionStatsResponse?> GetIngestionStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/ingestion/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionLogsResponse?> GetIngestionLogsAsync(string ingestionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/ingestion/{ingestionId}/logs");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionLogsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Ingestion control operations
        public async Task<IngestionRetryResponse?> RetryIngestionsAsync(IngestionRetryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/ingestion/retry", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionRetryResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionCancelResponse?> CancelIngestionsAsync(IngestionCancelRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/ingestion/cancel", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionCancelResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Streaming support for real-time updates
        public async Task<IAsyncEnumerable<IngestionResponse>?> StreamIngestionUpdatesAsync(List<string> ingestionIds)
        {
            try
            {
                var request = new { ingestion_ids = ingestionIds };
                var response = await _httpClient.PostAsJsonAsync("/v3/ingestion/stream", request);
                response.EnsureSuccessStatusCode();

                return StreamIngestionResponseAsync(response);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private async IAsyncEnumerable<IngestionResponse> StreamIngestionResponseAsync(HttpResponseMessage response)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data != "[DONE]")
                    {
                        var ingestionResponse = TryDeserializeIngestionResponse(data);
                        if (ingestionResponse != null)
                            yield return ingestionResponse;
                    }
                }
            }
        }

        private static IngestionResponse? TryDeserializeIngestionResponse(string data)
        {
            try
            {
                return JsonSerializer.Deserialize<IngestionResponse>(data);
            }
            catch (JsonException)
            {
                // Skip invalid JSON
                return null;
            }
        }

        // Bulk operations
        public async Task<MessageResponse2?> DeleteMultipleIngestionsAsync(List<string> ingestionIds)
        {
            try
            {
                var request = new { ingestion_ids = ingestionIds };
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/v3/ingestion/bulk")
                {
                    Content = JsonContent.Create(request)
                });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionListResponse?> GetIngestionsByDocumentAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/ingestion/document/{documentId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionStatsResponse?> GetIngestionStatsByStatusAsync(string status)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/ingestion/stats/status/{status}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Pipeline management
        public async Task<MessageResponse2?> PauseIngestionPipelineAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/ingestion/pipeline/pause", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse2?> ResumeIngestionPipelineAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/ingestion/pipeline/resume", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse2?> GetIngestionPipelineStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/ingestion/pipeline/status");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Configuration management
        public async Task<MessageResponse2?> UpdateIngestionConfigAsync(IngestionConfig config)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/v3/ingestion/config", config);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IngestionConfig?> GetIngestionConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/ingestion/config");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IngestionConfig>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
