using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Extensions;

/// <summary>
/// Extension methods for controllers to standardize API responses
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Returns a standardized success response
    /// </summary>
    public static IActionResult Success<T>(this ControllerBase controller, T data, string? message = null, Dictionary<string, object>? metadata = null)
    {
        var response = ApiResponse<T>.SuccessResponse(data, message, metadata);
        response.TraceId = GetTraceId(controller);
        return controller.Ok(response);
    }

    /// <summary>
    /// Returns a standardized success response without data
    /// </summary>
    public static IActionResult Success(this ControllerBase controller, string? message = null)
    {
        var response = ApiResponse.SuccessResponse(message);
        response.TraceId = GetTraceId(controller);
        return controller.Ok(response);
    }

    /// <summary>
    /// Returns a standardized error response
    /// </summary>
    public static IActionResult Error(this ControllerBase controller, string message, List<string>? errors = null, int statusCode = 400)
    {
        var response = ApiResponse.ErrorResponse(message, errors, statusCode);
        response.TraceId = GetTraceId(controller);
        
        return statusCode switch
        {
            400 => controller.BadRequest(response),
            401 => controller.Unauthorized(response),
            403 => controller.StatusCode(403, response),
            404 => controller.NotFound(response),
            422 => controller.StatusCode(422, response),
            500 => controller.StatusCode(500, response),
            _ => controller.StatusCode(statusCode, response)
        };
    }

    /// <summary>
    /// Returns a standardized error response with single error
    /// </summary>
    public static IActionResult Error(this ControllerBase controller, string message, string error, int statusCode = 400)
    {
        return controller.Error(message, new List<string> { error }, statusCode);
    }

    /// <summary>
    /// Returns a standardized not found response
    /// </summary>
    public static IActionResult NotFound<T>(this ControllerBase controller, string? message = null)
    {
        var response = ApiResponse<T>.NotFoundResponse(message);
        response.TraceId = GetTraceId(controller);
        return controller.NotFound(response);
    }

    /// <summary>
    /// Returns a standardized unauthorized response
    /// </summary>
    public static IActionResult Unauthorized<T>(this ControllerBase controller, string? message = null)
    {
        var response = ApiResponse<T>.UnauthorizedResponse(message);
        response.TraceId = GetTraceId(controller);
        return controller.Unauthorized(response);
    }

    /// <summary>
    /// Returns a standardized forbidden response
    /// </summary>
    public static IActionResult Forbidden<T>(this ControllerBase controller, string? message = null)
    {
        var response = ApiResponse<T>.ForbiddenResponse(message);
        response.TraceId = GetTraceId(controller);
        return controller.StatusCode(403, response);
    }

    /// <summary>
    /// Returns a standardized validation error response
    /// </summary>
    public static IActionResult ValidationError<T>(this ControllerBase controller, Dictionary<string, List<string>> validationErrors)
    {
        var response = ApiResponse<T>.ValidationErrorResponse(validationErrors);
        response.TraceId = GetTraceId(controller);
        return controller.StatusCode(422, response);
    }

    /// <summary>
    /// Returns a standardized paginated response
    /// </summary>
    public static IActionResult Paginated<T>(this ControllerBase controller, 
        IEnumerable<T> data, 
        int page, 
        int pageSize, 
        long totalItems,
        string? message = null)
    {
        var response = PaginatedResponse<T>.SuccessPaginatedResponse(data, page, pageSize, totalItems, message);
        response.TraceId = GetTraceId(controller);
        return controller.Ok(response);
    }

    /// <summary>
    /// Gets the current user's ID from claims
    /// </summary>
    public static Guid? GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user's company ID from claims
    /// </summary>
    public static Guid? GetCurrentCompanyId(this ControllerBase controller)
    {
        var companyIdClaim = controller.User.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : null;
    }

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    public static string? GetCurrentUserRole(this ControllerBase controller)
    {
        return controller.User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    public static string? GetCurrentUserEmail(this ControllerBase controller)
    {
        return controller.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Checks if the current user has the specified role
    /// </summary>
    public static bool HasRole(this ControllerBase controller, string role)
    {
        return controller.User.IsInRole(role);
    }

    /// <summary>
    /// Checks if the current user is an admin
    /// </summary>
    public static bool IsAdmin(this ControllerBase controller)
    {
        return controller.HasRole("1"); // Admin role
    }

    /// <summary>
    /// Checks if the current user is a company admin
    /// </summary>
    public static bool IsCompanyAdmin(this ControllerBase controller)
    {
        return controller.HasRole("2"); // Company role
    }

    /// <summary>
    /// Checks if the current user is a regular user
    /// </summary>
    public static bool IsUser(this ControllerBase controller)
    {
        return controller.HasRole("3"); // User role
    }

    /// <summary>
    /// Validates that the user belongs to the specified company
    /// </summary>
    public static bool BelongsToCompany(this ControllerBase controller, Guid companyId)
    {
        var userCompanyId = controller.GetCurrentCompanyId();
        return userCompanyId.HasValue && userCompanyId.Value == companyId;
    }

    /// <summary>
    /// Returns an error if the user doesn't belong to the specified company
    /// </summary>
    public static IActionResult? ValidateCompanyAccess(this ControllerBase controller, Guid companyId)
    {
        if (!controller.BelongsToCompany(companyId))
        {
            return controller.Forbidden<object>("Access denied: You don't have permission to access this company's data");
        }
        return null;
    }

    /// <summary>
    /// Returns an error if the user doesn't own the resource or isn't an admin
    /// </summary>
    public static IActionResult? ValidateResourceOwnership(this ControllerBase controller, Guid resourceUserId)
    {
        var currentUserId = controller.GetCurrentUserId();
        
        if (!controller.IsAdmin() && (!currentUserId.HasValue || currentUserId.Value != resourceUserId))
        {
            return controller.Forbidden<object>("Access denied: You don't have permission to access this resource");
        }
        return null;
    }

    /// <summary>
    /// Gets the trace ID for request correlation
    /// </summary>
    private static string GetTraceId(ControllerBase controller)
    {
        return controller.HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Adds tenant context metadata to response
    /// </summary>
    public static Dictionary<string, object> AddTenantMetadata(this ControllerBase controller, Dictionary<string, object>? metadata = null)
    {
        metadata ??= new Dictionary<string, object>();
        
        var companyId = controller.GetCurrentCompanyId();
        var userId = controller.GetCurrentUserId();
        var userRole = controller.GetCurrentUserRole();
        
        if (companyId.HasValue)
            metadata["CompanyId"] = companyId.Value;
        
        if (userId.HasValue)
            metadata["UserId"] = userId.Value;
        
        if (!string.IsNullOrEmpty(userRole))
            metadata["UserRole"] = userRole;
        
        return metadata;
    }
}
