namespace Hybrid.CleverDocs2.WebServices.Services.Queue;

/// <summary>
/// Interface for rate limiting service using token bucket algorithm for R2R API calls
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Checks if a request can be made for the specified operation type
    /// </summary>
    Task<bool> CanMakeRequestAsync(string operationType);

    /// <summary>
    /// Waits until a request can be made for the specified operation type
    /// </summary>
    Task WaitForAvailabilityAsync(string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limit status for an operation type
    /// </summary>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string operationType);

    /// <summary>
    /// Resets the rate limit for an operation type (admin function)
    /// </summary>
    Task ResetRateLimitAsync(string operationType);

    /// <summary>
    /// Consumes tokens for a specific operation type
    /// </summary>
    Task<bool> ConsumeTokensAsync(string operationType, int tokensRequested = 1);
}

/// <summary>
/// Rate limit status information
/// </summary>
public class RateLimitStatus
{
    public string OperationType { get; set; } = string.Empty;
    public int AvailableTokens { get; set; }
    public int MaxTokens { get; set; }
    public int RefillRate { get; set; }
    public DateTime LastRefill { get; set; }
    public TimeSpan EstimatedWaitTime { get; set; }
    public bool IsHealthy { get; set; } = true;
}

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    public Dictionary<string, OperationLimit> OperationLimits { get; set; } = new();
    public bool EnableRateLimiting { get; set; } = true;
    public int DefaultCapacity { get; set; } = 5;
    public int DefaultRefillRate { get; set; } = 3;
}

/// <summary>
/// Operation limit configuration
/// </summary>
public class OperationLimit
{
    public int Capacity { get; set; }
    public int RefillRate { get; set; }
    public bool Enabled { get; set; } = true;
}
