using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.WebDev
{
    public class WebDevRequest
    {
        [JsonPropertyName("project_name")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("project_type")]
        public string ProjectType { get; set; } = "web"; // "web", "api", "spa", "static", "microservice"

        [JsonPropertyName("framework")]
        public string Framework { get; set; } = string.Empty; // "react", "vue", "angular", "blazor", "next"

        [JsonPropertyName("source_files")]
        public List<SourceFile> SourceFiles { get; set; } = new();

        [JsonPropertyName("configuration")]
        public ProjectConfiguration Configuration { get; set; } = new();

        [JsonPropertyName("deployment_config")]
        public DeploymentConfig? DeploymentConfig { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SourceFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty; // "html", "css", "js", "ts", "json", "md"

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = "utf-8";

        [JsonPropertyName("size_bytes")]
        public long SizeBytes { get; set; }

        [JsonPropertyName("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new();
    }

    public class ProjectConfiguration
    {
        [JsonPropertyName("build_tool")]
        public string BuildTool { get; set; } = string.Empty; // "webpack", "vite", "rollup", "parcel"

        [JsonPropertyName("package_manager")]
        public string PackageManager { get; set; } = "npm"; // "npm", "yarn", "pnpm"

        [JsonPropertyName("node_version")]
        public string NodeVersion { get; set; } = "18";

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string> Dependencies { get; set; } = new();

        [JsonPropertyName("dev_dependencies")]
        public Dictionary<string, string> DevDependencies { get; set; } = new();

        [JsonPropertyName("scripts")]
        public Dictionary<string, string> Scripts { get; set; } = new();

        [JsonPropertyName("environment_variables")]
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        [JsonPropertyName("build_settings")]
        public BuildSettings BuildSettings { get; set; } = new();
    }

    public class BuildSettings
    {
        [JsonPropertyName("output_directory")]
        public string OutputDirectory { get; set; } = "dist";

        [JsonPropertyName("source_directory")]
        public string SourceDirectory { get; set; } = "src";

        [JsonPropertyName("public_directory")]
        public string PublicDirectory { get; set; } = "public";

        [JsonPropertyName("minify")]
        public bool Minify { get; set; } = true;

        [JsonPropertyName("source_maps")]
        public bool SourceMaps { get; set; } = true;

        [JsonPropertyName("tree_shaking")]
        public bool TreeShaking { get; set; } = true;

        [JsonPropertyName("code_splitting")]
        public bool CodeSplitting { get; set; } = true;

        [JsonPropertyName("target_browsers")]
        public List<string> TargetBrowsers { get; set; } = new();

        [JsonPropertyName("optimization_level")]
        public string OptimizationLevel { get; set; } = "production"; // "development", "production"
    }

    public class DeploymentConfig
    {
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty; // "vercel", "netlify", "aws", "azure", "docker"

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "production"; // "development", "staging", "production"

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("ssl_enabled")]
        public bool SslEnabled { get; set; } = true;

        [JsonPropertyName("cdn_enabled")]
        public bool CdnEnabled { get; set; } = true;

        [JsonPropertyName("auto_deploy")]
        public bool AutoDeploy { get; set; } = false;

        [JsonPropertyName("deployment_settings")]
        public Dictionary<string, object> DeploymentSettings { get; set; } = new();

        [JsonPropertyName("health_check")]
        public HealthCheckConfig? HealthCheck { get; set; }
    }

    public class HealthCheckConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "/health";

        [JsonPropertyName("interval_seconds")]
        public int IntervalSeconds { get; set; } = 30;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 10;

        [JsonPropertyName("retries")]
        public int Retries { get; set; } = 3;

        [JsonPropertyName("expected_status_codes")]
        public List<int> ExpectedStatusCodes { get; set; } = new() { 200 };
    }

    public class ProjectCreateRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("template")]
        public string Template { get; set; } = string.Empty; // "react", "vue", "angular", "static"

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("repository_url")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("initial_files")]
        public List<SourceFile> InitialFiles { get; set; } = new();

        [JsonPropertyName("configuration")]
        public ProjectConfiguration Configuration { get; set; } = new();

        [JsonPropertyName("team_members")]
        public List<string> TeamMembers { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }

    public class BuildRequest
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("branch")]
        public string Branch { get; set; } = "main";

        [JsonPropertyName("commit_hash")]
        public string? CommitHash { get; set; }

        [JsonPropertyName("build_config")]
        public BuildConfig BuildConfig { get; set; } = new();

        [JsonPropertyName("environment_variables")]
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        [JsonPropertyName("cache_enabled")]
        public bool CacheEnabled { get; set; } = true;

        [JsonPropertyName("parallel_build")]
        public bool ParallelBuild { get; set; } = true;
    }

    public class BuildConfig
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = "npm run build";

        [JsonPropertyName("output_directory")]
        public string OutputDirectory { get; set; } = "dist";

        [JsonPropertyName("install_command")]
        public string InstallCommand { get; set; } = "npm install";

        [JsonPropertyName("timeout_minutes")]
        public int TimeoutMinutes { get; set; } = 15;

        [JsonPropertyName("node_version")]
        public string NodeVersion { get; set; } = "18";

        [JsonPropertyName("pre_build_commands")]
        public List<string> PreBuildCommands { get; set; } = new();

        [JsonPropertyName("post_build_commands")]
        public List<string> PostBuildCommands { get; set; } = new();
    }

    public class DeploymentRequest
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("build_id")]
        public string? BuildId { get; set; }

        [JsonPropertyName("deployment_config")]
        public DeploymentConfig DeploymentConfig { get; set; } = new();

        [JsonPropertyName("rollback_enabled")]
        public bool RollbackEnabled { get; set; } = true;

        [JsonPropertyName("blue_green_deployment")]
        public bool BlueGreenDeployment { get; set; } = false;

        [JsonPropertyName("canary_deployment")]
        public CanaryDeployment? CanaryDeployment { get; set; }

        [JsonPropertyName("notifications")]
        public List<NotificationConfig> Notifications { get; set; } = new();
    }

    public class CanaryDeployment
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("traffic_percentage")]
        public int TrafficPercentage { get; set; } = 10;

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; } = 30;

        [JsonPropertyName("success_criteria")]
        public SuccessCriteria SuccessCriteria { get; set; } = new();

        [JsonPropertyName("auto_promote")]
        public bool AutoPromote { get; set; } = false;
    }

    public class SuccessCriteria
    {
        [JsonPropertyName("error_rate_threshold")]
        public double ErrorRateThreshold { get; set; } = 0.01;

        [JsonPropertyName("response_time_threshold")]
        public int ResponseTimeThreshold { get; set; } = 1000;

        [JsonPropertyName("min_requests")]
        public int MinRequests { get; set; } = 100;

        [JsonPropertyName("success_rate_threshold")]
        public double SuccessRateThreshold { get; set; } = 0.99;
    }

    public class NotificationConfig
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "email", "slack", "webhook", "teams"

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<string> Events { get; set; } = new(); // "build_started", "build_completed", "deployment_started", "deployment_completed"

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class MonitoringRequest
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("metrics")]
        public List<string> Metrics { get; set; } = new(); // "performance", "errors", "uptime", "traffic"

        [JsonPropertyName("time_range")]
        public TimeRange TimeRange { get; set; } = new();

        [JsonPropertyName("aggregation")]
        public string Aggregation { get; set; } = "average"; // "average", "sum", "min", "max", "count"

        [JsonPropertyName("filters")]
        public Dictionary<string, object> Filters { get; set; } = new();
    }

    public class TimeRange
    {
        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; } = "1h"; // "1m", "5m", "1h", "1d"
    }

    public class OptimizationRequest
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("optimization_type")]
        public string OptimizationType { get; set; } = string.Empty; // "performance", "seo", "accessibility", "security"

        [JsonPropertyName("target_metrics")]
        public Dictionary<string, double> TargetMetrics { get; set; } = new();

        [JsonPropertyName("auto_apply")]
        public bool AutoApply { get; set; } = false;

        [JsonPropertyName("analysis_depth")]
        public string AnalysisDepth { get; set; } = "standard"; // "basic", "standard", "comprehensive"
    }
}
