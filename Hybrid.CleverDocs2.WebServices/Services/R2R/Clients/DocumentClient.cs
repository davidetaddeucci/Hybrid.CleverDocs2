using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Document;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class DocumentClient : IDocumentClient
    {
        private readonly HttpClient _httpClient;

        public DocumentClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Core CRUD operations
        public async Task<DocumentResponse?> CreateAsync(DocumentRequest request)
        {
            try
            {
                HttpResponseMessage response;

                if (request.File != null)
                {
                    // Handle file upload
                    using var content = new MultipartFormDataContent();
                    using var fileStream = request.File.OpenReadStream();
                    using var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.File.ContentType);
                    content.Add(streamContent, "file", request.File.FileName);

                    if (request.Metadata.Any())
                    {
                        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(request.Metadata)), "metadata");
                    }

                    response = await _httpClient.PostAsync("/v3/documents", content);
                }
                else
                {
                    // Handle JSON request
                    response = await _httpClient.PostAsJsonAsync("/v3/documents", request);
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<DocumentResponse?> GetAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<DocumentListResponse?> ListAsync(DocumentListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    if (request.Ids?.Any() == true)
                        queryParams.Add($"ids={string.Join(",", request.Ids)}");
                    
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    queryParams.Add($"include_summary_embeddings={request.IncludeSummaryEmbeddings.ToString().ToLower()}");
                    queryParams.Add($"owner_only={request.OwnerOnly.ToString().ToLower()}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/documents{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<DocumentResponse?> UpdateAsync(string id, DocumentRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/documents/{id}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/v3/documents/{id}");
            response.EnsureSuccessStatusCode();
        }

        // File operations
        public async Task<Stream?> DownloadAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}/download");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> DownloadZipAsync(List<string> documentIds)
        {
            try
            {
                var queryString = "?" + string.Join("&", documentIds.Select(id => $"document_ids={id}"));
                var response = await _httpClient.GetAsync($"/v3/documents/download_zip{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Chunk operations
        public async Task<List<DocumentChunk>?> GetChunksAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}/chunks");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<DocumentChunk>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Metadata operations
        public async Task<DocumentResponse?> UpdateMetadataAsync(string id, DocumentMetadataRequest request)
        {
            try
            {
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/v3/documents/{id}/metadata")
                {
                    Content = JsonContent.Create(request)
                });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<DocumentResponse?> ReplaceMetadataAsync(string id, DocumentMetadataRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/documents/{id}/metadata", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Search operations
        public async Task<List<DocumentResponse>?> SearchAsync(DocumentSearchRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/documents/search", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<DocumentResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Knowledge graph operations
        public async Task StartExtractionAsync(string id, DocumentExtractionRequest? request = null)
        {
            var response = await _httpClient.PostAsJsonAsync($"/v3/documents/{id}/extract", request ?? new DocumentExtractionRequest());
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<DocumentEntityResponse>?> GetEntitiesAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}/entities");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<DocumentEntityResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<DocumentRelationshipResponse>?> GetRelationshipsAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}/relationships");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<DocumentRelationshipResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Bulk operations
        public async Task DeleteByFilterAsync(Dictionary<string, object> filters)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/v3/documents/by-filter")
            {
                Content = JsonContent.Create(new { filters })
            });
            response.EnsureSuccessStatusCode();
        }

        public async Task<Stream?> ExportAsync(DocumentExportRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/documents/export", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> ExportEntitiesAsync(string id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/documents/{id}/entities/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> ExportRelationshipsAsync(string id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/documents/{id}/relationships/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Deduplication
        public async Task StartDeduplicationAsync(string id)
        {
            var response = await _httpClient.PostAsync($"/v3/documents/{id}/deduplicate", null);
            response.EnsureSuccessStatusCode();
        }

        // Collections
        public async Task<List<string>?> GetCollectionsAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/documents/{id}/collections");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
