using System;

namespace Hybrid.CleverDocs2.WebServices.Messages
{
    public interface IngestionChunkMessage
    {
        Guid JobId { get; }
        Guid ChunkId { get; }
        int Sequence { get; }
        string? Data { get; }
    }
}
