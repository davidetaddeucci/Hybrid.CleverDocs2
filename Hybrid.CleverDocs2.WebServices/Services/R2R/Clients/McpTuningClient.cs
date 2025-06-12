using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.McpTuning;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class McpTuningClient : IMcpTuningClient
    {
        private readonly HttpClient _httpClient;

        public McpTuningClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Model tuning operations
        public async Task<McpTuningResponse?> StartTuningAsync(McpTuningRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tuning", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<McpTuningResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<McpTuningResponse?> GetTuningStatusAsync(string tuningId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/{tuningId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<McpTuningResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> CancelTuningAsync(string tuningId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/{tuningId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> PauseTuningAsync(string tuningId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/{tuningId}/pause", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> ResumeTuningAsync(string tuningId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/{tuningId}/resume", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Tuning job management
        public async Task<TuningJobResponse?> CreateTuningJobAsync(TuningJobRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tuning/jobs", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TuningJobResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<TuningJobResponse?> GetTuningJobAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/jobs/{jobId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TuningJobResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<TuningJobListResponse?> ListTuningJobsAsync(TuningJobListRequest request)
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

                if (!string.IsNullOrEmpty(request.ModelName))
                    queryParams.Add($"model_name={request.ModelName}");

                if (request.CreatedAfter.HasValue)
                    queryParams.Add($"created_after={request.CreatedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}");

                if (request.CreatedBefore.HasValue)
                    queryParams.Add($"created_before={request.CreatedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/tuning/jobs{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TuningJobListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> CancelTuningJobAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/jobs/{jobId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> DeleteTuningJobAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/tuning/jobs/{jobId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Hyperparameter optimization
        public async Task<HyperparameterOptimizationResponse?> StartHyperparameterOptimizationAsync(HyperparameterOptimizationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tuning/hyperparameter-optimization", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HyperparameterOptimizationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<HyperparameterOptimizationResponse?> GetOptimizationStatusAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/hyperparameter-optimization/{optimizationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HyperparameterOptimizationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> CancelOptimizationAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/hyperparameter-optimization/{optimizationId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<OptimizationTrial>?> GetOptimizationTrialsAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/hyperparameter-optimization/{optimizationId}/trials");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<OptimizationTrial>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<OptimizationTrial?> GetBestTrialAsync(string optimizationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/hyperparameter-optimization/{optimizationId}/best-trial");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OptimizationTrial>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Model evaluation
        public async Task<ModelEvaluationResponse?> StartModelEvaluationAsync(ModelEvaluationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tuning/evaluation", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ModelEvaluationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ModelEvaluationResponse?> GetEvaluationStatusAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/evaluation/{evaluationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ModelEvaluationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> CancelEvaluationAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/evaluation/{evaluationId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<PredictionResult>?> GetEvaluationPredictionsAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/evaluation/{evaluationId}/predictions");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<PredictionResult>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ConfusionMatrix?> GetEvaluationConfusionMatrixAsync(string evaluationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/evaluation/{evaluationId}/confusion-matrix");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConfusionMatrix>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Model deployment
        public async Task<ModelDeploymentResponse?> DeployModelAsync(ModelDeploymentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/tuning/deployment", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ModelDeploymentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ModelDeploymentResponse?> GetDeploymentStatusAsync(string deploymentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/deployment/{deploymentId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ModelDeploymentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<ModelDeploymentResponse>?> ListDeploymentsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/tuning/deployments");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ModelDeploymentResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> UpdateDeploymentAsync(string deploymentId, ModelDeploymentRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/tuning/deployment/{deploymentId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> StopDeploymentAsync(string deploymentId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/tuning/deployment/{deploymentId}/stop", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> DeleteDeploymentAsync(string deploymentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/tuning/deployment/{deploymentId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Model versioning
        public async Task<List<McpTuningResponse>?> GetModelVersionsAsync(string modelName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/models/{modelName}/versions");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<McpTuningResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<McpTuningResponse?> GetModelVersionAsync(string modelName, string version)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/models/{modelName}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<McpTuningResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> PromoteModelVersionAsync(string modelName, string version, string environment)
        {
            try
            {
                var request = new { environment };
                var response = await _httpClient.PostAsJsonAsync($"/v3/tuning/models/{modelName}/versions/{version}/promote", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> DeleteModelVersionAsync(string modelName, string version)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/tuning/models/{modelName}/versions/{version}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Performance monitoring
        public async Task<Dictionary<string, object>?> GetTuningMetricsAsync(string tuningId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/{tuningId}/metrics");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, object>?> GetDeploymentMetricsAsync(string deploymentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/deployment/{deploymentId}/metrics");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<Dictionary<string, object>>?> GetModelPerformanceHistoryAsync(string modelName, int days = 30)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/tuning/models/{modelName}/performance-history?days={days}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Configuration management
        public async Task<Dictionary<string, object>?> GetTuningConfigurationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/tuning/config");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse4?> UpdateTuningConfigurationAsync(Dictionary<string, object> config)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/v3/tuning/config", config);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse4>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetSupportedOptimizersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/tuning/optimizers");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<string>?> GetSupportedMetricsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/tuning/metrics");
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
