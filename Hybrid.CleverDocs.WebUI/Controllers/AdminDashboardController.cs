using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Diagnostics;
using Hybrid.CleverDocs.WebUI.Models;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "1")] // Only Admin role (Backend enum: Admin=1)
    public class AdminDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(IApiService apiService, ILogger<AdminDashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = GetCurrentUserFromClaims();
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Load data in parallel
                var companiesTask = _apiService.GetAsync<int>("admin/companies/count");
                var usersTask = _apiService.GetAsync<int>("admin/users/count");
                var documentsTask = _apiService.GetAsync<int>("admin/documents/count");
                var companyStatsTask = _apiService.GetAsync<List<CompanyStatsDto>>("admin/companies/stats");
                var activitiesTask = _apiService.GetAsync<List<RecentActivityDto>>("admin/activities/recent");

                // Wait for all tasks to complete
                var companies = await companiesTask;
                var users = await usersTask;
                var documents = await documentsTask;
                var companyStats = await companyStatsTask;
                var activities = await activitiesTask;

                var viewModel = new AdminDashboardViewModel
                {
                    CurrentUser = user,
                    TotalCompanies = companies,
                    TotalUsers = users,
                    TotalDocuments = documents,
                    CompanyStats = companyStats ?? new List<CompanyStatsDto>(),
                    RecentActivities = activities ?? new List<RecentActivityDto>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                var user = GetCurrentUserFromClaims();
                return View(new AdminDashboardViewModel { CurrentUser = user });
            }
        }

        private UserInfo? GetCurrentUserFromClaims()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return null;

            return new UserInfo
            {
                Id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString()),
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                FirstName = User.FindFirst(ClaimTypes.Name)?.Value?.Split(' ').FirstOrDefault() ?? "",
                LastName = User.FindFirst(ClaimTypes.Name)?.Value?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User",
                CompanyId = User.FindFirst("CompanyId")?.Value != null ? 
                    Guid.Parse(User.FindFirst("CompanyId")?.Value!) : null
            };
        }
    }
}