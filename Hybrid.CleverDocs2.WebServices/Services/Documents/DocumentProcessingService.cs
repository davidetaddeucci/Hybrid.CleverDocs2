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
        IDocumentClient documentClient)
    {
        _rateLimitingService = rateLimitingService;
        _cacheService = cacheService;
        _logger = logger;
        _correlationService = correlationService;
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _documentClient = documentClient;

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
                    L2TTL = TimeSpan.FromHours(24),
                    UseL3Cache = true
                });

            // Broadcast queued status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

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
                queueItem.Status = R2RProcessingStatusDto.Completed;
                queueItem.CompletedAt = DateTime.UtcNow;
                _consecutiveFailures = 0;

                // Save document to database
                await SaveDocumentToDatabaseAsync(queueItem);

                // Broadcast completion status via SignalR
                await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

                _logger.LogInformation("R2R processing completed successfully for document: {DocumentId}, R2RDocumentId: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);
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

            // Create IFormFile from temporary file
            var fileBytes = await File.ReadAllBytesAsync(queueItem.FilePath);
            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", queueItem.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = queueItem.ContentType
            };

            // Prepare document request for R2R API
            var documentRequest = new DocumentRequest
            {
                File = formFile,
                Title = queueItem.FileName,
                DocumentType = queueItem.ContentType,
                Metadata = new Dictionary<string, object>
                {
                    ["original_filename"] = queueItem.FileName,
                    ["file_size"] = queueItem.FileSize,
                    ["upload_timestamp"] = DateTime.UtcNow,
                    ["user_id"] = queueItem.UserId,
                    ["company_id"] = queueItem.CompanyId
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

            // R2R API can return either immediate response (200) or async response (202)
            if (documentResponse != null && !string.IsNullOrEmpty(documentResponse.Id))
            {
                // Immediate success - store R2R document ID from response
                queueItem.R2RDocumentId = documentResponse.Id;

                _logger.LogInformation("R2R processing completed successfully (immediate) for document {DocumentId}, R2R ID: {R2RDocumentId}, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, queueItem.R2RDocumentId, correlationId);

                return true;
            }
            else
            {
                // Async processing (HTTP 202) - R2R accepted the document for background processing
                // This is normal for R2R async ingestion workflow
                _logger.LogInformation("R2R processing accepted (async) for document {DocumentId}, will complete in background, CorrelationId: {CorrelationId}",
                    queueItem.DocumentId, correlationId);

                // Mark as successfully submitted to R2R (even without immediate ID)
                // The document will be saved to database and SignalR will notify when R2R completes
                queueItem.R2RDocumentId = $"pending_{queueItem.DocumentId}"; // Temporary ID for tracking
                queueItem.Status = R2RProcessingStatusDto.Processing; // Keep processing status for async workflow

                return true; // Success - document accepted by R2R for async processing
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error during R2R processing for document {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "R2R processing failed for document {DocumentId}, CorrelationId: {CorrelationId}",
                queueItem.DocumentId, correlationId);
            return false;
        }
    }

    private async Task HandleProcessingFailure(R2RProcessingQueueItemDto queueItem, string errorMessage)
    {
        queueItem.Status = R2RProcessingStatusDto.Failed;
        queueItem.ErrorMessage = errorMessage;
        queueItem.RetryCount++;

        _consecutiveFailures++;
        _lastR2RFailure = DateTime.UtcNow;

        // Broadcast failure status via SignalR
        await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

        // Check if we should open circuit breaker
        if (_consecutiveFailures >= _options.CircuitBreakerThreshold)
        {
            _circuitBreakerOpen = true;
            _logger.LogWarning("Circuit breaker opened due to {ConsecutiveFailures} consecutive failures", _consecutiveFailures);
        }

        // Determine if we should retry
        if (queueItem.RetryCount < queueItem.MaxRetries)
        {
            var delay = CalculateRetryDelay(queueItem.RetryCount);
            queueItem.NextRetryAt = DateTime.UtcNow.Add(delay);
            queueItem.Status = R2RProcessingStatusDto.Retrying;

            // Broadcast retry status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogInformation("Scheduling retry {RetryCount}/{MaxRetries} for document {DocumentId} in {Delay}",
                queueItem.RetryCount, queueItem.MaxRetries, queueItem.DocumentId, delay);

            // Re-queue for retry
            _processingQueue.Enqueue(queueItem);
        }
        else
        {
            // Max retries reached, move to failed items
            _failedItems[queueItem.Id] = queueItem;

            // Broadcast final failure status via SignalR
            await _hubContext.BroadcastR2RProcessingUpdate(queueItem.UserId, queueItem);

            _logger.LogError("Document processing failed permanently after {MaxRetries} retries: {DocumentId}, Error: {ErrorMessage}",
                queueItem.MaxRetries, queueItem.DocumentId, errorMessage);
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
                existingDocument.Status = (int)Data.Entities.DocumentStatus.Ready;
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

                var document = new Document
                {
                    Id = queueItem.DocumentId,
                    Name = queueItem.FileName,
                    FileName = queueItem.FileName,
                    OriginalFileName = queueItem.FileName,
                    SizeBytes = queueItem.FileSize,
                    Size = queueItem.FileSize,
                    ContentType = queueItem.ContentType,
                    Status = (int)Data.Entities.DocumentStatus.Ready,
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

            // Invalidate document cache patterns to ensure fresh data is loaded
            await _cacheService.InvalidateAsync($"*type:pageddocumentresultdto:documents:search:*");
            await _cacheService.InvalidateAsync($"*user:documents:{queueItem.UserId}*");
            if (queueItem.CollectionId.HasValue)
            {
                await _cacheService.InvalidateAsync($"*collection:documents:{queueItem.CollectionId}*");
                // Also invalidate collection details cache to update document count
                await _cacheService.InvalidateAsync($"*collection:details:{queueItem.CollectionId}*");
            }

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
}
