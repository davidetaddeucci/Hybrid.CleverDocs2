using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.McpTuning
{
    public class McpTuningResponse
    {
        [JsonPropertyName("tuning_id")]
        public string TuningId { get; set; } = string.Empty;

        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "pending", "running", "completed", "failed", "cancelled"

        [JsonPropertyName("progress")]
        public TuningProgress Progress { get; set; } = new();

        [JsonPropertyName("metrics")]
        public TuningMetrics Metrics { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("result_model_id")]
        public string? ResultModelId { get; set; }
    }

    public class TuningProgress
    {
        [JsonPropertyName("current_epoch")]
        public int CurrentEpoch { get; set; }

        [JsonPropertyName("total_epochs")]
        public int TotalEpochs { get; set; }

        [JsonPropertyName("current_step")]
        public int CurrentStep { get; set; }

        [JsonPropertyName("total_steps")]
        public int TotalSteps { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("estimated_completion")]
        public DateTime? EstimatedCompletion { get; set; }

        [JsonPropertyName("elapsed_time_seconds")]
        public long ElapsedTimeSeconds { get; set; }
    }

    public class TuningMetrics
    {
        [JsonPropertyName("training_loss")]
        public double? TrainingLoss { get; set; }

        [JsonPropertyName("validation_loss")]
        public double? ValidationLoss { get; set; }

        [JsonPropertyName("training_accuracy")]
        public double? TrainingAccuracy { get; set; }

        [JsonPropertyName("validation_accuracy")]
        public double? ValidationAccuracy { get; set; }

        [JsonPropertyName("learning_rate")]
        public double? LearningRate { get; set; }

        [JsonPropertyName("custom_metrics")]
        public Dictionary<string, double> CustomMetrics { get; set; } = new();

        [JsonPropertyName("best_epoch")]
        public int? BestEpoch { get; set; }

        [JsonPropertyName("best_validation_score")]
        public double? BestValidationScore { get; set; }
    }

    public class TuningJobResponse
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("job_name")]
        public string JobName { get; set; } = string.Empty;

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("tuning_response")]
        public McpTuningResponse? TuningResponse { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("retry_count")]
        public int RetryCount { get; set; }

        [JsonPropertyName("max_retries")]
        public int MaxRetries { get; set; }
    }

    public class HyperparameterOptimizationResponse
    {
        [JsonPropertyName("optimization_id")]
        public string OptimizationId { get; set; } = string.Empty;

        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("optimization_method")]
        public string OptimizationMethod { get; set; } = string.Empty;

        [JsonPropertyName("objective_metric")]
        public string ObjectiveMetric { get; set; } = string.Empty;

        [JsonPropertyName("best_trial")]
        public OptimizationTrial? BestTrial { get; set; }

        [JsonPropertyName("trials_completed")]
        public int TrialsCompleted { get; set; }

        [JsonPropertyName("total_trials")]
        public int TotalTrials { get; set; }

        [JsonPropertyName("progress_percentage")]
        public double ProgressPercentage { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("trials")]
        public List<OptimizationTrial> Trials { get; set; } = new();
    }

    public class OptimizationTrial
    {
        [JsonPropertyName("trial_id")]
        public string TrialId { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("objective_value")]
        public double? ObjectiveValue { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, double> Metrics { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long? DurationSeconds { get; set; }
    }

    public class ModelEvaluationResponse
    {
        [JsonPropertyName("evaluation_id")]
        public string EvaluationId { get; set; } = string.Empty;

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("test_dataset_id")]
        public string TestDatasetId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("metrics")]
        public EvaluationMetrics Metrics { get; set; } = new();

        [JsonPropertyName("predictions")]
        public List<PredictionResult>? Predictions { get; set; }

        [JsonPropertyName("confusion_matrix")]
        public ConfusionMatrix? ConfusionMatrix { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("evaluation_time_seconds")]
        public long? EvaluationTimeSeconds { get; set; }
    }

    public class EvaluationMetrics
    {
        [JsonPropertyName("accuracy")]
        public double? Accuracy { get; set; }

        [JsonPropertyName("precision")]
        public double? Precision { get; set; }

        [JsonPropertyName("recall")]
        public double? Recall { get; set; }

        [JsonPropertyName("f1_score")]
        public double? F1Score { get; set; }

        [JsonPropertyName("auc_roc")]
        public double? AucRoc { get; set; }

        [JsonPropertyName("loss")]
        public double? Loss { get; set; }

        [JsonPropertyName("custom_metrics")]
        public Dictionary<string, double> CustomMetrics { get; set; } = new();

        [JsonPropertyName("per_class_metrics")]
        public Dictionary<string, ClassMetrics> PerClassMetrics { get; set; } = new();
    }

    public class ClassMetrics
    {
        [JsonPropertyName("precision")]
        public double Precision { get; set; }

        [JsonPropertyName("recall")]
        public double Recall { get; set; }

        [JsonPropertyName("f1_score")]
        public double F1Score { get; set; }

        [JsonPropertyName("support")]
        public int Support { get; set; }
    }

    public class PredictionResult
    {
        [JsonPropertyName("input")]
        public object Input { get; set; } = new();

        [JsonPropertyName("predicted")]
        public object Predicted { get; set; } = new();

        [JsonPropertyName("actual")]
        public object? Actual { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("probabilities")]
        public Dictionary<string, double>? Probabilities { get; set; }
    }

    public class ConfusionMatrix
    {
        [JsonPropertyName("matrix")]
        public int[][] Matrix { get; set; } = Array.Empty<int[]>();

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("normalized")]
        public bool Normalized { get; set; }
    }

    public class ModelDeploymentResponse
    {
        [JsonPropertyName("deployment_id")]
        public string DeploymentId { get; set; } = string.Empty;

        [JsonPropertyName("deployment_name")]
        public string DeploymentName { get; set; } = string.Empty;

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "deploying", "active", "inactive", "failed"

        [JsonPropertyName("endpoint_url")]
        public string? EndpointUrl { get; set; }

        [JsonPropertyName("current_instances")]
        public int CurrentInstances { get; set; }

        [JsonPropertyName("health_status")]
        public string HealthStatus { get; set; } = string.Empty;

        [JsonPropertyName("deployment_metrics")]
        public DeploymentMetrics DeploymentMetrics { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }

    public class DeploymentMetrics
    {
        [JsonPropertyName("requests_per_minute")]
        public double RequestsPerMinute { get; set; }

        [JsonPropertyName("average_response_time_ms")]
        public double AverageResponseTimeMs { get; set; }

        [JsonPropertyName("error_rate_percentage")]
        public double ErrorRatePercentage { get; set; }

        [JsonPropertyName("cpu_utilization")]
        public double CpuUtilization { get; set; }

        [JsonPropertyName("memory_utilization")]
        public double MemoryUtilization { get; set; }

        [JsonPropertyName("throughput")]
        public double Throughput { get; set; }
    }

    public class TuningJobListResponse
    {
        [JsonPropertyName("jobs")]
        public List<TuningJobResponse> Jobs { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class MessageResponse4
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
