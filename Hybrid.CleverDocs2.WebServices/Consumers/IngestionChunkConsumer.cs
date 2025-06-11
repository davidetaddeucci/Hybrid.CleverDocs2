using System.Threading.Tasks;
using MassTransit;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Messages;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion;

namespace Hybrid.CleverDocs2.WebServices.Consumers
{
    public class IngestionChunkConsumer : IConsumer<IngestionChunkMessage>
    {
        private readonly ApplicationDbContext _db;
        private readonly IIngestionClient _r2rClient;

        public IngestionChunkConsumer(ApplicationDbContext db, IIngestionClient r2rClient)
        {
            _db = db;
            _r2rClient = r2rClient;
        }

        public async Task Consume(ConsumeContext<IngestionChunkMessage> context)
        {
            var msg = context.Message;
            // Call R2R ingestion API
            var request = new IngestionRequest { JobId = msg.JobId, Sequence = msg.Sequence, Data = msg.Data };
            var result = await _r2rClient.CreateAsync(request);

            // Update DB
            var chunk = await _db.DocumentChunks.FindAsync(msg.ChunkId);
            if (chunk != null)
            {
                chunk.Status = ChunkStatus.Completed;
                chunk.R2RResult = result.ToString();
            }
            await _db.SaveChangesAsync(context.CancellationToken);
        }
    }
}
