using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Maintenance
{
    public class MaintenanceRequest
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;

        [JsonPropertyName("dry_run")]
        public bool DryRun { get; set; } = false;
    }

    public class HealthCheckRequest
    {
        [JsonPropertyName("components")]
        public List<string>? Components { get; set; }

        [JsonPropertyName("detailed")]
        public bool Detailed { get; set; } = false;
    }

    public class SystemStatsRequest
    {
        [JsonPropertyName("include_metrics")]
        public bool IncludeMetrics { get; set; } = true;

        [JsonPropertyName("include_performance")]
        public bool IncludePerformance { get; set; } = true;

        [JsonPropertyName("time_range")]
        public string? TimeRange { get; set; } // "1h", "24h", "7d", "30d"
    }

    public class DatabaseMaintenanceRequest
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty; // "vacuum", "reindex", "analyze", "cleanup"

        [JsonPropertyName("tables")]
        public List<string>? Tables { get; set; }

        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class IndexMaintenanceRequest
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty; // "rebuild", "optimize", "cleanup"

        [JsonPropertyName("index_types")]
        public List<string>? IndexTypes { get; set; } // "vector", "text", "metadata"

        [JsonPropertyName("collection_ids")]
        public List<string>? CollectionIds { get; set; }

        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;
    }

    public class BackupRequest
    {
        [JsonPropertyName("backup_type")]
        public string BackupType { get; set; } = "full"; // "full", "incremental", "differential"

        [JsonPropertyName("include_vectors")]
        public bool IncludeVectors { get; set; } = true;

        [JsonPropertyName("include_documents")]
        public bool IncludeDocuments { get; set; } = true;

        [JsonPropertyName("include_metadata")]
        public bool IncludeMetadata { get; set; } = true;

        [JsonPropertyName("compression")]
        public bool Compression { get; set; } = true;

        [JsonPropertyName("destination")]
        public string? Destination { get; set; }
    }

    public class RestoreRequest
    {
        [JsonPropertyName("backup_id")]
        public string BackupId { get; set; } = string.Empty;

        [JsonPropertyName("restore_type")]
        public string RestoreType { get; set; } = "full"; // "full", "selective"

        [JsonPropertyName("components")]
        public List<string>? Components { get; set; }

        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;

        [JsonPropertyName("verify")]
        public bool Verify { get; set; } = true;
    }

    public class CleanupRequest
    {
        [JsonPropertyName("cleanup_type")]
        public string CleanupType { get; set; } = string.Empty; // "orphaned", "expired", "temporary", "logs"

        [JsonPropertyName("older_than")]
        public string? OlderThan { get; set; } // "7d", "30d", "90d"

        [JsonPropertyName("dry_run")]
        public bool DryRun { get; set; } = true;

        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;
    }

    public class LogsRequest
    {
        [JsonPropertyName("level")]
        public string? Level { get; set; } // "debug", "info", "warning", "error"

        [JsonPropertyName("component")]
        public string? Component { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }
}
