using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.Users;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("admin")]
    // [Authorize(Roles = "Admin")] // Temporarily disabled for R2R sync testing
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;
        private readonly IMultiLevelCacheService _cacheService;
        private readonly ICorrelationService _correlationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IUserSyncService userSyncService,
            IMultiLevelCacheService cacheService,
            ICorrelationService correlationService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userSyncService = userSyncService;
            _cacheService = cacheService;
            _correlationService = correlationService;
            _logger = logger;
        }

        [HttpGet("companies/count")]
        public async Task<ActionResult<int>> GetCompaniesCount()
        {
            try
            {
                var count = await _context.Companies.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("users/count")]
        public async Task<ActionResult<int>> GetUsersCount()
        {
            try
            {
                var count = await _context.Users.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("documents/count")]
        public async Task<ActionResult<int>> GetDocumentsCount()
        {
            try
            {
                var count = await _context.Documents.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("collections/count")]
        public async Task<ActionResult<int>> GetCollectionsCount()
        {
            try
            {
                var count = await _context.Collections.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collections count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("companies/stats")]
        public async Task<ActionResult<object>> GetCompaniesStats()
        {
            try
            {
                var stats = new
                {
                    total = await _context.Companies.CountAsync(),
                    active = await _context.Companies.CountAsync(c => c.IsActive),
                    inactive = await _context.Companies.CountAsync(c => !c.IsActive)
                };
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies stats");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("activities/recent")]
        public async Task<ActionResult<object[]>> GetRecentActivities()
        {
            try
            {
                var activities = await _context.AuditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new
                    {
                        id = a.Id,
                        action = a.Action,
                        entityType = a.EntityType,
                        details = a.Details,
                        createdAt = a.CreatedAt,
                        userId = a.UserId
                    })
                    .ToArrayAsync();
                
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Synchronize all users with R2R after a clean R2R reset
        /// </summary>
        [HttpPost("sync-all-users-r2r")]
        public async Task<IActionResult> SyncAllUsersWithR2R()
        {
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogInformation("🔄 Starting R2R user synchronization for all users, CorrelationId: {CorrelationId}", correlationId);

            try
            {
                // 1. Get all active users that need R2R sync
                var usersToSync = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.IsActive && (string.IsNullOrEmpty(u.R2RUserId)))
                    .OrderBy(u => u.CompanyId)
                    .ThenBy(u => u.Email)
                    .ToListAsync();

                _logger.LogInformation("📊 Found {UserCount} users to sync with R2R, CorrelationId: {CorrelationId}",
                    usersToSync.Count, correlationId);

                var results = new List<object>();
                var successCount = 0;
                var errorCount = 0;

                // 2. Clear user mapping cache
                _logger.LogInformation("🧹 Clearing user mapping cache, CorrelationId: {CorrelationId}", correlationId);
                foreach (var user in usersToSync)
                {
                    try
                    {
                        await _cacheService.RemoveAsync($"r2r:user:mapping:{user.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to clear cache for user {UserId}, CorrelationId: {CorrelationId}",
                            user.Id, correlationId);
                    }
                }

                // 3. Sync each user with R2R
                foreach (var user in usersToSync)
                {
                    try
                    {
                        _logger.LogInformation("👤 Syncing user {Email} (ID: {UserId}) with R2R, CorrelationId: {CorrelationId}",
                            user.Email, user.Id, correlationId);

                        // Ensure Name field is populated for R2R compatibility
                        if (string.IsNullOrEmpty(user.Name))
                        {
                            user.Name = $"{user.FirstName} {user.LastName}".Trim();
                            if (string.IsNullOrEmpty(user.Name))
                            {
                                user.Name = user.Email.Split('@')[0]; // Fallback to email prefix
                            }
                        }

                        // Create user in R2R
                        var r2rUserId = await _userSyncService.CreateR2RUserAsync(user);

                        if (!string.IsNullOrEmpty(r2rUserId))
                        {
                            // Update database with new R2R user ID
                            user.R2RUserId = r2rUserId;
                            user.UpdatedAt = DateTime.UtcNow;

                            successCount++;
                            results.Add(new
                            {
                                UserId = user.Id,
                                Email = user.Email,
                                R2RUserId = r2rUserId,
                                Status = "Success",
                                Company = user.Company?.Name
                            });

                            _logger.LogInformation("✅ Successfully created R2R user {R2RUserId} for {Email}, CorrelationId: {CorrelationId}",
                                r2rUserId, user.Email, correlationId);
                        }
                        else
                        {
                            errorCount++;
                            results.Add(new
                            {
                                UserId = user.Id,
                                Email = user.Email,
                                R2RUserId = (string?)null,
                                Status = "Failed - No R2R ID returned",
                                Company = user.Company?.Name
                            });

                            _logger.LogError("❌ Failed to create R2R user for {Email} - no ID returned, CorrelationId: {CorrelationId}",
                                user.Email, correlationId);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        results.Add(new
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            R2RUserId = (string?)null,
                            Status = $"Error: {ex.Message}",
                            Company = user.Company?.Name
                        });

                        _logger.LogError(ex, "❌ Error syncing user {Email} with R2R, CorrelationId: {CorrelationId}",
                            user.Email, correlationId);
                    }
                }

                // 4. Save all changes to database
                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("💾 Saved {SuccessCount} user R2R mappings to database, CorrelationId: {CorrelationId}",
                        successCount, correlationId);
                }

                // 5. Return summary
                var summary = new
                {
                    TotalUsers = usersToSync.Count,
                    SuccessCount = successCount,
                    ErrorCount = errorCount,
                    CorrelationId = correlationId,
                    Results = results
                };

                _logger.LogInformation("🎯 R2R user synchronization completed: {SuccessCount} success, {ErrorCount} errors, CorrelationId: {CorrelationId}",
                    successCount, errorCount, correlationId);

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Critical error during R2R user synchronization, CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new {
                    Error = "Critical error during synchronization",
                    Message = ex.Message,
                    CorrelationId = correlationId
                });
            }
        }

        /// <summary>
        /// Verify R2R user synchronization status
        /// </summary>
        [HttpGet("verify-r2r-sync")]
        public async Task<IActionResult> VerifyR2RSync()
        {
            try
            {
                var activeUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.R2RUserId,
                        HasR2RUserId = !string.IsNullOrEmpty(u.R2RUserId),
                        u.UpdatedAt
                    })
                    .ToListAsync();

                var syncedCount = activeUsers.Count(u => u.HasR2RUserId);
                var unsyncedCount = activeUsers.Count(u => !u.HasR2RUserId);

                return Ok(new
                {
                    TotalActiveUsers = activeUsers.Count,
                    SyncedUsers = syncedCount,
                    UnsyncedUsers = unsyncedCount,
                    SyncPercentage = activeUsers.Count > 0 ? (syncedCount * 100.0 / activeUsers.Count) : 0,
                    Users = activeUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying R2R sync status");
                return StatusCode(500, new { Error = "Error verifying sync status", Message = ex.Message });
            }
        }
    }
}