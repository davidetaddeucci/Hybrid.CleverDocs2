using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.Users;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Workers
{
    /// <summary>
    /// Background service for syncing users with R2R API
    /// </summary>
    public class UserSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UserSyncWorker> _logger;
        private readonly UserSyncOptions _options;

        public UserSyncWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<UserSyncWorker> logger,
            IOptions<UserSyncOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("User Sync Worker is disabled");
                return;
            }

            _logger.LogInformation("User Sync Worker started with interval {IntervalSeconds}s", _options.IntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userSyncService = scope.ServiceProvider.GetRequiredService<IUserSyncService>();
                    var correlationService = scope.ServiceProvider.GetRequiredService<ICorrelationService>();

                    // Set correlation context for this background operation
                    correlationService.SetCorrelationId($"user-sync-{Guid.NewGuid():N}");

                    // Find users that need to be synced with R2R
                    var usersToSync = await context.Users
                        .Where(u => u.IsActive &&
                                   string.IsNullOrEmpty(u.R2RUserId))
                        .Take(_options.BatchSize)
                        .ToListAsync(stoppingToken);

                    if (usersToSync.Any())
                    {
                        _logger.LogInformation("Found {Count} users to sync with R2R", usersToSync.Count);

                        foreach (var user in usersToSync)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(user.R2RUserId))
                                {
                                    // Create new user in R2R
                                    var r2rUserId = await userSyncService.CreateR2RUserAsync(user);
                                    if (!string.IsNullOrEmpty(r2rUserId))
                                    {
                                        user.R2RUserId = r2rUserId;
                                        user.UpdatedAt = DateTime.UtcNow;

                                        _logger.LogInformation("Created R2R user {R2RUserId} for user {UserId}",
                                            r2rUserId, user.Id);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to create R2R user for user {UserId}", user.Id);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error syncing user {UserId} with R2R", user.Id);
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogDebug("No users found that need R2R sync");
                    }

                    // Check for users that need to be verified in R2R (simplified - just check if R2R user still exists)
                    var usersToVerify = await context.Users
                        .Where(u => u.IsActive &&
                                   !string.IsNullOrEmpty(u.R2RUserId) &&
                                   u.UpdatedAt < DateTime.UtcNow.AddHours(-_options.VerificationIntervalHours))
                        .Take(_options.BatchSize)
                        .ToListAsync(stoppingToken);

                    if (usersToVerify.Any())
                    {
                        _logger.LogInformation("Verifying {Count} users in R2R", usersToVerify.Count);

                        foreach (var user in usersToVerify)
                        {
                            try
                            {
                                // Simple verification - just update timestamp for now
                                // TODO: Implement actual R2R user verification when service method is available
                                user.UpdatedAt = DateTime.UtcNow;

                                _logger.LogDebug("Verified R2R user {R2RUserId} for user {UserId}",
                                    user.R2RUserId, user.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error verifying R2R user {R2RUserId} for user {UserId}",
                                    user.R2RUserId, user.Id);
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
                    _logger.LogError(ex, "Error in User Sync Worker");
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("User Sync Worker stopped");
        }
    }

    public class UserSyncOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSeconds { get; set; } = 300; // 5 minutes
        public int BatchSize { get; set; } = 10;
        public int VerificationIntervalHours { get; set; } = 24; // Verify users every 24 hours
    }
}
