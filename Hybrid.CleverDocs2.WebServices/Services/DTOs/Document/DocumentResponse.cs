using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;

public class DocumentResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("collection_ids")]
    public List<string> CollectionIds { get; set; } = new();

    [JsonPropertyName("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("document_type")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("size_in_bytes")]
    public long SizeInBytes { get; set; }

    [JsonPropertyName("ingestion_status")]
    public string IngestionStatus { get; set; } = string.Empty;

    [JsonPropertyName("extraction_status")]
    public string ExtractionStatus { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("ingestion_attempt_number")]
    public int IngestionAttemptNumber { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("summary_embedding")]
    public List<double>? SummaryEmbedding { get; set; }

    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }

    [JsonPropertyName("chunks")]
    public List<DocumentChunk>? Chunks { get; set; }
}

// DTO for handling R2R async ingestion responses (HTTP 202)
public class R2RTaskResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("monitor_url")]
    public string? MonitorUrl { get; set; }

    [JsonPropertyName("eta")]
    public DateTime? EstimatedCompletion { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// DTO for R2R task status polling
public class R2RTaskStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("document_id")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("progress")]
    public int? Progress { get; set; }
}

// Unified response for both sync and async operations
public class R2RIngestionResponse
{
    public bool IsAsync { get; set; }
    public DocumentResponse? Document { get; set; }
    public R2RTaskResponse? Task { get; set; }
    public string? DocumentId => Document?.Id ?? Task?.TaskId;
}

public class DocumentChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("collection_ids")]
    public List<string> CollectionIds { get; set; } = new();

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double? Score { get; set; }
}
