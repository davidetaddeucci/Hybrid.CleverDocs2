using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Queue;

/// <summary>
/// Rate limiting service using token bucket algorithm for R2R API calls
/// </summary>
public class RateLimitingService : IRateLimitingService, IDisposable
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly ILogger<RateLimitingService> _logger;
    private readonly IOptions<RateLimitingOptions> _options;
    private readonly ICorrelationService _correlationService;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public RateLimitingService(
        ILogger<RateLimitingService> logger, 
        IOptions<RateLimitingOptions> options,
        ICorrelationService correlationService)
    {
        _logger = logger;
        _options = options;
        _correlationService = correlationService;

        // Cleanup unused buckets every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedBuckets, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<bool> CanMakeRequestAsync(string operationType)
    {
        if (!_options.Value.EnableRateLimiting)
            return true;

        var bucket = GetOrCreateBucket(operationType);
        var canProceed = await bucket.TryConsumeAsync();
        
        if (!canProceed)
        {
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogWarning("Rate limit exceeded for operation type: {OperationType}, CorrelationId: {CorrelationId}", 
                operationType, correlationId);
        }
        
        return canProceed;
    }

    public async Task<bool> ConsumeTokensAsync(string operationType, int tokensRequested = 1)
    {
        if (!_options.Value.EnableRateLimiting)
            return true;

        var bucket = GetOrCreateBucket(operationType);
        return await bucket.TryConsumeAsync(tokensRequested);
    }

    public async Task WaitForAvailabilityAsync(string operationType, CancellationToken cancellationToken = default)
    {
        if (!_options.Value.EnableRateLimiting)
            return;

        var bucket = GetOrCreateBucket(operationType);
        var correlationId = _correlationService.GetCorrelationId();
        
        while (!await bucket.TryConsumeAsync() && !cancellationToken.IsCancellationRequested)
        {
            var waitTime = bucket.GetEstimatedWaitTime();
            _logger.LogInformation("Waiting {WaitTime}ms for rate limit availability for {OperationType}, CorrelationId: {CorrelationId}", 
                waitTime.TotalMilliseconds, operationType, correlationId);
            
            try
            {
                await Task.Delay(waitTime, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Rate limiting wait cancelled for {OperationType}, CorrelationId: {CorrelationId}", 
                    operationType, correlationId);
                throw;
            }
        }
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string operationType)
    {
        var bucket = GetOrCreateBucket(operationType);
        return await Task.FromResult(bucket.GetStatus(operationType));
    }

    public async Task ResetRateLimitAsync(string operationType)
    {
        if (_buckets.TryRemove(operationType, out var bucket))
        {
            bucket.Dispose();
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogInformation("Rate limit reset for operation type: {OperationType}, CorrelationId: {CorrelationId}", 
                operationType, correlationId);
        }
        
        await Task.CompletedTask;
    }

    private TokenBucket GetOrCreateBucket(string operationType)
    {
        return _buckets.GetOrAdd(operationType, key =>
        {
            var (capacity, rate, enabled) = GetRateLimitsForOperation(key);
            var correlationId = _correlationService.GetCorrelationId();
            
            _logger.LogInformation("Created rate limit bucket for {OperationType}: capacity={Capacity}, rate={Rate}/sec, enabled={Enabled}, CorrelationId: {CorrelationId}", 
                key, capacity, rate, enabled, correlationId);
            
            return new TokenBucket(capacity, rate, enabled, _logger);
        });
    }

    private (int capacity, int rate, bool enabled) GetRateLimitsForOperation(string operationType)
    {
        var config = _options.Value.OperationLimits.GetValueOrDefault(operationType);
        if (config != null)
        {
            return (config.Capacity, config.RefillRate, config.Enabled);
        }

        // Default limits based on R2R API constraints (slightly under actual limits for safety)
        return operationType switch
        {
            "document_ingestion" => (10, 8, true),    // R2R limit: 10/sec, we use 8/sec
            "embedding_generation" => (5, 4, true),   // R2R limit: 5/sec, we use 4/sec
            "search_operation" => (20, 18, true),     // R2R limit: 20/sec, we use 18/sec
            "collection_operation" => (15, 12, true), // R2R limit: 15/sec, we use 12/sec
            "conversation_operation" => (10, 8, true), // R2R limit: 10/sec, we use 8/sec
            "graph_operation" => (8, 6, true),        // R2R limit: 8/sec, we use 6/sec
            "auth_operation" => (30, 25, true),       // R2R limit: 30/sec, we use 25/sec
            _ => (_options.Value.DefaultCapacity, _options.Value.DefaultRefillRate, true) // Conservative default
        };
    }

    private void CleanupUnusedBuckets(object? state)
    {
        if (_disposed) return;

        var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Remove buckets unused for 10 minutes
        var bucketsToRemove = new List<string>();

        foreach (var kvp in _buckets)
        {
            if (kvp.Value.LastUsed < cutoffTime)
            {
                bucketsToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in bucketsToRemove)
        {
            if (_buckets.TryRemove(key, out var bucket))
            {
                bucket.Dispose();
                _logger.LogDebug("Cleaned up unused rate limit bucket for operation: {OperationType}", key);
            }
        }

        if (bucketsToRemove.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} unused rate limit buckets", bucketsToRemove.Count);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cleanupTimer?.Dispose();

        // Dispose all buckets
        foreach (var bucket in _buckets.Values)
        {
            bucket.Dispose();
        }
        _buckets.Clear();

        _logger.LogInformation("Rate limiting service disposed");
    }
}

/// <summary>
/// Token bucket implementation for rate limiting with exponential backoff
/// </summary>
public class TokenBucket : IDisposable
{
    private readonly int _capacity;
    private readonly int _refillRate;
    private readonly bool _enabled;
    private int _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();
    private readonly ILogger _logger;
    private readonly Timer _refillTimer;
    private bool _disposed;

    public DateTime LastUsed { get; private set; } = DateTime.UtcNow;

    public TokenBucket(int capacity, int refillRatePerSecond, bool enabled, ILogger logger)
    {
        _capacity = capacity;
        _refillRate = refillRatePerSecond;
        _enabled = enabled;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
        _logger = logger;

        // Set up timer to refill tokens every 100ms for smooth rate limiting
        _refillTimer = new Timer(RefillTokens, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }

    public async Task<bool> TryConsumeAsync(int tokensRequested = 1)
    {
        if (!_enabled)
            return true;

        LastUsed = DateTime.UtcNow;

        lock (_lock)
        {
            if (_tokens >= tokensRequested)
            {
                _tokens -= tokensRequested;
                _logger.LogTrace("Consumed {TokensRequested} tokens, remaining: {RemainingTokens}/{Capacity}",
                    tokensRequested, _tokens, _capacity);
                return true;
            }

            _logger.LogTrace("Insufficient tokens: requested {TokensRequested}, available: {AvailableTokens}",
                tokensRequested, _tokens);
            return false;
        }
    }

    public TimeSpan GetEstimatedWaitTime()
    {
        if (!_enabled)
            return TimeSpan.Zero;

        lock (_lock)
        {
            if (_tokens > 0) return TimeSpan.Zero;

            // Calculate time needed to get at least 1 token
            var tokensNeeded = 1;
            var secondsToWait = (double)tokensNeeded / _refillRate;

            // Add jitter to prevent thundering herd (±20%)
            var jitter = Random.Shared.NextDouble() * 0.4 - 0.2; // -20% to +20%
            secondsToWait *= (1 + jitter);

            return TimeSpan.FromSeconds(Math.Max(0.1, secondsToWait)); // Minimum 100ms wait
        }
    }

    public RateLimitStatus GetStatus(string operationType)
    {
        lock (_lock)
        {
            return new RateLimitStatus
            {
                OperationType = operationType,
                AvailableTokens = _tokens,
                MaxTokens = _capacity,
                RefillRate = _refillRate,
                LastRefill = _lastRefill,
                EstimatedWaitTime = GetEstimatedWaitTime(),
                IsHealthy = _enabled && _tokens >= 0
            };
        }
    }

    private void RefillTokens(object? state)
    {
        if (_disposed || !_enabled) return;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var timePassed = now - _lastRefill;
            var tokensToAdd = (int)(timePassed.TotalSeconds * _refillRate);

            if (tokensToAdd > 0)
            {
                var oldTokens = _tokens;
                _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                _lastRefill = now;

                if (_tokens > oldTokens)
                {
                    _logger.LogTrace("Refilled {TokensAdded} tokens, total: {TotalTokens}/{Capacity}",
                        _tokens - oldTokens, _tokens, _capacity);
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _refillTimer?.Dispose();
            _disposed = true;
        }
    }
}
