using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Graph;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class GraphClient : IGraphClient
    {
        private readonly HttpClient _httpClient;

        public GraphClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Graph operations
        public async Task<GraphResponse?> GetGraphAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphPullResponse?> PullGraphAsync(string collectionId, GraphPullRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/pull", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphPullResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphResetResponse?> ResetGraphAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/graphs/{collectionId}/reset", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphResetResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphStatusResponse?> GetGraphStatusAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/status");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphStatusResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Community operations
        public async Task<GraphCommunityResponse?> BuildCommunitiesAsync(string collectionId, GraphCommunityRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/communities/build", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphCommunityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Entity operations
        public async Task<EntityResponse?> CreateEntityAsync(string collectionId, EntityRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/entities", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EntityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<EntityResponse?> GetEntityAsync(string collectionId, string entityId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/entities/{entityId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EntityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphListResponse<EntityResponse>?> ListEntitiesAsync(string collectionId, GraphListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.EntityNames?.Any() == true)
                        queryParams.Add($"entity_names={string.Join(",", request.EntityNames)}");
                    
                    if (!string.IsNullOrEmpty(request.EntityTableName))
                        queryParams.Add($"entity_table_name={request.EntityTableName}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/entities{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphListResponse<EntityResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<EntityResponse?> UpdateEntityAsync(string collectionId, string entityId, EntityRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/graphs/{collectionId}/entities/{entityId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EntityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteEntityAsync(string collectionId, string entityId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/graphs/{collectionId}/entities/{entityId}");
            response.EnsureSuccessStatusCode();
        }

        // Relationship operations
        public async Task<RelationshipResponse?> CreateRelationshipAsync(string collectionId, RelationshipRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/relationships", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RelationshipResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<RelationshipResponse?> GetRelationshipAsync(string collectionId, string relationshipId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/relationships/{relationshipId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RelationshipResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphListResponse<RelationshipResponse>?> ListRelationshipsAsync(string collectionId, GraphListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.RelationshipTypes?.Any() == true)
                        queryParams.Add($"relationship_types={string.Join(",", request.RelationshipTypes)}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/relationships{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphListResponse<RelationshipResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<RelationshipResponse?> UpdateRelationshipAsync(string collectionId, string relationshipId, RelationshipRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/graphs/{collectionId}/relationships/{relationshipId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RelationshipResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteRelationshipAsync(string collectionId, string relationshipId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/graphs/{collectionId}/relationships/{relationshipId}");
            response.EnsureSuccessStatusCode();
        }

        // Community operations
        public async Task<CommunityResponse?> CreateCommunityAsync(string collectionId, CommunityRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/communities", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CommunityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CommunityResponse?> GetCommunityAsync(string collectionId, string communityId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/communities/{communityId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CommunityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<GraphListResponse<CommunityResponse>?> ListCommunitiesAsync(string collectionId, GraphListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.CommunityNumbers?.Any() == true)
                        queryParams.Add($"community_numbers={string.Join(",", request.CommunityNumbers)}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/graphs/{collectionId}/communities{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphListResponse<CommunityResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CommunityResponse?> UpdateCommunityAsync(string collectionId, string communityId, CommunityRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/graphs/{collectionId}/communities/{communityId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CommunityResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteCommunityAsync(string collectionId, string communityId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/graphs/{collectionId}/communities/{communityId}");
            response.EnsureSuccessStatusCode();
        }

        // Search operations
        public async Task<GraphSearchResponse?> SearchGraphAsync(string collectionId, GraphSearchRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/graphs/{collectionId}/search", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GraphSearchResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Bulk operations
        public async Task DeleteEntitiesByFilterAsync(string collectionId, Dictionary<string, object> filters)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/graphs/{collectionId}/entities/by-filter")
            {
                Content = JsonContent.Create(new { filters })
            });
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteRelationshipsByFilterAsync(string collectionId, Dictionary<string, object> filters)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/graphs/{collectionId}/relationships/by-filter")
            {
                Content = JsonContent.Create(new { filters })
            });
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteCommunitiesByFilterAsync(string collectionId, Dictionary<string, object> filters)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/graphs/{collectionId}/communities/by-filter")
            {
                Content = JsonContent.Create(new { filters })
            });
            response.EnsureSuccessStatusCode();
        }

        // Export operations
        public async Task<Stream?> ExportEntitiesAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/graphs/{collectionId}/entities/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> ExportRelationshipsAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/graphs/{collectionId}/relationships/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> ExportCommunitiesAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/graphs/{collectionId}/communities/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Stream?> ExportGraphAsync(string collectionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/graphs/{collectionId}/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
