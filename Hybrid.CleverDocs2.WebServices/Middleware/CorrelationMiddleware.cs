using Hybrid.CleverDocs2.WebServices.Services.Logging;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Middleware;

/// <summary>
/// Middleware for managing correlation IDs and request context for distributed tracing
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationService correlationService)
    {
        // Extract or generate correlation ID
        var correlationId = ExtractCorrelationId(context);
        correlationService.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Set user context if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            SetUserContext(context, correlationService);
        }

        // Set tenant context if available
        SetTenantContext(context, correlationService);

        // Log request start
        LogRequestStart(context, correlationId);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogRequestError(context, correlationId, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogRequestEnd(context, correlationId, stopwatch.ElapsedMilliseconds);
        }
    }

    private string ExtractCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            var correlationId = correlationIdHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }
        }

        // Try to get from query parameters (for debugging)
        if (context.Request.Query.TryGetValue("correlationId", out var correlationIdQuery))
        {
            var correlationId = correlationIdQuery.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N")[..12];
    }

    private void SetUserContext(HttpContext context, ICorrelationService correlationService)
    {
        try
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
            var firstNameClaim = context.User.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastNameClaim = context.User.FindFirst(ClaimTypes.Surname)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId) && !string.IsNullOrEmpty(emailClaim))
            {
                var userContext = new UserContext
                {
                    UserId = userId,
                    Email = emailClaim,
                    Role = roleClaim ?? "Unknown",
                    FirstName = firstNameClaim,
                    LastName = lastNameClaim,
                    IpAddress = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers.UserAgent.FirstOrDefault()
                };

                correlationService.SetUserContext(userContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set user context for correlation service");
        }
    }

    private void SetTenantContext(HttpContext context, ICorrelationService correlationService)
    {
        try
        {
            var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;
            var companyNameClaim = context.User.FindFirst("CompanyName")?.Value;

            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                var tenantContext = new TenantContext
                {
                    CompanyId = companyId,
                    CompanyName = companyNameClaim ?? "Unknown Company",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow // This would normally come from database
                };

                correlationService.SetTenantContext(tenantContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set tenant context for correlation service");
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void LogRequestStart(HttpContext context, string correlationId)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "RequestMethod", context.Request.Method },
            { "RequestPath", context.Request.Path },
            { "QueryString", context.Request.QueryString.ToString() },
            { "UserAgent", context.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown" },
            { "ClientIP", GetClientIpAddress(context) }
        });

        _logger.LogInformation("Request started: {Method} {Path}",
            context.Request.Method, context.Request.Path);
    }

    private void LogRequestEnd(HttpContext context, string correlationId, long elapsedMs)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "RequestMethod", context.Request.Method },
            { "RequestPath", context.Request.Path },
            { "StatusCode", context.Response.StatusCode },
            { "ElapsedMs", elapsedMs }
        });

        var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        
        _logger.Log(logLevel, "Request completed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);
    }

    private void LogRequestError(HttpContext context, string correlationId, Exception exception, long elapsedMs)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "RequestMethod", context.Request.Method },
            { "RequestPath", context.Request.Path },
            { "ElapsedMs", elapsedMs },
            { "ExceptionType", exception.GetType().Name },
            { "ExceptionMessage", exception.Message }
        });

        _logger.LogError(exception, "Request failed: {Method} {Path} after {ElapsedMs}ms",
            context.Request.Method, context.Request.Path, elapsedMs);
    }
}

/// <summary>
/// Extension methods for registering correlation middleware
/// </summary>
public static class CorrelationMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationMiddleware>();
    }

    /// <summary>
    /// Registers correlation services in DI container
    /// </summary>
    public static IServiceCollection AddCorrelationServices(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationService, CorrelationService>();
        return services;
    }
}
