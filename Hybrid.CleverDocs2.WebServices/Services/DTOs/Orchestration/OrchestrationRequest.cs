using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration
{
    public class OrchestrationRequest
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_name")]
        public string WorkflowName { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "normal"; // "low", "normal", "high", "urgent"

        [JsonPropertyName("timeout_seconds")]
        public int? TimeoutSeconds { get; set; }

        [JsonPropertyName("retry_config")]
        public RetryConfig? RetryConfig { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RetryConfig
    {
        [JsonPropertyName("max_retries")]
        public int MaxRetries { get; set; } = 3;

        [JsonPropertyName("retry_delay_seconds")]
        public int RetryDelaySeconds { get; set; } = 5;

        [JsonPropertyName("exponential_backoff")]
        public bool ExponentialBackoff { get; set; } = true;

        [JsonPropertyName("retry_on_failure_types")]
        public List<string> RetryOnFailureTypes { get; set; } = new();
    }

    public class WorkflowDefinitionRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("steps")]
        public List<WorkflowStep> Steps { get; set; } = new();

        [JsonPropertyName("triggers")]
        public List<WorkflowTrigger> Triggers { get; set; } = new();

        [JsonPropertyName("variables")]
        public Dictionary<string, object> Variables { get; set; } = new();

        [JsonPropertyName("timeout_seconds")]
        public int? TimeoutSeconds { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }

    public class WorkflowStep
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "action", "condition", "parallel", "loop"

        [JsonPropertyName("action")]
        public WorkflowAction? Action { get; set; }

        [JsonPropertyName("condition")]
        public WorkflowCondition? Condition { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new();

        [JsonPropertyName("timeout_seconds")]
        public int? TimeoutSeconds { get; set; }

        [JsonPropertyName("retry_config")]
        public RetryConfig? RetryConfig { get; set; }

        [JsonPropertyName("on_success")]
        public List<string> OnSuccess { get; set; } = new();

        [JsonPropertyName("on_failure")]
        public List<string> OnFailure { get; set; } = new();
    }

    public class WorkflowAction
    {
        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("input_mapping")]
        public Dictionary<string, string> InputMapping { get; set; } = new();

        [JsonPropertyName("output_mapping")]
        public Dictionary<string, string> OutputMapping { get; set; } = new();
    }

    public class WorkflowCondition
    {
        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty;

        [JsonPropertyName("true_steps")]
        public List<string> TrueSteps { get; set; } = new();

        [JsonPropertyName("false_steps")]
        public List<string> FalseSteps { get; set; } = new();
    }

    public class WorkflowTrigger
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "manual", "scheduled", "event", "webhook"

        [JsonPropertyName("schedule")]
        public string? Schedule { get; set; } // Cron expression

        [JsonPropertyName("event_type")]
        public string? EventType { get; set; }

        [JsonPropertyName("webhook_config")]
        public WebhookConfig? WebhookConfig { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class WebhookConfig
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = "POST";

        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();

        [JsonPropertyName("authentication")]
        public WebhookAuthentication? Authentication { get; set; }
    }

    public class WebhookAuthentication
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "none", "basic", "bearer", "api_key"

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("api_key")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("api_key_header")]
        public string? ApiKeyHeader { get; set; }
    }

    public class WorkflowExecutionRequest
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("input_data")]
        public Dictionary<string, object> InputData { get; set; } = new();

        [JsonPropertyName("execution_mode")]
        public string ExecutionMode { get; set; } = "async"; // "sync", "async"

        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "normal";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }

    public class WorkflowListRequest
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } // "active", "inactive", "draft"

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [JsonPropertyName("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }

    public class ExecutionListRequest
    {
        [JsonPropertyName("workflow_id")]
        public string? WorkflowId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; } // "pending", "running", "completed", "failed", "cancelled"

        [JsonPropertyName("started_after")]
        public DateTime? StartedAfter { get; set; }

        [JsonPropertyName("started_before")]
        public DateTime? StartedBefore { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }
}
