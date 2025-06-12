using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "1")] // Only Company role
    public class CompanyDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<CompanyDashboardController> _logger;

        public CompanyDashboardController(IApiService apiService, ILogger<CompanyDashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = User.FindFirst("CompanyId")?.Value;

                var model = new CompanyDashboardViewModel
                {
                    TotalUsers = await _apiService.GetAsync<int>($"company/{companyId}/users/count"),
                    TotalDocuments = await _apiService.GetAsync<int>($"company/{companyId}/documents/count"),
                    TotalCollections = await _apiService.GetAsync<int>($"company/{companyId}/collections/count"),
                    UserStats = await _apiService.GetAsync<List<UserStatsDto>>($"company/{companyId}/users/stats") ?? new(),
                    DocumentStats = await _apiService.GetAsync<List<DocumentStatsDto>>($"company/{companyId}/documents/stats") ?? new(),
                    RecentActivities = await _apiService.GetAsync<List<RecentActivityDto>>($"company/{companyId}/activities/recent") ?? new()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company dashboard");
                return View(new CompanyDashboardViewModel());
            }
        }
    }
}