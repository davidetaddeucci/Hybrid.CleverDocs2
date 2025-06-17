using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    // JWT Authentication: Authorization handled client-side with JWT tokens
    [Route("UserDashboard")]
    public class UserDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<UserDashboardController> _logger;

        public UserDashboardController(IApiService apiService, ILogger<UserDashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // JWT Authentication: User data will be loaded via JavaScript from localStorage
                // For now, return empty model and let JavaScript populate the dashboard
                var model = new UserDashboardViewModel
                {
                    DocumentCount = 0,
                    CollectionCount = 0,
                    ConversationCount = 0,
                    RecentDocuments = new List<RecentDocumentDto>(),
                    RecentConversations = new List<RecentConversationDto>()
                };

                _logger.LogInformation("User dashboard loaded (JWT Authentication mode)");
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