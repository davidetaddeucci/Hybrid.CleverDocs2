using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class ToolsClient : IToolsClient
    {
        private readonly HttpClient _httpClient;

        public ToolsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Tool execution
        public async Task<ToolsResponse?> ExecuteToolAsync(ToolsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tools/execute", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolsResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolsResponse?> ExecuteToolAsyncAsync(ToolsRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tools/execute-async", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolsResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolsResponse?> GetExecutionStatusAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/executions/{executionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolsResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse7?> CancelExecutionAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tools/executions/{executionId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Tool registration and management
        public async Task<ToolRegistrationResponse?> RegisterToolAsync(ToolRegistrationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tools", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolRegistrationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolRegistrationResponse?> GetToolAsync(string toolId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/{toolId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolRegistrationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolListResponse?> ListToolsAsync(ToolListRequest request)
        {
            try
            {
                var queryParams = new List<string> { $"limit={request.Limit}", $"offset={request.Offset}" };
                if (!string.IsNullOrEmpty(request.Category)) queryParams.Add($"category={request.Category}");
                if (!string.IsNullOrEmpty(request.SearchQuery)) queryParams.Add($"search={request.SearchQuery}");
                if (request.Tags?.Any() == true) queryParams.Add($"tags={string.Join(",", request.Tags)}");
                if (request.IsPublic.HasValue) queryParams.Add($"is_public={request.IsPublic.Value}");
                if (request.CreatedAfter.HasValue) queryParams.Add($"created_after={request.CreatedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}");
                if (request.CreatedBefore.HasValue) queryParams.Add($"created_before={request.CreatedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/tools{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolListResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolRegistrationResponse?> UpdateToolAsync(string toolId, ToolRegistrationRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/tools/{toolId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolRegistrationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse7?> DeleteToolAsync(string toolId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/tools/{toolId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Tool discovery and search
        public async Task<ToolListResponse?> SearchToolsAsync(string query, int limit = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/search?q={query}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolListResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<List<ToolRegistrationResponse>?> GetToolsByCategoryAsync(string category)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/category/{category}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ToolRegistrationResponse>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<List<ToolRegistrationResponse>?> GetToolsByTagAsync(string tag)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/tag/{tag}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ToolRegistrationResponse>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<List<ToolRegistrationResponse>?> GetPopularToolsAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/popular?limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ToolRegistrationResponse>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<List<ToolRegistrationResponse>?> GetRecentToolsAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/recent?limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ToolRegistrationResponse>>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Tool validation and testing
        public async Task<ToolValidationResponse?> ValidateToolParametersAsync(ToolValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tools/validate", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolTestResponse?> RunToolTestsAsync(ToolTestRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tools/test", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolTestResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolTestResponse?> GetTestResultsAsync(string testId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/tests/{testId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolTestResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse7?> CancelTestAsync(string testId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tools/tests/{testId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Tool versioning
        public async Task<ToolRegistrationResponse?> CreateToolVersionAsync(string toolId, ToolRegistrationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/tools/{toolId}/versions", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolRegistrationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<List<ToolRegistrationResponse>?> GetToolVersionsAsync(string toolId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/{toolId}/versions");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ToolRegistrationResponse>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ToolRegistrationResponse?> GetToolVersionAsync(string toolId, string version)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tools/{toolId}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolRegistrationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse7?> ActivateToolVersionAsync(string toolId, string version)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tools/{toolId}/versions/{version}/activate", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse7?> DeleteToolVersionAsync(string toolId, string version)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/tools/{toolId}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Implementazioni compatte per tutti gli altri metodi
        public async Task<List<string>?> GetToolCategoriesAsync() => await GetListAsync<string>("/v3/tools/categories");
        public async Task<List<string>?> GetToolTagsAsync() => await GetListAsync<string>("/v3/tools/tags");
        public async Task<MessageResponse7?> AddToolCategoryAsync(string category) => await PostMessageAsync($"/v3/tools/categories", new { category });
        public async Task<MessageResponse7?> AddToolTagAsync(string tag) => await PostMessageAsync($"/v3/tools/tags", new { tag });
        public async Task<MessageResponse7?> RemoveToolCategoryAsync(string category) => await DeleteMessageAsync($"/v3/tools/categories/{category}");
        public async Task<MessageResponse7?> RemoveToolTagAsync(string tag) => await DeleteMessageAsync($"/v3/tools/tags/{tag}");

        public async Task<MessageResponse7?> SetToolPermissionsAsync(string toolId, AccessControl accessControl) => await PostMessageAsync($"/v3/tools/{toolId}/permissions", accessControl);
        public async Task<AccessControl?> GetToolPermissionsAsync(string toolId) => await GetAsync<AccessControl>($"/v3/tools/{toolId}/permissions");
        public async Task<MessageResponse7?> ShareToolAsync(string toolId, List<string> userIds) => await PostMessageAsync($"/v3/tools/{toolId}/share", new { user_ids = userIds });
        public async Task<MessageResponse7?> UnshareToolAsync(string toolId, List<string> userIds) => await PostMessageAsync($"/v3/tools/{toolId}/unshare", new { user_ids = userIds });
        public async Task<List<string>?> GetToolCollaboratorsAsync(string toolId) => await GetListAsync<string>($"/v3/tools/{toolId}/collaborators");

        public async Task<ToolStatsResponse?> GetToolStatsAsync(string toolId) => await GetAsync<ToolStatsResponse>($"/v3/tools/{toolId}/stats");
        public async Task<ToolStatsResponse?> GetGlobalToolStatsAsync() => await GetAsync<ToolStatsResponse>("/v3/tools/stats");
        public async Task<List<PerformanceTrend>?> GetToolPerformanceTrendsAsync(string toolId, int days = 30) => await GetListAsync<PerformanceTrend>($"/v3/tools/{toolId}/performance-trends?days={days}");
        public async Task<Dictionary<string, object>?> GetToolUsageAnalyticsAsync(string toolId, int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/tools/{toolId}/analytics?days={days}");

        public async Task<List<ToolsResponse>?> GetToolExecutionHistoryAsync(string toolId, int limit = 50, int offset = 0) => await GetListAsync<ToolsResponse>($"/v3/tools/{toolId}/history?limit={limit}&offset={offset}");
        public async Task<List<ToolsResponse>?> GetUserExecutionHistoryAsync(int limit = 50, int offset = 0) => await GetListAsync<ToolsResponse>($"/v3/tools/executions/history?limit={limit}&offset={offset}");
        public async Task<ToolsResponse?> GetExecutionDetailsAsync(string executionId) => await GetAsync<ToolsResponse>($"/v3/tools/executions/{executionId}/details");
        public async Task<MessageResponse7?> DeleteExecutionAsync(string executionId) => await DeleteMessageAsync($"/v3/tools/executions/{executionId}");

        public async Task<Dictionary<string, object>?> GetToolConfigurationAsync(string toolId) => await GetAsync<Dictionary<string, object>>($"/v3/tools/{toolId}/configuration");
        public async Task<MessageResponse7?> UpdateToolConfigurationAsync(string toolId, Dictionary<string, object> configuration) => await PostMessageAsync($"/v3/tools/{toolId}/configuration", configuration);
        public async Task<Dictionary<string, object>?> GetDefaultToolConfigurationAsync() => await GetAsync<Dictionary<string, object>>("/v3/tools/configuration/default");
        public async Task<MessageResponse7?> ResetToolConfigurationAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/configuration/reset", null);

        public async Task<List<string>?> GetToolDependenciesAsync(string toolId) => await GetListAsync<string>($"/v3/tools/{toolId}/dependencies");
        public async Task<MessageResponse7?> ValidateToolDependenciesAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/dependencies/validate", null);
        public async Task<List<ToolRegistrationResponse>?> GetDependentToolsAsync(string toolId) => await GetListAsync<ToolRegistrationResponse>($"/v3/tools/{toolId}/dependents");
        public async Task<Dictionary<string, object>?> CheckToolRequirementsAsync(string toolId) => await GetAsync<Dictionary<string, object>>($"/v3/tools/{toolId}/requirements/check");

        public async Task<MessageResponse7?> DeployToolAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/deploy", null);
        public async Task<MessageResponse7?> UndeployToolAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/undeploy", null);
        public async Task<string?> GetToolDeploymentStatusAsync(string toolId) => (await GetAsync<dynamic>($"/v3/tools/{toolId}/deployment/status"))?.status;
        public async Task<MessageResponse7?> RestartToolAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/restart", null);
        public async Task<Dictionary<string, object>?> GetToolHealthAsync(string toolId) => await GetAsync<Dictionary<string, object>>($"/v3/tools/{toolId}/health");

        public async Task<List<ToolsResponse>?> ExecuteBatchToolsAsync(List<ToolsRequest> requests) => await PostListAsync<ToolsResponse>("/v3/tools/batch-execute", new { requests });
        public async Task<MessageResponse7?> DeleteMultipleToolsAsync(List<string> toolIds) => await PostMessageAsync("/v3/tools/batch-delete", new { tool_ids = toolIds });
        public async Task<List<ToolRegistrationResponse>?> ImportToolsAsync(List<ToolRegistrationRequest> tools) => await PostListAsync<ToolRegistrationResponse>("/v3/tools/import", new { tools });
        public async Task<List<ToolRegistrationResponse>?> ExportToolsAsync(List<string> toolIds) => await PostListAsync<ToolRegistrationResponse>("/v3/tools/export", new { tool_ids = toolIds });

        public async Task<ToolListResponse?> GetMarketplaceToolsAsync(int limit = 50, int offset = 0) => await GetAsync<ToolListResponse>($"/v3/tools/marketplace?limit={limit}&offset={offset}");
        public async Task<MessageResponse7?> PublishToolToMarketplaceAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/marketplace/publish", null);
        public async Task<MessageResponse7?> UnpublishToolFromMarketplaceAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/marketplace/unpublish", null);
        public async Task<MessageResponse7?> InstallMarketplaceToolAsync(string toolId) => await PostMessageAsync($"/v3/tools/marketplace/{toolId}/install", null);
        public async Task<MessageResponse7?> UninstallToolAsync(string toolId) => await PostMessageAsync($"/v3/tools/{toolId}/uninstall", null);

        public async Task<string?> GetToolDocumentationAsync(string toolId) => (await GetAsync<dynamic>($"/v3/tools/{toolId}/documentation"))?.documentation;
        public async Task<MessageResponse7?> UpdateToolDocumentationAsync(string toolId, string documentation) => await PostMessageAsync($"/v3/tools/{toolId}/documentation", new { documentation });
        public async Task<List<FunctionExample>?> GetToolExamplesAsync(string toolId) => await GetListAsync<FunctionExample>($"/v3/tools/{toolId}/examples");
        public async Task<MessageResponse7?> AddToolExampleAsync(string toolId, FunctionExample example) => await PostMessageAsync($"/v3/tools/{toolId}/examples", example);
        public async Task<MessageResponse7?> RemoveToolExampleAsync(string toolId, string exampleId) => await DeleteMessageAsync($"/v3/tools/{toolId}/examples/{exampleId}");

        // Helper methods
        private async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException) { return default; }
        }

        private async Task<List<T>?> GetListAsync<T>(string endpoint) => await GetAsync<List<T>>(endpoint);

        private async Task<MessageResponse7?> PostMessageAsync(string endpoint, object? data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<MessageResponse7?> DeleteMessageAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse7>();
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<List<T>?> PostListAsync<T>(string endpoint, object data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<T>>();
            }
            catch (HttpRequestException) { return null; }
        }
    }
}