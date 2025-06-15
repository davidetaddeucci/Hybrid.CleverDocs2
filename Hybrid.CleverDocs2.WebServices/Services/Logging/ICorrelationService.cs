namespace Hybrid.CleverDocs2.WebServices.Services.Logging;

/// <summary>
/// Service for managing correlation IDs across requests for distributed tracing
/// </summary>
public interface ICorrelationService
{
    /// <summary>
    /// Gets the current correlation ID for the request
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Sets the correlation ID for the current request
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    string GenerateCorrelationId();

    /// <summary>
    /// Gets the current user context for logging
    /// </summary>
    UserContext? GetUserContext();

    /// <summary>
    /// Sets the user context for the current request
    /// </summary>
    void SetUserContext(UserContext userContext);

    /// <summary>
    /// Gets the current tenant context for logging
    /// </summary>
    TenantContext? GetTenantContext();

    /// <summary>
    /// Sets the tenant context for the current request
    /// </summary>
    void SetTenantContext(TenantContext tenantContext);
}

/// <summary>
/// Implementation of correlation service using AsyncLocal for thread-safe context
/// </summary>
public class CorrelationService : ICorrelationService
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    private static readonly AsyncLocal<UserContext?> _userContext = new();
    private static readonly AsyncLocal<TenantContext?> _tenantContext = new();

    public string GetCorrelationId()
    {
        return _correlationId.Value ?? GenerateCorrelationId();
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12]; // Short correlation ID
        _correlationId.Value = correlationId;
        return correlationId;
    }

    public UserContext? GetUserContext()
    {
        return _userContext.Value;
    }

    public void SetUserContext(UserContext userContext)
    {
        _userContext.Value = userContext;
    }

    public TenantContext? GetTenantContext()
    {
        return _tenantContext.Value;
    }

    public void SetTenantContext(TenantContext tenantContext)
    {
        _tenantContext.Value = tenantContext;
    }
}

/// <summary>
/// User context information for logging
/// </summary>
public class UserContext
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "UserId", UserId },
            { "Email", Email },
            { "Role", Role },
            { "FullName", FullName },
            { "IpAddress", IpAddress ?? "Unknown" },
            { "UserAgent", UserAgent ?? "Unknown" }
        };
    }
}

/// <summary>
/// Tenant context information for logging
/// </summary>
public class TenantContext
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyDomain { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "CompanyId", CompanyId },
            { "CompanyName", CompanyName },
            { "CompanyDomain", CompanyDomain ?? "Unknown" },
            { "IsActive", IsActive }
        };
    }
}

/// <summary>
/// Extension methods for ILogger to include correlation context
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs with correlation context automatically included
    /// </summary>
    public static void LogWithContext<T>(this ILogger<T> logger, LogLevel logLevel, string message, params object[] args)
    {
        var correlationService = GetCorrelationService();
        if (correlationService == null)
        {
            logger.Log(logLevel, message, args);
            return;
        }

        var correlationId = correlationService.GetCorrelationId();
        var userContext = correlationService.GetUserContext();
        var tenantContext = correlationService.GetTenantContext();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "UserContext", userContext?.ToDictionary() ?? new Dictionary<string, object>() },
            { "TenantContext", tenantContext?.ToDictionary() ?? new Dictionary<string, object>() }
        });

        logger.Log(logLevel, message, args);
    }

    /// <summary>
    /// Logs information with correlation context
    /// </summary>
    public static void LogInformationWithContext<T>(this ILogger<T> logger, string message, params object[] args)
    {
        logger.LogWithContext(LogLevel.Information, message, args);
    }

    /// <summary>
    /// Logs warning with correlation context
    /// </summary>
    public static void LogWarningWithContext<T>(this ILogger<T> logger, string message, params object[] args)
    {
        logger.LogWithContext(LogLevel.Warning, message, args);
    }

    /// <summary>
    /// Logs error with correlation context
    /// </summary>
    public static void LogErrorWithContext<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        var correlationService = GetCorrelationService();
        if (correlationService == null)
        {
            logger.LogError(exception, message, args);
            return;
        }

        var correlationId = correlationService.GetCorrelationId();
        var userContext = correlationService.GetUserContext();
        var tenantContext = correlationService.GetTenantContext();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "UserContext", userContext?.ToDictionary() ?? new Dictionary<string, object>() },
            { "TenantContext", tenantContext?.ToDictionary() ?? new Dictionary<string, object>() },
            { "ExceptionType", exception.GetType().Name },
            { "ExceptionMessage", exception.Message }
        });

        logger.LogError(exception, message, args);
    }

    /// <summary>
    /// Gets correlation service from DI container (simplified approach)
    /// In a real implementation, this would be injected properly
    /// </summary>
    private static ICorrelationService? GetCorrelationService()
    {
        // This is a simplified approach. In a real implementation,
        // you would inject ICorrelationService properly through DI
        return null;
    }
}
