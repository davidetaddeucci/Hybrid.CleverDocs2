using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Prompt;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IPromptClient
    {
        // Prompt execution
        Task<PromptResponse?> ExecutePromptAsync(PromptRequest request);
        Task<PromptResponse?> ExecutePromptFromTemplateAsync(string templateId, Dictionary<string, object> variables);
        Task<PromptResponse?> ExecutePromptWithConfigAsync(PromptRequest request, ModelConfig modelConfig);

        // Prompt template management
        Task<PromptTemplateResponse?> CreateTemplateAsync(PromptTemplateRequest request);
        Task<PromptTemplateResponse?> GetTemplateAsync(string templateId);
        Task<PromptListResponse?> ListTemplatesAsync(PromptListRequest request);
        Task<PromptTemplateResponse?> UpdateTemplateAsync(string templateId, PromptTemplateRequest request);
        Task<MessageResponse6?> DeleteTemplateAsync(string templateId);

        // Template versioning
        Task<PromptVersionResponse?> CreateTemplateVersionAsync(PromptVersionRequest request);
        Task<List<PromptVersionResponse>?> GetTemplateVersionsAsync(string templateId);
        Task<PromptVersionResponse?> GetTemplateVersionAsync(string templateId, string version);
        Task<MessageResponse6?> ActivateTemplateVersionAsync(string templateId, string version);
        Task<MessageResponse6?> DeleteTemplateVersionAsync(string templateId, string version);

        // Prompt optimization
        Task<PromptOptimizationResponse?> StartPromptOptimizationAsync(PromptOptimizationRequest request);
        Task<PromptOptimizationResponse?> GetOptimizationStatusAsync(string optimizationId);
        Task<MessageResponse6?> CancelOptimizationAsync(string optimizationId);
        Task<List<OptimizationIteration>?> GetOptimizationIterationsAsync(string optimizationId);
        Task<OptimizationResults?> GetOptimizationResultsAsync(string optimizationId);

        // Prompt evaluation
        Task<PromptEvaluationResponse?> StartPromptEvaluationAsync(PromptEvaluationRequest request);
        Task<PromptEvaluationResponse?> GetEvaluationStatusAsync(string evaluationId);
        Task<MessageResponse6?> CancelEvaluationAsync(string evaluationId);
        Task<List<TestResult>?> GetEvaluationResultsAsync(string evaluationId);
        Task<EvaluationMetrics?> GetEvaluationMetricsAsync(string evaluationId);

        // Template categories and tags
        Task<List<string>?> GetTemplateCategoriesAsync();
        Task<List<string>?> GetTemplateTagsAsync();
        Task<List<PromptTemplateResponse>?> GetTemplatesByCategoryAsync(string category);
        Task<List<PromptTemplateResponse>?> GetTemplatesByTagAsync(string tag);

        // Template search and discovery
        Task<PromptListResponse?> SearchTemplatesAsync(string query, int limit = 20);
        Task<List<PromptTemplateResponse>?> GetRecommendedTemplatesAsync(string context);
        Task<List<PromptTemplateResponse>?> GetPopularTemplatesAsync(int limit = 10);
        Task<List<PromptTemplateResponse>?> GetRecentTemplatesAsync(int limit = 10);

        // Template sharing and collaboration
        Task<MessageResponse6?> ShareTemplateAsync(string templateId, List<string> userIds);
        Task<MessageResponse6?> UnshareTemplateAsync(string templateId, List<string> userIds);
        Task<List<string>?> GetTemplateCollaboratorsAsync(string templateId);
        Task<MessageResponse6?> SetTemplatePermissionsAsync(string templateId, Dictionary<string, string> permissions);

        // Template validation
        Task<Dictionary<string, object>?> ValidateTemplateAsync(PromptTemplateRequest request);
        Task<Dictionary<string, object>?> ValidateTemplateVariablesAsync(string templateId, Dictionary<string, object> variables);
        Task<List<string>?> GetTemplateValidationErrorsAsync(string templateId);

        // Prompt analytics and monitoring
        Task<PromptStatsResponse?> GetPromptStatsAsync();
        Task<PromptStatsResponse?> GetTemplateStatsAsync(string templateId);
        Task<List<PerformanceTrend>?> GetPromptPerformanceTrendsAsync(int days = 30);
        Task<Dictionary<string, object>?> GetPromptUsageAnalyticsAsync(string templateId, int days = 30);

        // Model configuration management
        Task<List<ModelConfig>?> GetAvailableModelsAsync();
        Task<ModelConfig?> GetModelConfigAsync(string modelName);
        Task<MessageResponse6?> SaveModelConfigAsync(string configName, ModelConfig config);
        Task<List<ModelConfig>?> GetSavedModelConfigsAsync();

        // Prompt history and logging
        Task<List<PromptResponse>?> GetPromptHistoryAsync(int limit = 50, int offset = 0);
        Task<List<PromptResponse>?> GetTemplateExecutionHistoryAsync(string templateId, int limit = 50);
        Task<PromptResponse?> GetPromptExecutionAsync(string executionId);
        Task<MessageResponse6?> DeletePromptExecutionAsync(string executionId);

        // Batch operations
        Task<List<PromptResponse>?> ExecuteBatchPromptsAsync(List<PromptRequest> requests);
        Task<MessageResponse6?> DeleteMultipleTemplatesAsync(List<string> templateIds);
        Task<List<PromptTemplateResponse>?> ImportTemplatesAsync(List<PromptTemplateRequest> templates);
        Task<List<PromptTemplateResponse>?> ExportTemplatesAsync(List<string> templateIds);
    }
}
