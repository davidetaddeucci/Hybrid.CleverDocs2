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
            var user = GetCurrentUserFromClaims();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return user.Role.ToLower() switch
            {
                "0" or "admin" => await AdminDashboard(),
                "1" or "company" => await CompanyDashboard(user),
                "2" or "user" => await UserDashboard(user),
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
            var user = GetCurrentUserFromClaims();
            SetViewBagUserInfo(user);
            
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
            SetViewBagUserInfo(user);
            
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
            SetViewBagUserInfo(user);
            
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

    private void SetViewBagUserInfo(Models.UserInfo? user)
    {
        if (user != null)
        {
            ViewBag.UserName = user.FullName;
            ViewBag.CompanyName = user.CompanyName ?? "Unknown Company";
            ViewBag.UserRole = user.Role;
            ViewBag.LogoUrl = user.LogoUrl;
        }
    }

    // Helper methods for data retrieval
    private async Task<int> GetTotalCompanies()
    {
        try
        {
            var result = await _apiService.GetAsync<int>("/api/admin/companies/count");
            return result;
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
            var result = await _apiService.GetAsync<int>("/api/admin/users/count");
            return result;
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
            var result = await _apiService.GetAsync<int>("/api/admin/documents/count");
            return result;
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

    private async Task<int> GetCompanyUsers(Guid? companyId)
    {
        try
        {
            if (companyId == null) return 0;
            var result = await _apiService.GetAsync<int>($"/api/company/{companyId}/users/count");
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyDocuments(Guid? companyId)
    {
        try
        {
            if (companyId == null) return 0;
            var result = await _apiService.GetAsync<int>($"/api/company/{companyId}/documents/count");
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyCollections(Guid? companyId)
    {
        try
        {
            if (companyId == null) return 0;
            var result = await _apiService.GetAsync<int>($"/api/company/{companyId}/collections/count");
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetCompanyDocumentsThisMonth(Guid? companyId)
    {
        try
        {
            if (companyId == null) return 0;
            var result = await _apiService.GetAsync<int>($"/api/company/{companyId}/documents/count/month");
            return result;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<List<UserStatsDto>> GetCompanyUserStats(Guid? companyId)
    {
        try
        {
            if (companyId == null) return new List<UserStatsDto>();
            var result = await _apiService.GetAsync<List<UserStatsDto>>($"/api/company/{companyId}/users/stats");
            return result ?? new List<UserStatsDto>();
        }
        catch
        {
            return new List<UserStatsDto>();
        }
    }

    private async Task<List<DocumentStatsDto>> GetCompanyDocumentStats(Guid? companyId)
    {
        try
        {
            if (companyId == null) return new List<DocumentStatsDto>();
            var result = await _apiService.GetAsync<List<DocumentStatsDto>>($"/api/company/{companyId}/documents/stats");
            return result ?? new List<DocumentStatsDto>();
        }
        catch
        {
            return new List<DocumentStatsDto>();
        }
    }

    private async Task<List<RecentActivityDto>> GetCompanyRecentActivities(Guid? companyId)
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

    private async Task<int> GetUserDocuments(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/documents/count
        // For now, return mock data to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return 0;
    }

    private async Task<int> GetUserCollections(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/collections/count
        // For now, return mock data to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return 0;
    }

    private async Task<int> GetUserConversations(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/conversations/count
        // For now, return mock data to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return 0;
    }

    private async Task<int> GetUserDocumentsThisWeek(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/documents/count/week
        // For now, return mock data to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return 0;
    }

    private async Task<List<RecentDocumentDto>> GetUserRecentDocuments(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/documents/recent
        // For now, return empty list to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return new List<RecentDocumentDto>();
    }

    private async Task<List<RecentConversationDto>> GetUserRecentConversations(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/conversations/recent
        // For now, return empty list to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return new List<RecentConversationDto>();
    }

    private async Task<QuotaUsageDto?> GetUserQuotaUsage(Guid userId)
    {
        // TODO: Implement API endpoint /api/user/{userId}/quota
        // For now, return mock data to avoid slow API calls
        await Task.Delay(1); // Simulate async operation
        return new QuotaUsageDto
        {
            DocumentQuotaLimit = 100,
            QueryQuotaLimit = 50,
            QuotaResetDate = DateTime.UtcNow.AddDays(30)
        };
    }

    private Models.UserInfo? GetCurrentUserFromClaims()
    {
        _logger.LogInformation("GetCurrentUserFromClaims: IsAuthenticated = {IsAuthenticated}", User.Identity?.IsAuthenticated);
        
        if (User.Identity?.IsAuthenticated != true)
            return null;

        try
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            _logger.LogInformation("Claims: Email={Email}, Name={Name}, Id={Id}, Role={Role}", email, name, idClaim, role);

            if (string.IsNullOrEmpty(email))
                return null;

            // Try to parse the ID, if it fails use a default GUID
            Guid userId = Guid.NewGuid();
            if (!string.IsNullOrEmpty(idClaim) && !Guid.TryParse(idClaim, out userId))
            {
                userId = Guid.NewGuid();
            }

            var user = new Models.UserInfo
            {
                Id = userId,
                Email = email,
                Role = role ?? "User"
            };

            if (!string.IsNullOrEmpty(name))
            {
                var nameParts = name.Split(' ', 2);
                user.FirstName = nameParts[0];
                user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            }

            if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var companyId))
            {
                user.CompanyId = companyId;
            }

            return user;
        }
        catch
        {
            return null;
        }
    }
}