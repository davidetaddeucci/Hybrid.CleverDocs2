using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Validation;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class ValidationClient : IValidationClient
    {
        private readonly HttpClient _httpClient;

        public ValidationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Core validation operations
        public async Task<ValidationResponse?> ValidateDataAsync(ValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/data", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ValidationResponse?> ValidateSchemaAsync(ValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/schema", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ValidationResponse?> GetValidationResultAsync(string validationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/validation/results/{validationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<MessageResponse8?> CancelValidationAsync(string validationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/validation/{validationId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse8>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Content validation
        public async Task<ContentValidationResponse?> ValidateContentAsync(ContentValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/content", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContentValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ContentValidationResponse?> ValidateContentQualityAsync(ContentValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/content/quality", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContentValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ContentValidationResponse?> CheckGrammarAndSpellingAsync(ContentValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/content/grammar", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContentValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ContentValidationResponse?> AnalyzeReadabilityAsync(ContentValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/content/readability", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContentValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Compliance validation
        public async Task<ComplianceValidationResponse?> ValidateComplianceAsync(ComplianceValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/compliance", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ComplianceValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ComplianceValidationResponse?> CheckGDPRComplianceAsync(ComplianceValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/compliance/gdpr", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ComplianceValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ComplianceValidationResponse?> CheckHIPAAComplianceAsync(ComplianceValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/compliance/hipaa", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ComplianceValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ComplianceValidationResponse?> ValidateDataRetentionAsync(ComplianceValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/compliance/retention", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ComplianceValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Business rule validation
        public async Task<BusinessRuleValidationResponse?> ValidateBusinessRulesAsync(BusinessRuleValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/business-rules", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BusinessRuleValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<BusinessRuleValidationResponse?> ExecuteBusinessRulesAsync(BusinessRuleValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/business-rules/execute", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BusinessRuleValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<BusinessRuleValidationResponse?> ValidateWorkflowRulesAsync(BusinessRuleValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/business-rules/workflow", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BusinessRuleValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<BusinessRuleValidationResponse?> CheckApprovalRulesAsync(BusinessRuleValidationRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/validation/business-rules/approval", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BusinessRuleValidationResponse>();
            }
            catch (HttpRequestException) { return null; }
        }

        // Implementazioni compatte per tutti gli altri metodi usando helper methods
        public async Task<ValidationSchema?> CreateSchemaAsync(ValidationSchema schema) => await PostAsync<ValidationSchema>("/v3/validation/schemas", schema);
        public async Task<ValidationSchema?> GetSchemaAsync(string schemaId) => await GetAsync<ValidationSchema>($"/v3/validation/schemas/{schemaId}");
        public async Task<List<ValidationSchema>?> ListSchemasAsync(int limit = 50, int offset = 0) => await GetListAsync<ValidationSchema>($"/v3/validation/schemas?limit={limit}&offset={offset}");
        public async Task<ValidationSchema?> UpdateSchemaAsync(string schemaId, ValidationSchema schema) => await PutAsync<ValidationSchema>($"/v3/validation/schemas/{schemaId}", schema);
        public async Task<MessageResponse8?> DeleteSchemaAsync(string schemaId) => await DeleteMessageAsync($"/v3/validation/schemas/{schemaId}");

        public async Task<ValidationRule?> CreateRuleAsync(ValidationRule rule) => await PostAsync<ValidationRule>("/v3/validation/rules", rule);
        public async Task<ValidationRule?> GetRuleAsync(string ruleId) => await GetAsync<ValidationRule>($"/v3/validation/rules/{ruleId}");
        public async Task<List<ValidationRule>?> ListRulesAsync(string? ruleType = null, int limit = 50, int offset = 0) => await GetListAsync<ValidationRule>($"/v3/validation/rules?type={ruleType}&limit={limit}&offset={offset}");
        public async Task<ValidationRule?> UpdateRuleAsync(string ruleId, ValidationRule rule) => await PutAsync<ValidationRule>($"/v3/validation/rules/{ruleId}", rule);
        public async Task<MessageResponse8?> DeleteRuleAsync(string ruleId) => await DeleteMessageAsync($"/v3/validation/rules/{ruleId}");
        public async Task<MessageResponse8?> ActivateRuleAsync(string ruleId) => await PostMessageAsync($"/v3/validation/rules/{ruleId}/activate", null);
        public async Task<MessageResponse8?> DeactivateRuleAsync(string ruleId) => await PostMessageAsync($"/v3/validation/rules/{ruleId}/deactivate", null);

        public async Task<BatchValidationResponse?> ValidateBatchAsync(BatchValidationRequest request) => await PostAsync<BatchValidationResponse>("/v3/validation/batch", request);
        public async Task<BatchValidationResponse?> GetBatchResultAsync(string batchId) => await GetAsync<BatchValidationResponse>($"/v3/validation/batch/{batchId}");
        public async Task<MessageResponse8?> CancelBatchValidationAsync(string batchId) => await PostMessageAsync($"/v3/validation/batch/{batchId}/cancel", null);

        public async Task<List<ValidationResponse>?> GetValidationHistoryAsync(int limit = 50, int offset = 0) => await GetListAsync<ValidationResponse>($"/v3/validation/history?limit={limit}&offset={offset}");
        public async Task<List<ValidationResponse>?> GetValidationsByTypeAsync(string validationType, int limit = 50, int offset = 0) => await GetListAsync<ValidationResponse>($"/v3/validation/history/type/{validationType}?limit={limit}&offset={offset}");
        public async Task<Dictionary<string, object>?> GetValidationStatsAsync(int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/validation/stats?days={days}");
        public async Task<Dictionary<string, object>?> GetValidationAnalyticsAsync(string validationType, int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/validation/analytics/{validationType}?days={days}");

        public async Task<List<ValidationError>?> GetValidationErrorsAsync(string validationId) => await GetListAsync<ValidationError>($"/v3/validation/{validationId}/errors");
        public async Task<List<ValidationWarning>?> GetValidationWarningsAsync(string validationId) => await GetListAsync<ValidationWarning>($"/v3/validation/{validationId}/warnings");
        public async Task<MessageResponse8?> AcknowledgeWarningAsync(string warningId) => await PostMessageAsync($"/v3/validation/warnings/{warningId}/acknowledge", null);
        public async Task<MessageResponse8?> ResolveErrorAsync(string errorId, string resolution) => await PostMessageAsync($"/v3/validation/errors/{errorId}/resolve", new { resolution });

        public async Task<Dictionary<string, double>?> GetQualityMetricsAsync(string validationId) => await GetAsync<Dictionary<string, double>>($"/v3/validation/{validationId}/quality-metrics");
        public async Task<double?> CalculateValidationScoreAsync(ValidationRequest request) => (await PostAsync<dynamic>("/v3/validation/calculate-score", request))?.score;
        public async Task<Dictionary<string, object>?> GetQualityTrendsAsync(int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/validation/quality-trends?days={days}");
        public async Task<List<ContentSuggestion>?> GetImprovementSuggestionsAsync(string validationId) => await GetListAsync<ContentSuggestion>($"/v3/validation/{validationId}/suggestions");

        public async Task<Dictionary<string, object>?> GetValidationConfigAsync() => await GetAsync<Dictionary<string, object>>("/v3/validation/config");
        public async Task<MessageResponse8?> UpdateValidationConfigAsync(Dictionary<string, object> config) => await PostMessageAsync("/v3/validation/config", config);
        public async Task<Dictionary<string, object>?> GetDefaultValidationRulesAsync() => await GetAsync<Dictionary<string, object>>("/v3/validation/config/default-rules");
        public async Task<MessageResponse8?> ResetValidationConfigAsync() => await PostMessageAsync("/v3/validation/config/reset", null);

        public async Task<ValidationRequest?> CreateValidationTemplateAsync(string templateName, ValidationRequest template) => await PostAsync<ValidationRequest>($"/v3/validation/templates/{templateName}", template);
        public async Task<ValidationRequest?> GetValidationTemplateAsync(string templateName) => await GetAsync<ValidationRequest>($"/v3/validation/templates/{templateName}");
        public async Task<List<string>?> ListValidationTemplatesAsync() => await GetListAsync<string>("/v3/validation/templates");
        public async Task<MessageResponse8?> DeleteValidationTemplateAsync(string templateName) => await DeleteMessageAsync($"/v3/validation/templates/{templateName}");

        public async Task<MessageResponse8?> RegisterValidationWebhookAsync(string webhookUrl, List<string> eventTypes) => await PostMessageAsync("/v3/validation/webhooks", new { webhook_url = webhookUrl, event_types = eventTypes });
        public async Task<MessageResponse8?> UnregisterValidationWebhookAsync(string webhookId) => await DeleteMessageAsync($"/v3/validation/webhooks/{webhookId}");
        public async Task<List<Dictionary<string, object>>?> GetValidationWebhooksAsync() => await GetListAsync<Dictionary<string, object>>("/v3/validation/webhooks");
        public async Task<MessageResponse8?> TestValidationWebhookAsync(string webhookId) => await PostMessageAsync($"/v3/validation/webhooks/{webhookId}/test", null);

        public async Task<Dictionary<string, object>?> GetValidationPerformanceAsync(int days = 7) => await GetAsync<Dictionary<string, object>>($"/v3/validation/performance?days={days}");
        public async Task<List<Dictionary<string, object>>?> GetSlowValidationsAsync(int limit = 10) => await GetListAsync<Dictionary<string, object>>($"/v3/validation/performance/slow?limit={limit}");
        public async Task<Dictionary<string, object>?> GetValidationHealthAsync() => await GetAsync<Dictionary<string, object>>("/v3/validation/health");
        public async Task<MessageResponse8?> OptimizeValidationPerformanceAsync() => await PostMessageAsync("/v3/validation/performance/optimize", null);

        public async Task<string?> ExportValidationResultsAsync(List<string> validationIds, string format = "json") => (await PostAsync<dynamic>("/v3/validation/export", new { validation_ids = validationIds, format }))?.export_url;
        public async Task<string?> GenerateValidationReportAsync(string reportType, Dictionary<string, object> parameters) => (await PostAsync<dynamic>("/v3/validation/reports", new { report_type = reportType, parameters }))?.report_url;
        public async Task<List<Dictionary<string, object>>?> GetValidationSummaryAsync(DateTime startDate, DateTime endDate) => await GetListAsync<Dictionary<string, object>>($"/v3/validation/summary?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}");
        public async Task<string?> ExportValidationConfigAsync() => (await GetAsync<dynamic>("/v3/validation/config/export"))?.config_url;

        public async Task<ValidationResponse?> ValidateWithCustomRulesAsync(ValidationRequest request, List<ValidationRule> customRules) => await PostAsync<ValidationResponse>("/v3/validation/custom-rules", new { request, custom_rules = customRules });
        public async Task<ValidationResponse?> ValidateIncrementalAsync(string baseValidationId, object deltaData) => await PostAsync<ValidationResponse>("/v3/validation/incremental", new { base_validation_id = baseValidationId, delta_data = deltaData });
        public async Task<ValidationResponse?> ValidateWithContextAsync(ValidationRequest request, Dictionary<string, object> context) => await PostAsync<ValidationResponse>("/v3/validation/with-context", new { request, context });
        public async Task<List<ValidationResponse>?> CompareValidationResultsAsync(List<string> validationIds) => await PostListAsync<ValidationResponse>("/v3/validation/compare", new { validation_ids = validationIds });

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

        private async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException) { return default; }
        }

        private async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException) { return default; }
        }

        private async Task<MessageResponse8?> PostMessageAsync(string endpoint, object? data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse8>();
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<MessageResponse8?> DeleteMessageAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse8>();
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