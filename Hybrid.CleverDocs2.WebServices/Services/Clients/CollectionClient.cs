using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Collection;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class CollectionClient : ICollectionClient
    {
        private readonly HttpClient _httpClient;

        public CollectionClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Collection CRUD operations
        public async Task<CollectionCreateResponse?> CreateCollectionAsync(CollectionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/collections", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionCreateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionResponse?> GetCollectionAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/collections/{collectionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionListResponse?> ListCollectionsAsync(CollectionListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.CollectionIds?.Any() == true)
                        queryParams.Add($"collection_ids={string.Join(",", request.CollectionIds)}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/collections{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionResponse?> UpdateCollectionAsync(string collectionId, CollectionUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/collections/{collectionId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteCollectionAsync(string collectionId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/collections/{collectionId}");
            response.EnsureSuccessStatusCode();
        }

        // Document management in collections
        public async Task<CollectionDocumentResponse?> AddDocumentToCollectionAsync(string collectionId, CollectionDocumentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/collections/{collectionId}/documents", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionDocumentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionDocumentsListResponse?> ListCollectionDocumentsAsync(string collectionId, int offset = 0, int limit = 100)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/collections/{collectionId}/documents?offset={offset}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionDocumentsListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task RemoveDocumentFromCollectionAsync(string collectionId, string documentId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/collections/{collectionId}/documents/{documentId}");
            response.EnsureSuccessStatusCode();
        }

        // User access management
        public async Task<CollectionUserResponse?> AddUserToCollectionAsync(string collectionId, CollectionUserRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/collections/{collectionId}/users", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionUserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionUsersListResponse?> ListCollectionUsersAsync(string collectionId, int offset = 0, int limit = 100)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/collections/{collectionId}/users?offset={offset}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionUsersListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CollectionUserResponse?> UpdateUserPermissionAsync(string collectionId, string userId, CollectionPermissionRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/collections/{collectionId}/users/{userId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionUserResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task RemoveUserFromCollectionAsync(string collectionId, string userId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/collections/{collectionId}/users/{userId}");
            response.EnsureSuccessStatusCode();
        }

        // Collection statistics and analytics
        public async Task<CollectionStatsResponse?> GetCollectionStatsAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/collections/{collectionId}/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Bulk operations
        public async Task<MessageResponse?> AddMultipleDocumentsAsync(string collectionId, List<string> documentIds)
        {
            try
            {
                var request = new { document_ids = documentIds };
                var response = await _httpClient.PostAsJsonAsync($"/v3/collections/{collectionId}/documents/bulk", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> RemoveMultipleDocumentsAsync(string collectionId, List<string> documentIds)
        {
            try
            {
                var request = new { document_ids = documentIds };
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/collections/{collectionId}/documents/bulk")
                {
                    Content = JsonContent.Create(request)
                });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> AddMultipleUsersAsync(string collectionId, List<CollectionUserRequest> users)
        {
            try
            {
                var request = new { users };
                var response = await _httpClient.PostAsJsonAsync($"/v3/collections/{collectionId}/users/bulk", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> RemoveMultipleUsersAsync(string collectionId, List<string> userIds)
        {
            try
            {
                var request = new { user_ids = userIds };
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/collections/{collectionId}/users/bulk")
                {
                    Content = JsonContent.Create(request)
                });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Collection cloning and templates
        public async Task<CollectionCreateResponse?> CloneCollectionAsync(string collectionId, CollectionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/collections/{collectionId}/clone", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CollectionCreateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> ExportCollectionAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/collections/{collectionId}/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> ImportCollectionAsync(CollectionRequest request, Stream dataStream)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(JsonContent.Create(request), "collection");
                content.Add(new StreamContent(dataStream), "data", "collection_data.json");

                var response = await _httpClient.PostAsync("/v3/collections/import", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}