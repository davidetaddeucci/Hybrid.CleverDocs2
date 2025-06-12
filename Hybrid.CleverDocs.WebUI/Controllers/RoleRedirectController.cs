using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize]
    public class RoleRedirectController : Controller
    {
        private readonly ILogger<RoleRedirectController> _logger;

        public RoleRedirectController(ILogger<RoleRedirectController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            
            _logger.LogInformation("Redirecting user {Email} with role {Role}", userEmail, userRole);

            return userRole switch
            {
                "0" => RedirectToAction("Index", "AdminDashboard"),     // Admin Dashboard
                "1" => RedirectToAction("Index", "CompanyDashboard"),   // Company Dashboard  
                "2" => RedirectToAction("Index", "UserDashboard"),      // User Dashboard
                _ => RedirectToAction("Index", "UserDashboard")         // Default to User Dashboard
            };
        }
    }
}