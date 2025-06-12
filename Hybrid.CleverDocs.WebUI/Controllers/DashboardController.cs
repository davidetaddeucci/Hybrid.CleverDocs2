using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;

namespace Hybrid.CleverDocs.WebUI.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAuthService _authService;
    private readonly IApiService _apiService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IAuthService authService, IApiService apiService, ILogger<DashboardController> logger)
    {
        _authService = authService;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return user.Role.ToLower() switch
            {
                "admin" => await AdminDashboard(),
                "company" => await CompanyDashboard(user),
                "user" => await UserDashboard(user),
                _ => RedirectToAction("Login", "Auth")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            TempData["ErrorMessage"] = "Error loading dashboard. Please try again.";
            return RedirectToAction("Login", "Auth");
        }
    }

    private async Task<IActionResult> AdminDashboard()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            var model = new AdminDashboardViewModel
            {
                CurrentUser = user,
                TotalCompanies = await GetTotalCompanies(),
                TotalUsers = await GetTotalUsers(),
                TotalDocuments = await GetTotalDocuments(),
                SystemHealth = await GetSystemHealth(),
                CompanyStats = await GetCompanyStats(),
                RecentActivities = await GetRecentActivities()
            };

            return View("Admin", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            return View("Error");
        }
    }

    private async Task<IActionResult> CompanyDashboard(Models.UserInfo user)
    {
        try
        {
            var model = new CompanyDashboardViewModel
            {
                CurrentUser = user,
                TotalUsers = await GetCompanyUsers(user.CompanyId),
                TotalDocuments = await GetCompanyDocuments(user.CompanyId),
                TotalCollections = await GetCompanyCollections(user.CompanyId),
                DocumentsThisMonth = await GetCompanyDocumentsThisMonth(user.CompanyId),
                UserStats = await GetCompanyUserStats(user.CompanyId),
                DocumentStats = await GetCompanyDocumentStats(user.CompanyId),
                RecentActivities = await GetCompanyRecentActivities(user.CompanyId)
            };

            return View("Company", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading company dashboard for company {CompanyId}", user.CompanyId);
            return View("Error");
        }
    }

    private async Task<IActionResult> UserDashboard(Models.UserInfo user)
    {
        try
        {
            var model = new UserDashboardViewModel
            {
                CurrentUser = user,
                DocumentCount = await GetUserDocuments(user.Id),
                CollectionCount = await GetUserCollections(user.Id),
                ConversationCount = await GetUserConversations(user.Id),
                DocumentsThisWeek = await GetUserDocumentsThisWeek(user.Id),
                RecentDocuments = await GetUserRecentDocuments(user.Id),
                RecentConversations = await GetUserRecentConversations(user.Id),
                QuotaUsage = await GetUserQuotaUsage(user.Id)
            };

            return View("User", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user dashboard for user {UserId}", user.Id);
            return View("Error");
        }
    }

    // Helper methods for data retrieval
    private async Task<int> GetTotalCompanies()
    {
        try
        {
            return await _apiService.GetAsync<int>("/api/admin/companies/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetTotalUsers()
    {
        try
        {
            return await _apiService.GetAsync<int>("/api/admin/users/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetTotalDocuments()
    {
        try
        {
            return await _apiService.GetAsync<int>("/api/admin/documents/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<SystemHealthDto?> GetSystemHealth()
    {
        try
        {
            return await _apiService.GetAsync<SystemHealthDto>("/api/health") ?? new SystemHealthDto
            {
                IsHealthy = false,
                Status = "Unknown",
                LastChecked = DateTime.UtcNow
            };
        }
        catch
        {
            return new SystemHealthDto
            {
                IsHealthy = false,
                Status = "Error",
                LastChecked = DateTime.UtcNow
            };
        }
    }

    private async Task<List<CompanyStatsDto>> GetCompanyStats()
    {
        try
        {
            return await _apiService.GetAsync<List<CompanyStatsDto>>("/api/admin/companies/stats") ?? new List<CompanyStatsDto>();
        }
        catch
        {
            return new List<CompanyStatsDto>();
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivities()
    {
        try
        {
            return await _apiService.GetAsync<List<RecentActivityDto>>("/api/admin/activities/recent") ?? new List<RecentActivityDto>();
        }
        catch
        {
            return new List<RecentActivityDto>();
        }
    }

    private async Task<int> GetCompanyUsers(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/company/{companyId}/users/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyDocuments(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/company/{companyId}/documents/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyCollections(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/company/{companyId}/collections/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyDocumentsThisMonth(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/company/{companyId}/documents/count/month") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<List<UserStatsDto>> GetCompanyUserStats(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<List<UserStatsDto>>($"/api/company/{companyId}/users/stats") ?? new List<UserStatsDto>();
        }
        catch
        {
            return new List<UserStatsDto>();
        }
    }

    private async Task<List<DocumentStatsDto>> GetCompanyDocumentStats(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<List<DocumentStatsDto>>($"/api/company/{companyId}/documents/stats") ?? new List<DocumentStatsDto>();
        }
        catch
        {
            return new List<DocumentStatsDto>();
        }
    }

    private async Task<List<RecentActivityDto>> GetCompanyRecentActivities(int companyId)
    {
        try
        {
            return await _apiService.GetAsync<List<RecentActivityDto>>($"/api/company/{companyId}/activities/recent") ?? new List<RecentActivityDto>();
        }
        catch
        {
            return new List<RecentActivityDto>();
        }
    }

    private async Task<int> GetUserDocuments(int userId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/user/{userId}/documents/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetUserCollections(int userId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/user/{userId}/collections/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetUserConversations(int userId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/user/{userId}/conversations/count") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetUserDocumentsThisWeek(int userId)
    {
        try
        {
            return await _apiService.GetAsync<int>($"/api/user/{userId}/documents/count/week") ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<List<RecentDocumentDto>> GetUserRecentDocuments(int userId)
    {
        try
        {
            return await _apiService.GetAsync<List<RecentDocumentDto>>($"/api/user/{userId}/documents/recent") ?? new List<RecentDocumentDto>();
        }
        catch
        {
            return new List<RecentDocumentDto>();
        }
    }

    private async Task<List<RecentConversationDto>> GetUserRecentConversations(int userId)
    {
        try
        {
            return await _apiService.GetAsync<List<RecentConversationDto>>($"/api/user/{userId}/conversations/recent") ?? new List<RecentConversationDto>();
        }
        catch
        {
            return new List<RecentConversationDto>();
        }
    }

    private async Task<QuotaUsageDto?> GetUserQuotaUsage(int userId)
    {
        try
        {
            return await _apiService.GetAsync<QuotaUsageDto>($"/api/user/{userId}/quota") ?? new QuotaUsageDto
            {
                DocumentQuotaLimit = 100,
                QueryQuotaLimit = 50,
                QuotaResetDate = DateTime.UtcNow.AddDays(30)
            };
        }
        catch
        {
            return new QuotaUsageDto
            {
                DocumentQuotaLimit = 100,
                QueryQuotaLimit = 50,
                QuotaResetDate = DateTime.UtcNow.AddDays(30)
            };
        }
    }
}