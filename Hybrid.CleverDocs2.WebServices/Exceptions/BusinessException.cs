namespace Hybrid.CleverDocs2.WebServices.Exceptions;

/// <summary>
/// Exception for business logic violations
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// Error code for categorizing the business error
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Additional details about the error
    /// </summary>
    public Dictionary<string, object>? Details { get; }

    /// <summary>
    /// Creates a new business exception
    /// </summary>
    public BusinessException(string message, string? errorCode = null, Dictionary<string, object>? details = null) 
        : base(message)
    {
        ErrorCode = errorCode ?? "BUSINESS_ERROR";
        Details = details;
    }

    /// <summary>
    /// Creates a new business exception with inner exception
    /// </summary>
    public BusinessException(string message, Exception innerException, string? errorCode = null, Dictionary<string, object>? details = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? "BUSINESS_ERROR";
        Details = details;
    }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Dictionary of field names and their validation errors
    /// </summary>
    public Dictionary<string, List<string>> Errors { get; }

    /// <summary>
    /// Creates a new validation exception
    /// </summary>
    public ValidationException(Dictionary<string, List<string>> errors) 
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a new validation exception with a single error
    /// </summary>
    public ValidationException(string field, string error) 
        : base($"Validation error in field '{field}': {error}")
    {
        Errors = new Dictionary<string, List<string>>
        {
            { field, new List<string> { error } }
        };
    }

    /// <summary>
    /// Creates a new validation exception with multiple errors for a single field
    /// </summary>
    public ValidationException(string field, List<string> errors) 
        : base($"Validation errors in field '{field}'")
    {
        Errors = new Dictionary<string, List<string>>
        {
            { field, errors }
        };
    }
}

/// <summary>
/// Exception for resource not found errors
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Type of resource that was not found
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// Identifier of the resource that was not found
    /// </summary>
    public object ResourceId { get; }

    /// <summary>
    /// Creates a new not found exception
    /// </summary>
    public NotFoundException(string resourceType, object resourceId) 
        : base($"{resourceType} with ID '{resourceId}' was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Creates a new not found exception with custom message
    /// </summary>
    public NotFoundException(string resourceType, object resourceId, string message) 
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Exception for unauthorized access to resources
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Resource that access was denied to
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// Action that was attempted
    /// </summary>
    public string? Action { get; }

    /// <summary>
    /// Creates a new forbidden exception
    /// </summary>
    public ForbiddenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new forbidden exception with resource and action details
    /// </summary>
    public ForbiddenException(string resource, string action) 
        : base($"Access denied to {action} {resource}")
    {
        Resource = resource;
        Action = action;
    }

    /// <summary>
    /// Creates a new forbidden exception with custom message and details
    /// </summary>
    public ForbiddenException(string message, string resource, string action) 
        : base(message)
    {
        Resource = resource;
        Action = action;
    }
}

/// <summary>
/// Exception for conflicts (e.g., duplicate resources)
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Type of conflict that occurred
    /// </summary>
    public string ConflictType { get; }

    /// <summary>
    /// Additional details about the conflict
    /// </summary>
    public Dictionary<string, object>? Details { get; }

    /// <summary>
    /// Creates a new conflict exception
    /// </summary>
    public ConflictException(string message, string? conflictType = null, Dictionary<string, object>? details = null) 
        : base(message)
    {
        ConflictType = conflictType ?? "CONFLICT";
        Details = details;
    }
}

/// <summary>
/// Exception for external service errors
/// </summary>
public class ExternalServiceException : Exception
{
    /// <summary>
    /// Name of the external service that failed
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// HTTP status code returned by the service (if applicable)
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Creates a new external service exception
    /// </summary>
    public ExternalServiceException(string serviceName, string message, int? statusCode = null) 
        : base($"External service '{serviceName}' error: {message}")
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a new external service exception with inner exception
    /// </summary>
    public ExternalServiceException(string serviceName, string message, Exception innerException, int? statusCode = null) 
        : base($"External service '{serviceName}' error: {message}", innerException)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception for rate limiting violations
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Type of operation that was rate limited
    /// </summary>
    public string OperationType { get; }

    /// <summary>
    /// Time until the rate limit resets
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Creates a new rate limit exceeded exception
    /// </summary>
    public RateLimitExceededException(string operationType, TimeSpan retryAfter) 
        : base($"Rate limit exceeded for {operationType}. Retry after {retryAfter.TotalSeconds} seconds.")
    {
        OperationType = operationType;
        RetryAfter = retryAfter;
    }
}
