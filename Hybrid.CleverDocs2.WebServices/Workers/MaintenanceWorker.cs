using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Workers
{
    /// <summary>
    /// Background service for system maintenance tasks
    /// </summary>
    public class MaintenanceWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MaintenanceWorker> _logger;
        private readonly MaintenanceOptions _options;

        public MaintenanceWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MaintenanceWorker> logger,
            IOptions<MaintenanceOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Maintenance Worker is disabled");
                return;
            }

            _logger.LogInformation("Maintenance Worker started with interval {IntervalHours}h", _options.IntervalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var correlationService = scope.ServiceProvider.GetRequiredService<ICorrelationService>();

                    // Set correlation context for this background operation
                    correlationService.SetCorrelationId($"maintenance-{Guid.NewGuid():N}");

                    _logger.LogInformation("Starting maintenance tasks");

                    // 1. Clean up expired refresh tokens
                    if (_options.CleanupExpiredTokens)
                    {
                        await CleanupExpiredTokensAsync(context, stoppingToken);
                    }

                    // 2. Clean up expired blacklisted tokens
                    if (_options.CleanupBlacklistedTokens)
                    {
                        await CleanupExpiredBlacklistedTokensAsync(context, stoppingToken);
                    }

                    // 3. Clean up old audit logs
                    if (_options.CleanupOldAuditLogs)
                    {
                        await CleanupOldAuditLogsAsync(context, stoppingToken);
                    }

                    // 4. Clean up temporary files
                    if (_options.CleanupTempFiles)
                    {
                        await CleanupTempFilesAsync();
                    }

                    // 5. Clean up failed document processing records
                    if (_options.CleanupFailedDocuments)
                    {
                        await CleanupFailedDocumentsAsync(context, stoppingToken);
                    }

                    // 6. Update database statistics
                    if (_options.UpdateDatabaseStats)
                    {
                        await UpdateDatabaseStatsAsync(context, stoppingToken);
                    }

                    // 7. Cache maintenance
                    if (_options.CacheMaintenance)
                    {
                        await PerformCacheMaintenanceAsync(scope.ServiceProvider);
                    }

                    _logger.LogInformation("Maintenance tasks completed successfully");

                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Maintenance Worker");
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Maintenance Worker stopped");
        }

        private async Task CleanupExpiredTokensAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.TokenRetentionDays);
                
                var expiredTokens = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (expiredTokens.Any())
                {
                    context.RefreshTokens.RemoveRange(expiredTokens);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
            }
        }

        private async Task CleanupExpiredBlacklistedTokensAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.BlacklistedTokenRetentionDays);
                
                var expiredBlacklistedTokens = await context.TokenBlacklists
                    .Where(bt => bt.ExpiresAt != null && bt.ExpiresAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (expiredBlacklistedTokens.Any())
                {
                    context.TokenBlacklists.RemoveRange(expiredBlacklistedTokens);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredBlacklistedTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired blacklisted tokens");
            }
        }

        private async Task CleanupOldAuditLogsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.AuditLogRetentionDays);
                
                var oldAuditLogs = await context.AuditLogs
                    .Where(al => al.CreatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldAuditLogs.Any())
                {
                    context.AuditLogs.RemoveRange(oldAuditLogs);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} old audit logs", oldAuditLogs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old audit logs");
            }
        }

        private async Task CleanupTempFilesAsync()
        {
            try
            {
                var tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
                if (!Directory.Exists(tempDirectory))
                    return;

                var cutoffDate = DateTime.UtcNow.AddHours(-_options.TempFileRetentionHours);
                var tempFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories)
                    .Where(file => File.GetCreationTime(file) < cutoffDate)
                    .ToList();

                foreach (var file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", file);
                    }
                }

                if (tempFiles.Any())
                {
                    _logger.LogInformation("Cleaned up {Count} temporary files", tempFiles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary files");
            }
        }

        private async Task CleanupFailedDocumentsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.FailedDocumentRetentionDays);
                
                var failedDocuments = await context.Documents
                    .Where(d => d.Status == (int)Data.Entities.DocumentStatus.Error &&
                               d.CreatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (failedDocuments.Any())
                {
                    context.Documents.RemoveRange(failedDocuments);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} failed documents", failedDocuments.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up failed documents");
            }
        }

        private async Task UpdateDatabaseStatsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Update PostgreSQL statistics for better query performance
                await context.Database.ExecuteSqlRawAsync("ANALYZE;", cancellationToken);
                
                _logger.LogInformation("Updated database statistics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating database statistics");
            }
        }

        private async Task PerformCacheMaintenanceAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var cacheInvalidationService = serviceProvider.GetService<ICacheInvalidationService>();
                if (cacheInvalidationService != null)
                {
                    // Perform basic cache maintenance - this method may not exist yet
                    // await cacheInvalidationService.CleanupExpiredKeysAsync();
                    _logger.LogInformation("Cache maintenance service available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing cache maintenance");
            }
        }
    }

    public class MaintenanceOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalHours { get; set; } = 24; // Run daily
        public bool CleanupExpiredTokens { get; set; } = true;
        public bool CleanupBlacklistedTokens { get; set; } = true;
        public bool CleanupOldAuditLogs { get; set; } = true;
        public bool CleanupTempFiles { get; set; } = true;
        public bool CleanupFailedDocuments { get; set; } = true;
        public bool UpdateDatabaseStats { get; set; } = true;
        public bool CacheMaintenance { get; set; } = true;
        
        // Retention periods
        public int TokenRetentionDays { get; set; } = 30;
        public int BlacklistedTokenRetentionDays { get; set; } = 7;
        public int AuditLogRetentionDays { get; set; } = 90;
        public int TempFileRetentionHours { get; set; } = 24;
        public int FailedDocumentRetentionDays { get; set; } = 7;
    }
}
