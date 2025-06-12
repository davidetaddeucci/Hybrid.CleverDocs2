using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Maintenance;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/maintenance")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceClient _client;
        public MaintenanceController(IMaintenanceClient client) => _client = client;

        // System health and monitoring
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthCheck([FromQuery] HealthCheckRequest? request = null) => Ok(await _client.GetHealthCheckAsync(request));

        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats([FromQuery] SystemStatsRequest? request = null) => Ok(await _client.GetSystemStatsAsync(request));

        [HttpGet("logs")]
        public async Task<IActionResult> GetSystemLogs([FromQuery] LogsRequest request) => Ok(await _client.GetSystemLogsAsync(request));

        // Maintenance operations
        [HttpPost]
        public async Task<IActionResult> StartMaintenance(MaintenanceRequest request) => Ok(await _client.StartMaintenanceAsync(request));

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetMaintenanceStatus(string jobId) => Ok(await _client.GetMaintenanceStatusAsync(jobId));

        [HttpPost("{jobId}/cancel")]
        public async Task<IActionResult> CancelMaintenance(string jobId) => Ok(await _client.CancelMaintenanceAsync(jobId));

        // Database maintenance
        [HttpPost("database")]
        public async Task<IActionResult> PerformDatabaseMaintenance(DatabaseMaintenanceRequest request) => Ok(await _client.PerformDatabaseMaintenanceAsync(request));

        [HttpPost("database/vacuum")]
        public async Task<IActionResult> VacuumDatabase([FromQuery] bool force = false) => Ok(await _client.VacuumDatabaseAsync(force));

        [HttpPost("database/reindex")]
        public async Task<IActionResult> ReindexDatabase([FromBody] List<string>? tables = null) => Ok(await _client.ReindexDatabaseAsync(tables));

        [HttpPost("database/analyze")]
        public async Task<IActionResult> AnalyzeDatabase([FromBody] List<string>? tables = null) => Ok(await _client.AnalyzeDatabaseAsync(tables));

        // Index maintenance
        [HttpPost("indexes")]
        public async Task<IActionResult> PerformIndexMaintenance(IndexMaintenanceRequest request) => Ok(await _client.PerformIndexMaintenanceAsync(request));

        [HttpPost("indexes/rebuild")]
        public async Task<IActionResult> RebuildIndexes([FromBody] RebuildIndexesRequest request) => Ok(await _client.RebuildIndexesAsync(request.IndexTypes, request.Force));

        [HttpPost("indexes/optimize")]
        public async Task<IActionResult> OptimizeIndexes([FromBody] List<string>? collectionIds = null) => Ok(await _client.OptimizeIndexesAsync(collectionIds));

        // Backup and restore operations
        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup(BackupRequest request) => Ok(await _client.CreateBackupAsync(request));

        [HttpPost("restore")]
        public async Task<IActionResult> RestoreFromBackup(RestoreRequest request) => Ok(await _client.RestoreFromBackupAsync(request));

        [HttpGet("backups")]
        public async Task<IActionResult> ListBackups() => Ok(await _client.ListBackupsAsync());

        [HttpGet("backup/{backupId}")]
        public async Task<IActionResult> GetBackupStatus(string backupId) => Ok(await _client.GetBackupStatusAsync(backupId));

        [HttpDelete("backup/{backupId}")]
        public async Task<IActionResult> DeleteBackup(string backupId) => Ok(await _client.DeleteBackupAsync(backupId));

        // Cleanup operations
        [HttpPost("cleanup")]
        public async Task<IActionResult> PerformCleanup(CleanupRequest request) => Ok(await _client.PerformCleanupAsync(request));

        [HttpPost("cleanup/orphaned")]
        public async Task<IActionResult> CleanupOrphanedData([FromQuery] bool dryRun = true) => Ok(await _client.CleanupOrphanedDataAsync(dryRun));

        [HttpPost("cleanup/expired")]
        public async Task<IActionResult> CleanupExpiredData([FromQuery] string olderThan = "30d", [FromQuery] bool dryRun = true) => Ok(await _client.CleanupExpiredDataAsync(olderThan, dryRun));

        [HttpPost("cleanup/temporary")]
        public async Task<IActionResult> CleanupTemporaryFiles([FromQuery] bool dryRun = true) => Ok(await _client.CleanupTemporaryFilesAsync(dryRun));

        [HttpPost("cleanup/logs")]
        public async Task<IActionResult> CleanupLogs([FromQuery] string olderThan = "7d", [FromQuery] bool dryRun = true) => Ok(await _client.CleanupLogsAsync(olderThan, dryRun));

        // System control
        [HttpPost("system/restart")]
        public async Task<IActionResult> RestartSystem([FromQuery] bool force = false) => Ok(await _client.RestartSystemAsync(force));

        [HttpPost("system/shutdown")]
        public async Task<IActionResult> ShutdownSystem([FromQuery] bool force = false) => Ok(await _client.ShutdownSystemAsync(force));

        [HttpPost("system/reload-config")]
        public async Task<IActionResult> ReloadConfiguration() => Ok(await _client.ReloadConfigurationAsync());

        // Performance optimization
        [HttpPost("optimize")]
        public async Task<IActionResult> OptimizePerformance() => Ok(await _client.OptimizePerformanceAsync());

        [HttpPost("clear-caches")]
        public async Task<IActionResult> ClearCaches() => Ok(await _client.ClearCachesAsync());

        [HttpPost("compact")]
        public async Task<IActionResult> CompactDatabase() => Ok(await _client.CompactDatabaseAsync());
    }

    // Helper DTOs for controller endpoints
    public class RebuildIndexesRequest
    {
        public List<string>? IndexTypes { get; set; }
        public bool Force { get; set; } = false;
    }
}
