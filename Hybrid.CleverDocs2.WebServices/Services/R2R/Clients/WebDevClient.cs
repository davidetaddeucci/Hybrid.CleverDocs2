using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.WebDev;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public class WebDevClient : IWebDevClient
    {
        private readonly HttpClient _httpClient;

        public WebDevClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Project management
        public async Task<ProjectResponse?> CreateProjectAsync(ProjectCreateRequest request) => await PostAsync<ProjectResponse>("/v3/webdev/projects", request);
        public async Task<ProjectResponse?> GetProjectAsync(string projectId) => await GetAsync<ProjectResponse>($"/v3/webdev/projects/{projectId}");
        public async Task<ProjectListResponse?> ListProjectsAsync(int page = 1, int pageSize = 50, string? filter = null) => await GetAsync<ProjectListResponse>($"/v3/webdev/projects?page={page}&page_size={pageSize}&filter={filter}");
        public async Task<ProjectResponse?> UpdateProjectAsync(string projectId, ProjectCreateRequest request) => await PutAsync<ProjectResponse>($"/v3/webdev/projects/{projectId}", request);
        public async Task<MessageResponse9?> DeleteProjectAsync(string projectId) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}");
        public async Task<MessageResponse9?> ArchiveProjectAsync(string projectId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/archive", null);
        public async Task<MessageResponse9?> RestoreProjectAsync(string projectId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/restore", null);

        // Build operations
        public async Task<BuildResponse?> StartBuildAsync(BuildRequest request) => await PostAsync<BuildResponse>("/v3/webdev/builds", request);
        public async Task<BuildResponse?> GetBuildAsync(string buildId) => await GetAsync<BuildResponse>($"/v3/webdev/builds/{buildId}");
        public async Task<List<BuildResponse>?> ListBuildsAsync(string projectId, int limit = 50, int offset = 0) => await GetListAsync<BuildResponse>($"/v3/webdev/projects/{projectId}/builds?limit={limit}&offset={offset}");
        public async Task<MessageResponse9?> CancelBuildAsync(string buildId) => await PostMessageAsync($"/v3/webdev/builds/{buildId}/cancel", null);
        public async Task<MessageResponse9?> RetryBuildAsync(string buildId) => await PostMessageAsync($"/v3/webdev/builds/{buildId}/retry", null);
        public async Task<List<BuildLog>?> GetBuildLogsAsync(string buildId, string? level = null) => await GetListAsync<BuildLog>($"/v3/webdev/builds/{buildId}/logs?level={level}");
        public async Task<List<BuildArtifact>?> GetBuildArtifactsAsync(string buildId) => await GetListAsync<BuildArtifact>($"/v3/webdev/builds/{buildId}/artifacts");
        public async Task<string?> DownloadBuildArtifactAsync(string buildId, string artifactName) => (await GetAsync<dynamic>($"/v3/webdev/builds/{buildId}/artifacts/{artifactName}/download"))?.download_url;

        // Deployment operations
        public async Task<DeploymentResponse?> DeployAsync(DeploymentRequest request) => await PostAsync<DeploymentResponse>("/v3/webdev/deployments", request);
        public async Task<DeploymentResponse?> GetDeploymentAsync(string deploymentId) => await GetAsync<DeploymentResponse>($"/v3/webdev/deployments/{deploymentId}");
        public async Task<List<DeploymentResponse>?> ListDeploymentsAsync(string projectId, string? environment = null, int limit = 50, int offset = 0) => await GetListAsync<DeploymentResponse>($"/v3/webdev/projects/{projectId}/deployments?environment={environment}&limit={limit}&offset={offset}");
        public async Task<MessageResponse9?> RollbackDeploymentAsync(string deploymentId, string? targetDeploymentId = null) => await PostMessageAsync($"/v3/webdev/deployments/{deploymentId}/rollback", new { target_deployment_id = targetDeploymentId });
        public async Task<MessageResponse9?> CancelDeploymentAsync(string deploymentId) => await PostMessageAsync($"/v3/webdev/deployments/{deploymentId}/cancel", null);
        public async Task<List<DeploymentLog>?> GetDeploymentLogsAsync(string deploymentId) => await GetListAsync<DeploymentLog>($"/v3/webdev/deployments/{deploymentId}/logs");
        public async Task<List<HealthCheckResult>?> GetHealthChecksAsync(string deploymentId) => await GetListAsync<HealthCheckResult>($"/v3/webdev/deployments/{deploymentId}/health-checks");
        public async Task<MessageResponse9?> TriggerHealthCheckAsync(string deploymentId) => await PostMessageAsync($"/v3/webdev/deployments/{deploymentId}/health-checks/trigger", null);

        // Environment management
        public async Task<MessageResponse9?> CreateEnvironmentAsync(string projectId, string environmentName, Dictionary<string, string> variables) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/environments", new { name = environmentName, variables });
        public async Task<Dictionary<string, string>?> GetEnvironmentVariablesAsync(string projectId, string environment) => await GetAsync<Dictionary<string, string>>($"/v3/webdev/projects/{projectId}/environments/{environment}/variables");
        public async Task<MessageResponse9?> UpdateEnvironmentVariablesAsync(string projectId, string environment, Dictionary<string, string> variables) => await PutMessageAsync($"/v3/webdev/projects/{projectId}/environments/{environment}/variables", new { variables });
        public async Task<MessageResponse9?> DeleteEnvironmentAsync(string projectId, string environment) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/environments/{environment}");
        public async Task<List<string>?> ListEnvironmentsAsync(string projectId) => await GetListAsync<string>($"/v3/webdev/projects/{projectId}/environments");

        // Domain and SSL management
        public async Task<MessageResponse9?> AddCustomDomainAsync(string projectId, string domain, bool sslEnabled = true) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/domains", new { domain, ssl_enabled = sslEnabled });
        public async Task<MessageResponse9?> RemoveCustomDomainAsync(string projectId, string domain) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/domains/{domain}");
        public async Task<List<string>?> ListCustomDomainsAsync(string projectId) => await GetListAsync<string>($"/v3/webdev/projects/{projectId}/domains");
        public async Task<MessageResponse9?> RenewSSLCertificateAsync(string projectId, string domain) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/domains/{domain}/ssl/renew", null);
        public async Task<Dictionary<string, object>?> GetSSLStatusAsync(string projectId, string domain) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/domains/{domain}/ssl/status");

        // Monitoring and analytics
        public async Task<MonitoringResponse?> GetMonitoringDataAsync(MonitoringRequest request) => await PostAsync<MonitoringResponse>("/v3/webdev/monitoring", request);
        public async Task<Dictionary<string, object>?> GetProjectAnalyticsAsync(string projectId, int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/analytics?days={days}");
        public async Task<List<Alert>?> GetActiveAlertsAsync(string projectId) => await GetListAsync<Alert>($"/v3/webdev/projects/{projectId}/alerts");
        public async Task<MessageResponse9?> AcknowledgeAlertAsync(string alertId) => await PostMessageAsync($"/v3/webdev/alerts/{alertId}/acknowledge", null);
        public async Task<MessageResponse9?> ResolveAlertAsync(string alertId) => await PostMessageAsync($"/v3/webdev/alerts/{alertId}/resolve", null);
        public async Task<Dictionary<string, double>?> GetPerformanceMetricsAsync(string projectId, int hours = 24) => await GetAsync<Dictionary<string, double>>($"/v3/webdev/projects/{projectId}/performance?hours={hours}");
        public async Task<Dictionary<string, object>?> GetUptimeStatsAsync(string projectId, int days = 30) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/uptime?days={days}");

        // Optimization and recommendations
        public async Task<OptimizationResponse?> AnalyzeProjectAsync(OptimizationRequest request) => await PostAsync<OptimizationResponse>("/v3/webdev/optimization/analyze", request);
        public async Task<OptimizationResponse?> GetOptimizationResultAsync(string optimizationId) => await GetAsync<OptimizationResponse>($"/v3/webdev/optimization/{optimizationId}");
        public async Task<MessageResponse9?> ApplyOptimizationAsync(string optimizationId, List<string> recommendationIds) => await PostMessageAsync($"/v3/webdev/optimization/{optimizationId}/apply", new { recommendation_ids = recommendationIds });
        public async Task<List<OptimizationRecommendation>?> GetOptimizationRecommendationsAsync(string projectId, string? category = null) => await GetListAsync<OptimizationRecommendation>($"/v3/webdev/projects/{projectId}/optimization/recommendations?category={category}");
        public async Task<Dictionary<string, object>?> GetOptimizationHistoryAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/optimization/history");

        // File and asset management
        public async Task<MessageResponse9?> UploadFileAsync(string projectId, string filePath, byte[] content, string contentType) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/files", new { file_path = filePath, content = Convert.ToBase64String(content), content_type = contentType });
        public async Task<byte[]?> DownloadFileAsync(string projectId, string filePath) => Convert.FromBase64String((await GetAsync<dynamic>($"/v3/webdev/projects/{projectId}/files/{filePath}"))?.content ?? "");
        public async Task<MessageResponse9?> DeleteFileAsync(string projectId, string filePath) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/files/{filePath}");
        public async Task<List<SourceFile>?> ListProjectFilesAsync(string projectId, string? directory = null) => await GetListAsync<SourceFile>($"/v3/webdev/projects/{projectId}/files?directory={directory}");
        public async Task<MessageResponse9?> CreateDirectoryAsync(string projectId, string directoryPath) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/directories", new { directory_path = directoryPath });
        public async Task<MessageResponse9?> DeleteDirectoryAsync(string projectId, string directoryPath) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/directories/{directoryPath}");

        // Collaboration and team management
        public async Task<MessageResponse9?> AddTeamMemberAsync(string projectId, string userId, string role = "developer") => await PostMessageAsync($"/v3/webdev/projects/{projectId}/team", new { user_id = userId, role });
        public async Task<MessageResponse9?> RemoveTeamMemberAsync(string projectId, string userId) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/team/{userId}");
        public async Task<List<string>?> GetTeamMembersAsync(string projectId) => await GetListAsync<string>($"/v3/webdev/projects/{projectId}/team");
        public async Task<MessageResponse9?> UpdateMemberRoleAsync(string projectId, string userId, string role) => await PutMessageAsync($"/v3/webdev/projects/{projectId}/team/{userId}", new { role });
        public async Task<Dictionary<string, string>?> GetMemberPermissionsAsync(string projectId, string userId) => await GetAsync<Dictionary<string, string>>($"/v3/webdev/projects/{projectId}/team/{userId}/permissions");

        // Backup and restore
        public async Task<MessageResponse9?> CreateBackupAsync(string projectId, string backupName, bool includeDatabase = false) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/backups", new { backup_name = backupName, include_database = includeDatabase });
        public async Task<List<Dictionary<string, object>>?> ListBackupsAsync(string projectId) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/backups");
        public async Task<MessageResponse9?> RestoreFromBackupAsync(string projectId, string backupId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/restore", new { backup_id = backupId });
        public async Task<MessageResponse9?> DeleteBackupAsync(string backupId) => await DeleteMessageAsync($"/v3/webdev/backups/{backupId}");
        public async Task<string?> ExportProjectAsync(string projectId, string format = "zip") => (await PostAsync<dynamic>($"/v3/webdev/projects/{projectId}/export", new { format }))?.export_url;

        // CI/CD integration
        public async Task<MessageResponse9?> ConnectRepositoryAsync(string projectId, string repositoryUrl, string branch = "main") => await PostMessageAsync($"/v3/webdev/projects/{projectId}/repository", new { repository_url = repositoryUrl, branch });
        public async Task<MessageResponse9?> DisconnectRepositoryAsync(string projectId) => await DeleteMessageAsync($"/v3/webdev/projects/{projectId}/repository");
        public async Task<Dictionary<string, object>?> GetRepositoryStatusAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/repository/status");
        public async Task<MessageResponse9?> TriggerWebhookAsync(string projectId, string webhookType, Dictionary<string, object> payload) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/webhooks/{webhookType}/trigger", payload);
        public async Task<List<Dictionary<string, object>>?> GetWebhookHistoryAsync(string projectId, int limit = 50) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/webhooks/history?limit={limit}");

        // Templates and scaffolding
        public async Task<List<Dictionary<string, object>>?> ListProjectTemplatesAsync(string? category = null) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/templates?category={category}");
        public async Task<ProjectResponse?> CreateFromTemplateAsync(string templateId, ProjectCreateRequest request) => await PostAsync<ProjectResponse>($"/v3/webdev/templates/{templateId}/create", request);
        public async Task<MessageResponse9?> SaveAsTemplateAsync(string projectId, string templateName, string description) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/save-as-template", new { template_name = templateName, description });
        public async Task<MessageResponse9?> DeleteTemplateAsync(string templateId) => await DeleteMessageAsync($"/v3/webdev/templates/{templateId}");

        // Performance and caching
        public async Task<MessageResponse9?> ClearCacheAsync(string projectId, string? cacheType = null) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/cache/clear", new { cache_type = cacheType });
        public async Task<Dictionary<string, object>?> GetCacheStatsAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/cache/stats");
        public async Task<MessageResponse9?> EnableCDNAsync(string projectId, Dictionary<string, object>? settings = null) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/cdn/enable", settings);
        public async Task<MessageResponse9?> DisableCDNAsync(string projectId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/cdn/disable", null);
        public async Task<Dictionary<string, object>?> GetCDNStatsAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/cdn/stats");

        // Security and compliance
        public async Task<Dictionary<string, object>?> RunSecurityScanAsync(string projectId) => await PostAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/security/scan", null);
        public async Task<List<Dictionary<string, object>>?> GetSecurityVulnerabilitiesAsync(string projectId) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/security/vulnerabilities");
        public async Task<MessageResponse9?> FixSecurityIssueAsync(string projectId, string issueId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/security/issues/{issueId}/fix", null);
        public async Task<Dictionary<string, object>?> GetComplianceReportAsync(string projectId, string standard = "GDPR") => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/compliance/{standard}");
        public async Task<MessageResponse9?> EnableSecurityHeadersAsync(string projectId, Dictionary<string, string> headers) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/security/headers", new { headers });

        // Logs and debugging
        public async Task<List<Dictionary<string, object>>?> GetApplicationLogsAsync(string projectId, DateTime? startTime = null, DateTime? endTime = null, string? level = null, int limit = 1000) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/logs?start_time={startTime:yyyy-MM-ddTHH:mm:ssZ}&end_time={endTime:yyyy-MM-ddTHH:mm:ssZ}&level={level}&limit={limit}");
        public async Task<List<Dictionary<string, object>>?> GetErrorLogsAsync(string projectId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100) => await GetListAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/logs/errors?start_time={startTime:yyyy-MM-ddTHH:mm:ssZ}&end_time={endTime:yyyy-MM-ddTHH:mm:ssZ}&limit={limit}");
        public async Task<MessageResponse9?> EnableDebugModeAsync(string projectId, int durationMinutes = 60) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/debug/enable", new { duration_minutes = durationMinutes });
        public async Task<MessageResponse9?> DisableDebugModeAsync(string projectId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/debug/disable", null);
        public async Task<Dictionary<string, object>?> GetDebugInfoAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/debug/info");

        // Configuration and settings
        public async Task<Dictionary<string, object>?> GetProjectConfigAsync(string projectId) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/projects/{projectId}/config");
        public async Task<MessageResponse9?> UpdateProjectConfigAsync(string projectId, Dictionary<string, object> config) => await PutMessageAsync($"/v3/webdev/projects/{projectId}/config", config);
        public async Task<Dictionary<string, object>?> GetDefaultConfigAsync(string projectType) => await GetAsync<Dictionary<string, object>>($"/v3/webdev/config/defaults/{projectType}");
        public async Task<MessageResponse9?> ResetConfigAsync(string projectId) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/config/reset", null);
        public async Task<string?> ExportConfigAsync(string projectId) => (await GetAsync<dynamic>($"/v3/webdev/projects/{projectId}/config/export"))?.config_url;
        public async Task<MessageResponse9?> ImportConfigAsync(string projectId, string configData) => await PostMessageAsync($"/v3/webdev/projects/{projectId}/config/import", new { config_data = configData });

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

        private async Task<T?> PostAsync<T>(string endpoint, object? data)
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

        private async Task<MessageResponse9?> PostMessageAsync(string endpoint, object? data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse9>();
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<MessageResponse9?> PutMessageAsync(string endpoint, object data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse9>();
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<MessageResponse9?> DeleteMessageAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse9>();
            }
            catch (HttpRequestException) { return null; }
        }
    }
}