using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.McpTuning;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IMcpTuningClient
    {
        // Model tuning operations
        Task<McpTuningResponse?> StartTuningAsync(McpTuningRequest request);
        Task<McpTuningResponse?> GetTuningStatusAsync(string tuningId);
        Task<MessageResponse4?> CancelTuningAsync(string tuningId);
        Task<MessageResponse4?> PauseTuningAsync(string tuningId);
        Task<MessageResponse4?> ResumeTuningAsync(string tuningId);

        // Tuning job management
        Task<TuningJobResponse?> CreateTuningJobAsync(TuningJobRequest request);
        Task<TuningJobResponse?> GetTuningJobAsync(string jobId);
        Task<TuningJobListResponse?> ListTuningJobsAsync(TuningJobListRequest request);
        Task<MessageResponse4?> CancelTuningJobAsync(string jobId);
        Task<MessageResponse4?> DeleteTuningJobAsync(string jobId);

        // Hyperparameter optimization
        Task<HyperparameterOptimizationResponse?> StartHyperparameterOptimizationAsync(HyperparameterOptimizationRequest request);
        Task<HyperparameterOptimizationResponse?> GetOptimizationStatusAsync(string optimizationId);
        Task<MessageResponse4?> CancelOptimizationAsync(string optimizationId);
        Task<List<OptimizationTrial>?> GetOptimizationTrialsAsync(string optimizationId);
        Task<OptimizationTrial?> GetBestTrialAsync(string optimizationId);

        // Model evaluation
        Task<ModelEvaluationResponse?> StartModelEvaluationAsync(ModelEvaluationRequest request);
        Task<ModelEvaluationResponse?> GetEvaluationStatusAsync(string evaluationId);
        Task<MessageResponse4?> CancelEvaluationAsync(string evaluationId);
        Task<List<PredictionResult>?> GetEvaluationPredictionsAsync(string evaluationId);
        Task<ConfusionMatrix?> GetEvaluationConfusionMatrixAsync(string evaluationId);

        // Model deployment
        Task<ModelDeploymentResponse?> DeployModelAsync(ModelDeploymentRequest request);
        Task<ModelDeploymentResponse?> GetDeploymentStatusAsync(string deploymentId);
        Task<List<ModelDeploymentResponse>?> ListDeploymentsAsync();
        Task<MessageResponse4?> UpdateDeploymentAsync(string deploymentId, ModelDeploymentRequest request);
        Task<MessageResponse4?> StopDeploymentAsync(string deploymentId);
        Task<MessageResponse4?> DeleteDeploymentAsync(string deploymentId);

        // Model versioning
        Task<List<McpTuningResponse>?> GetModelVersionsAsync(string modelName);
        Task<McpTuningResponse?> GetModelVersionAsync(string modelName, string version);
        Task<MessageResponse4?> PromoteModelVersionAsync(string modelName, string version, string environment);
        Task<MessageResponse4?> DeleteModelVersionAsync(string modelName, string version);

        // Performance monitoring
        Task<Dictionary<string, object>?> GetTuningMetricsAsync(string tuningId);
        Task<Dictionary<string, object>?> GetDeploymentMetricsAsync(string deploymentId);
        Task<List<Dictionary<string, object>>?> GetModelPerformanceHistoryAsync(string modelName, int days = 30);

        // Configuration management
        Task<Dictionary<string, object>?> GetTuningConfigurationAsync();
        Task<MessageResponse4?> UpdateTuningConfigurationAsync(Dictionary<string, object> config);
        Task<List<string>?> GetSupportedOptimizersAsync();
        Task<List<string>?> GetSupportedMetricsAsync();
    }
}
