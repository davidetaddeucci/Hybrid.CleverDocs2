using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Validation
{
    public class ValidationRequest
    {
        [JsonPropertyName("data")]
        public object Data { get; set; } = new();

        [JsonPropertyName("validation_rules")]
        public List<ValidationRule> ValidationRules { get; set; } = new();

        [JsonPropertyName("schema")]
        public ValidationSchema? Schema { get; set; }

        [JsonPropertyName("validation_type")]
        public string ValidationType { get; set; } = "comprehensive";

        [JsonPropertyName("strict_mode")]
        public bool StrictMode { get; set; } = true;

        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ValidationRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("rule_type")]
        public string RuleType { get; set; } = string.Empty;

        [JsonPropertyName("field_path")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("condition")]
        public RuleCondition Condition { get; set; } = new();

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "error";

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 1;
    }

    public class RuleCondition
    {
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("values")]
        public List<object>? Values { get; set; }

        [JsonPropertyName("min_value")]
        public object? MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public object? MaxValue { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("custom_function")]
        public string? CustomFunction { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class ValidationSchema
    {
        [JsonPropertyName("schema_id")]
        public string SchemaId { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("schema_type")]
        public string SchemaType { get; set; } = "json";

        [JsonPropertyName("schema_definition")]
        public object SchemaDefinition { get; set; } = new();

        [JsonPropertyName("required_fields")]
        public List<string> RequiredFields { get; set; } = new();

        [JsonPropertyName("field_definitions")]
        public Dictionary<string, FieldDefinition> FieldDefinitions { get; set; } = new();

        [JsonPropertyName("constraints")]
        public List<SchemaConstraint> Constraints { get; set; } = new();
    }

    public class FieldDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("min_length")]
        public int? MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("min_value")]
        public double? MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public double? MaxValue { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("enum_values")]
        public List<object>? EnumValues { get; set; }

        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; } = false;

        [JsonPropertyName("default_value")]
        public object? DefaultValue { get; set; }
    }

    public class SchemaConstraint
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<string> Fields { get; set; } = new();

        [JsonPropertyName("condition")]
        public string Condition { get; set; } = string.Empty;

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ContentValidationRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = "text";

        [JsonPropertyName("validation_checks")]
        public List<string> ValidationChecks { get; set; } = new();

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";

        [JsonPropertyName("quality_threshold")]
        public double QualityThreshold { get; set; } = 0.8;

        [JsonPropertyName("custom_rules")]
        public List<ContentRule> CustomRules { get; set; } = new();
    }

    public class ContentRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = string.Empty;

        [JsonPropertyName("replacement")]
        public string? Replacement { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "warning";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ComplianceValidationRequest
    {
        [JsonPropertyName("data")]
        public object Data { get; set; } = new();

        [JsonPropertyName("compliance_standards")]
        public List<string> ComplianceStandards { get; set; } = new();

        [JsonPropertyName("jurisdiction")]
        public string Jurisdiction { get; set; } = "US";

        [JsonPropertyName("data_classification")]
        public string DataClassification { get; set; } = "public";

        [JsonPropertyName("audit_trail")]
        public bool AuditTrail { get; set; } = true;

        [JsonPropertyName("retention_policy")]
        public RetentionPolicy? RetentionPolicy { get; set; }
    }

    public class RetentionPolicy
    {
        [JsonPropertyName("retention_period_days")]
        public int RetentionPeriodDays { get; set; }

        [JsonPropertyName("deletion_method")]
        public string DeletionMethod { get; set; } = "secure";

        [JsonPropertyName("backup_required")]
        public bool BackupRequired { get; set; } = true;

        [JsonPropertyName("approval_required")]
        public bool ApprovalRequired { get; set; } = false;
    }

    public class BusinessRuleValidationRequest
    {
        [JsonPropertyName("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [JsonPropertyName("entity_data")]
        public object EntityData { get; set; } = new();

        [JsonPropertyName("business_rules")]
        public List<BusinessRule> BusinessRules { get; set; } = new();

        [JsonPropertyName("context")]
        public BusinessContext Context { get; set; } = new();

        [JsonPropertyName("validation_scope")]
        public string ValidationScope { get; set; } = "full";
    }

    public class BusinessRule
    {
        [JsonPropertyName("rule_id")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("rule_expression")]
        public string RuleExpression { get; set; } = string.Empty;

        [JsonPropertyName("rule_type")]
        public string RuleType { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 1;

        [JsonPropertyName("is_blocking")]
        public bool IsBlocking { get; set; } = true;

        [JsonPropertyName("error_action")]
        public string ErrorAction { get; set; } = "reject";
    }

    public class BusinessContext
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("organization_id")]
        public string? OrganizationId { get; set; }

        [JsonPropertyName("transaction_type")]
        public string? TransactionType { get; set; }

        [JsonPropertyName("effective_date")]
        public DateTime? EffectiveDate { get; set; }

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "production";

        [JsonPropertyName("additional_context")]
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
    }

    public class BatchValidationRequest
    {
        [JsonPropertyName("validation_requests")]
        public List<ValidationRequest> ValidationRequests { get; set; } = new();

        [JsonPropertyName("batch_config")]
        public BatchConfig BatchConfig { get; set; } = new();

        [JsonPropertyName("parallel_processing")]
        public bool ParallelProcessing { get; set; } = true;

        [JsonPropertyName("stop_on_first_error")]
        public bool StopOnFirstError { get; set; } = false;
    }

    public class BatchConfig
    {
        [JsonPropertyName("batch_size")]
        public int BatchSize { get; set; } = 100;

        [JsonPropertyName("max_concurrent")]
        public int MaxConcurrent { get; set; } = 10;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 300;

        [JsonPropertyName("retry_count")]
        public int RetryCount { get; set; } = 3;

        [JsonPropertyName("progress_callback")]
        public string? ProgressCallback { get; set; }
    }
}
