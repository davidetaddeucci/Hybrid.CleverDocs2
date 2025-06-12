using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract tenant information from user claims
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var companyIdClaim = context.User.FindFirst("companyId")?.Value;
                var tenantIdClaim = context.User.FindFirst("tenantId")?.Value; // Backward compatibility
                
                var tenantId = companyIdClaim ?? tenantIdClaim;
                
                if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    // Add tenant context to request
                    context.Items["TenantId"] = parsedTenantId;
                    context.Items["CompanyId"] = parsedTenantId;
                    
                    _logger.LogDebug("Tenant resolved: {TenantId} for user {UserId}", 
                        parsedTenantId, context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                }
                else
                {
                    _logger.LogWarning("No valid tenant ID found in user claims for user {UserId}", 
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                }
            }

            await _next(context);
        }
    }

    // Extension methods for easy access to tenant context
    public static class HttpContextExtensions
    {
        public static Guid? GetTenantId(this HttpContext context)
        {
            return context.Items["TenantId"] as Guid?;
        }

        public static Guid? GetCompanyId(this HttpContext context)
        {
            return context.Items["CompanyId"] as Guid?;
        }

        public static Guid? GetUserId(this HttpContext context)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public static string? GetUserEmail(this HttpContext context)
        {
            return context.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        public static string? GetUserRole(this HttpContext context)
        {
            return context.User.FindFirst(ClaimTypes.Role)?.Value;
        }

        public static bool IsInRole(this HttpContext context, string role)
        {
            return context.User.IsInRole(role);
        }

        public static bool HasTenantAccess(this HttpContext context, Guid tenantId)
        {
            var userTenantId = context.GetTenantId();
            return userTenantId.HasValue && userTenantId.Value == tenantId;
        }
    }
}