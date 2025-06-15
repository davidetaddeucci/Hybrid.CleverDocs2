using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Exceptions;
using System.Net;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Middleware;

/// <summary>
/// Global exception handling middleware for standardized error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationService correlationService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, correlationService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ICorrelationService correlationService)
    {
        var correlationId = correlationService.GetCorrelationId();
        var userContext = correlationService.GetUserContext();
        var tenantContext = correlationService.GetTenantContext();

        // Log the exception with full context
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "RequestPath", context.Request.Path },
            { "RequestMethod", context.Request.Method },
            { "UserContext", userContext?.ToDictionary() ?? new Dictionary<string, object>() },
            { "TenantContext", tenantContext?.ToDictionary() ?? new Dictionary<string, object>() },
            { "ExceptionType", exception.GetType().Name },
            { "StackTrace", exception.StackTrace ?? "No stack trace available" }
        });

        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        // Create standardized error response
        var response = CreateErrorResponse(exception, correlationId);

        // Set response properties
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ApiResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        var response = exception switch
        {
            BusinessException businessEx => CreateBusinessExceptionResponse(businessEx),
            ValidationException validationEx => CreateValidationExceptionResponse(validationEx),
            UnauthorizedAccessException => CreateUnauthorizedResponse(),
            ArgumentException argEx => CreateArgumentExceptionResponse(argEx),
            InvalidOperationException invalidOpEx => CreateInvalidOperationResponse(invalidOpEx),
            TimeoutException => CreateTimeoutResponse(),
            HttpRequestException httpEx => CreateHttpRequestExceptionResponse(httpEx),
            _ => CreateGenericExceptionResponse(exception)
        };

        response.TraceId = correlationId;
        return response;
    }

    private ApiResponse CreateBusinessExceptionResponse(BusinessException exception)
    {
        return new ApiResponse
        {
            Success = false,
            Message = exception.Message,
            Errors = new List<string> { exception.Message },
            StatusCode = (int)HttpStatusCode.BadRequest,
            Metadata = exception.Details
        };
    }

    private ApiResponse CreateValidationExceptionResponse(ValidationException exception)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = exception.Errors.SelectMany(kvp => 
                kvp.Value.Select(error => $"{kvp.Key}: {error}")).ToList(),
            StatusCode = (int)HttpStatusCode.UnprocessableEntity,
            Metadata = new Dictionary<string, object> { { "ValidationErrors", exception.Errors } }
        };
    }

    private ApiResponse CreateUnauthorizedResponse()
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Unauthorized access",
            Errors = new List<string> { "You are not authorized to perform this action" },
            StatusCode = (int)HttpStatusCode.Unauthorized
        };
    }

    private ApiResponse CreateArgumentExceptionResponse(ArgumentException exception)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Invalid argument provided",
            Errors = new List<string> { GetSafeErrorMessage(exception.Message) },
            StatusCode = (int)HttpStatusCode.BadRequest
        };
    }

    private ApiResponse CreateInvalidOperationResponse(InvalidOperationException exception)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Invalid operation",
            Errors = new List<string> { GetSafeErrorMessage(exception.Message) },
            StatusCode = (int)HttpStatusCode.BadRequest
        };
    }

    private ApiResponse CreateTimeoutResponse()
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Request timeout",
            Errors = new List<string> { "The request took too long to process. Please try again." },
            StatusCode = (int)HttpStatusCode.RequestTimeout
        };
    }

    private ApiResponse CreateHttpRequestExceptionResponse(HttpRequestException exception)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "External service error",
            Errors = new List<string> { "An error occurred while communicating with an external service" },
            StatusCode = (int)HttpStatusCode.BadGateway
        };
    }

    private ApiResponse CreateGenericExceptionResponse(Exception exception)
    {
        var message = _environment.IsDevelopment() 
            ? exception.Message 
            : "An unexpected error occurred. Please try again later.";

        var errors = _environment.IsDevelopment() 
            ? new List<string> { exception.Message, exception.StackTrace ?? "No stack trace" }
            : new List<string> { "Internal server error" };

        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Metadata = _environment.IsDevelopment() 
                ? new Dictionary<string, object> 
                { 
                    { "ExceptionType", exception.GetType().Name },
                    { "InnerException", exception.InnerException?.Message ?? "None" }
                }
                : null
        };
    }

    private string GetSafeErrorMessage(string originalMessage)
    {
        // In production, sanitize error messages to avoid exposing sensitive information
        if (_environment.IsProduction())
        {
            // Remove potentially sensitive information like file paths, connection strings, etc.
            return "An error occurred while processing your request";
        }

        return originalMessage;
    }
}

/// <summary>
/// Extension methods for registering global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
