using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation
{
    public class ValidationResponse
    {
        [JsonPropertyName("validation_id")]
        public string ValidationId { get; set; } = string.Empty;

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("validation_status")]
        public string ValidationStatus { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public List<ValidationError> Errors { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<ValidationWarning> Warnings { get; set; } = new();

        [JsonPropertyName("summary")]
        public ValidationSummary Summary { get; set; } = new();

        [JsonPropertyName("validation_time_ms")]
        public long ValidationTimeMs { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ValidationError
    {
        [JsonPropertyName("error_id")]
        public string ErrorId { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; } = string.Empty;

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "error";

        [JsonPropertyName("rule_id")]
        public string? RuleId { get; set; }

        [JsonPropertyName("actual_value")]
        public object? ActualValue { get; set; }

        [JsonPropertyName("expected_value")]
        public object? ExpectedValue { get; set; }

        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; } = new();

        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; } = new();
    }

    public class ValidationWarning
    {
        [JsonPropertyName("warning_id")]
        public string WarningId { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("warning_code")]
        public string WarningCode { get; set; } = string.Empty;

        [JsonPropertyName("warning_message")]
        public string WarningMessage { get; set; } = string.Empty;

        [JsonPropertyName("rule_id")]
        public string? RuleId { get; set; }

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
    }

    public class ValidationSummary
    {
        [JsonPropertyName("total_fields_validated")]
        public int TotalFieldsValidated { get; set; }

        [JsonPropertyName("total_rules_applied")]
        public int TotalRulesApplied { get; set; }

        [JsonPropertyName("error_count")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("warning_count")]
        public int WarningCount { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("validation_score")]
        public double ValidationScore { get; set; }

        [JsonPropertyName("critical_errors")]
        public int CriticalErrors { get; set; }

        [JsonPropertyName("blocking_errors")]
        public int BlockingErrors { get; set; }
    }

    public class ContentValidationResponse
    {
        [JsonPropertyName("validation_id")]
        public string ValidationId { get; set; } = string.Empty;

        [JsonPropertyName("content_quality_score")]
        public double ContentQualityScore { get; set; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("content_issues")]
        public List<ContentIssue> ContentIssues { get; set; } = new();

        [JsonPropertyName("quality_metrics")]
        public ContentQualityMetrics QualityMetrics { get; set; } = new();

        [JsonPropertyName("suggestions")]
        public List<ContentSuggestion> Suggestions { get; set; } = new();

        [JsonPropertyName("corrected_content")]
        public string? CorrectedContent { get; set; }

        [JsonPropertyName("validation_time_ms")]
        public long ValidationTimeMs { get; set; }
    }

    public class ContentIssue
    {
        [JsonPropertyName("issue_type")]
        public string IssueType { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("position")]
        public TextPosition Position { get; set; } = new();

        [JsonPropertyName("original_text")]
        public string OriginalText { get; set; } = string.Empty;

        [JsonPropertyName("suggested_replacement")]
        public string? SuggestedReplacement { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    public class TextPosition
    {
        [JsonPropertyName("start_offset")]
        public int StartOffset { get; set; }

        [JsonPropertyName("end_offset")]
        public int EndOffset { get; set; }

        [JsonPropertyName("line_number")]
        public int LineNumber { get; set; }

        [JsonPropertyName("column_number")]
        public int ColumnNumber { get; set; }
    }

    public class ContentQualityMetrics
    {
        [JsonPropertyName("readability_score")]
        public double ReadabilityScore { get; set; }

        [JsonPropertyName("grammar_score")]
        public double GrammarScore { get; set; }

        [JsonPropertyName("spelling_score")]
        public double SpellingScore { get; set; }

        [JsonPropertyName("style_score")]
        public double StyleScore { get; set; }

        [JsonPropertyName("sentiment_score")]
        public double SentimentScore { get; set; }

        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }

        [JsonPropertyName("sentence_count")]
        public int SentenceCount { get; set; }

        [JsonPropertyName("paragraph_count")]
        public int ParagraphCount { get; set; }

        [JsonPropertyName("average_sentence_length")]
        public double AverageSentenceLength { get; set; }
    }

    public class ContentSuggestion
    {
        [JsonPropertyName("suggestion_type")]
        public string SuggestionType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("impact")]
        public string Impact { get; set; } = string.Empty;

        [JsonPropertyName("action_required")]
        public bool ActionRequired { get; set; }
    }

    public class ComplianceValidationResponse
    {
        [JsonPropertyName("validation_id")]
        public string ValidationId { get; set; } = string.Empty;

        [JsonPropertyName("compliance_status")]
        public string ComplianceStatus { get; set; } = string.Empty;

        [JsonPropertyName("compliance_score")]
        public double ComplianceScore { get; set; }

        [JsonPropertyName("compliance_results")]
        public List<ComplianceResult> ComplianceResults { get; set; } = new();

        [JsonPropertyName("violations")]
        public List<ComplianceViolation> Violations { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<ComplianceRecommendation> Recommendations { get; set; } = new();

        [JsonPropertyName("audit_trail")]
        public AuditTrail? AuditTrail { get; set; }

        [JsonPropertyName("validation_time_ms")]
        public long ValidationTimeMs { get; set; }
    }

    public class ComplianceResult
    {
        [JsonPropertyName("standard")]
        public string Standard { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("requirements_checked")]
        public int RequirementsChecked { get; set; }

        [JsonPropertyName("requirements_passed")]
        public int RequirementsPassed { get; set; }

        [JsonPropertyName("critical_violations")]
        public int CriticalViolations { get; set; }
    }

    public class ComplianceViolation
    {
        [JsonPropertyName("violation_id")]
        public string ViolationId { get; set; } = string.Empty;

        [JsonPropertyName("standard")]
        public string Standard { get; set; } = string.Empty;

        [JsonPropertyName("requirement")]
        public string Requirement { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("remediation_steps")]
        public List<string> RemediationSteps { get; set; } = new();

        [JsonPropertyName("risk_level")]
        public string RiskLevel { get; set; } = string.Empty;
    }

    public class ComplianceRecommendation
    {
        [JsonPropertyName("recommendation_id")]
        public string RecommendationId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("implementation_effort")]
        public string ImplementationEffort { get; set; } = string.Empty;

        [JsonPropertyName("business_impact")]
        public string BusinessImpact { get; set; } = string.Empty;
    }

    public class AuditTrail
    {
        [JsonPropertyName("audit_id")]
        public string AuditId { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public Dictionary<string, object> Details { get; set; } = new();

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("user_agent")]
        public string? UserAgent { get; set; }
    }

    public class BusinessRuleValidationResponse
    {
        [JsonPropertyName("validation_id")]
        public string ValidationId { get; set; } = string.Empty;

        [JsonPropertyName("business_validation_status")]
        public string BusinessValidationStatus { get; set; } = string.Empty;

        [JsonPropertyName("rule_results")]
        public List<BusinessRuleResult> RuleResults { get; set; } = new();

        [JsonPropertyName("blocking_violations")]
        public List<BusinessRuleViolation> BlockingViolations { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<BusinessRuleWarning> Warnings { get; set; } = new();

        [JsonPropertyName("auto_corrections")]
        public List<AutoCorrection> AutoCorrections { get; set; } = new();

        [JsonPropertyName("validation_time_ms")]
        public long ValidationTimeMs { get; set; }
    }

    public class BusinessRuleResult
    {
        [JsonPropertyName("rule_id")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("rule_name")]
        public string RuleName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("execution_time_ms")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("result_value")]
        public object? ResultValue { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    public class BusinessRuleViolation
    {
        [JsonPropertyName("violation_id")]
        public string ViolationId { get; set; } = string.Empty;

        [JsonPropertyName("rule_id")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("violation_type")]
        public string ViolationType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("current_value")]
        public object? CurrentValue { get; set; }

        [JsonPropertyName("expected_value")]
        public object? ExpectedValue { get; set; }

        [JsonPropertyName("is_blocking")]
        public bool IsBlocking { get; set; }
    }

    public class BusinessRuleWarning
    {
        [JsonPropertyName("warning_id")]
        public string WarningId { get; set; } = string.Empty;

        [JsonPropertyName("rule_id")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }
    }

    public class AutoCorrection
    {
        [JsonPropertyName("correction_id")]
        public string CorrectionId { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("original_value")]
        public object? OriginalValue { get; set; }

        [JsonPropertyName("corrected_value")]
        public object? CorrectedValue { get; set; }

        [JsonPropertyName("correction_reason")]
        public string CorrectionReason { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    public class BatchValidationResponse
    {
        [JsonPropertyName("batch_id")]
        public string BatchId { get; set; } = string.Empty;

        [JsonPropertyName("batch_status")]
        public string BatchStatus { get; set; } = string.Empty;

        [JsonPropertyName("total_validations")]
        public int TotalValidations { get; set; }

        [JsonPropertyName("successful_validations")]
        public int SuccessfulValidations { get; set; }

        [JsonPropertyName("failed_validations")]
        public int FailedValidations { get; set; }

        [JsonPropertyName("validation_results")]
        public List<ValidationResponse> ValidationResults { get; set; } = new();

        [JsonPropertyName("batch_summary")]
        public BatchSummary BatchSummary { get; set; } = new();

        [JsonPropertyName("processing_time_ms")]
        public long ProcessingTimeMs { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    public class BatchSummary
    {
        [JsonPropertyName("overall_success_rate")]
        public double OverallSuccessRate { get; set; }

        [JsonPropertyName("total_errors")]
        public int TotalErrors { get; set; }

        [JsonPropertyName("total_warnings")]
        public int TotalWarnings { get; set; }

        [JsonPropertyName("average_validation_time")]
        public double AverageValidationTime { get; set; }

        [JsonPropertyName("performance_metrics")]
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    }

    public class MessageResponse8
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("operation_id")]
        public string? OperationId { get; set; }
    }
}
