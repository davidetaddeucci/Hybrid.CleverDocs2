using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Prompt;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class PromptClient : IPromptClient
    {
        private readonly HttpClient _httpClient;

        public PromptClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Prompt execution
        public async Task<PromptResponse?> ExecutePromptAsync(PromptRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/execute", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptResponse?> ExecutePromptFromTemplateAsync(string templateId, Dictionary<string, object> variables)
        {
            try
            {
                var request = new { template_id = templateId, variables };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/execute-template", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptResponse?> ExecutePromptWithConfigAsync(PromptRequest request, ModelConfig modelConfig)
        {
            try
            {
                request.ModelConfig = modelConfig;
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/execute-with-config", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Prompt template management
        public async Task<PromptTemplateResponse?> CreateTemplateAsync(PromptTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptTemplateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptTemplateResponse?> GetTemplateAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptTemplateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptListResponse?> ListTemplatesAsync(PromptListRequest request)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"limit={request.Limit}",
                    $"offset={request.Offset}"
                };

                if (!string.IsNullOrEmpty(request.Category))
                    queryParams.Add($"category={request.Category}");

                if (!string.IsNullOrEmpty(request.SearchQuery))
                    queryParams.Add($"search={request.SearchQuery}");

                if (request.Tags?.Any() == true)
                    queryParams.Add($"tags={string.Join(",", request.Tags)}");

                if (request.CreatedAfter.HasValue)
                    queryParams.Add($"created_after={request.CreatedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}");

                if (request.CreatedBefore.HasValue)
                    queryParams.Add($"created_before={request.CreatedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}");

                if (request.IsPublic.HasValue)
                    queryParams.Add($"is_public={request.IsPublic.Value}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/prompts/templates{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptTemplateResponse?> UpdateTemplateAsync(string templateId, PromptTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/prompts/templates/{templateId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptTemplateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> DeleteTemplateAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/prompts/templates/{templateId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Template versioning
        public async Task<PromptVersionResponse?> CreateTemplateVersionAsync(PromptVersionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates/versions", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptVersionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptVersionResponse>?> GetTemplateVersionsAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/versions");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptVersionResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptVersionResponse?> GetTemplateVersionAsync(string templateId, string version)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptVersionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> ActivateTemplateVersionAsync(string templateId, string version)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/prompts/templates/{templateId}/versions/{version}/activate", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> DeleteTemplateVersionAsync(string templateId, string version)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/prompts/templates/{templateId}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Prompt optimization
        public async Task<PromptOptimizationResponse?> StartPromptOptimizationAsync(PromptOptimizationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/optimization", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptOptimizationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptOptimizationResponse?> GetOptimizationStatusAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/optimization/{optimizationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptOptimizationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> CancelOptimizationAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/prompts/optimization/{optimizationId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<OptimizationIteration>?> GetOptimizationIterationsAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/optimization/{optimizationId}/iterations");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<OptimizationIteration>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<OptimizationResults?> GetOptimizationResultsAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/optimization/{optimizationId}/results");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OptimizationResults>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Prompt evaluation
        public async Task<PromptEvaluationResponse?> StartPromptEvaluationAsync(PromptEvaluationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/evaluation", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptEvaluationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptEvaluationResponse?> GetEvaluationStatusAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/evaluation/{evaluationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptEvaluationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> CancelEvaluationAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/prompts/evaluation/{evaluationId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<TestResult>?> GetEvaluationResultsAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/evaluation/{evaluationId}/results");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TestResult>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<EvaluationMetrics?> GetEvaluationMetricsAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/evaluation/{evaluationId}/metrics");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EvaluationMetrics>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Template categories and tags
        public async Task<List<string>?> GetTemplateCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/prompts/templates/categories");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetTemplateTagsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/prompts/templates/tags");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> GetTemplatesByCategoryAsync(string category)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/category/{category}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> GetTemplatesByTagAsync(string tag)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/tag/{tag}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Template search and discovery
        public async Task<PromptListResponse?> SearchTemplatesAsync(string query, int limit = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/search?q={query}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> GetRecommendedTemplatesAsync(string context)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/recommended?context={context}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> GetPopularTemplatesAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/popular?limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> GetRecentTemplatesAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/recent?limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Template sharing and collaboration
        public async Task<MessageResponse6?> ShareTemplateAsync(string templateId, List<string> userIds)
        {
            try
            {
                var request = new { user_ids = userIds };
                var response = await _httpClient.PostAsJsonAsync($"/v3/prompts/templates/{templateId}/share", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> UnshareTemplateAsync(string templateId, List<string> userIds)
        {
            try
            {
                var request = new { user_ids = userIds };
                var response = await _httpClient.PostAsJsonAsync($"/v3/prompts/templates/{templateId}/unshare", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetTemplateCollaboratorsAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/collaborators");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> SetTemplatePermissionsAsync(string templateId, Dictionary<string, string> permissions)
        {
            try
            {
                var request = new { permissions };
                var response = await _httpClient.PostAsJsonAsync($"/v3/prompts/templates/{templateId}/permissions", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Template validation
        public async Task<Dictionary<string, object>?> ValidateTemplateAsync(PromptTemplateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates/validate", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, object>?> ValidateTemplateVariablesAsync(string templateId, Dictionary<string, object> variables)
        {
            try
            {
                var request = new { variables };
                var response = await _httpClient.PostAsJsonAsync($"/v3/prompts/templates/{templateId}/validate-variables", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetTemplateValidationErrorsAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/validation-errors");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Prompt analytics and monitoring
        public async Task<PromptStatsResponse?> GetPromptStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/prompts/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptStatsResponse?> GetTemplateStatsAsync(string templateId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PerformanceTrend>?> GetPromptPerformanceTrendsAsync(int days = 30)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/performance-trends?days={days}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PerformanceTrend>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, object>?> GetPromptUsageAnalyticsAsync(string templateId, int days = 30)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/analytics?days={days}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Model configuration management
        public async Task<List<ModelConfig>?> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/prompts/models");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ModelConfig>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ModelConfig?> GetModelConfigAsync(string modelName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/models/{modelName}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ModelConfig>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> SaveModelConfigAsync(string configName, ModelConfig config)
        {
            try
            {
                var request = new { name = configName, config };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/model-configs", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<ModelConfig>?> GetSavedModelConfigsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/prompts/model-configs");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ModelConfig>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Prompt history and logging
        public async Task<List<PromptResponse>?> GetPromptHistoryAsync(int limit = 50, int offset = 0)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/history?limit={limit}&offset={offset}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptResponse>?> GetTemplateExecutionHistoryAsync(string templateId, int limit = 50)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/templates/{templateId}/history?limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<PromptResponse?> GetPromptExecutionAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/prompts/executions/{executionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PromptResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> DeletePromptExecutionAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/prompts/executions/{executionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Batch operations
        public async Task<List<PromptResponse>?> ExecuteBatchPromptsAsync(List<PromptRequest> requests)
        {
            try
            {
                var request = new { prompts = requests };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/batch-execute", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse6?> DeleteMultipleTemplatesAsync(List<string> templateIds)
        {
            try
            {
                var request = new { template_ids = templateIds };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates/batch-delete", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse6>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> ImportTemplatesAsync(List<PromptTemplateRequest> templates)
        {
            try
            {
                var request = new { templates };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates/import", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PromptTemplateResponse>?> ExportTemplatesAsync(List<string> templateIds)
        {
            try
            {
                var request = new { template_ids = templateIds };
                var response = await _httpClient.PostAsJsonAsync("/v3/prompts/templates/export", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PromptTemplateResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
