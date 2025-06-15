using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Models.Common;

/// <summary>
/// Standardized API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">Type of the response data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data (null if operation failed)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of error messages (empty if operation succeeded)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Additional metadata about the response
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonIgnore]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            Metadata = metadata,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors ?? new List<string>(),
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates an error response with single error
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, string error, int statusCode = 400)
    {
        return ErrorResponse(message, new List<string> { error }, statusCode);
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    public static ApiResponse<T> NotFoundResponse(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message ?? "Resource not found",
            StatusCode = 404
        };
    }

    /// <summary>
    /// Creates an unauthorized response
    /// </summary>
    public static ApiResponse<T> UnauthorizedResponse(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message ?? "Unauthorized access",
            StatusCode = 401
        };
    }

    /// <summary>
    /// Creates a forbidden response
    /// </summary>
    public static ApiResponse<T> ForbiddenResponse(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message ?? "Access forbidden",
            StatusCode = 403
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ApiResponse<T> ValidationErrorResponse(Dictionary<string, List<string>> validationErrors)
    {
        var errors = validationErrors.SelectMany(kvp => 
            kvp.Value.Select(error => $"{kvp.Key}: {error}")).ToList();

        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = "Validation failed",
            Errors = errors,
            Metadata = new Dictionary<string, object> { { "ValidationErrors", validationErrors } },
            StatusCode = 422
        };
    }
}

/// <summary>
/// Non-generic version for responses without data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            StatusCode = 200
        };
    }

    /// <summary>
    /// Creates an error response without data
    /// </summary>
    public static new ApiResponse ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>(),
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of the items in the collection</typeparam>
public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    public static PaginatedResponse<T> SuccessPaginatedResponse(
        IEnumerable<T> data, 
        int page, 
        int pageSize, 
        long totalItems,
        string? message = null)
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return new PaginatedResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Data retrieved successfully",
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            StatusCode = 200
        };
    }
}
