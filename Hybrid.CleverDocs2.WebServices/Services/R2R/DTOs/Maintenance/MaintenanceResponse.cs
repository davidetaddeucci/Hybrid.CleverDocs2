using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Maintenance
{
    public class MaintenanceResponse
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "pending", "running", "completed", "failed"

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_ms")]
        public long? DurationMs { get; set; }

        [JsonPropertyName("result")]
        public Dictionary<string, object> Result { get; set; } = new();

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("progress")]
        public MaintenanceProgress? Progress { get; set; }
    }

    public class MaintenanceProgress
    {
        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("current_step")]
        public string CurrentStep { get; set; } = string.Empty;

        [JsonPropertyName("total_steps")]
        public int TotalSteps { get; set; }

        [JsonPropertyName("completed_steps")]
        public int CompletedSteps { get; set; }

        [JsonPropertyName("estimated_completion")]
        public DateTime? EstimatedCompletion { get; set; }
    }

    public class HealthCheckResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "healthy", "degraded", "unhealthy"

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("components")]
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();

        [JsonPropertyName("overall_score")]
        public double OverallScore { get; set; }

        [JsonPropertyName("response_time_ms")]
        public long ResponseTimeMs { get; set; }
    }

    public class ComponentHealth
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("response_time_ms")]
        public long ResponseTimeMs { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, object> Metrics { get; set; } = new();

        [JsonPropertyName("last_check")]
        public DateTime LastCheck { get; set; }
    }

    public class SystemStatsResponse
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("system_info")]
        public SystemInfo SystemInfo { get; set; } = new();

        [JsonPropertyName("performance_metrics")]
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();

        [JsonPropertyName("database_stats")]
        public DatabaseStats DatabaseStats { get; set; } = new();

        [JsonPropertyName("vector_stats")]
        public VectorStats VectorStats { get; set; } = new();

        [JsonPropertyName("api_stats")]
        public ApiStats ApiStats { get; set; } = new();
    }

    public class SystemInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("uptime_seconds")]
        public long UptimeSeconds { get; set; }

        [JsonPropertyName("cpu_count")]
        public int CpuCount { get; set; }

        [JsonPropertyName("memory_total_gb")]
        public double MemoryTotalGb { get; set; }

        [JsonPropertyName("disk_total_gb")]
        public double DiskTotalGb { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;
    }

    public class PerformanceMetrics
    {
        [JsonPropertyName("cpu_usage_percent")]
        public double CpuUsagePercent { get; set; }

        [JsonPropertyName("memory_usage_percent")]
        public double MemoryUsagePercent { get; set; }

        [JsonPropertyName("disk_usage_percent")]
        public double DiskUsagePercent { get; set; }

        [JsonPropertyName("network_io")]
        public NetworkIO NetworkIO { get; set; } = new();

        [JsonPropertyName("disk_io")]
        public DiskIO DiskIO { get; set; } = new();
    }

    public class NetworkIO
    {
        [JsonPropertyName("bytes_sent")]
        public long BytesSent { get; set; }

        [JsonPropertyName("bytes_received")]
        public long BytesReceived { get; set; }

        [JsonPropertyName("packets_sent")]
        public long PacketsSent { get; set; }

        [JsonPropertyName("packets_received")]
        public long PacketsReceived { get; set; }
    }

    public class DiskIO
    {
        [JsonPropertyName("read_bytes")]
        public long ReadBytes { get; set; }

        [JsonPropertyName("write_bytes")]
        public long WriteBytes { get; set; }

        [JsonPropertyName("read_operations")]
        public long ReadOperations { get; set; }

        [JsonPropertyName("write_operations")]
        public long WriteOperations { get; set; }
    }

    public class DatabaseStats
    {
        [JsonPropertyName("total_documents")]
        public long TotalDocuments { get; set; }

        [JsonPropertyName("total_chunks")]
        public long TotalChunks { get; set; }

        [JsonPropertyName("total_collections")]
        public long TotalCollections { get; set; }

        [JsonPropertyName("database_size_gb")]
        public double DatabaseSizeGb { get; set; }

        [JsonPropertyName("index_size_gb")]
        public double IndexSizeGb { get; set; }

        [JsonPropertyName("connection_count")]
        public int ConnectionCount { get; set; }
    }

    public class VectorStats
    {
        [JsonPropertyName("total_vectors")]
        public long TotalVectors { get; set; }

        [JsonPropertyName("vector_dimensions")]
        public int VectorDimensions { get; set; }

        [JsonPropertyName("index_size_gb")]
        public double IndexSizeGb { get; set; }

        [JsonPropertyName("search_latency_ms")]
        public double SearchLatencyMs { get; set; }

        [JsonPropertyName("indexing_rate")]
        public double IndexingRate { get; set; }
    }

    public class ApiStats
    {
        [JsonPropertyName("total_requests")]
        public long TotalRequests { get; set; }

        [JsonPropertyName("requests_per_minute")]
        public double RequestsPerMinute { get; set; }

        [JsonPropertyName("average_response_time_ms")]
        public double AverageResponseTimeMs { get; set; }

        [JsonPropertyName("error_rate_percent")]
        public double ErrorRatePercent { get; set; }

        [JsonPropertyName("active_connections")]
        public int ActiveConnections { get; set; }
    }

    public class BackupResponse
    {
        [JsonPropertyName("backup_id")]
        public string BackupId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("backup_type")]
        public string BackupType { get; set; } = string.Empty;

        [JsonPropertyName("size_gb")]
        public double SizeGb { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("checksum")]
        public string Checksum { get; set; } = string.Empty;
    }

    public class RestoreResponse
    {
        [JsonPropertyName("restore_id")]
        public string RestoreId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("backup_id")]
        public string BackupId { get; set; } = string.Empty;

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_ms")]
        public long? DurationMs { get; set; }

        [JsonPropertyName("restored_components")]
        public List<string> RestoredComponents { get; set; } = new();

        [JsonPropertyName("verification_status")]
        public string VerificationStatus { get; set; } = string.Empty;
    }

    public class CleanupResponse
    {
        [JsonPropertyName("cleanup_id")]
        public string CleanupId { get; set; } = string.Empty;

        [JsonPropertyName("cleanup_type")]
        public string CleanupType { get; set; } = string.Empty;

        [JsonPropertyName("items_found")]
        public long ItemsFound { get; set; }

        [JsonPropertyName("items_cleaned")]
        public long ItemsCleaned { get; set; }

        [JsonPropertyName("space_freed_gb")]
        public double SpaceFreedGb { get; set; }

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }

        [JsonPropertyName("dry_run")]
        public bool DryRun { get; set; }

        [JsonPropertyName("details")]
        public List<CleanupDetail> Details { get; set; } = new();
    }

    public class CleanupDetail
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("items_count")]
        public long ItemsCount { get; set; }

        [JsonPropertyName("size_gb")]
        public double SizeGb { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class LogsResponse
    {
        [JsonPropertyName("logs")]
        public List<LogEntry> Logs { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class LogEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("component")]
        public string Component { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("trace_id")]
        public string? TraceId { get; set; }
    }

    public class MessageResponse3
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
