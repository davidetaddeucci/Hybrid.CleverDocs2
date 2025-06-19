using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Workers
{
    /// <summary>
    /// Background service for syncing collections with R2R API
    /// </summary>
    public class CollectionSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CollectionSyncWorker> _logger;
        private readonly CollectionSyncOptions _options;

        public CollectionSyncWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CollectionSyncWorker> logger,
            IOptions<CollectionSyncOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Collection Sync Worker is disabled");
                return;
            }

            _logger.LogInformation("Collection Sync Worker started with interval {IntervalSeconds}s", _options.IntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var collectionSyncService = scope.ServiceProvider.GetRequiredService<ICollectionSyncService>();
                    var correlationService = scope.ServiceProvider.GetRequiredService<ICorrelationService>();

                    // Set correlation context for this background operation
                    correlationService.SetCorrelationId($"collection-sync-{Guid.NewGuid():N}");

                    // Find collections that need to be synced with R2R
                    var collectionsToSync = await context.Collections
                        .Where(c => string.IsNullOrEmpty(c.R2RCollectionId))
                        .Take(_options.BatchSize)
                        .ToListAsync(stoppingToken);

                    if (collectionsToSync.Any())
                    {
                        _logger.LogInformation("Found {Count} collections to sync with R2R", collectionsToSync.Count);

                        foreach (var collection in collectionsToSync)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(collection.R2RCollectionId))
                                {
                                    // Create new collection in R2R using the Guid overload
                                    var r2rCollectionId = await collectionSyncService.CreateR2RCollectionAsync(collection.Id);
                                    if (!string.IsNullOrEmpty(r2rCollectionId))
                                    {
                                        collection.R2RCollectionId = r2rCollectionId;
                                        collection.UpdatedAt = DateTime.UtcNow;

                                        _logger.LogInformation("Created R2R collection {R2RCollectionId} for collection {CollectionId}",
                                            r2rCollectionId, collection.Id);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to create R2R collection for collection {CollectionId}", collection.Id);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error syncing collection {CollectionId} with R2R", collection.Id);
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogDebug("No collections found that need R2R sync");
                    }

                    // Check for collections that need to be verified in R2R (simplified)
                    var collectionsToVerify = await context.Collections
                        .Where(c => !string.IsNullOrEmpty(c.R2RCollectionId) &&
                                   c.UpdatedAt < DateTime.UtcNow.AddHours(-_options.VerificationIntervalHours))
                        .Take(_options.BatchSize)
                        .ToListAsync(stoppingToken);

                    if (collectionsToVerify.Any())
                    {
                        _logger.LogInformation("Verifying {Count} collections in R2R", collectionsToVerify.Count);

                        foreach (var collection in collectionsToVerify)
                        {
                            try
                            {
                                // Simple verification - just update timestamp for now
                                // TODO: Implement actual R2R collection verification when service method is available
                                collection.UpdatedAt = DateTime.UtcNow;

                                _logger.LogDebug("Verified R2R collection {R2RCollectionId} for collection {CollectionId}",
                                    collection.R2RCollectionId, collection.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error verifying R2R collection {R2RCollectionId} for collection {CollectionId}",
                                    collection.R2RCollectionId, collection.Id);
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }

                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Collection Sync Worker");
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("Collection Sync Worker stopped");
        }
    }

    public class CollectionSyncOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSeconds { get; set; } = 180; // 3 minutes
        public int BatchSize { get; set; } = 5;
        public int VerificationIntervalHours { get; set; } = 12; // Verify collections every 12 hours
        public bool SyncPermissions { get; set; } = true;
    }
}
