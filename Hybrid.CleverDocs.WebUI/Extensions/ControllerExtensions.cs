using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;

namespace Hybrid.CleverDocs.WebUI.Extensions
{
    /// <summary>
    /// Extension methods for MVC Controllers to simplify common operations
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Get the current user's ID from claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>User ID or null if not found</returns>
        public static string? GetCurrentUserId(this Controller controller)
        {
            return controller.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Get the current user's email from claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>User email or null if not found</returns>
        public static string? GetCurrentUserEmail(this Controller controller)
        {
            return controller.User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Get the current user's role from claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>User role or null if not found</returns>
        public static string? GetCurrentUserRole(this Controller controller)
        {
            return controller.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Get the current user's company ID from claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>Company ID or null if not found</returns>
        public static string? GetCurrentCompanyId(this Controller controller)
        {
            return controller.User?.FindFirst("CompanyId")?.Value;
        }

        /// <summary>
        /// Get the current user's tenant ID from claims
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>Tenant ID or null if not found</returns>
        public static string? GetCurrentTenantId(this Controller controller)
        {
            return controller.User?.FindFirst("TenantId")?.Value;
        }

        /// <summary>
        /// Check if the current user is in a specific role
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="role">Role to check (1=Admin, 2=Company, 3=User)</param>
        /// <returns>True if user is in the role</returns>
        public static bool IsInRole(this Controller controller, string role)
        {
            return controller.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Check if the current user is an Admin
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>True if user is Admin</returns>
        public static bool IsAdmin(this Controller controller)
        {
            return controller.IsInRole("1");
        }

        /// <summary>
        /// Check if the current user is a Company admin
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>True if user is Company admin</returns>
        public static bool IsCompanyAdmin(this Controller controller)
        {
            return controller.IsInRole("2");
        }

        /// <summary>
        /// Check if the current user is a regular User
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <returns>True if user is regular User</returns>
        public static bool IsUser(this Controller controller)
        {
            return controller.IsInRole("3");
        }

        /// <summary>
        /// Validate that the current user belongs to the specified company
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="companyId">Company ID to validate</param>
        /// <returns>True if user belongs to the company or is Admin</returns>
        public static bool ValidateCompanyAccess(this Controller controller, string companyId)
        {
            // Admins can access any company
            if (controller.IsAdmin())
                return true;

            // Other users can only access their own company
            var userCompanyId = controller.GetCurrentCompanyId();
            return !string.IsNullOrEmpty(userCompanyId) && userCompanyId.Equals(companyId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate that the current user can access the specified resource
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="resourceUserId">User ID of the resource owner</param>
        /// <param name="resourceCompanyId">Company ID of the resource</param>
        /// <returns>True if user can access the resource</returns>
        public static bool ValidateResourceAccess(this Controller controller, string? resourceUserId = null, string? resourceCompanyId = null)
        {
            var currentUserId = controller.GetCurrentUserId();
            var currentCompanyId = controller.GetCurrentCompanyId();

            // Admins can access any resource
            if (controller.IsAdmin())
                return true;

            // Company admins can access resources in their company
            if (controller.IsCompanyAdmin() && !string.IsNullOrEmpty(resourceCompanyId))
                return controller.ValidateCompanyAccess(resourceCompanyId);

            // Regular users can only access their own resources
            if (!string.IsNullOrEmpty(resourceUserId))
                return currentUserId?.Equals(resourceUserId, StringComparison.OrdinalIgnoreCase) ?? false;

            // If no specific user ID, check company access
            if (!string.IsNullOrEmpty(resourceCompanyId))
                return controller.ValidateCompanyAccess(resourceCompanyId);

            return false;
        }

        /// <summary>
        /// Create a standardized error response for unauthorized access
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="message">Custom error message</param>
        /// <returns>Forbidden result</returns>
        public static IActionResult CreateUnauthorizedResponse(this Controller controller, string? message = null)
        {
            controller.TempData["ErrorMessage"] = message ?? "You are not authorized to access this resource.";
            return controller.Forbid();
        }

        /// <summary>
        /// Create a standardized error response for not found resources
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="resourceType">Type of resource that was not found</param>
        /// <param name="resourceId">ID of the resource that was not found</param>
        /// <returns>NotFound result</returns>
        public static IActionResult CreateNotFoundResponse(this Controller controller, string resourceType, string? resourceId = null)
        {
            var message = string.IsNullOrEmpty(resourceId) 
                ? $"{resourceType} not found."
                : $"{resourceType} with ID '{resourceId}' not found.";
            
            controller.TempData["ErrorMessage"] = message;
            return controller.NotFound();
        }

        /// <summary>
        /// Create a standardized success response with message
        /// </summary>
        /// <param name="controller">Controller instance</param>
        /// <param name="message">Success message</param>
        /// <param name="redirectAction">Action to redirect to</param>
        /// <param name="redirectController">Controller to redirect to</param>
        /// <returns>Redirect result</returns>
        public static IActionResult CreateSuccessResponse(this Controller controller, string message, string redirectAction = "Index", string? redirectController = null)
        {
            controller.TempData["SuccessMessage"] = message;
            
            if (string.IsNullOrEmpty(redirectController))
                return controller.RedirectToAction(redirectAction);
            
            return controller.RedirectToAction(redirectAction, redirectController);
        }

        /// <summary>
        /// Handle API response and convert to appropriate MVC result
        /// </summary>
        /// <typeparam name="T">Type of API response data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="apiResponse">API response to handle</param>
        /// <param name="successAction">Action to redirect to on success</param>
        /// <param name="successController">Controller to redirect to on success</param>
        /// <returns>Appropriate MVC result</returns>
        public static IActionResult HandleApiResponse<T>(this Controller controller, ApiResponse<T>? apiResponse, string successAction = "Index", string? successController = null)
        {
            if (apiResponse == null)
            {
                controller.TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return controller.RedirectToAction(successAction, successController);
            }

            if (apiResponse.Success)
            {
                controller.TempData["SuccessMessage"] = "Operation completed successfully.";
                return controller.RedirectToAction(successAction, successController);
            }

            // Handle different types of errors
            if (apiResponse.IsClientError)
            {
                controller.TempData["ErrorMessage"] = apiResponse.Message ?? "Invalid request.";
            }
            else if (apiResponse.IsServerError)
            {
                controller.TempData["ErrorMessage"] = "A server error occurred. Please try again later.";
            }
            else
            {
                controller.TempData["ErrorMessage"] = apiResponse.Message ?? "An error occurred.";
            }

            return controller.RedirectToAction(successAction, successController);
        }
    }
}
