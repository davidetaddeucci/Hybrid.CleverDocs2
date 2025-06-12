using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.WebDev;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IWebDevClient
    {
        // Project management
        Task<ProjectResponse?> CreateProjectAsync(ProjectCreateRequest request);
        Task<ProjectResponse?> GetProjectAsync(string projectId);
        Task<ProjectListResponse?> ListProjectsAsync(int page = 1, int pageSize = 50, string? filter = null);
        Task<ProjectResponse?> UpdateProjectAsync(string projectId, ProjectCreateRequest request);
        Task<MessageResponse9?> DeleteProjectAsync(string projectId);
        Task<MessageResponse9?> ArchiveProjectAsync(string projectId);
        Task<MessageResponse9?> RestoreProjectAsync(string projectId);

        // Build operations
        Task<BuildResponse?> StartBuildAsync(BuildRequest request);
        Task<BuildResponse?> GetBuildAsync(string buildId);
        Task<List<BuildResponse>?> ListBuildsAsync(string projectId, int limit = 50, int offset = 0);
        Task<MessageResponse9?> CancelBuildAsync(string buildId);
        Task<MessageResponse9?> RetryBuildAsync(string buildId);
        Task<List<BuildLog>?> GetBuildLogsAsync(string buildId, string? level = null);
        Task<List<BuildArtifact>?> GetBuildArtifactsAsync(string buildId);
        Task<string?> DownloadBuildArtifactAsync(string buildId, string artifactName);

        // Deployment operations
        Task<DeploymentResponse?> DeployAsync(DeploymentRequest request);
        Task<DeploymentResponse?> GetDeploymentAsync(string deploymentId);
        Task<List<DeploymentResponse>?> ListDeploymentsAsync(string projectId, string? environment = null, int limit = 50, int offset = 0);
        Task<MessageResponse9?> RollbackDeploymentAsync(string deploymentId, string? targetDeploymentId = null);
        Task<MessageResponse9?> CancelDeploymentAsync(string deploymentId);
        Task<List<DeploymentLog>?> GetDeploymentLogsAsync(string deploymentId);
        Task<List<HealthCheckResult>?> GetHealthChecksAsync(string deploymentId);
        Task<MessageResponse9?> TriggerHealthCheckAsync(string deploymentId);

        // Environment management
        Task<MessageResponse9?> CreateEnvironmentAsync(string projectId, string environmentName, Dictionary<string, string> variables);
        Task<Dictionary<string, string>?> GetEnvironmentVariablesAsync(string projectId, string environment);
        Task<MessageResponse9?> UpdateEnvironmentVariablesAsync(string projectId, string environment, Dictionary<string, string> variables);
        Task<MessageResponse9?> DeleteEnvironmentAsync(string projectId, string environment);
        Task<List<string>?> ListEnvironmentsAsync(string projectId);

        // Domain and SSL management
        Task<MessageResponse9?> AddCustomDomainAsync(string projectId, string domain, bool sslEnabled = true);
        Task<MessageResponse9?> RemoveCustomDomainAsync(string projectId, string domain);
        Task<List<string>?> ListCustomDomainsAsync(string projectId);
        Task<MessageResponse9?> RenewSSLCertificateAsync(string projectId, string domain);
        Task<Dictionary<string, object>?> GetSSLStatusAsync(string projectId, string domain);

        // Monitoring and analytics
        Task<MonitoringResponse?> GetMonitoringDataAsync(MonitoringRequest request);
        Task<Dictionary<string, object>?> GetProjectAnalyticsAsync(string projectId, int days = 30);
        Task<List<Alert>?> GetActiveAlertsAsync(string projectId);
        Task<MessageResponse9?> AcknowledgeAlertAsync(string alertId);
        Task<MessageResponse9?> ResolveAlertAsync(string alertId);
        Task<Dictionary<string, double>?> GetPerformanceMetricsAsync(string projectId, int hours = 24);
        Task<Dictionary<string, object>?> GetUptimeStatsAsync(string projectId, int days = 30);

        // Optimization and recommendations
        Task<OptimizationResponse?> AnalyzeProjectAsync(OptimizationRequest request);
        Task<OptimizationResponse?> GetOptimizationResultAsync(string optimizationId);
        Task<MessageResponse9?> ApplyOptimizationAsync(string optimizationId, List<string> recommendationIds);
        Task<List<OptimizationRecommendation>?> GetOptimizationRecommendationsAsync(string projectId, string? category = null);
        Task<Dictionary<string, object>?> GetOptimizationHistoryAsync(string projectId);

        // File and asset management
        Task<MessageResponse9?> UploadFileAsync(string projectId, string filePath, byte[] content, string contentType);
        Task<byte[]?> DownloadFileAsync(string projectId, string filePath);
        Task<MessageResponse9?> DeleteFileAsync(string projectId, string filePath);
        Task<List<SourceFile>?> ListProjectFilesAsync(string projectId, string? directory = null);
        Task<MessageResponse9?> CreateDirectoryAsync(string projectId, string directoryPath);
        Task<MessageResponse9?> DeleteDirectoryAsync(string projectId, string directoryPath);

        // Collaboration and team management
        Task<MessageResponse9?> AddTeamMemberAsync(string projectId, string userId, string role = "developer");
        Task<MessageResponse9?> RemoveTeamMemberAsync(string projectId, string userId);
        Task<List<string>?> GetTeamMembersAsync(string projectId);
        Task<MessageResponse9?> UpdateMemberRoleAsync(string projectId, string userId, string role);
        Task<Dictionary<string, string>?> GetMemberPermissionsAsync(string projectId, string userId);

        // Backup and restore
        Task<MessageResponse9?> CreateBackupAsync(string projectId, string backupName, bool includeDatabase = false);
        Task<List<Dictionary<string, object>>?> ListBackupsAsync(string projectId);
        Task<MessageResponse9?> RestoreFromBackupAsync(string projectId, string backupId);
        Task<MessageResponse9?> DeleteBackupAsync(string backupId);
        Task<string?> ExportProjectAsync(string projectId, string format = "zip");

        // CI/CD integration
        Task<MessageResponse9?> ConnectRepositoryAsync(string projectId, string repositoryUrl, string branch = "main");
        Task<MessageResponse9?> DisconnectRepositoryAsync(string projectId);
        Task<Dictionary<string, object>?> GetRepositoryStatusAsync(string projectId);
        Task<MessageResponse9?> TriggerWebhookAsync(string projectId, string webhookType, Dictionary<string, object> payload);
        Task<List<Dictionary<string, object>>?> GetWebhookHistoryAsync(string projectId, int limit = 50);

        // Templates and scaffolding
        Task<List<Dictionary<string, object>>?> ListProjectTemplatesAsync(string? category = null);
        Task<ProjectResponse?> CreateFromTemplateAsync(string templateId, ProjectCreateRequest request);
        Task<MessageResponse9?> SaveAsTemplateAsync(string projectId, string templateName, string description);
        Task<MessageResponse9?> DeleteTemplateAsync(string templateId);

        // Performance and caching
        Task<MessageResponse9?> ClearCacheAsync(string projectId, string? cacheType = null);
        Task<Dictionary<string, object>?> GetCacheStatsAsync(string projectId);
        Task<MessageResponse9?> EnableCDNAsync(string projectId, Dictionary<string, object>? settings = null);
        Task<MessageResponse9?> DisableCDNAsync(string projectId);
        Task<Dictionary<string, object>?> GetCDNStatsAsync(string projectId);

        // Security and compliance
        Task<Dictionary<string, object>?> RunSecurityScanAsync(string projectId);
        Task<List<Dictionary<string, object>>?> GetSecurityVulnerabilitiesAsync(string projectId);
        Task<MessageResponse9?> FixSecurityIssueAsync(string projectId, string issueId);
        Task<Dictionary<string, object>?> GetComplianceReportAsync(string projectId, string standard = "GDPR");
        Task<MessageResponse9?> EnableSecurityHeadersAsync(string projectId, Dictionary<string, string> headers);

        // Logs and debugging
        Task<List<Dictionary<string, object>>?> GetApplicationLogsAsync(string projectId, DateTime? startTime = null, DateTime? endTime = null, string? level = null, int limit = 1000);
        Task<List<Dictionary<string, object>>?> GetErrorLogsAsync(string projectId, DateTime? startTime = null, DateTime? endTime = null, int limit = 100);
        Task<MessageResponse9?> EnableDebugModeAsync(string projectId, int durationMinutes = 60);
        Task<MessageResponse9?> DisableDebugModeAsync(string projectId);
        Task<Dictionary<string, object>?> GetDebugInfoAsync(string projectId);

        // Configuration and settings
        Task<Dictionary<string, object>?> GetProjectConfigAsync(string projectId);
        Task<MessageResponse9?> UpdateProjectConfigAsync(string projectId, Dictionary<string, object> config);
        Task<Dictionary<string, object>?> GetDefaultConfigAsync(string projectType);
        Task<MessageResponse9?> ResetConfigAsync(string projectId);
        Task<string?> ExportConfigAsync(string projectId);
        Task<MessageResponse9?> ImportConfigAsync(string projectId, string configData);
    }
}
