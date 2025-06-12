using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    public enum ChunkStatus { Pending, Processing, Completed, Failed }

    public class DocumentChunk
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document Document { get; set; } = null!;
        
        public Guid? IngestionJobId { get; set; }
        public IngestionJob? IngestionJob { get; set; }
        
        [Required]
        public int Sequence { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public string? Data { get; set; }
        public string? Metadata { get; set; }
        public ChunkStatus Status { get; set; } = ChunkStatus.Pending;
        public string? R2RResult { get; set; }

        // R2R Integration
        public string? R2RChunkId { get; set; }
        public string? R2RVectorId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
