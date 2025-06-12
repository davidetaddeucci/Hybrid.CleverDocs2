using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Prompt;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/prompts")]
    public class PromptController : ControllerBase
    {
        private readonly IPromptClient _client;
        public PromptController(IPromptClient client) => _client = client;

        // Prompt execution
        [HttpPost("execute")]
        public async Task<IActionResult> ExecutePrompt(PromptRequest request) => Ok(await _client.ExecutePromptAsync(request));

        [HttpPost("execute-template")]
        public async Task<IActionResult> ExecutePromptFromTemplate([FromBody] ExecuteTemplateRequest request) => Ok(await _client.ExecutePromptFromTemplateAsync(request.TemplateId, request.Variables));

        [HttpPost("execute-with-config")]
        public async Task<IActionResult> ExecutePromptWithConfig([FromBody] ExecuteWithConfigRequest request) => Ok(await _client.ExecutePromptWithConfigAsync(request.PromptRequest, request.ModelConfig));

        // Prompt template management
        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate(PromptTemplateRequest request) => Ok(await _client.CreateTemplateAsync(request));

        [HttpGet("templates/{templateId}")]
        public async Task<IActionResult> GetTemplate(string templateId) => Ok(await _client.GetTemplateAsync(templateId));

        [HttpGet("templates")]
        public async Task<IActionResult> ListTemplates([FromQuery] PromptListRequest request) => Ok(await _client.ListTemplatesAsync(request));

        [HttpPut("templates/{templateId}")]
        public async Task<IActionResult> UpdateTemplate(string templateId, PromptTemplateRequest request) => Ok(await _client.UpdateTemplateAsync(templateId, request));

        [HttpDelete("templates/{templateId}")]
        public async Task<IActionResult> DeleteTemplate(string templateId) => Ok(await _client.DeleteTemplateAsync(templateId));

        // Template versioning
        [HttpPost("templates/versions")]
        public async Task<IActionResult> CreateTemplateVersion(PromptVersionRequest request) => Ok(await _client.CreateTemplateVersionAsync(request));

        [HttpGet("templates/{templateId}/versions")]
        public async Task<IActionResult> GetTemplateVersions(string templateId) => Ok(await _client.GetTemplateVersionsAsync(templateId));

        [HttpGet("templates/{templateId}/versions/{version}")]
        public async Task<IActionResult> GetTemplateVersion(string templateId, string version) => Ok(await _client.GetTemplateVersionAsync(templateId, version));

        [HttpPost("templates/{templateId}/versions/{version}/activate")]
        public async Task<IActionResult> ActivateTemplateVersion(string templateId, string version) => Ok(await _client.ActivateTemplateVersionAsync(templateId, version));

        [HttpDelete("templates/{templateId}/versions/{version}")]
        public async Task<IActionResult> DeleteTemplateVersion(string templateId, string version) => Ok(await _client.DeleteTemplateVersionAsync(templateId, version));

        // Prompt optimization
        [HttpPost("optimization")]
        public async Task<IActionResult> StartPromptOptimization(PromptOptimizationRequest request) => Ok(await _client.StartPromptOptimizationAsync(request));

        [HttpGet("optimization/{optimizationId}")]
        public async Task<IActionResult> GetOptimizationStatus(string optimizationId) => Ok(await _client.GetOptimizationStatusAsync(optimizationId));

        [HttpPost("optimization/{optimizationId}/cancel")]
        public async Task<IActionResult> CancelOptimization(string optimizationId) => Ok(await _client.CancelOptimizationAsync(optimizationId));

        [HttpGet("optimization/{optimizationId}/iterations")]
        public async Task<IActionResult> GetOptimizationIterations(string optimizationId) => Ok(await _client.GetOptimizationIterationsAsync(optimizationId));

        [HttpGet("optimization/{optimizationId}/results")]
        public async Task<IActionResult> GetOptimizationResults(string optimizationId) => Ok(await _client.GetOptimizationResultsAsync(optimizationId));

        // Prompt evaluation
        [HttpPost("evaluation")]
        public async Task<IActionResult> StartPromptEvaluation(PromptEvaluationRequest request) => Ok(await _client.StartPromptEvaluationAsync(request));

        [HttpGet("evaluation/{evaluationId}")]
        public async Task<IActionResult> GetEvaluationStatus(string evaluationId) => Ok(await _client.GetEvaluationStatusAsync(evaluationId));

        [HttpPost("evaluation/{evaluationId}/cancel")]
        public async Task<IActionResult> CancelEvaluation(string evaluationId) => Ok(await _client.CancelEvaluationAsync(evaluationId));

        [HttpGet("evaluation/{evaluationId}/results")]
        public async Task<IActionResult> GetEvaluationResults(string evaluationId) => Ok(await _client.GetEvaluationResultsAsync(evaluationId));

        [HttpGet("evaluation/{evaluationId}/metrics")]
        public async Task<IActionResult> GetEvaluationMetrics(string evaluationId) => Ok(await _client.GetEvaluationMetricsAsync(evaluationId));

        // Template categories and tags
        [HttpGet("templates/categories")]
        public async Task<IActionResult> GetTemplateCategories() => Ok(await _client.GetTemplateCategoriesAsync());

        [HttpGet("templates/tags")]
        public async Task<IActionResult> GetTemplateTags() => Ok(await _client.GetTemplateTagsAsync());

        [HttpGet("templates/category/{category}")]
        public async Task<IActionResult> GetTemplatesByCategory(string category) => Ok(await _client.GetTemplatesByCategoryAsync(category));

        [HttpGet("templates/tag/{tag}")]
        public async Task<IActionResult> GetTemplatesByTag(string tag) => Ok(await _client.GetTemplatesByTagAsync(tag));

        // Template search and discovery
        [HttpGet("templates/search")]
        public async Task<IActionResult> SearchTemplates([FromQuery] string query, [FromQuery] int limit = 20) => Ok(await _client.SearchTemplatesAsync(query, limit));

        [HttpGet("templates/recommended")]
        public async Task<IActionResult> GetRecommendedTemplates([FromQuery] string context) => Ok(await _client.GetRecommendedTemplatesAsync(context));

        [HttpGet("templates/popular")]
        public async Task<IActionResult> GetPopularTemplates([FromQuery] int limit = 10) => Ok(await _client.GetPopularTemplatesAsync(limit));

        [HttpGet("templates/recent")]
        public async Task<IActionResult> GetRecentTemplates([FromQuery] int limit = 10) => Ok(await _client.GetRecentTemplatesAsync(limit));

        // Template sharing and collaboration
        [HttpPost("templates/{templateId}/share")]
        public async Task<IActionResult> ShareTemplate(string templateId, [FromBody] ShareTemplateRequest request) => Ok(await _client.ShareTemplateAsync(templateId, request.UserIds));

        [HttpPost("templates/{templateId}/unshare")]
        public async Task<IActionResult> UnshareTemplate(string templateId, [FromBody] ShareTemplateRequest request) => Ok(await _client.UnshareTemplateAsync(templateId, request.UserIds));

        [HttpGet("templates/{templateId}/collaborators")]
        public async Task<IActionResult> GetTemplateCollaborators(string templateId) => Ok(await _client.GetTemplateCollaboratorsAsync(templateId));

        [HttpPost("templates/{templateId}/permissions")]
        public async Task<IActionResult> SetTemplatePermissions(string templateId, [FromBody] SetPermissionsRequest request) => Ok(await _client.SetTemplatePermissionsAsync(templateId, request.Permissions));

        // Template validation
        [HttpPost("templates/validate")]
        public async Task<IActionResult> ValidateTemplate(PromptTemplateRequest request) => Ok(await _client.ValidateTemplateAsync(request));

        [HttpPost("templates/{templateId}/validate-variables")]
        public async Task<IActionResult> ValidateTemplateVariables(string templateId, [FromBody] ValidateVariablesRequest request) => Ok(await _client.ValidateTemplateVariablesAsync(templateId, request.Variables));

        [HttpGet("templates/{templateId}/validation-errors")]
        public async Task<IActionResult> GetTemplateValidationErrors(string templateId) => Ok(await _client.GetTemplateValidationErrorsAsync(templateId));

        // Prompt analytics and monitoring
        [HttpGet("stats")]
        public async Task<IActionResult> GetPromptStats() => Ok(await _client.GetPromptStatsAsync());

        [HttpGet("templates/{templateId}/stats")]
        public async Task<IActionResult> GetTemplateStats(string templateId) => Ok(await _client.GetTemplateStatsAsync(templateId));

        [HttpGet("performance-trends")]
        public async Task<IActionResult> GetPromptPerformanceTrends([FromQuery] int days = 30) => Ok(await _client.GetPromptPerformanceTrendsAsync(days));

        [HttpGet("templates/{templateId}/analytics")]
        public async Task<IActionResult> GetPromptUsageAnalytics(string templateId, [FromQuery] int days = 30) => Ok(await _client.GetPromptUsageAnalyticsAsync(templateId, days));

        // Model configuration management
        [HttpGet("models")]
        public async Task<IActionResult> GetAvailableModels() => Ok(await _client.GetAvailableModelsAsync());

        [HttpGet("models/{modelName}")]
        public async Task<IActionResult> GetModelConfig(string modelName) => Ok(await _client.GetModelConfigAsync(modelName));

        [HttpPost("model-configs")]
        public async Task<IActionResult> SaveModelConfig([FromBody] SaveModelConfigRequest request) => Ok(await _client.SaveModelConfigAsync(request.ConfigName, request.Config));

        [HttpGet("model-configs")]
        public async Task<IActionResult> GetSavedModelConfigs() => Ok(await _client.GetSavedModelConfigsAsync());

        // Prompt history and logging
        [HttpGet("history")]
        public async Task<IActionResult> GetPromptHistory([FromQuery] int limit = 50, [FromQuery] int offset = 0) => Ok(await _client.GetPromptHistoryAsync(limit, offset));

        [HttpGet("templates/{templateId}/history")]
        public async Task<IActionResult> GetTemplateExecutionHistory(string templateId, [FromQuery] int limit = 50) => Ok(await _client.GetTemplateExecutionHistoryAsync(templateId, limit));

        [HttpGet("executions/{executionId}")]
        public async Task<IActionResult> GetPromptExecution(string executionId) => Ok(await _client.GetPromptExecutionAsync(executionId));

        [HttpDelete("executions/{executionId}")]
        public async Task<IActionResult> DeletePromptExecution(string executionId) => Ok(await _client.DeletePromptExecutionAsync(executionId));

        // Batch operations
        [HttpPost("batch-execute")]
        public async Task<IActionResult> ExecuteBatchPrompts([FromBody] BatchExecuteRequest request) => Ok(await _client.ExecuteBatchPromptsAsync(request.Requests));

        [HttpPost("templates/batch-delete")]
        public async Task<IActionResult> DeleteMultipleTemplates([FromBody] BatchDeleteRequest request) => Ok(await _client.DeleteMultipleTemplatesAsync(request.TemplateIds));

        [HttpPost("templates/import")]
        public async Task<IActionResult> ImportTemplates([FromBody] ImportTemplatesRequest request) => Ok(await _client.ImportTemplatesAsync(request.Templates));

        [HttpPost("templates/export")]
        public async Task<IActionResult> ExportTemplates([FromBody] ExportTemplatesRequest request) => Ok(await _client.ExportTemplatesAsync(request.TemplateIds));
    }

    // Helper DTOs for controller endpoints
    public class ExecuteTemplateRequest
    {
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public class ExecuteWithConfigRequest
    {
        public PromptRequest PromptRequest { get; set; } = new();
        public ModelConfig ModelConfig { get; set; } = new();
    }

    public class ShareTemplateRequest
    {
        public List<string> UserIds { get; set; } = new();
    }

    public class SetPermissionsRequest
    {
        public Dictionary<string, string> Permissions { get; set; } = new();
    }

    public class ValidateVariablesRequest
    {
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public class SaveModelConfigRequest
    {
        public string ConfigName { get; set; } = string.Empty;
        public ModelConfig Config { get; set; } = new();
    }

    public class BatchExecuteRequest
    {
        public List<PromptRequest> Requests { get; set; } = new();
    }

    public class BatchDeleteRequest
    {
        public List<string> TemplateIds { get; set; } = new();
    }

    public class ImportTemplatesRequest
    {
        public List<PromptTemplateRequest> Templates { get; set; } = new();
    }

    public class ExportTemplatesRequest
    {
        public List<string> TemplateIds { get; set; } = new();
    }
}
