using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Orchestration
{
    public class OrchestrationResponse
    {
        [JsonPropertyName("execution_id")]
        public string ExecutionId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_name")]
        public string WorkflowName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "pending", "running", "completed", "failed", "cancelled"

        [JsonPropertyName("progress")]
        public ExecutionProgress Progress { get; set; } = new();

        [JsonPropertyName("result")]
        public Dictionary<string, object> Result { get; set; } = new();

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long? DurationSeconds { get; set; }

        [JsonPropertyName("step_executions")]
        public List<StepExecution> StepExecutions { get; set; } = new();
    }

    public class ExecutionProgress
    {
        [JsonPropertyName("current_step")]
        public string CurrentStep { get; set; } = string.Empty;

        [JsonPropertyName("completed_steps")]
        public int CompletedSteps { get; set; }

        [JsonPropertyName("total_steps")]
        public int TotalSteps { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("estimated_completion")]
        public DateTime? EstimatedCompletion { get; set; }
    }

    public class StepExecution
    {
        [JsonPropertyName("step_id")]
        public string StepId { get; set; } = string.Empty;

        [JsonPropertyName("step_name")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "pending", "running", "completed", "failed", "skipped"

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long? DurationSeconds { get; set; }

        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; } = new();

        [JsonPropertyName("output")]
        public Dictionary<string, object> Output { get; set; } = new();

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("retry_count")]
        public int RetryCount { get; set; }
    }

    public class WorkflowDefinitionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "active", "inactive", "draft"

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

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("execution_count")]
        public long ExecutionCount { get; set; }

        [JsonPropertyName("last_execution")]
        public DateTime? LastExecution { get; set; }
    }

    public class WorkflowExecutionResponse
    {
        [JsonPropertyName("execution_id")]
        public string ExecutionId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_name")]
        public string WorkflowName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("execution_mode")]
        public string ExecutionMode { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public ExecutionProgress Progress { get; set; } = new();

        [JsonPropertyName("input_data")]
        public Dictionary<string, object> InputData { get; set; } = new();

        [JsonPropertyName("output_data")]
        public Dictionary<string, object> OutputData { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long? DurationSeconds { get; set; }

        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    public class WorkflowListResponse
    {
        [JsonPropertyName("workflows")]
        public List<WorkflowDefinitionResponse> Workflows { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class ExecutionListResponse
    {
        [JsonPropertyName("executions")]
        public List<WorkflowExecutionResponse> Executions { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class WorkflowStatsResponse
    {
        [JsonPropertyName("total_workflows")]
        public long TotalWorkflows { get; set; }

        [JsonPropertyName("active_workflows")]
        public long ActiveWorkflows { get; set; }

        [JsonPropertyName("total_executions")]
        public long TotalExecutions { get; set; }

        [JsonPropertyName("running_executions")]
        public long RunningExecutions { get; set; }

        [JsonPropertyName("completed_executions")]
        public long CompletedExecutions { get; set; }

        [JsonPropertyName("failed_executions")]
        public long FailedExecutions { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("average_execution_time")]
        public double AverageExecutionTime { get; set; }

        [JsonPropertyName("executions_last_24h")]
        public long ExecutionsLast24h { get; set; }

        [JsonPropertyName("most_used_workflows")]
        public List<WorkflowUsageStats> MostUsedWorkflows { get; set; } = new();
    }

    public class WorkflowUsageStats
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("workflow_name")]
        public string WorkflowName { get; set; } = string.Empty;

        [JsonPropertyName("execution_count")]
        public long ExecutionCount { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("average_duration")]
        public double AverageDuration { get; set; }

        [JsonPropertyName("last_execution")]
        public DateTime? LastExecution { get; set; }
    }

    public class WorkflowValidationResponse
    {
        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("errors")]
        public List<ValidationError> Errors { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<ValidationWarning> Warnings { get; set; } = new();

        [JsonPropertyName("estimated_execution_time")]
        public int? EstimatedExecutionTime { get; set; }

        [JsonPropertyName("complexity_score")]
        public int ComplexityScore { get; set; }
    }

    public class ValidationError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("step_id")]
        public string? StepId { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }
    }

    public class ValidationWarning
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("step_id")]
        public string? StepId { get; set; }

        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }
    }

    public class MessageResponse5
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
