using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools
{
    public class ToolsResponse
    {
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("execution_id")]
        public string ExecutionId { get; set; } = string.Empty;

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("execution_time_ms")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("error")]
        public ToolError? Error { get; set; }
    }

    public class ToolError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public Dictionary<string, object> Details { get; set; } = new();

        [JsonPropertyName("stack_trace")]
        public string? StackTrace { get; set; }

        [JsonPropertyName("retry_after_seconds")]
        public int? RetryAfterSeconds { get; set; }
    }

    public class ToolRegistrationResponse
    {
        [JsonPropertyName("tool_id")]
        public string ToolId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("function_definition")]
        public FunctionDefinition FunctionDefinition { get; set; } = new();

        [JsonPropertyName("implementation")]
        public ToolImplementation Implementation { get; set; } = new();

        [JsonPropertyName("requirements")]
        public ToolRequirements Requirements { get; set; } = new();

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("access_control")]
        public AccessControl? AccessControl { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "active", "inactive", "deprecated"

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("usage_count")]
        public long UsageCount { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("last_used")]
        public DateTime? LastUsed { get; set; }
    }

    public class ToolListResponse
    {
        [JsonPropertyName("tools")]
        public List<ToolRegistrationResponse> Tools { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }

    public class ToolValidationResponse
    {
        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("validation_errors")]
        public List<ValidationError> ValidationErrors { get; set; } = new();

        [JsonPropertyName("suggestions")]
        public List<ValidationSuggestion> Suggestions { get; set; } = new();

        [JsonPropertyName("validated_parameters")]
        public Dictionary<string, object> ValidatedParameters { get; set; } = new();

        [JsonPropertyName("validation_time_ms")]
        public long ValidationTimeMs { get; set; }
    }

    public class ValidationError
    {
        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("error_type")]
        public string ErrorType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("expected_value")]
        public object? ExpectedValue { get; set; }

        [JsonPropertyName("actual_value")]
        public object? ActualValue { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "error"; // "error", "warning", "info"
    }

    public class ValidationSuggestion
    {
        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; } = string.Empty;

        [JsonPropertyName("suggested_value")]
        public object? SuggestedValue { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ToolTestResponse
    {
        [JsonPropertyName("test_id")]
        public string TestId { get; set; } = string.Empty;

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "running", "completed", "failed"

        [JsonPropertyName("overall_result")]
        public string OverallResult { get; set; } = string.Empty; // "passed", "failed", "partial"

        [JsonPropertyName("test_results")]
        public List<TestCaseResult> TestResults { get; set; } = new();

        [JsonPropertyName("summary")]
        public TestSummary Summary { get; set; } = new();

        [JsonPropertyName("performance_metrics")]
        public PerformanceMetrics? PerformanceMetrics { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_ms")]
        public long? DurationMs { get; set; }
    }

    public class TestCaseResult
    {
        [JsonPropertyName("test_case_name")]
        public string TestCaseName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "passed", "failed", "skipped"

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("expected_output")]
        public object? ExpectedOutput { get; set; }

        [JsonPropertyName("assertion_results")]
        public List<AssertionResult> AssertionResults { get; set; } = new();

        [JsonPropertyName("execution_time_ms")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("error")]
        public ToolError? Error { get; set; }

        [JsonPropertyName("logs")]
        public List<string> Logs { get; set; } = new();
    }

    public class AssertionResult
    {
        [JsonPropertyName("assertion_type")]
        public string AssertionType { get; set; } = string.Empty;

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("expected")]
        public object? Expected { get; set; }

        [JsonPropertyName("actual")]
        public object? Actual { get; set; }

        [JsonPropertyName("details")]
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class TestSummary
    {
        [JsonPropertyName("total_tests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("passed_tests")]
        public int PassedTests { get; set; }

        [JsonPropertyName("failed_tests")]
        public int FailedTests { get; set; }

        [JsonPropertyName("skipped_tests")]
        public int SkippedTests { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("total_assertions")]
        public int TotalAssertions { get; set; }

        [JsonPropertyName("passed_assertions")]
        public int PassedAssertions { get; set; }

        [JsonPropertyName("failed_assertions")]
        public int FailedAssertions { get; set; }
    }

    public class PerformanceMetrics
    {
        [JsonPropertyName("average_execution_time")]
        public double AverageExecutionTime { get; set; }

        [JsonPropertyName("min_execution_time")]
        public long MinExecutionTime { get; set; }

        [JsonPropertyName("max_execution_time")]
        public long MaxExecutionTime { get; set; }

        [JsonPropertyName("memory_usage_mb")]
        public double? MemoryUsageMb { get; set; }

        [JsonPropertyName("cpu_usage_percent")]
        public double? CpuUsagePercent { get; set; }

        [JsonPropertyName("throughput_per_second")]
        public double? ThroughputPerSecond { get; set; }

        [JsonPropertyName("error_rate")]
        public double ErrorRate { get; set; }
    }

    public class ToolStatsResponse
    {
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("total_executions")]
        public long TotalExecutions { get; set; }

        [JsonPropertyName("successful_executions")]
        public long SuccessfulExecutions { get; set; }

        [JsonPropertyName("failed_executions")]
        public long FailedExecutions { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("average_execution_time")]
        public double AverageExecutionTime { get; set; }

        [JsonPropertyName("total_execution_time")]
        public long TotalExecutionTime { get; set; }

        [JsonPropertyName("last_execution")]
        public DateTime? LastExecution { get; set; }

        [JsonPropertyName("performance_trends")]
        public List<PerformanceTrend> PerformanceTrends { get; set; } = new();

        [JsonPropertyName("error_distribution")]
        public Dictionary<string, int> ErrorDistribution { get; set; } = new();

        [JsonPropertyName("usage_by_user")]
        public Dictionary<string, long> UsageByUser { get; set; } = new();
    }

    public class PerformanceTrend
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("executions")]
        public long Executions { get; set; }

        [JsonPropertyName("average_time")]
        public double AverageTime { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("error_count")]
        public long ErrorCount { get; set; }
    }

    public class MessageResponse7
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("operation_id")]
        public string? OperationId { get; set; }
    }
}
