using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    // JWT Authentication: Authorization handled client-side with JWT tokens
    public class CompanyDashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<CompanyDashboardController> _logger;

        public CompanyDashboardController(IDashboardService dashboardService, ILogger<CompanyDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
                {
                    _logger.LogWarning("Invalid or missing CompanyId claim");
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Loading company dashboard for company {CompanyId}", companyId);

                // Use optimized dashboard service with caching
                var viewModel = await _dashboardService.GetCompanyDashboardAsync(companyId);

                stopwatch.Stop();
                _logger.LogInformation("Company dashboard loaded in {ElapsedMs}ms for company {CompanyId}",
                    stopwatch.ElapsedMilliseconds, companyId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error loading company dashboard after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                // Return fallback view with minimal data
                var fallbackViewModel = new CompanyDashboardViewModel
                {
                    TotalUsers = 0,
                    TotalDocuments = 0,
                    TotalCollections = 0,
                    UserStats = new List<UserStatsDto>(),
                    DocumentStats = new List<DocumentStatsDto>(),
                    RecentActivities = new List<RecentActivityDto>()
                };

                ViewBag.ErrorMessage = "Dashboard data temporarily unavailable. Please refresh the page.";
                return View(fallbackViewModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshCache()
        {
            try
            {
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
                {
                    return Json(new { success = false, message = "Invalid company ID" });
                }

                await _dashboardService.InvalidateDashboardCacheAsync(companyId: companyId);
                _logger.LogInformation("Cache invalidated for company {CompanyId}", companyId);

                return Json(new { success = true, message = "Cache refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache");
                return Json(new { success = false, message = "Error refreshing cache" });
            }
        }
    }
}