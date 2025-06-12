using System;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public enum ChunkStatus { Pending, Processing, Completed, Failed }

    public class DocumentChunk
    {
        public Guid Id { get; set; }
        public Guid IngestionJobId { get; set; }
        public IngestionJob IngestionJob { get; set; } = null!;
        public int Sequence { get; set; }
        public string? Data { get; set; }
        public ChunkStatus Status { get; set; }
        public string? R2RResult { get; set; }
    }
}
