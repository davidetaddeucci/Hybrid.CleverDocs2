using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion
{
    public class IngestionRequest
    {
        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("ingestion_config")]
        public IngestionConfig IngestionConfig { get; set; } = new();
    }

    public class IngestionConfig
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "r2r";

        [JsonPropertyName("strategy")]
        public string Strategy { get; set; } = "auto";

        [JsonPropertyName("chunking_strategy")]
        public string ChunkingStrategy { get; set; } = "recursive";

        [JsonPropertyName("chunk_size")]
        public int ChunkSize { get; set; } = 1024;

        [JsonPropertyName("chunk_overlap")]
        public int ChunkOverlap { get; set; } = 256;

        [JsonPropertyName("enable_summarization")]
        public bool EnableSummarization { get; set; } = false;

        [JsonPropertyName("summarization_model")]
        public string? SummarizationModel { get; set; }

        [JsonPropertyName("summarization_config")]
        public Dictionary<string, object> SummarizationConfig { get; set; } = new();
    }

    public class IngestionStatusRequest
    {
        [JsonPropertyName("ingestion_ids")]
        public List<string>? IngestionIds { get; set; }
    }

    public class IngestionListRequest
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("document_ids")]
        public List<string>? DocumentIds { get; set; }

        [JsonPropertyName("ingestion_ids")]
        public List<string>? IngestionIds { get; set; }
    }

    public class IngestionUpdateRequest
    {
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("ingestion_config")]
        public IngestionConfig? IngestionConfig { get; set; }
    }

    public class IngestionRetryRequest
    {
        [JsonPropertyName("ingestion_ids")]
        public List<string> IngestionIds { get; set; } = new();

        [JsonPropertyName("retry_config")]
        public Dictionary<string, object> RetryConfig { get; set; } = new();
    }

    public class IngestionCancelRequest
    {
        [JsonPropertyName("ingestion_ids")]
        public List<string> IngestionIds { get; set; } = new();

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
