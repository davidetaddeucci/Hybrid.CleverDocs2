using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "2")] // Only User role
    public class UserDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<UserDashboardController> _logger;

        public UserDashboardController(IApiService apiService, ILogger<UserDashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var companyId = User.FindFirst("CompanyId")?.Value;

                var model = new UserDashboardViewModel
                {
                    DocumentCount = await _apiService.GetAsync<int>($"user/{userId}/documents/count"),
                    CollectionCount = await _apiService.GetAsync<int>($"user/{userId}/collections/count"),
                    ConversationCount = await _apiService.GetAsync<int>($"user/{userId}/conversations/count"),
                    RecentDocuments = await _apiService.GetAsync<List<RecentDocumentDto>>($"user/{userId}/documents/recent") ?? new(),
                    RecentConversations = await _apiService.GetAsync<List<RecentConversationDto>>($"user/{userId}/conversations/recent") ?? new()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard");
                return View(new UserDashboardViewModel());
            }
        }
    }
}