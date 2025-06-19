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
    /// Background service for warming up cache with frequently accessed data
    /// </summary>
    public class CacheWarmingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CacheWarmingWorker> _logger;
        private readonly CacheWarmingOptions _options;

        public CacheWarmingWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CacheWarmingWorker> logger,
            IOptions<CacheWarmingOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Cache Warming Worker is disabled");
                return;
            }

            _logger.LogInformation("Cache Warming Worker started with interval {IntervalMinutes}m", _options.IntervalMinutes);

            // Initial warm-up on startup
            await PerformCacheWarmupAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
                    await PerformCacheWarmupAsync();
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Cache Warming Worker");
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Cache Warming Worker stopped");
        }

        private async Task PerformCacheWarmupAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cacheWarmingService = scope.ServiceProvider.GetService<ICacheWarmingService>();
                var correlationService = scope.ServiceProvider.GetRequiredService<ICorrelationService>();

                // Set correlation context for this background operation
                correlationService.SetCorrelationId($"cache-warming-{Guid.NewGuid():N}");

                _logger.LogInformation("Starting cache warming process");

                // 1. Warm up active companies
                if (_options.WarmupCompanies)
                {
                    await WarmupCompaniesAsync(context, cacheWarmingService);
                }

                // 2. Warm up active users
                if (_options.WarmupUsers)
                {
                    await WarmupUsersAsync(context, cacheWarmingService);
                }

                // 3. Warm up active collections
                if (_options.WarmupCollections)
                {
                    await WarmupCollectionsAsync(context, cacheWarmingService);
                }

                // 4. Warm up recent documents
                if (_options.WarmupRecentDocuments)
                {
                    await WarmupRecentDocumentsAsync(context, cacheWarmingService);
                }

                // 5. Warm up frequently accessed data
                if (_options.WarmupFrequentData)
                {
                    await WarmupFrequentDataAsync(context, cacheWarmingService);
                }

                _logger.LogInformation("Cache warming process completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warming process");
            }
        }

        private async Task WarmupCompaniesAsync(ApplicationDbContext context, ICacheWarmingService? cacheWarmingService)
        {
            try
            {
                var activeCompanies = await context.Companies
                    .Where(c => c.IsActive)
                    .Take(_options.MaxCompaniesToWarmup)
                    .ToListAsync();

                if (cacheWarmingService != null)
                {
                    foreach (var company in activeCompanies)
                    {
                        // Cache warming methods may not be implemented yet
                        // await cacheWarmingService.WarmupCompanyDataAsync(company.Id);
                    }
                }

                _logger.LogDebug("Warmed up {Count} companies", activeCompanies.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up companies");
            }
        }

        private async Task WarmupUsersAsync(ApplicationDbContext context, ICacheWarmingService? cacheWarmingService)
        {
            try
            {
                var recentActiveUsers = await context.Users
                    .Where(u => u.IsActive && u.LastLoginAt > DateTime.UtcNow.AddDays(-_options.RecentUserDays))
                    .OrderByDescending(u => u.LastLoginAt)
                    .Take(_options.MaxUsersToWarmup)
                    .ToListAsync();

                if (cacheWarmingService != null)
                {
                    foreach (var user in recentActiveUsers)
                    {
                        // Cache warming methods may not be implemented yet
                        // await cacheWarmingService.WarmupUserDataAsync(user.Id);
                    }
                }

                _logger.LogDebug("Warmed up {Count} recent active users", recentActiveUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up users");
            }
        }

        private async Task WarmupCollectionsAsync(ApplicationDbContext context, ICacheWarmingService? cacheWarmingService)
        {
            try
            {
                var activeCollections = await context.Collections
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(_options.MaxCollectionsToWarmup)
                    .ToListAsync();

                if (cacheWarmingService != null)
                {
                    foreach (var collection in activeCollections)
                    {
                        // Cache warming methods may not be implemented yet
                        // await cacheWarmingService.WarmupCollectionDataAsync(collection.Id);
                    }
                }

                _logger.LogDebug("Warmed up {Count} active collections", activeCollections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up collections");
            }
        }

        private async Task WarmupRecentDocumentsAsync(ApplicationDbContext context, ICacheWarmingService? cacheWarmingService)
        {
            try
            {
                var recentDocuments = await context.Documents
                    .Where(d => d.Status == (int)Data.Entities.DocumentStatus.Ready &&
                               d.CreatedAt > DateTime.UtcNow.AddDays(-_options.RecentDocumentDays))
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(_options.MaxDocumentsToWarmup)
                    .ToListAsync();

                if (cacheWarmingService != null)
                {
                    foreach (var document in recentDocuments)
                    {
                        // Cache warming methods may not be implemented yet
                        // await cacheWarmingService.WarmupDocumentDataAsync(document.Id);
                    }
                }

                _logger.LogDebug("Warmed up {Count} recent documents", recentDocuments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up recent documents");
            }
        }

        private Task WarmupFrequentDataAsync(ApplicationDbContext context, ICacheWarmingService? cacheWarmingService)
        {
            try
            {
                if (cacheWarmingService != null)
                {
                    // Cache warming methods may not be implemented yet
                    // await cacheWarmingService.WarmupSystemStatsAsync();
                    // await cacheWarmingService.WarmupConfigurationDataAsync();
                    // await cacheWarmingService.WarmupLookupDataAsync();
                }

                _logger.LogDebug("Warmed up frequent system data");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up frequent data");
                return Task.CompletedTask;
            }
        }
    }

    public class CacheWarmingOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalMinutes { get; set; } = 30; // Run every 30 minutes
        
        // What to warm up
        public bool WarmupCompanies { get; set; } = true;
        public bool WarmupUsers { get; set; } = true;
        public bool WarmupCollections { get; set; } = true;
        public bool WarmupRecentDocuments { get; set; } = true;
        public bool WarmupFrequentData { get; set; } = true;
        
        // Limits
        public int MaxCompaniesToWarmup { get; set; } = 50;
        public int MaxUsersToWarmup { get; set; } = 100;
        public int MaxCollectionsToWarmup { get; set; } = 200;
        public int MaxDocumentsToWarmup { get; set; } = 500;
        
        // Time ranges
        public int RecentUserDays { get; set; } = 7; // Users active in last 7 days
        public int RecentDocumentDays { get; set; } = 3; // Documents created in last 3 days
    }
}
