using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Maintenance;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class MaintenanceClient : IMaintenanceClient
    {
        private readonly HttpClient _httpClient;

        public MaintenanceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // System health and monitoring
        public async Task<HealthCheckResponse?> GetHealthCheckAsync(HealthCheckRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    if (request.Components?.Any() == true)
                        queryParams.Add($"components={string.Join(",", request.Components)}");
                    
                    if (request.Detailed)
                        queryParams.Add("detailed=true");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/health{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<SystemStatsResponse?> GetSystemStatsAsync(SystemStatsRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    if (!request.IncludeMetrics)
                        queryParams.Add("include_metrics=false");
                    
                    if (!request.IncludePerformance)
                        queryParams.Add("include_performance=false");
                    
                    if (!string.IsNullOrEmpty(request.TimeRange))
                        queryParams.Add($"time_range={request.TimeRange}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/system/stats{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SystemStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<LogsResponse?> GetSystemLogsAsync(LogsRequest request)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"limit={request.Limit}",
                    $"offset={request.Offset}"
                };
                
                if (!string.IsNullOrEmpty(request.Level))
                    queryParams.Add($"level={request.Level}");
                
                if (!string.IsNullOrEmpty(request.Component))
                    queryParams.Add($"component={request.Component}");
                
                if (request.StartTime.HasValue)
                    queryParams.Add($"start_time={request.StartTime.Value:yyyy-MM-ddTHH:mm:ssZ}");
                
                if (request.EndTime.HasValue)
                    queryParams.Add($"end_time={request.EndTime.Value:yyyy-MM-ddTHH:mm:ssZ}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/v3/system/logs{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<LogsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Maintenance operations
        public async Task<MaintenanceResponse?> StartMaintenanceAsync(MaintenanceRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MaintenanceResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MaintenanceResponse?> GetMaintenanceStatusAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/maintenance/{jobId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MaintenanceResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> CancelMaintenanceAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/maintenance/{jobId}/cancel", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Database maintenance
        public async Task<MaintenanceResponse?> PerformDatabaseMaintenanceAsync(DatabaseMaintenanceRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/database", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MaintenanceResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> VacuumDatabaseAsync(bool force = false)
        {
            try
            {
                var request = new DatabaseMaintenanceRequest
                {
                    Operation = "vacuum",
                    Force = force
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/database/vacuum", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> ReindexDatabaseAsync(List<string>? tables = null)
        {
            try
            {
                var request = new DatabaseMaintenanceRequest
                {
                    Operation = "reindex",
                    Tables = tables
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/database/reindex", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> AnalyzeDatabaseAsync(List<string>? tables = null)
        {
            try
            {
                var request = new DatabaseMaintenanceRequest
                {
                    Operation = "analyze",
                    Tables = tables
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/database/analyze", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Index maintenance
        public async Task<MaintenanceResponse?> PerformIndexMaintenanceAsync(IndexMaintenanceRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/indexes", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MaintenanceResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> RebuildIndexesAsync(List<string>? indexTypes = null, bool force = false)
        {
            try
            {
                var request = new IndexMaintenanceRequest
                {
                    Operation = "rebuild",
                    IndexTypes = indexTypes,
                    Force = force
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/indexes/rebuild", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> OptimizeIndexesAsync(List<string>? collectionIds = null)
        {
            try
            {
                var request = new IndexMaintenanceRequest
                {
                    Operation = "optimize",
                    CollectionIds = collectionIds
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/indexes/optimize", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Backup and restore operations
        public async Task<BackupResponse?> CreateBackupAsync(BackupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/backup", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BackupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<RestoreResponse?> RestoreFromBackupAsync(RestoreRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/restore", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RestoreResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<BackupResponse>?> ListBackupsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/maintenance/backups");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<BackupResponse>>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<BackupResponse?> GetBackupStatusAsync(string backupId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/maintenance/backup/{backupId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BackupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> DeleteBackupAsync(string backupId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/v3/maintenance/backup/{backupId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Cleanup operations
        public async Task<CleanupResponse?> PerformCleanupAsync(CleanupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/cleanup", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CleanupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CleanupResponse?> CleanupOrphanedDataAsync(bool dryRun = true)
        {
            try
            {
                var request = new CleanupRequest
                {
                    CleanupType = "orphaned",
                    DryRun = dryRun
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/cleanup/orphaned", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CleanupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CleanupResponse?> CleanupExpiredDataAsync(string olderThan = "30d", bool dryRun = true)
        {
            try
            {
                var request = new CleanupRequest
                {
                    CleanupType = "expired",
                    OlderThan = olderThan,
                    DryRun = dryRun
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/cleanup/expired", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CleanupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CleanupResponse?> CleanupTemporaryFilesAsync(bool dryRun = true)
        {
            try
            {
                var request = new CleanupRequest
                {
                    CleanupType = "temporary",
                    DryRun = dryRun
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/cleanup/temporary", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CleanupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<CleanupResponse?> CleanupLogsAsync(string olderThan = "7d", bool dryRun = true)
        {
            try
            {
                var request = new CleanupRequest
                {
                    CleanupType = "logs",
                    OlderThan = olderThan,
                    DryRun = dryRun
                };
                var response = await _httpClient.PostAsJsonAsync("/v3/maintenance/cleanup/logs", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CleanupResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // System control
        public async Task<MessageResponse3?> RestartSystemAsync(bool force = false)
        {
            try
            {
                var request = new { force };
                var response = await _httpClient.PostAsJsonAsync("/v3/system/restart", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> ShutdownSystemAsync(bool force = false)
        {
            try
            {
                var request = new { force };
                var response = await _httpClient.PostAsJsonAsync("/v3/system/shutdown", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> ReloadConfigurationAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/system/reload-config", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Performance optimization
        public async Task<MessageResponse3?> OptimizePerformanceAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/maintenance/optimize", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> ClearCachesAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/maintenance/clear-caches", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse3?> CompactDatabaseAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/v3/maintenance/compact", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse3>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
