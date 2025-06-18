using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Models.Documents;

namespace Hybrid.CleverDocs2.WebServices.Workers
{
    /// <summary>
    /// Background service for continuously processing R2R document queue
    /// </summary>
    public class R2RDocumentProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<R2RDocumentProcessingWorker> _logger;
        private readonly DocumentProcessingOptions _options;

        public R2RDocumentProcessingWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<R2RDocumentProcessingWorker> logger,
            IOptions<DocumentProcessingOptions> options)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("R2R Document Processing Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    // Try to get services - if Redis is not available, skip this iteration
                    IDocumentProcessingService? processingService;
                    ICorrelationService? correlationService;

                    try
                    {
                        processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                        correlationService = scope.ServiceProvider.GetRequiredService<ICorrelationService>();
                    }
                    catch (Exception serviceEx) when (serviceEx.Message.Contains("Redis") || serviceEx.Message.Contains("NOAUTH"))
                    {
                        _logger.LogWarning("Redis connection not available, skipping worker iteration: {Error}", serviceEx.Message);
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }

                    // Get processing queue status
                    var queueItems = await processingService.GetProcessingQueueAsync();

                    if (queueItems.Any())
                    {
                        _logger.LogDebug("Found {Count} items in R2R processing queue", queueItems.Count);

                        // Process items that are ready (not rate limited or delayed)
                        var readyItems = queueItems
                            .Where(item => item.Status == R2RProcessingStatusDto.Queued &&
                                          (item.NextRetryAt == null || item.NextRetryAt <= DateTime.UtcNow))
                            .OrderByDescending(item => (int)item.Priority)
                            .ThenBy(item => item.CreatedAt)
                            .Take(_options.MaxConcurrentProcessing)
                            .ToList();

                        // Check for documents in Processing state that might be completed by R2R
                        var processingItems = queueItems
                            .Where(item => item.Status == R2RProcessingStatusDto.Processing &&
                                          item.R2RDocumentId != null &&
                                          item.R2RDocumentId.StartsWith("pending_"))
                            .ToList();

                        if (readyItems.Any())
                        {
                            _logger.LogInformation("Processing {Count} ready R2R documents", readyItems.Count);

                            // Process items in parallel with concurrency limit
                            var tasks = readyItems.Select(async item =>
                            {
                                try
                                {
                                    using var itemScope = _serviceScopeFactory.CreateScope();
                                    var itemProcessingService = itemScope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

                                    await itemProcessingService.ProcessDocumentAsync(item);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing R2R document {DocumentId} in background worker", item.DocumentId);
                                }
                            });

                            await Task.WhenAll(tasks);
                        }
                        else
                        {
                            _logger.LogDebug("No ready items found in R2R processing queue (all items are rate limited or delayed)");
                        }

                        // Check R2R status for documents in Processing state
                        if (processingItems.Any())
                        {
                            _logger.LogDebug("Checking R2R status for {Count} processing documents", processingItems.Count);

                            var statusCheckTasks = processingItems.Select(async item =>
                            {
                                try
                                {
                                    using var itemScope = _serviceScopeFactory.CreateScope();
                                    var itemProcessingService = itemScope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

                                    await itemProcessingService.CheckR2RStatusAndUpdateAsync(item);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error checking R2R status for document {DocumentId} in background worker", item.DocumentId);
                                }
                            });

                            await Task.WhenAll(statusCheckTasks);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("R2R processing queue is empty");
                    }

                    // Wait before next iteration
                    var delayMs = queueItems.Any() ? _options.ProcessingIntervalMs : _options.IdleIntervalMs;
                    await Task.Delay(delayMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in R2R Document Processing Worker");
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("R2R Document Processing Worker stopped");
        }
    }
}
