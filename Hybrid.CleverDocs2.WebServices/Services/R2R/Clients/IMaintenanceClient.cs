using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Maintenance;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IMaintenanceClient
    {
        // System health and monitoring
        Task<HealthCheckResponse?> GetHealthCheckAsync(HealthCheckRequest? request = null);
        Task<SystemStatsResponse?> GetSystemStatsAsync(SystemStatsRequest? request = null);
        Task<LogsResponse?> GetSystemLogsAsync(LogsRequest request);

        // Maintenance operations
        Task<MaintenanceResponse?> StartMaintenanceAsync(MaintenanceRequest request);
        Task<MaintenanceResponse?> GetMaintenanceStatusAsync(string jobId);
        Task<MessageResponse3?> CancelMaintenanceAsync(string jobId);

        // Database maintenance
        Task<MaintenanceResponse?> PerformDatabaseMaintenanceAsync(DatabaseMaintenanceRequest request);
        Task<MessageResponse3?> VacuumDatabaseAsync(bool force = false);
        Task<MessageResponse3?> ReindexDatabaseAsync(List<string>? tables = null);
        Task<MessageResponse3?> AnalyzeDatabaseAsync(List<string>? tables = null);

        // Index maintenance
        Task<MaintenanceResponse?> PerformIndexMaintenanceAsync(IndexMaintenanceRequest request);
        Task<MessageResponse3?> RebuildIndexesAsync(List<string>? indexTypes = null, bool force = false);
        Task<MessageResponse3?> OptimizeIndexesAsync(List<string>? collectionIds = null);

        // Backup and restore operations
        Task<BackupResponse?> CreateBackupAsync(BackupRequest request);
        Task<RestoreResponse?> RestoreFromBackupAsync(RestoreRequest request);
        Task<List<BackupResponse>?> ListBackupsAsync();
        Task<BackupResponse?> GetBackupStatusAsync(string backupId);
        Task<MessageResponse3?> DeleteBackupAsync(string backupId);

        // Cleanup operations
        Task<CleanupResponse?> PerformCleanupAsync(CleanupRequest request);
        Task<CleanupResponse?> CleanupOrphanedDataAsync(bool dryRun = true);
        Task<CleanupResponse?> CleanupExpiredDataAsync(string olderThan = "30d", bool dryRun = true);
        Task<CleanupResponse?> CleanupTemporaryFilesAsync(bool dryRun = true);
        Task<CleanupResponse?> CleanupLogsAsync(string olderThan = "7d", bool dryRun = true);

        // System control
        Task<MessageResponse3?> RestartSystemAsync(bool force = false);
        Task<MessageResponse3?> ShutdownSystemAsync(bool force = false);
        Task<MessageResponse3?> ReloadConfigurationAsync();

        // Performance optimization
        Task<MessageResponse3?> OptimizePerformanceAsync();
        Task<MessageResponse3?> ClearCachesAsync();
        Task<MessageResponse3?> CompactDatabaseAsync();
    }
}
