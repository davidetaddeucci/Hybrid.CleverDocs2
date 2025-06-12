using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IValidationClient
    {
        // Core validation operations
        Task<ValidationResponse?> ValidateDataAsync(ValidationRequest request);
        Task<ValidationResponse?> ValidateSchemaAsync(ValidationRequest request);
        Task<ValidationResponse?> GetValidationResultAsync(string validationId);
        Task<MessageResponse8?> CancelValidationAsync(string validationId);

        // Content validation
        Task<ContentValidationResponse?> ValidateContentAsync(ContentValidationRequest request);
        Task<ContentValidationResponse?> ValidateContentQualityAsync(ContentValidationRequest request);
        Task<ContentValidationResponse?> CheckGrammarAndSpellingAsync(ContentValidationRequest request);
        Task<ContentValidationResponse?> AnalyzeReadabilityAsync(ContentValidationRequest request);

        // Compliance validation
        Task<ComplianceValidationResponse?> ValidateComplianceAsync(ComplianceValidationRequest request);
        Task<ComplianceValidationResponse?> CheckGDPRComplianceAsync(ComplianceValidationRequest request);
        Task<ComplianceValidationResponse?> CheckHIPAAComplianceAsync(ComplianceValidationRequest request);
        Task<ComplianceValidationResponse?> ValidateDataRetentionAsync(ComplianceValidationRequest request);

        // Business rule validation
        Task<BusinessRuleValidationResponse?> ValidateBusinessRulesAsync(BusinessRuleValidationRequest request);
        Task<BusinessRuleValidationResponse?> ExecuteBusinessRulesAsync(BusinessRuleValidationRequest request);
        Task<BusinessRuleValidationResponse?> ValidateWorkflowRulesAsync(BusinessRuleValidationRequest request);
        Task<BusinessRuleValidationResponse?> CheckApprovalRulesAsync(BusinessRuleValidationRequest request);

        // Schema management
        Task<ValidationSchema?> CreateSchemaAsync(ValidationSchema schema);
        Task<ValidationSchema?> GetSchemaAsync(string schemaId);
        Task<List<ValidationSchema>?> ListSchemasAsync(int limit = 50, int offset = 0);
        Task<ValidationSchema?> UpdateSchemaAsync(string schemaId, ValidationSchema schema);
        Task<MessageResponse8?> DeleteSchemaAsync(string schemaId);

        // Rule management
        Task<ValidationRule?> CreateRuleAsync(ValidationRule rule);
        Task<ValidationRule?> GetRuleAsync(string ruleId);
        Task<List<ValidationRule>?> ListRulesAsync(string? ruleType = null, int limit = 50, int offset = 0);
        Task<ValidationRule?> UpdateRuleAsync(string ruleId, ValidationRule rule);
        Task<MessageResponse8?> DeleteRuleAsync(string ruleId);
        Task<MessageResponse8?> ActivateRuleAsync(string ruleId);
        Task<MessageResponse8?> DeactivateRuleAsync(string ruleId);

        // Batch validation
        Task<BatchValidationResponse?> ValidateBatchAsync(BatchValidationRequest request);
        Task<BatchValidationResponse?> GetBatchResultAsync(string batchId);
        Task<MessageResponse8?> CancelBatchValidationAsync(string batchId);

        // Validation history and analytics
        Task<List<ValidationResponse>?> GetValidationHistoryAsync(int limit = 50, int offset = 0);
        Task<List<ValidationResponse>?> GetValidationsByTypeAsync(string validationType, int limit = 50, int offset = 0);
        Task<Dictionary<string, object>?> GetValidationStatsAsync(int days = 30);
        Task<Dictionary<string, object>?> GetValidationAnalyticsAsync(string validationType, int days = 30);

        // Error and warning management
        Task<List<ValidationError>?> GetValidationErrorsAsync(string validationId);
        Task<List<ValidationWarning>?> GetValidationWarningsAsync(string validationId);
        Task<MessageResponse8?> AcknowledgeWarningAsync(string warningId);
        Task<MessageResponse8?> ResolveErrorAsync(string errorId, string resolution);

        // Quality metrics and scoring
        Task<Dictionary<string, double>?> GetQualityMetricsAsync(string validationId);
        Task<double?> CalculateValidationScoreAsync(ValidationRequest request);
        Task<Dictionary<string, object>?> GetQualityTrendsAsync(int days = 30);
        Task<List<ContentSuggestion>?> GetImprovementSuggestionsAsync(string validationId);

        // Configuration and settings
        Task<Dictionary<string, object>?> GetValidationConfigAsync();
        Task<MessageResponse8?> UpdateValidationConfigAsync(Dictionary<string, object> config);
        Task<Dictionary<string, object>?> GetDefaultValidationRulesAsync();
        Task<MessageResponse8?> ResetValidationConfigAsync();

        // Template and preset management
        Task<ValidationRequest?> CreateValidationTemplateAsync(string templateName, ValidationRequest template);
        Task<ValidationRequest?> GetValidationTemplateAsync(string templateName);
        Task<List<string>?> ListValidationTemplatesAsync();
        Task<MessageResponse8?> DeleteValidationTemplateAsync(string templateName);

        // Integration and webhook support
        Task<MessageResponse8?> RegisterValidationWebhookAsync(string webhookUrl, List<string> eventTypes);
        Task<MessageResponse8?> UnregisterValidationWebhookAsync(string webhookId);
        Task<List<Dictionary<string, object>>?> GetValidationWebhooksAsync();
        Task<MessageResponse8?> TestValidationWebhookAsync(string webhookId);

        // Performance and monitoring
        Task<Dictionary<string, object>?> GetValidationPerformanceAsync(int days = 7);
        Task<List<Dictionary<string, object>>?> GetSlowValidationsAsync(int limit = 10);
        Task<Dictionary<string, object>?> GetValidationHealthAsync();
        Task<MessageResponse8?> OptimizeValidationPerformanceAsync();

        // Export and reporting
        Task<string?> ExportValidationResultsAsync(List<string> validationIds, string format = "json");
        Task<string?> GenerateValidationReportAsync(string reportType, Dictionary<string, object> parameters);
        Task<List<Dictionary<string, object>>?> GetValidationSummaryAsync(DateTime startDate, DateTime endDate);
        Task<string?> ExportValidationConfigAsync();

        // Advanced validation features
        Task<ValidationResponse?> ValidateWithCustomRulesAsync(ValidationRequest request, List<ValidationRule> customRules);
        Task<ValidationResponse?> ValidateIncrementalAsync(string baseValidationId, object deltaData);
        Task<ValidationResponse?> ValidateWithContextAsync(ValidationRequest request, Dictionary<string, object> context);
        Task<List<ValidationResponse>?> CompareValidationResultsAsync(List<string> validationIds);
    }
}
