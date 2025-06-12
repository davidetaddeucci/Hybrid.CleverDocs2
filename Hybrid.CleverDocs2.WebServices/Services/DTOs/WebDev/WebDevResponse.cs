using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.WebDev
{
    public class WebDevResponse
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ProjectResponse
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("project_type")]
        public string ProjectType { get; set; } = string.Empty;

        [JsonPropertyName("framework")]
        public string Framework { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("repository_url")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("live_url")]
        public string? LiveUrl { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("configuration")]
        public ProjectConfiguration Configuration { get; set; } = new();

        [JsonPropertyName("deployment_config")]
        public DeploymentConfig? DeploymentConfig { get; set; }

        [JsonPropertyName("team_members")]
        public List<string> TeamMembers { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("last_deployment")]
        public DateTime? LastDeployment { get; set; }

        [JsonPropertyName("build_count")]
        public int BuildCount { get; set; }

        [JsonPropertyName("deployment_count")]
        public int DeploymentCount { get; set; }
    }

    public class BuildResponse
    {
        [JsonPropertyName("build_id")]
        public string BuildId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("branch")]
        public string Branch { get; set; } = string.Empty;

        [JsonPropertyName("commit_hash")]
        public string? CommitHash { get; set; }

        [JsonPropertyName("commit_message")]
        public string? CommitMessage { get; set; }

        [JsonPropertyName("build_logs")]
        public List<BuildLog> BuildLogs { get; set; } = new();

        [JsonPropertyName("artifacts")]
        public List<BuildArtifact> Artifacts { get; set; } = new();

        [JsonPropertyName("build_time_seconds")]
        public int BuildTimeSeconds { get; set; }

        [JsonPropertyName("cache_hit")]
        public bool CacheHit { get; set; }

        [JsonPropertyName("build_size_mb")]
        public double BuildSizeMb { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();

        [JsonPropertyName("performance_metrics")]
        public BuildPerformanceMetrics? PerformanceMetrics { get; set; }
    }

    public class BuildLog
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
    }

    public class BuildArtifact
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("size_bytes")]
        public long SizeBytes { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("checksum")]
        public string Checksum { get; set; } = string.Empty;

        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }
    }

    public class BuildPerformanceMetrics
    {
        [JsonPropertyName("bundle_size")]
        public long BundleSize { get; set; }

        [JsonPropertyName("gzipped_size")]
        public long GzippedSize { get; set; }

        [JsonPropertyName("chunk_count")]
        public int ChunkCount { get; set; }

        [JsonPropertyName("asset_count")]
        public int AssetCount { get; set; }

        [JsonPropertyName("dependency_count")]
        public int DependencyCount { get; set; }

        [JsonPropertyName("tree_shaking_savings")]
        public long TreeShakingSavings { get; set; }

        [JsonPropertyName("code_splitting_efficiency")]
        public double CodeSplittingEfficiency { get; set; }
    }

    public class DeploymentResponse
    {
        [JsonPropertyName("deployment_id")]
        public string DeploymentId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("build_id")]
        public string BuildId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("deployment_logs")]
        public List<DeploymentLog> DeploymentLogs { get; set; } = new();

        [JsonPropertyName("health_checks")]
        public List<HealthCheckResult> HealthChecks { get; set; } = new();

        [JsonPropertyName("deployment_time_seconds")]
        public int DeploymentTimeSeconds { get; set; }

        [JsonPropertyName("rollback_enabled")]
        public bool RollbackEnabled { get; set; }

        [JsonPropertyName("canary_deployment")]
        public CanaryDeploymentStatus? CanaryDeployment { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("deployed_by")]
        public string DeployedBy { get; set; } = string.Empty;

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    public class DeploymentLog
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;
    }

    public class HealthCheckResult
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("response_time_ms")]
        public int ResponseTimeMs { get; set; }

        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("checked_at")]
        public DateTime CheckedAt { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    public class CanaryDeploymentStatus
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("current_traffic_percentage")]
        public int CurrentTrafficPercentage { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("success_criteria_met")]
        public bool SuccessCriteriaMet { get; set; }

        [JsonPropertyName("metrics")]
        public CanaryMetrics Metrics { get; set; } = new();

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("ends_at")]
        public DateTime EndsAt { get; set; }
    }

    public class CanaryMetrics
    {
        [JsonPropertyName("error_rate")]
        public double ErrorRate { get; set; }

        [JsonPropertyName("response_time")]
        public double ResponseTime { get; set; }

        [JsonPropertyName("request_count")]
        public int RequestCount { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }
    }

    public class MonitoringResponse
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("metrics")]
        public Dictionary<string, List<MetricDataPoint>> Metrics { get; set; } = new();

        [JsonPropertyName("time_range")]
        public TimeRange TimeRange { get; set; } = new();

        [JsonPropertyName("summary")]
        public MonitoringSummary Summary { get; set; } = new();

        [JsonPropertyName("alerts")]
        public List<Alert> Alerts { get; set; } = new();

        [JsonPropertyName("generated_at")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class MetricDataPoint
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;
    }

    public class MonitoringSummary
    {
        [JsonPropertyName("uptime_percentage")]
        public double UptimePercentage { get; set; }

        [JsonPropertyName("average_response_time")]
        public double AverageResponseTime { get; set; }

        [JsonPropertyName("total_requests")]
        public long TotalRequests { get; set; }

        [JsonPropertyName("error_count")]
        public long ErrorCount { get; set; }

        [JsonPropertyName("error_rate")]
        public double ErrorRate { get; set; }

        [JsonPropertyName("peak_traffic")]
        public double PeakTraffic { get; set; }

        [JsonPropertyName("data_transfer_gb")]
        public double DataTransferGb { get; set; }
    }

    public class Alert
    {
        [JsonPropertyName("alert_id")]
        public string AlertId { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("metric")]
        public string Metric { get; set; } = string.Empty;

        [JsonPropertyName("threshold")]
        public double Threshold { get; set; }

        [JsonPropertyName("current_value")]
        public double CurrentValue { get; set; }

        [JsonPropertyName("triggered_at")]
        public DateTime TriggeredAt { get; set; }

        [JsonPropertyName("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class OptimizationResponse
    {
        [JsonPropertyName("optimization_id")]
        public string OptimizationId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("optimization_type")]
        public string OptimizationType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public List<OptimizationRecommendation> Recommendations { get; set; } = new();

        [JsonPropertyName("current_metrics")]
        public Dictionary<string, double> CurrentMetrics { get; set; } = new();

        [JsonPropertyName("projected_improvements")]
        public Dictionary<string, double> ProjectedImprovements { get; set; } = new();

        [JsonPropertyName("analysis_time_seconds")]
        public int AnalysisTimeSeconds { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    public class OptimizationRecommendation
    {
        [JsonPropertyName("recommendation_id")]
        public string RecommendationId { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("impact")]
        public string Impact { get; set; } = string.Empty;

        [JsonPropertyName("effort")]
        public string Effort { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("estimated_improvement")]
        public Dictionary<string, double> EstimatedImprovement { get; set; } = new();

        [JsonPropertyName("implementation_steps")]
        public List<string> ImplementationSteps { get; set; } = new();

        [JsonPropertyName("auto_applicable")]
        public bool AutoApplicable { get; set; }

        [JsonPropertyName("applied")]
        public bool Applied { get; set; }
    }

    public class ProjectListResponse
    {
        [JsonPropertyName("projects")]
        public List<ProjectResponse> Projects { get; set; } = new();

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class MessageResponse9
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("operation_id")]
        public string? OperationId { get; set; }
    }
}
