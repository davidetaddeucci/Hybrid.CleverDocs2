using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents;

/// <summary>
/// Service for processing documents with R2R API integration and intelligent rate limiting
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly ICorrelationService _correlationService;
    private readonly DocumentProcessingOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<DocumentUploadHub> _hubContext;
    private readonly IDocumentClient _documentClient;
    private readonly IR2RComplianceService _complianceService;

    // Thread-safe queue management
    private readonly ConcurrentQueue<R2RProcessingQueueItemDto> _processingQueue = new();
    private readonly ConcurrentDictionary<Guid, R2RProcessingQueueItemDto> _activeProcessing = new();
    private readonly ConcurrentDictionary<Guid, R2RProcessingQueueItemDto> _failedItems = new();

    // Rate limiting and circuit breaker
    private readonly SemaphoreSlim _processingLimiter;
    private DateTime _lastR2RFailure = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private bool _circuitBreakerOpen = false;

    public DocumentProcessingService(
        IRateLimitingService rateLimitingService,
        IMultiLevelCacheService cacheService,
        ILogger<DocumentProcessingService> logger,
        ICorrelationService correlationService,
        IOptions<DocumentProcessingOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<DocumentUploadHub> hubContext,
        IDocumentClient documentClient,
        IR2RComplianceService complianceService)
    {
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _documentClient = documentClient;
        _complianceService = complianceService;

        _processingLimiter = new SemaphoreSlim(_options.MaxConcurrentProcessing, _options.MaxConcurrentProcessing);
    }

    public async Task<bool> QueueDocumentForProcessingAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Queuing document for R2R processing: {FileName} ({FileSize} bytes), DocumentId: {DocumentId}, Priority: {Priority}, CorrelationId: {CorrelationId}", 
                queueItem.FileName, queueItem.FileSize, queueItem.DocumentId, queueItem.Priority, correlationId);

            // Set initial status
            queueItem.Status = R2RProcessingStatusDto.Queued;
            queueItem.CreatedAt = DateTime.UtcNow;

            // Add to queue based on priority
            _processingQueue.Enqueue(queueItem);

            // Cache for persistence
            await _cacheService.SetAsync($"r2r:queue:{queueItem.Id}", queueItem,
                new CacheOptions
                {
                    UseL1Cache = true,  // ✅ ENABLED - Fast access for active processing
                    UseL2Cache = true,  // ✅ ENABLED - Redis for R2R processing queue
                    UseL3Cache = true,  // ✅ ENABLED - Persistent storage for processing state
                    L1TTL = TimeSpan.FromMinutes(30),
                    L2TTL = TimeSpan.FromHours(24),
                    L3TTL = TimeSpan.FromDays(7)
                });

            // Broadcast queued status via SignalR with cache invalidation
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem, _cacheService);

            _logger.LogInformation("Document queued successfully: {DocumentId}, Queue position: {QueueSize}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, _processingQueue.Count, correlationId);

            // Trigger processing if not at capacity
            _ = Task.Run(async () => await ProcessQueueAsync());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing document for processing: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, correlationId);
            return false;
        }
    }

    public async Task<bool> ProcessDocumentAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting R2R processing for document: {FileName}, DocumentId: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.FileName, queueItem.DocumentId, correlationId);

            // Check circuit breaker
            if (IsCircuitBreakerOpen())
            {
                _logger.LogWarning("Circuit breaker is open, delaying processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    queueItem.DocumentId, correlationId);
                
                queueItem.NextRetryAt = DateTime.UtcNow.AddMinutes(_options.CircuitBreakerDelayMinutes);
                return false;
            }

            // Check rate limiting
            var canProceed = await _rateLimitingService.CanMakeRequestAsync("r2r_document_ingestion");
            if (!canProceed)
            {
                _logger.LogInformation("Rate limit reached, delaying processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    queueItem.DocumentId, correlationId);
                
                queueItem.NextRetryAt = DateTime.UtcNow.AddSeconds(_options.RateLimitDelaySeconds);
                return false;
            }

            // Update status
            queueItem.Status = R2RProcessingStatusDto.Processing;
            queueItem.StartedAt = DateTime.UtcNow;
            _activeProcessing[queueItem.Id] = queueItem;

            // Broadcast processing status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            // Process with R2R API
            var success = await ProcessWithR2RAsync(queueItem);

            if (success)
            {
                // Check if this is async processing (R2R returned HTTP 202)
                if (queueItem.R2RDocumentId != null && queueItem.R2RDocumentId.StartsWith("pending_"))
                {
                    // Async processing - document stays in Processing state for worker to check
                    queueItem.Status = R2RProcessingStatusDto.Processing;
                    // Keep in _activeProcessing for worker to monitor

                    _logger.LogInformation("R2R async processing started for document: {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);
                }
                else
                {
                    // Synchronous processing completed
                    queueItem.Status = R2RProcessingStatusDto.Completed;
                    queueItem.CompletedAt = DateTime.UtcNow;

                    // Broadcast completion status via SignalR
                    await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

                    _logger.LogInformation("R2R processing completed successfully for document: {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);
                }

                _consecutiveFailures = 0;

                // Save document to database (with correct status)
                await SaveDocumentToDatabaseAsync(queueItem);
            }
            else
            {
                await HandleProcessingFailure(queueItem, "R2R processing failed");
            }

            // Remove from active processing
            _activeProcessing.TryRemove(queueItem.Id, out _);

            // Update cache
            await _cacheService.SetAsync($"r2r:queue:{queueItem.Id}", queueItem, 
                new CacheOptions 
                { 
                    L2TTL = TimeSpan.FromHours(24),
                    UseL3Cache = true 
                });

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document with R2R: {DocumentId}, CorrelationId: {CorrelationId}", 
                queueItem.DocumentId, correlationId);

            await HandleProcessingFailure(queueItem, ex.Message);
            _activeProcessing.TryRemove(queueItem.Id, out _);

            return false;
        }
    }

    public async Task<List<R2RProcessingQueueItemDto>> GetProcessingQueueAsync(string? userId = null)
    {
        var allItems = new List<R2RProcessingQueueItemDto>();

        // Add queued items
        allItems.AddRange(_processingQueue.Where(item => userId == null || item.UserId == userId));

        // Add active processing items
        allItems.AddRange(_activeProcessing.Values.Where(item => userId == null || item.UserId == userId));

        // Add failed items
        allItems.AddRange(_failedItems.Values.Where(item => userId == null || item.UserId == userId));

        return allItems.OrderBy(item => item.CreatedAt).ToList();
    }

    public async Task<bool> RetryProcessingAsync(Guid queueItemId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Try to find in failed items first
            if (_failedItems.TryRemove(queueItemId, out var failedItem))
            {
                failedItem.Status = R2RProcessingStatusDto.Queued;
                failedItem.RetryCount++;
                failedItem.NextRetryAt = null;
                failedItem.ErrorMessage = null;

                _processingQueue.Enqueue(failedItem);

                _logger.LogInformation("Retrying processing for document {DocumentId}, attempt {RetryCount}, CorrelationId: {CorrelationId}", 
                    failedItem.DocumentId, failedItem.RetryCount, correlationId);

                return true;
            }

            // Try to find in cache
            var cachedItem = await _cacheService.GetAsync<R2RProcessingQueueItemDto>($"r2r:queue:{queueItemId}");
            if (cachedItem != null && cachedItem.Status == R2RProcessingStatusDto.Failed)
            {
                cachedItem.Status = R2RProcessingStatusDto.Queued;
                cachedItem.RetryCount++;
                cachedItem.NextRetryAt = null;
                cachedItem.ErrorMessage = null;

                _processingQueue.Enqueue(cachedItem);

                _logger.LogInformation("Retrying processing for cached document {DocumentId}, attempt {RetryCount}, CorrelationId: {CorrelationId}", 
                    cachedItem.DocumentId, cachedItem.RetryCount, correlationId);

                return true;
            }

            _logger.LogWarning("Queue item {QueueItemId} not found for retry, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying processing for queue item {QueueItemId}, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);
            return false;
        }
    }

    public async Task<bool> CancelProcessingAsync(Guid queueItemId)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Remove from active processing
            if (_activeProcessing.TryRemove(queueItemId, out var activeItem))
            {
                activeItem.Status = R2RProcessingStatusDto.Cancelled;
                await _cacheService.SetAsync($"r2r:queue:{queueItemId}", activeItem, 
                    new CacheOptions { L2TTL = TimeSpan.FromHours(24) });

                _logger.LogInformation("Cancelled active processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    activeItem.DocumentId, correlationId);
                return true;
            }

            // Remove from failed items
            if (_failedItems.TryRemove(queueItemId, out var failedItem))
            {
                failedItem.Status = R2RProcessingStatusDto.Cancelled;
                await _cacheService.SetAsync($"r2r:queue:{queueItemId}", failedItem, 
                    new CacheOptions { L2TTL = TimeSpan.FromHours(24) });

                _logger.LogInformation("Cancelled failed processing for document {DocumentId}, CorrelationId: {CorrelationId}", 
                    failedItem.DocumentId, correlationId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling processing for queue item {QueueItemId}, CorrelationId: {CorrelationId}", 
                queueItemId, correlationId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetProcessingStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["queued_items"] = _processingQueue.Count,
            ["active_processing"] = _activeProcessing.Count,
            ["failed_items"] = _failedItems.Count,
            ["circuit_breaker_open"] = _circuitBreakerOpen,
            ["consecutive_failures"] = _consecutiveFailures,
            ["last_failure"] = _lastR2RFailure,
            ["processing_capacity"] = _options.MaxConcurrentProcessing,
            ["available_capacity"] = _processingLimiter.CurrentCount
        };

        return stats;
    }

    public async Task OptimizeProcessingQueueAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Optimizing R2R processing queue, CorrelationId: {CorrelationId}", correlationId);

            // Reorder queue by priority and retry time
            var queueItems = new List<R2RProcessingQueueItemDto>();
            while (_processingQueue.TryDequeue(out var item))
            {
                queueItems.Add(item);
            }

            // Sort by priority (higher first) and then by retry time
            var optimizedItems = queueItems
                .Where(item => item.NextRetryAt == null || item.NextRetryAt <= DateTime.UtcNow)
                .OrderByDescending(item => (int)item.Priority)
                .ThenBy(item => item.CreatedAt)
                .ToList();

            // Re-enqueue optimized items
            foreach (var item in optimizedItems)
            {
                _processingQueue.Enqueue(item);
            }

            // Re-enqueue items that are not ready for retry
            var delayedItems = queueItems
                .Where(item => item.NextRetryAt > DateTime.UtcNow)
                .ToList();

            foreach (var item in delayedItems)
            {
                _processingQueue.Enqueue(item);
            }

            _logger.LogDebug("Queue optimization completed: {OptimizedCount} items ready, {DelayedCount} items delayed, CorrelationId: {CorrelationId}", 
                optimizedItems.Count, delayedItems.Count, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing processing queue, CorrelationId: {CorrelationId}", correlationId);
        }
    }

    // Private helper methods
    private async Task ProcessQueueAsync()
    {
        while (_processingQueue.TryDequeue(out var queueItem))
        {
            // Check if we can process more items
            if (_processingLimiter.CurrentCount == 0)
            {
                // Re-queue the item and break
                _processingQueue.Enqueue(queueItem);
                break;
            }

            // Check if item is ready for processing
            if (queueItem.NextRetryAt > DateTime.UtcNow)
            {
                // Re-queue for later
                _processingQueue.Enqueue(queueItem);
                continue;
            }

            // Process item asynchronously
            _ = Task.Run(async () =>
            {
                await _processingLimiter.WaitAsync();
                try
                {
                    await ProcessDocumentAsync(queueItem);
                }
                finally
                {
                    _processingLimiter.Release();
                }
            });
        }
    }

    private async Task<bool> ProcessWithR2RAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Starting R2R processing for document {DocumentId}, File: {FileName}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.FileName, correlationId);

            // ✅ QUICK WIN 1 - Validate file for R2R compliance
            var (isValid, validationErrors) = await _complianceService.ValidateFileForR2RAsync(queueItem.FilePath, queueItem.ContentType);
            if (!isValid)
            {
                var errorMessage = $"File validation failed: {string.Join(", ", validationErrors)}";
                _logger.LogWarning("R2R validation failed for document {DocumentId}: {ErrorMessage}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, errorMessage, correlationId);

                queueItem.ErrorCategory = ErrorCategory.Validation;
                queueItem.ErrorMessage = errorMessage;
                return false;
            }

            // ✅ QUICK WIN 2 - Generate R2R compliant filename and checksum
            var fileBytes = await File.ReadAllBytesAsync(queueItem.FilePath);
            var r2rCompliantFilename = _complianceService.GenerateR2RCompliantFilename(queueItem.FileName, queueItem.DocumentId);
            var checksum = _complianceService.ComputeChecksum(fileBytes);

            // Store enhanced metadata
            queueItem.OriginalFileName = queueItem.FileName;
            queueItem.FileName = r2rCompliantFilename;
            queueItem.Checksum = checksum;
            queueItem.UploadTimestamp = DateTime.UtcNow;

            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", r2rCompliantFilename)
            {
                Headers = new HeaderDictionary(),
                ContentType = queueItem.ContentType
            };

            // ✅ QUICK WIN 3 - Enhanced metadata for R2R compliance
            var documentRequest = new DocumentRequest
            {
                File = formFile,
                Title = queueItem.OriginalFileName ?? queueItem.FileName,
                DocumentType = queueItem.ContentType,
                Metadata = new Dictionary<string, object>
                {
                    ["original_filename"] = queueItem.OriginalFileName ?? queueItem.FileName,
                    ["r2r_compliant_filename"] = queueItem.FileName,
                    ["file_size"] = queueItem.FileSize,
                    ["checksum"] = queueItem.Checksum ?? "",
                    ["upload_timestamp"] = queueItem.UploadTimestamp,
                    ["user_id"] = queueItem.UserId,
                    ["company_id"] = queueItem.CompanyId,
                    ["document_id"] = queueItem.DocumentId,
                    ["file_id"] = queueItem.FileId,
                    ["content_type"] = queueItem.ContentType,
                    ["processing_priority"] = queueItem.Priority.ToString()
                }
            };

            // Add R2R collection ID if provided
            if (queueItem.CollectionId.HasValue)
            {
                // Get R2R collection ID from cache or database
                var r2rCollectionId = await GetR2RCollectionIdAsync(queueItem.CollectionId.Value);
                if (!string.IsNullOrEmpty(r2rCollectionId))
                {
                    documentRequest.CollectionIds = new List<string> { r2rCollectionId };
                    _logger.LogInformation("Using R2R collection ID {R2RCollectionId} for document {DocumentId}, CorrelationId: {CorrelationId}",
                        r2rCollectionId, queueItem.DocumentId, correlationId);
                }
                else
                {
                    _logger.LogWarning("No R2R collection ID found for collection {CollectionId}, document {DocumentId} will be uploaded without collection, CorrelationId: {CorrelationId}",
                        queueItem.CollectionId.Value, queueItem.DocumentId, correlationId);
                }
            }

            // Call real R2R API via DocumentClient
            var documentResponse = await _documentClient.CreateAsync(documentRequest);

            // ✅ QUICK WIN 4 - Enhanced R2R response handling with TaskId support
            if (documentResponse != null)
            {
                if (!string.IsNullOrEmpty(documentResponse.Id))
                {
                    // Immediate success (HTTP 200) - store R2R document ID from response
                    queueItem.R2RDocumentId = documentResponse.Id;
                    queueItem.TaskId = null; // No task ID for immediate processing

                    _logger.LogInformation("R2R processing completed successfully (immediate) for document {DocumentId}, R2R ID: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);

                    return true;
                }
                else if (!string.IsNullOrEmpty(documentResponse.TaskId))
                {
                    // Async processing (HTTP 202) with TaskId - R2R standard approach
                    queueItem.TaskId = documentResponse.TaskId;
                    queueItem.R2RDocumentId = null; // Will be populated when task completes
                    queueItem.Status = R2RProcessingStatusDto.Processing;

                    _logger.LogInformation("R2R processing accepted (async) for document {DocumentId}, TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.TaskId, correlationId);

                    return true;
                }
                else
                {
                    // Fallback for older R2R versions without TaskId
                    queueItem.TaskId = $"legacy_{queueItem.DocumentId}";
                    queueItem.R2RDocumentId = null;
                    queueItem.Status = R2RProcessingStatusDto.Processing;

                    _logger.LogInformation("R2R processing accepted (legacy async) for document {DocumentId}, Legacy TaskId: {TaskId}, CorrelationId: {CorrelationId}",
                        queueItem.DocumentId, queueItem.TaskId, correlationId);

                    return true;
                }
            }
            else
            {
                _logger.LogError("R2R API returned null response for document {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            // ✅ QUICK WIN 5 - Enhanced error categorization
            queueItem.ErrorCategory = _complianceService.CategorizeError(httpEx.Message, httpEx);
            queueItem.ErrorMessage = httpEx.Message;

            _logger.LogError(httpEx, "HTTP error during R2R processing for document {DocumentId}, Category: {ErrorCategory}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.ErrorCategory, correlationId);
            return false;
        }
        catch (Exception ex)
        {
            queueItem.ErrorCategory = _complianceService.CategorizeError(ex.Message, ex);
            queueItem.ErrorMessage = ex.Message;

            _logger.LogError(ex, "R2R processing failed for document {DocumentId}, Category: {ErrorCategory}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.ErrorCategory, correlationId);
            return false;
        }
    }

    private async Task HandleProcessingFailure(R2RProcessingQueueItemDto queueItem, string errorMessage)
    {
        queueItem.Status = R2RProcessingStatusDto.Failed;
        queueItem.ErrorMessage = errorMessage;
        queueItem.RetryCount++;

        // ✅ QUICK WIN 6 - Enhanced error categorization for intelligent retry
        if (queueItem.ErrorCategory == ErrorCategory.None)
        {
            queueItem.ErrorCategory = _complianceService.CategorizeError(errorMessage);
        }

        _consecutiveFailures++;
        _lastR2RFailure = DateTime.UtcNow;

        // Broadcast failure status via SignalR with cache invalidation
        await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem, _cacheService);

        // Check if we should open circuit breaker
        if (_consecutiveFailures >= _options.CircuitBreakerThreshold)
        {
            _circuitBreakerOpen = true;
            _logger.LogWarning("Circuit breaker opened due to {ConsecutiveFailures} consecutive failures", _consecutiveFailures);
        }

        // ✅ QUICK WIN 7 - Intelligent retry based on error category
        var shouldRetry = ShouldRetryBasedOnErrorCategory(queueItem);

        if (shouldRetry && queueItem.RetryCount < queueItem.MaxRetries)
        {
            var delay = CalculateRetryDelayBasedOnCategory(queueItem);
            queueItem.NextRetryAt = DateTime.UtcNow.Add(delay);
            queueItem.Status = R2RProcessingStatusDto.Retrying;

            // Broadcast retry status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogInformation("Scheduling retry {RetryCount}/{MaxRetries} for document {DocumentId} in {Delay}, Category: {ErrorCategory}",
                queueItem.RetryCount, queueItem.MaxRetries, queueItem.DocumentId, delay, queueItem.ErrorCategory);

            // Re-queue for retry
            _processingQueue.Enqueue(queueItem);
        }
        else
        {
            // Max retries reached or permanent error, move to failed items
            _failedItems[queueItem.Id] = queueItem;

            // Broadcast final failure status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogError("Document processing failed permanently: {DocumentId}, Category: {ErrorCategory}, Error: {ErrorMessage}",
                queueItem.DocumentId, queueItem.ErrorCategory, errorMessage);
        }
    }

    private bool IsCircuitBreakerOpen()
    {
        if (!_circuitBreakerOpen) return false;

        // Check if we should close the circuit breaker
        var timeSinceLastFailure = DateTime.UtcNow - _lastR2RFailure;
        if (timeSinceLastFailure > TimeSpan.FromMinutes(_options.CircuitBreakerResetMinutes))
        {
            _circuitBreakerOpen = false;
            _consecutiveFailures = 0;
            _logger.LogInformation("Circuit breaker closed after {TimeSinceLastFailure}", timeSinceLastFailure);
            return false;
        }

        return true;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(_options.BaseRetryDelaySeconds);
        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

        return exponentialDelay.Add(jitter);
    }

    // ✅ QUICK WIN 8 - Intelligent retry logic based on error category
    private bool ShouldRetryBasedOnErrorCategory(R2RProcessingQueueItemDto queueItem)
    {
        return queueItem.ErrorCategory switch
        {
            ErrorCategory.Transient => true,        // Always retry transient errors
            ErrorCategory.RateLimit => true,        // Retry rate limit errors with longer delay
            ErrorCategory.Authentication => false,   // Don't retry auth errors
            ErrorCategory.Validation => false,      // Don't retry validation errors
            ErrorCategory.FileFormat => false,      // Don't retry format errors
            ErrorCategory.FileSize => false,        // Don't retry size errors
            ErrorCategory.Permanent => false,       // Don't retry permanent errors
            ErrorCategory.None => true,             // Default to retry for unknown errors
            _ => true
        };
    }

    private TimeSpan CalculateRetryDelayBasedOnCategory(R2RProcessingQueueItemDto queueItem)
    {
        var baseDelay = CalculateRetryDelay(queueItem.RetryCount);

        return queueItem.ErrorCategory switch
        {
            ErrorCategory.RateLimit => TimeSpan.FromMinutes(2 * queueItem.RetryCount), // Longer delay for rate limits
            ErrorCategory.Transient => baseDelay,                                      // Standard exponential backoff
            _ => baseDelay
        };
    }

    private async Task<string?> GetR2RCollectionIdAsync(Guid collectionId)
    {
        try
        {
            // First check cache
            var cacheKey = $"r2r:collection:mapping:{collectionId}";
            var cachedId = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedId))
            {
                return cachedId;
            }

            // Check database
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var collection = await context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (collection != null && !string.IsNullOrEmpty(collection.R2RCollectionId))
            {
                // Cache for future use
                await _cacheService.SetAsync(cacheKey, collection.R2RCollectionId,
                    new CacheOptions { L1TTL = TimeSpan.FromMinutes(30), L2TTL = TimeSpan.FromHours(2) });

                return collection.R2RCollectionId;
            }

            return null;
        }
        catch (Exception ex)
        {
            var correlationId = _correlationService.GetCorrelationId();
            _logger.LogError(ex, "Error getting R2R collection ID for collection {CollectionId}, CorrelationId: {CorrelationId}",
                collectionId, correlationId);
            return null;
        }
    }

    private async Task SaveDocumentToDatabaseAsync(R2RProcessingQueueItemDto queueItem)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var correlationId = _correlationService.GetCorrelationId();

            _logger.LogInformation("Saving document to database: {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);

            // Check if document already exists
            var existingDocument = await context.Documents
                .FirstOrDefaultAsync(d => d.Id == queueItem.DocumentId);

            if (existingDocument != null)
            {
                // Update existing document with R2R information
                existingDocument.R2RDocumentId = queueItem.R2RDocumentId;
                existingDocument.R2RProcessedAt = DateTime.UtcNow;

                // Set correct status based on R2R processing state
                if (queueItem.Status == R2RProcessingStatusDto.Completed)
                {
                    existingDocument.Status = (int)Data.Entities.DocumentStatus.Ready;
                }
                else if (queueItem.Status == R2RProcessingStatusDto.Processing)
                {
                    existingDocument.Status = (int)Data.Entities.DocumentStatus.Processing;
                }
                else if (queueItem.Status == R2RProcessingStatusDto.Failed)
                {
                    existingDocument.Status = (int)Data.Entities.DocumentStatus.Error;
                }

                existingDocument.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated existing document {DocumentId} with R2R data, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
            }
            else
            {
                // Create new document record
                // Try to parse UserId as Guid, if it fails, log the value and use a default
                Guid userGuid;
                if (!Guid.TryParse(queueItem.UserId, out userGuid))
                {
                    _logger.LogError("Invalid UserId format: '{UserId}', using empty Guid, CorrelationId: {CorrelationId}",
                        queueItem.UserId, correlationId);
                    userGuid = Guid.Empty;
                }

                // Get user's company ID from database using scoped context
                User? user;
                using (var userScope = _serviceScopeFactory.CreateScope())
                {
                    var userContext = userScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    user = await userContext.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
                }

                if (user == null)
                {
                    _logger.LogError("User {UserId} not found in database, CorrelationId: {CorrelationId}",
                        queueItem.UserId, correlationId);
                    return;
                }

                // Determine correct status based on R2R processing state
                int documentStatus;
                if (queueItem.Status == R2RProcessingStatusDto.Completed)
                {
                    documentStatus = (int)Data.Entities.DocumentStatus.Ready;
                }
                else if (queueItem.Status == R2RProcessingStatusDto.Processing)
                {
                    documentStatus = (int)Data.Entities.DocumentStatus.Processing;
                }
                else if (queueItem.Status == R2RProcessingStatusDto.Failed)
                {
                    documentStatus = (int)Data.Entities.DocumentStatus.Error;
                }
                else
                {
                    documentStatus = (int)Data.Entities.DocumentStatus.Draft; // Default for new documents
                }

                var document = new Document
                {
                    Id = queueItem.DocumentId,
                    Name = queueItem.FileName,
                    FileName = queueItem.FileName,
                    OriginalFileName = queueItem.FileName,
                    SizeInBytes = queueItem.FileSize,
                    ContentType = queueItem.ContentType,
                    Status = documentStatus,
                    R2RDocumentId = queueItem.R2RDocumentId,
                    R2RProcessedAt = DateTime.UtcNow,
                    UserId = userGuid,
                    CompanyId = user.CompanyId, // Use user's actual company ID
                    CollectionId = queueItem.CollectionId,
                    FilePath = string.Empty,
                    Tags = new List<string>(), // Empty list instead of dictionary
                    Metadata = new Dictionary<string, object>(), // This will be handled by EF Core
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Documents.Add(document);

                _logger.LogInformation("Created new document record {DocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);
            }

            // Save changes to database
            await context.SaveChangesAsync();

            // CRITICAL: Invalidate document caches BEFORE SignalR broadcast using ONLY tag-based invalidation
            var tags = new List<string> { "documents", "document-lists", $"user:{queueItem.UserId}" };
            if (queueItem.CollectionId.HasValue)
            {
                tags.Add($"collection:{queueItem.CollectionId}");
            }
            // CRITICAL FIX: Pass tenantId to ensure tag transformation matches SET operation
            // Use CompanyId as TenantId (they are the same in our multi-tenant architecture)
            var tenantId = queueItem.CompanyId?.ToString();
            await _cacheService.InvalidateByTagsAsync(tags, tenantId);

            _logger.LogInformation("Document {DocumentId} saved to database successfully and cache invalidated, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);

            // Broadcast document added/updated event to refresh collection view
            await _hubContext.Clients.Group($"user_{queueItem.UserId}")
                .SendAsync("DocumentUpdated", new {
                    documentId = queueItem.DocumentId,
                    collectionId = queueItem.CollectionId,
                    action = existingDocument != null ? "updated" : "added",
                    timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document {DocumentId} to database, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, _correlationService.GetCorrelationId());
            throw;
        }
    }

    public async Task<bool> CheckR2RStatusAndUpdateAsync(R2RProcessingQueueItemDto queueItem)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Checking R2R status for document {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);

            // For documents with pending R2R IDs, we need to check if R2R has completed processing
            if (queueItem.R2RDocumentId != null && queueItem.R2RDocumentId.StartsWith("pending_"))
            {
                // Extract the actual document ID from the pending ID
                var actualDocumentId = queueItem.R2RDocumentId.Replace("pending_", "");

                // Try to get the document from R2R API to see if it's been processed
                try
                {
                    var r2rDocument = await _documentClient.GetAsync(actualDocumentId);

                    if (r2rDocument != null && !string.IsNullOrEmpty(r2rDocument.Id))
                    {
                        // Document found in R2R - processing completed successfully
                        _logger.LogInformation("R2R processing completed for document {DocumentId}, R2R ID: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                            queueItem.DocumentId, r2rDocument.Id, correlationId);

                        // Update queue item with actual R2R document ID
                        queueItem.R2RDocumentId = r2rDocument.Id;
                        queueItem.Status = R2RProcessingStatusDto.Completed;
                        queueItem.CompletedAt = DateTime.UtcNow;

                        // Save document to database with completed status
                        await SaveDocumentToDatabaseAsync(queueItem);

                        // Broadcast completion status via SignalR
                        await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

                        // Remove from active processing
                        _activeProcessing.TryRemove(queueItem.Id, out _);

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // If we get a 404 or other error, the document might still be processing
                    _logger.LogDebug("R2R document {DocumentId} not yet available (still processing), CorrelationId: {CorrelationId}",
                        actualDocumentId, correlationId);
                }
            }

            // Check if document has been processing for too long (timeout)
            var processingDuration = DateTime.UtcNow - queueItem.StartedAt;
            if (processingDuration > TimeSpan.FromMinutes(10)) // 10 minute timeout
            {
                _logger.LogWarning("R2R processing timeout for document {DocumentId} after {Duration}, marking as failed, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, processingDuration, correlationId);

                await HandleProcessingFailure(queueItem, $"R2R processing timeout after {processingDuration}");
                return false;
            }

            // Document is still processing, no action needed
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking R2R status for document {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);
            return false;
        }
    }
}

/// <summary>
/// Configuration options for document processing service
/// </summary>
public class DocumentProcessingOptions
{
    public int MaxConcurrentProcessing { get; set; } = 5;
    public int RateLimitDelaySeconds { get; set; } = 10;
    public int BaseRetryDelaySeconds { get; set; } = 5;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDelayMinutes { get; set; } = 10;
    public int CircuitBreakerResetMinutes { get; set; } = 30;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnableOptimization { get; set; } = true;
    public TimeSpan OptimizationInterval { get; set; } = TimeSpan.FromMinutes(5);

    // Background worker configuration
    public int ProcessingIntervalMs { get; set; } = 5000; // 5 seconds when queue has items
    public int IdleIntervalMs { get; set; } = 30000; // 30 seconds when queue is empty
}
