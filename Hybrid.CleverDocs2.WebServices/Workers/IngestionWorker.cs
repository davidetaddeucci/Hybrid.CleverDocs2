using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Messages;

namespace Hybrid.CleverDocs2.WebServices.Workers
{
    public class IngestionWorker : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<IngestionWorker> _logger;

        public IngestionWorker(IServiceProvider provider, ILogger<IngestionWorker> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var jobs = db.IngestionJobs
                    .Where(j => j.Status == IngestionStatus.Pending)
                    .ToList();

                foreach (var job in jobs)
                {
                    job.Status = IngestionStatus.Processing;
                    foreach (var chunk in job.Chunks.Where(c => c.Status == ChunkStatus.Pending))
                    {
                        await publish.Publish<IngestionChunkMessage>(new
                        {
                            JobId = job.Id,
                            ChunkId = chunk.Id,
                            Sequence = chunk.Sequence,
                            Data = chunk.Data
                        }, stoppingToken);
                        chunk.Status = ChunkStatus.Processing;
                    }
                    db.Update(job);
                }
                await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
