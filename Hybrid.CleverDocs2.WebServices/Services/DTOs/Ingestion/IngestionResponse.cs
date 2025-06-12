using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion
{
    public class IngestionResponse
    {
        [JsonPropertyName("ingestion_id")]
        public string IngestionId { get; set; } = string.Empty;

        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // pending, processing, completed, failed, cancelled

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("ingestion_config")]
        public IngestionConfig IngestionConfig { get; set; } = new();

        [JsonPropertyName("progress")]
        public IngestionProgress? Progress { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("chunks_created")]
        public int ChunksCreated { get; set; }

        [JsonPropertyName("processing_time_ms")]
        public long ProcessingTimeMs { get; set; }
    }

    public class IngestionProgress
    {
        [JsonPropertyName("total_steps")]
        public int TotalSteps { get; set; }

        [JsonPropertyName("completed_steps")]
        public int CompletedSteps { get; set; }

        [JsonPropertyName("current_step")]
        public string CurrentStep { get; set; } = string.Empty;

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("estimated_completion")]
        public DateTime? EstimatedCompletion { get; set; }
    }

    public class IngestionCreateResponse
    {
        [JsonPropertyName("results")]
        public IngestionResponse Results { get; set; } = new();
    }

    public class IngestionListResponse
    {
        [JsonPropertyName("results")]
        public List<IngestionResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class IngestionStatusResponse
    {
        [JsonPropertyName("results")]
        public List<IngestionResponse> Results { get; set; } = new();
    }

    public class IngestionStatsResponse
    {
        [JsonPropertyName("total_ingestions")]
        public int TotalIngestions { get; set; }

        [JsonPropertyName("pending_ingestions")]
        public int PendingIngestions { get; set; }

        [JsonPropertyName("processing_ingestions")]
        public int ProcessingIngestions { get; set; }

        [JsonPropertyName("completed_ingestions")]
        public int CompletedIngestions { get; set; }

        [JsonPropertyName("failed_ingestions")]
        public int FailedIngestions { get; set; }

        [JsonPropertyName("cancelled_ingestions")]
        public int CancelledIngestions { get; set; }

        [JsonPropertyName("average_processing_time_ms")]
        public double AverageProcessingTimeMs { get; set; }

        [JsonPropertyName("total_chunks_created")]
        public int TotalChunksCreated { get; set; }

        [JsonPropertyName("total_documents_processed")]
        public int TotalDocumentsProcessed { get; set; }
    }

    public class IngestionRetryResponse
    {
        [JsonPropertyName("retried_ingestions")]
        public List<string> RetriedIngestions { get; set; } = new();

        [JsonPropertyName("failed_retries")]
        public List<string> FailedRetries { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class IngestionCancelResponse
    {
        [JsonPropertyName("cancelled_ingestions")]
        public List<string> CancelledIngestions { get; set; } = new();

        [JsonPropertyName("failed_cancellations")]
        public List<string> FailedCancellations { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class IngestionLogsResponse
    {
        [JsonPropertyName("ingestion_id")]
        public string IngestionId { get; set; } = string.Empty;

        [JsonPropertyName("logs")]
        public List<IngestionLogEntry> Logs { get; set; } = new();
    }

    public class IngestionLogEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty; // info, warning, error, debug

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("step")]
        public string? Step { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MessageResponse2
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
