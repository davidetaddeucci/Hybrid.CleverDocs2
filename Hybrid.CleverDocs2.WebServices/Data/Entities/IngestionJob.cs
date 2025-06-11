using System;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public enum IngestionStatus { Pending, Processing, Completed, Failed }

    public class IngestionJob
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public IngestionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
