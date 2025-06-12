using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class OrchestrationClient : IOrchestrationClient
    {
        private readonly HttpClient _httpClient;

        public OrchestrationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Workflow definition management
        public async Task<WorkflowDefinitionResponse?> CreateWorkflowAsync(WorkflowDefinitionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/orchestration/workflows", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowDefinitionResponse?> GetWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowListResponse?> ListWorkflowsAsync(WorkflowListRequest request)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"limit={request.Limit}",
                    $"offset={request.Offset}"
                };

                if (!string.IsNullOrEmpty(request.Status))
                    queryParams.Add($"status={request.Status}");

                if (request.Tags?.Any() == true)
                    queryParams.Add($"tags={string.Join(",", request.Tags)}");

                if (request.CreatedAfter.HasValue)
                    queryParams.Add($"created_after={request.CreatedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}");

                if (request.CreatedBefore.HasValue)
                    queryParams.Add($"created_before={request.CreatedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowDefinitionResponse?> UpdateWorkflowAsync(string workflowId, WorkflowDefinitionRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/orchestration/workflows/{workflowId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> DeleteWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/orchestration/workflows/{workflowId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow execution
        public async Task<WorkflowExecutionResponse?> ExecuteWorkflowAsync(WorkflowExecutionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/orchestration/executions", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowExecutionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<OrchestrationResponse?> GetExecutionStatusAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/executions/{executionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OrchestrationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ExecutionListResponse?> ListExecutionsAsync(ExecutionListRequest request)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"limit={request.Limit}",
                    $"offset={request.Offset}"
                };

                if (!string.IsNullOrEmpty(request.WorkflowId))
                    queryParams.Add($"workflow_id={request.WorkflowId}");

                if (!string.IsNullOrEmpty(request.Status))
                    queryParams.Add($"status={request.Status}");

                if (request.StartedAfter.HasValue)
                    queryParams.Add($"started_after={request.StartedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}");

                if (request.StartedBefore.HasValue)
                    queryParams.Add($"started_before={request.StartedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/orchestration/executions{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExecutionListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> CancelExecutionAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/executions/{executionId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> RetryExecutionAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/executions/{executionId}/retry", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow control
        public async Task<MessageResponse5?> PauseWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/pause", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> ResumeWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/resume", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> EnableWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/enable", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> DisableWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/disable", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Step execution management
        public async Task<List<StepExecution>?> GetExecutionStepsAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/executions/{executionId}/steps");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<StepExecution>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<StepExecution?> GetStepExecutionAsync(string executionId, string stepId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/executions/{executionId}/steps/{stepId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<StepExecution>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> RetryStepAsync(string executionId, string stepId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/executions/{executionId}/steps/{stepId}/retry", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> SkipStepAsync(string executionId, string stepId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/executions/{executionId}/steps/{stepId}/skip", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow validation
        public async Task<WorkflowValidationResponse?> ValidateWorkflowAsync(WorkflowDefinitionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/orchestration/workflows/validate", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowValidationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowValidationResponse?> ValidateWorkflowByIdAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/validate", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowValidationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow versioning
        public async Task<WorkflowDefinitionResponse?> CreateWorkflowVersionAsync(string workflowId, WorkflowDefinitionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/orchestration/workflows/{workflowId}/versions", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<WorkflowDefinitionResponse>?> GetWorkflowVersionsAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/versions");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<WorkflowDefinitionResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> PromoteWorkflowVersionAsync(string workflowId, string version)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/versions/{version}/promote", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> RollbackWorkflowVersionAsync(string workflowId, string version)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/versions/{version}/rollback", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow triggers
        public async Task<MessageResponse5?> TriggerWorkflowAsync(string workflowId, Dictionary<string, object> inputData)
        {
            try
            {
                var request = new { input_data = inputData };
                var response = await _httpClient.PostAsJsonAsync($"/v3/orchestration/workflows/{workflowId}/trigger", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<WorkflowTrigger>?> GetWorkflowTriggersAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/triggers");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<WorkflowTrigger>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> EnableTriggerAsync(string workflowId, string triggerId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/triggers/{triggerId}/enable", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> DisableTriggerAsync(string workflowId, string triggerId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/triggers/{triggerId}/disable", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow monitoring and analytics
        public async Task<WorkflowStatsResponse?> GetWorkflowStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/orchestration/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowStatsResponse?> GetWorkflowStatsAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<WorkflowUsageStats>?> GetWorkflowUsageStatsAsync(int days = 30)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/usage-stats?days={days}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<WorkflowUsageStats>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, object>?> GetExecutionMetricsAsync(string executionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/executions/{executionId}/metrics");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow templates and import/export
        public async Task<List<WorkflowDefinitionResponse>?> GetWorkflowTemplatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/orchestration/templates");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<WorkflowDefinitionResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowDefinitionResponse?> CreateWorkflowFromTemplateAsync(string templateId, Dictionary<string, object> parameters)
        {
            try
            {
                var request = new { template_id = templateId, parameters };
                var response = await _httpClient.PostAsJsonAsync("/v3/orchestration/workflows/from-template", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<string?> ExportWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/export");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<WorkflowDefinitionResponse?> ImportWorkflowAsync(string workflowDefinition)
        {
            try
            {
                var request = new { workflow_definition = workflowDefinition };
                var response = await _httpClient.PostAsJsonAsync("/v3/orchestration/workflows/import", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<WorkflowDefinitionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow scheduling
        public async Task<MessageResponse5?> ScheduleWorkflowAsync(string workflowId, string cronExpression)
        {
            try
            {
                var request = new { cron_expression = cronExpression };
                var response = await _httpClient.PostAsJsonAsync($"/v3/orchestration/workflows/{workflowId}/schedule", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> UnscheduleWorkflowAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/orchestration/workflows/{workflowId}/schedule");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<Dictionary<string, object>>?> GetScheduledWorkflowsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/orchestration/scheduled-workflows");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Workflow dependencies
        public async Task<List<string>?> GetWorkflowDependenciesAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/dependencies");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetWorkflowDependentsAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/orchestration/workflows/{workflowId}/dependents");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse5?> ValidateWorkflowDependenciesAsync(string workflowId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/orchestration/workflows/{workflowId}/validate-dependencies", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse5>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
