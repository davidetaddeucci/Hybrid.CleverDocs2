using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using System.Diagnostics;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            ICacheService cacheService,
            IDashboardService dashboardService,
            ILogger<PerformanceController> logger)
        {
            _cacheService = cacheService;
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("cache-status")]
        public async Task<IActionResult> GetCacheStatus()
        {
            try
            {
                var cacheKeys = new[]
                {
                    CacheKeys.ADMIN_DASHBOARD,
                    CacheKeys.ADMIN_COMPANIES_COUNT,
                    CacheKeys.ADMIN_USERS_COUNT,
                    CacheKeys.ADMIN_DOCUMENTS_COUNT,
                    CacheKeys.SYSTEM_HEALTH
                };

                var cacheStatus = new Dictionary<string, object>();
                
                foreach (var key in cacheKeys)
                {
                    var exists = await _cacheService.ExistsAsync(key);
                    cacheStatus[key] = new { exists, key };
                }

                return Ok(new
                {
                    success = true,
                    cacheStatus,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache status");
                return StatusCode(500, new { success = false, message = "Error getting cache status" });
            }
        }

        [HttpPost("warm-cache")]
        public async Task<IActionResult> WarmCache()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                var userIdClaim = User.FindFirst("UserId")?.Value;

                Guid? companyId = null;
                Guid? userId = null;

                if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var cId))
                    companyId = cId;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uId))
                    userId = uId;

                await _dashboardService.WarmUpCacheAsync(userId, companyId);

                stopwatch.Stop();
                _logger.LogInformation("Cache warmed up in {ElapsedMs}ms for User: {UserId}, Company: {CompanyId}", 
                    stopwatch.ElapsedMilliseconds, userId, companyId);

                return Ok(new
                {
                    success = true,
                    message = "Cache warmed up successfully",
                    elapsedMs = stopwatch.ElapsedMilliseconds,
                    userId,
                    companyId
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error warming cache after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { success = false, message = "Error warming cache" });
            }
        }

        [HttpDelete("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                var userIdClaim = User.FindFirst("UserId")?.Value;

                Guid? companyId = null;
                Guid? userId = null;

                if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var cId))
                    companyId = cId;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uId))
                    userId = uId;

                await _dashboardService.InvalidateDashboardCacheAsync(userId, companyId);

                _logger.LogInformation("Cache cleared for User: {UserId}, Company: {CompanyId}", userId, companyId);

                return Ok(new
                {
                    success = true,
                    message = "Cache cleared successfully",
                    userId,
                    companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return StatusCode(500, new { success = false, message = "Error clearing cache" });
            }
        }

        [HttpGet("dashboard-metrics")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var role = User.FindFirst("Role")?.Value;
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                var userIdClaim = User.FindFirst("UserId")?.Value;

                var metrics = new
                {
                    role,
                    loadStartTime = DateTime.UtcNow,
                    cacheEnabled = true,
                    redisConnected = await TestRedisConnection(),
                    estimatedLoadTime = GetEstimatedLoadTime(role)
                };

                stopwatch.Stop();

                return Ok(new
                {
                    success = true,
                    metrics,
                    responseTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting dashboard metrics");
                return StatusCode(500, new { success = false, message = "Error getting metrics" });
            }
        }

        private async Task<bool> TestRedisConnection()
        {
            try
            {
                await _cacheService.SetAsync("test-connection", new { timestamp = DateTime.UtcNow }, TimeSpan.FromSeconds(10));
                var result = await _cacheService.GetAsync<object>("test-connection");
                await _cacheService.RemoveAsync("test-connection");
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private static string GetEstimatedLoadTime(string? role)
        {
            return role switch
            {
                "1" => "< 2 seconds (Admin - cached)",
                "2" => "< 1.5 seconds (Company - cached)",
                "3" => "< 1 second (User - cached)",
                _ => "< 3 seconds (Unknown role)"
            };
        }
    }
}
