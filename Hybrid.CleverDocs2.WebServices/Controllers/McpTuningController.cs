using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.McpTuning;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/mcp-tuning")]
    public class McpTuningController : ControllerBase
    {
        private readonly IMcpTuningClient _client;
        public McpTuningController(IMcpTuningClient client) => _client = client;

        // Model tuning operations
        [HttpPost("tuning")]
        public async Task<IActionResult> StartTuning(McpTuningRequest request) => Ok(await _client.StartTuningAsync(request));

        [HttpGet("tuning/{tuningId}")]
        public async Task<IActionResult> GetTuningStatus(string tuningId) => Ok(await _client.GetTuningStatusAsync(tuningId));

        [HttpPost("tuning/{tuningId}/cancel")]
        public async Task<IActionResult> CancelTuning(string tuningId) => Ok(await _client.CancelTuningAsync(tuningId));

        [HttpPost("tuning/{tuningId}/pause")]
        public async Task<IActionResult> PauseTuning(string tuningId) => Ok(await _client.PauseTuningAsync(tuningId));

        [HttpPost("tuning/{tuningId}/resume")]
        public async Task<IActionResult> ResumeTuning(string tuningId) => Ok(await _client.ResumeTuningAsync(tuningId));

        // Tuning job management
        [HttpPost("jobs")]
        public async Task<IActionResult> CreateTuningJob(TuningJobRequest request) => Ok(await _client.CreateTuningJobAsync(request));

        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetTuningJob(string jobId) => Ok(await _client.GetTuningJobAsync(jobId));

        [HttpGet("jobs")]
        public async Task<IActionResult> ListTuningJobs([FromQuery] TuningJobListRequest request) => Ok(await _client.ListTuningJobsAsync(request));

        [HttpPost("jobs/{jobId}/cancel")]
        public async Task<IActionResult> CancelTuningJob(string jobId) => Ok(await _client.CancelTuningJobAsync(jobId));

        [HttpDelete("jobs/{jobId}")]
        public async Task<IActionResult> DeleteTuningJob(string jobId) => Ok(await _client.DeleteTuningJobAsync(jobId));

        // Hyperparameter optimization
        [HttpPost("hyperparameter-optimization")]
        public async Task<IActionResult> StartHyperparameterOptimization(HyperparameterOptimizationRequest request) => Ok(await _client.StartHyperparameterOptimizationAsync(request));

        [HttpGet("hyperparameter-optimization/{optimizationId}")]
        public async Task<IActionResult> GetOptimizationStatus(string optimizationId) => Ok(await _client.GetOptimizationStatusAsync(optimizationId));

        [HttpPost("hyperparameter-optimization/{optimizationId}/cancel")]
        public async Task<IActionResult> CancelOptimization(string optimizationId) => Ok(await _client.CancelOptimizationAsync(optimizationId));

        [HttpGet("hyperparameter-optimization/{optimizationId}/trials")]
        public async Task<IActionResult> GetOptimizationTrials(string optimizationId) => Ok(await _client.GetOptimizationTrialsAsync(optimizationId));

        [HttpGet("hyperparameter-optimization/{optimizationId}/best-trial")]
        public async Task<IActionResult> GetBestTrial(string optimizationId) => Ok(await _client.GetBestTrialAsync(optimizationId));

        // Model evaluation
        [HttpPost("evaluation")]
        public async Task<IActionResult> StartModelEvaluation(ModelEvaluationRequest request) => Ok(await _client.StartModelEvaluationAsync(request));

        [HttpGet("evaluation/{evaluationId}")]
        public async Task<IActionResult> GetEvaluationStatus(string evaluationId) => Ok(await _client.GetEvaluationStatusAsync(evaluationId));

        [HttpPost("evaluation/{evaluationId}/cancel")]
        public async Task<IActionResult> CancelEvaluation(string evaluationId) => Ok(await _client.CancelEvaluationAsync(evaluationId));

        [HttpGet("evaluation/{evaluationId}/predictions")]
        public async Task<IActionResult> GetEvaluationPredictions(string evaluationId) => Ok(await _client.GetEvaluationPredictionsAsync(evaluationId));

        [HttpGet("evaluation/{evaluationId}/confusion-matrix")]
        public async Task<IActionResult> GetEvaluationConfusionMatrix(string evaluationId) => Ok(await _client.GetEvaluationConfusionMatrixAsync(evaluationId));

        // Model deployment
        [HttpPost("deployment")]
        public async Task<IActionResult> DeployModel(ModelDeploymentRequest request) => Ok(await _client.DeployModelAsync(request));

        [HttpGet("deployment/{deploymentId}")]
        public async Task<IActionResult> GetDeploymentStatus(string deploymentId) => Ok(await _client.GetDeploymentStatusAsync(deploymentId));

        [HttpGet("deployments")]
        public async Task<IActionResult> ListDeployments() => Ok(await _client.ListDeploymentsAsync());

        [HttpPut("deployment/{deploymentId}")]
        public async Task<IActionResult> UpdateDeployment(string deploymentId, ModelDeploymentRequest request) => Ok(await _client.UpdateDeploymentAsync(deploymentId, request));

        [HttpPost("deployment/{deploymentId}/stop")]
        public async Task<IActionResult> StopDeployment(string deploymentId) => Ok(await _client.StopDeploymentAsync(deploymentId));

        [HttpDelete("deployment/{deploymentId}")]
        public async Task<IActionResult> DeleteDeployment(string deploymentId) => Ok(await _client.DeleteDeploymentAsync(deploymentId));

        // Model versioning
        [HttpGet("models/{modelName}/versions")]
        public async Task<IActionResult> GetModelVersions(string modelName) => Ok(await _client.GetModelVersionsAsync(modelName));

        [HttpGet("models/{modelName}/versions/{version}")]
        public async Task<IActionResult> GetModelVersion(string modelName, string version) => Ok(await _client.GetModelVersionAsync(modelName, version));

        [HttpPost("models/{modelName}/versions/{version}/promote")]
        public async Task<IActionResult> PromoteModelVersion(string modelName, string version, [FromBody] PromoteModelRequest request) => Ok(await _client.PromoteModelVersionAsync(modelName, version, request.Environment));

        [HttpDelete("models/{modelName}/versions/{version}")]
        public async Task<IActionResult> DeleteModelVersion(string modelName, string version) => Ok(await _client.DeleteModelVersionAsync(modelName, version));

        // Performance monitoring
        [HttpGet("tuning/{tuningId}/metrics")]
        public async Task<IActionResult> GetTuningMetrics(string tuningId) => Ok(await _client.GetTuningMetricsAsync(tuningId));

        [HttpGet("deployment/{deploymentId}/metrics")]
        public async Task<IActionResult> GetDeploymentMetrics(string deploymentId) => Ok(await _client.GetDeploymentMetricsAsync(deploymentId));

        [HttpGet("models/{modelName}/performance-history")]
        public async Task<IActionResult> GetModelPerformanceHistory(string modelName, [FromQuery] int days = 30) => Ok(await _client.GetModelPerformanceHistoryAsync(modelName, days));

        // Configuration management
        [HttpGet("config")]
        public async Task<IActionResult> GetTuningConfiguration() => Ok(await _client.GetTuningConfigurationAsync());

        [HttpPut("config")]
        public async Task<IActionResult> UpdateTuningConfiguration([FromBody] Dictionary<string, object> config) => Ok(await _client.UpdateTuningConfigurationAsync(config));

        [HttpGet("optimizers")]
        public async Task<IActionResult> GetSupportedOptimizers() => Ok(await _client.GetSupportedOptimizersAsync());

        [HttpGet("metrics")]
        public async Task<IActionResult> GetSupportedMetrics() => Ok(await _client.GetSupportedMetricsAsync());
    }

    // Helper DTOs for controller endpoints
    public class PromoteModelRequest
    {
        public string Environment { get; set; } = string.Empty;
    }
}
