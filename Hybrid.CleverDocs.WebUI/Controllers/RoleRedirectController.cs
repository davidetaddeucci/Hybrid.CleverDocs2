using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    // JWT Authentication: Authorization handled client-side with JWT tokens
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
            try
            {
                // Check for redirect loop prevention
                var redirectCount = HttpContext.Session.GetInt32("RedirectCount") ?? 0;
                if (redirectCount > 5)
                {
                    _logger.LogWarning("Redirect loop detected, clearing session and redirecting to fallback dashboard");
                    HttpContext.Session.Remove("RedirectCount");
                    return RedirectToAction("Index", "Dashboard"); // Fallback to generic dashboard
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var roleName = User.FindFirst("RoleName")?.Value;

                _logger.LogInformation("Redirecting user {Email} with role {Role} (name: {RoleName})",
                    userEmail, userRole, roleName);

                // Validate user claims
                if (string.IsNullOrEmpty(userRole) || string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Invalid user claims detected, redirecting to login");
                    return RedirectToAction("Login", "Auth");
                }

                // Clear redirect count on successful role resolution
                HttpContext.Session.Remove("RedirectCount");

                // Route based on corrected role values (Backend enum: Admin=1, Company=2, User=3)
                return userRole switch
                {
                    "1" => RedirectToAction("Index", "AdminDashboard"),     // Admin Dashboard
                    "2" => RedirectToAction("Index", "CompanyDashboard"),   // Company Dashboard
                    "3" => RedirectToAction("Index", "UserDashboard"),      // User Dashboard
                    _ => HandleUnknownRole(userRole, userEmail)             // Fallback with logging
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in role redirect for user {Email}",
                    User.FindFirst(ClaimTypes.Email)?.Value);
                return RedirectToAction("Index", "Dashboard"); // Safe fallback
            }
        }

        private IActionResult HandleUnknownRole(string? userRole, string? userEmail)
        {
            _logger.LogWarning("Unknown role {Role} for user {Email}, redirecting to default dashboard",
                userRole, userEmail);

            // Default to User dashboard for unknown roles
            return RedirectToAction("Index", "UserDashboard");
        }
    }
}