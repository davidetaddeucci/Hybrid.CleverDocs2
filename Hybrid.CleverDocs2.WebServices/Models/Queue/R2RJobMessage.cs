namespace Hybrid.CleverDocs2.WebServices.Models.Queue;

/// <summary>
/// Base message for R2R job operations
/// </summary>
public abstract class R2RJobMessage
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string JobId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Company ID for multi-tenant isolation
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// User ID who initiated the operation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Priority of the job (0 = highest, 255 = lowest)
    /// </summary>
    public byte Priority { get; set; } = 128;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of retry attempts allowed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Additional metadata for the job
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Message for document ingestion jobs
/// </summary>
public class DocumentIngestionMessage : R2RJobMessage
{
    /// <summary>
    /// Document ID in the local database
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// File path or URL to the document
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the document
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Document metadata
    /// </summary>
    public Dictionary<string, object> DocumentMetadata { get; set; } = new();

    /// <summary>
    /// Collection IDs to assign the document to
    /// </summary>
    public List<string> CollectionIds { get; set; } = new();
}

/// <summary>
/// Message for embedding generation jobs
/// </summary>
public class EmbeddingGenerationMessage : R2RJobMessage
{
    /// <summary>
    /// R2R document ID
    /// </summary>
    public string R2RDocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Text content to generate embeddings for
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Chunk size for processing
    /// </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>
    /// Overlap between chunks
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;
}

/// <summary>
/// Message for search operations
/// </summary>
public class SearchOperationMessage : R2RJobMessage
{
    /// <summary>
    /// Search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Collection IDs to search in
    /// </summary>
    public List<string> CollectionIds { get; set; } = new();

    /// <summary>
    /// Maximum number of results
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Search filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Message for collection operations
/// </summary>
public class CollectionOperationMessage : R2RJobMessage
{
    /// <summary>
    /// Type of collection operation
    /// </summary>
    public CollectionOperationType OperationType { get; set; }

    /// <summary>
    /// Collection ID
    /// </summary>
    public string CollectionId { get; set; } = string.Empty;

    /// <summary>
    /// Collection name
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Collection description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Document IDs to add/remove from collection
    /// </summary>
    public List<string> DocumentIds { get; set; } = new();
}

/// <summary>
/// Message for conversation operations
/// </summary>
public class ConversationOperationMessage : R2RJobMessage
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// User message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Collection IDs for context
    /// </summary>
    public List<string> CollectionIds { get; set; } = new();

    /// <summary>
    /// RAG configuration
    /// </summary>
    public Dictionary<string, object> RagConfig { get; set; } = new();
}

/// <summary>
/// Types of collection operations
/// </summary>
public enum CollectionOperationType
{
    Create,
    Update,
    Delete,
    AddDocuments,
    RemoveDocuments
}

/// <summary>
/// Message for job status updates
/// </summary>
public class JobStatusMessage
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Job status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error details if job failed
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp of status update
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Result data if job completed successfully
    /// </summary>
    public Dictionary<string, object>? ResultData { get; set; }
}

/// <summary>
/// Job status enumeration
/// </summary>
public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Retrying
}
