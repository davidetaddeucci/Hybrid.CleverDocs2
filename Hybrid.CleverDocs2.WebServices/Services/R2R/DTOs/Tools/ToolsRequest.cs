using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools
{
    public class ToolsRequest
    {
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; } = new();

        [JsonPropertyName("execution_config")]
        public ToolExecutionConfig? ExecutionConfig { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ToolExecutionConfig
    {
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        [JsonPropertyName("max_retries")]
        public int MaxRetries { get; set; } = 3;

        [JsonPropertyName("async_execution")]
        public bool AsyncExecution { get; set; } = false;

        [JsonPropertyName("cache_results")]
        public bool CacheResults { get; set; } = true;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "default";

        [JsonPropertyName("resource_limits")]
        public ResourceLimits? ResourceLimits { get; set; }
    }

    public class ResourceLimits
    {
        [JsonPropertyName("max_memory_mb")]
        public int? MaxMemoryMb { get; set; }

        [JsonPropertyName("max_cpu_percent")]
        public int? MaxCpuPercent { get; set; }

        [JsonPropertyName("max_execution_time")]
        public int? MaxExecutionTime { get; set; }

        [JsonPropertyName("max_file_size_mb")]
        public int? MaxFileSizeMb { get; set; }
    }

    public class ToolRegistrationRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

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
        public bool IsPublic { get; set; } = false;

        [JsonPropertyName("access_control")]
        public AccessControl? AccessControl { get; set; }
    }

    public class FunctionDefinition
    {
        [JsonPropertyName("parameters")]
        public List<ParameterDefinition> Parameters { get; set; } = new();

        [JsonPropertyName("return_type")]
        public string ReturnType { get; set; } = string.Empty;

        [JsonPropertyName("return_schema")]
        public Dictionary<string, object>? ReturnSchema { get; set; }

        [JsonPropertyName("examples")]
        public List<FunctionExample> Examples { get; set; } = new();

        [JsonPropertyName("documentation")]
        public string Documentation { get; set; } = string.Empty;
    }

    public class ParameterDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;

        [JsonPropertyName("default_value")]
        public object? DefaultValue { get; set; }

        [JsonPropertyName("validation")]
        public ParameterValidation? Validation { get; set; }

        [JsonPropertyName("schema")]
        public Dictionary<string, object>? Schema { get; set; }
    }

    public class ParameterValidation
    {
        [JsonPropertyName("min_value")]
        public double? MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public double? MaxValue { get; set; }

        [JsonPropertyName("min_length")]
        public int? MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("allowed_values")]
        public List<object>? AllowedValues { get; set; }

        [JsonPropertyName("custom_validator")]
        public string? CustomValidator { get; set; }
    }

    public class FunctionExample
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; } = new();

        [JsonPropertyName("expected_output")]
        public object? ExpectedOutput { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    public class ToolImplementation
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "code", "api", "docker", "lambda"

        [JsonPropertyName("source_code")]
        public string? SourceCode { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("entry_point")]
        public string? EntryPoint { get; set; }

        [JsonPropertyName("api_endpoint")]
        public string? ApiEndpoint { get; set; }

        [JsonPropertyName("docker_image")]
        public string? DockerImage { get; set; }

        [JsonPropertyName("environment_variables")]
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new();

        [JsonPropertyName("configuration")]
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class ToolRequirements
    {
        [JsonPropertyName("runtime")]
        public string Runtime { get; set; } = string.Empty;

        [JsonPropertyName("min_memory_mb")]
        public int MinMemoryMb { get; set; } = 128;

        [JsonPropertyName("min_cpu_cores")]
        public double MinCpuCores { get; set; } = 0.1;

        [JsonPropertyName("required_permissions")]
        public List<string> RequiredPermissions { get; set; } = new();

        [JsonPropertyName("network_access")]
        public bool NetworkAccess { get; set; } = false;

        [JsonPropertyName("file_system_access")]
        public bool FileSystemAccess { get; set; } = false;

        [JsonPropertyName("gpu_required")]
        public bool GpuRequired { get; set; } = false;
    }

    public class AccessControl
    {
        [JsonPropertyName("allowed_users")]
        public List<string> AllowedUsers { get; set; } = new();

        [JsonPropertyName("allowed_roles")]
        public List<string> AllowedRoles { get; set; } = new();

        [JsonPropertyName("rate_limit")]
        public RateLimit? RateLimit { get; set; }

        [JsonPropertyName("ip_whitelist")]
        public List<string> IpWhitelist { get; set; } = new();

        [JsonPropertyName("require_authentication")]
        public bool RequireAuthentication { get; set; } = true;
    }

    public class RateLimit
    {
        [JsonPropertyName("requests_per_minute")]
        public int RequestsPerMinute { get; set; } = 60;

        [JsonPropertyName("requests_per_hour")]
        public int RequestsPerHour { get; set; } = 1000;

        [JsonPropertyName("requests_per_day")]
        public int RequestsPerDay { get; set; } = 10000;

        [JsonPropertyName("burst_limit")]
        public int BurstLimit { get; set; } = 10;
    }

    public class ToolListRequest
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("search_query")]
        public string? SearchQuery { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [JsonPropertyName("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }

    public class ToolValidationRequest
    {
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("validation_level")]
        public string ValidationLevel { get; set; } = "strict"; // "strict", "moderate", "lenient"

        [JsonPropertyName("include_suggestions")]
        public bool IncludeSuggestions { get; set; } = true;
    }

    public class ToolTestRequest
    {
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("test_cases")]
        public List<ToolTestCase> TestCases { get; set; } = new();

        [JsonPropertyName("test_config")]
        public ToolTestConfig TestConfig { get; set; } = new();
    }

    public class ToolTestCase
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; } = new();

        [JsonPropertyName("expected_output")]
        public object? ExpectedOutput { get; set; }

        [JsonPropertyName("assertions")]
        public List<TestAssertion> Assertions { get; set; } = new();

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class TestAssertion
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "equals", "contains", "matches", "type_check"

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("expected_value")]
        public object? ExpectedValue { get; set; }

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class ToolTestConfig
    {
        [JsonPropertyName("parallel_execution")]
        public bool ParallelExecution { get; set; } = false;

        [JsonPropertyName("stop_on_failure")]
        public bool StopOnFailure { get; set; } = false;

        [JsonPropertyName("include_performance_metrics")]
        public bool IncludePerformanceMetrics { get; set; } = true;

        [JsonPropertyName("max_execution_time")]
        public int MaxExecutionTime { get; set; } = 300;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "test";
    }
}
