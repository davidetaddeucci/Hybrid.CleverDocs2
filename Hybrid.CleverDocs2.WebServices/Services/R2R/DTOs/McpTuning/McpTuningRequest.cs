using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.McpTuning
{
    public class McpTuningRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("tuning_type")]
        public string TuningType { get; set; } = "fine_tuning"; // "fine_tuning", "hyperparameter", "prompt_tuning"

        [JsonPropertyName("dataset_id")]
        public string? DatasetId { get; set; }

        [JsonPropertyName("training_config")]
        public TrainingConfig TrainingConfig { get; set; } = new();

        [JsonPropertyName("hyperparameters")]
        public Dictionary<string, object> Hyperparameters { get; set; } = new();

        [JsonPropertyName("validation_split")]
        public double ValidationSplit { get; set; } = 0.2;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TrainingConfig
    {
        [JsonPropertyName("epochs")]
        public int Epochs { get; set; } = 10;

        [JsonPropertyName("batch_size")]
        public int BatchSize { get; set; } = 32;

        [JsonPropertyName("learning_rate")]
        public double LearningRate { get; set; } = 0.001;

        [JsonPropertyName("optimizer")]
        public string Optimizer { get; set; } = "adam";

        [JsonPropertyName("loss_function")]
        public string LossFunction { get; set; } = "cross_entropy";

        [JsonPropertyName("early_stopping")]
        public bool EarlyStopping { get; set; } = true;

        [JsonPropertyName("patience")]
        public int Patience { get; set; } = 5;

        [JsonPropertyName("checkpoint_frequency")]
        public int CheckpointFrequency { get; set; } = 100;
    }

    public class TuningJobRequest
    {
        [JsonPropertyName("job_name")]
        public string JobName { get; set; } = string.Empty;

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("tuning_request")]
        public McpTuningRequest TuningRequest { get; set; } = new();

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "normal"; // "low", "normal", "high", "urgent"

        [JsonPropertyName("schedule")]
        public JobSchedule? Schedule { get; set; }
    }

    public class JobSchedule
    {
        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("max_duration_hours")]
        public int? MaxDurationHours { get; set; }

        [JsonPropertyName("retry_count")]
        public int RetryCount { get; set; } = 3;
    }

    public class HyperparameterOptimizationRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("optimization_method")]
        public string OptimizationMethod { get; set; } = "bayesian"; // "grid", "random", "bayesian"

        [JsonPropertyName("parameter_space")]
        public Dictionary<string, ParameterRange> ParameterSpace { get; set; } = new();

        [JsonPropertyName("objective_metric")]
        public string ObjectiveMetric { get; set; } = "accuracy";

        [JsonPropertyName("max_trials")]
        public int MaxTrials { get; set; } = 100;

        [JsonPropertyName("max_duration_hours")]
        public int MaxDurationHours { get; set; } = 24;
    }

    public class ParameterRange
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "float", "int", "categorical"

        [JsonPropertyName("min_value")]
        public double? MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public double? MaxValue { get; set; }

        [JsonPropertyName("values")]
        public List<object>? Values { get; set; }

        [JsonPropertyName("step")]
        public double? Step { get; set; }
    }

    public class ModelEvaluationRequest
    {
        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("test_dataset_id")]
        public string TestDatasetId { get; set; } = string.Empty;

        [JsonPropertyName("metrics")]
        public List<string> Metrics { get; set; } = new();

        [JsonPropertyName("evaluation_config")]
        public EvaluationConfig EvaluationConfig { get; set; } = new();
    }

    public class EvaluationConfig
    {
        [JsonPropertyName("batch_size")]
        public int BatchSize { get; set; } = 64;

        [JsonPropertyName("include_predictions")]
        public bool IncludePredictions { get; set; } = false;

        [JsonPropertyName("confidence_threshold")]
        public double? ConfidenceThreshold { get; set; }

        [JsonPropertyName("detailed_analysis")]
        public bool DetailedAnalysis { get; set; } = true;
    }

    public class ModelDeploymentRequest
    {
        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("deployment_name")]
        public string DeploymentName { get; set; } = string.Empty;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "staging"; // "staging", "production"

        [JsonPropertyName("scaling_config")]
        public ScalingConfig ScalingConfig { get; set; } = new();

        [JsonPropertyName("health_check_config")]
        public HealthCheckConfig HealthCheckConfig { get; set; } = new();
    }

    public class ScalingConfig
    {
        [JsonPropertyName("min_instances")]
        public int MinInstances { get; set; } = 1;

        [JsonPropertyName("max_instances")]
        public int MaxInstances { get; set; } = 10;

        [JsonPropertyName("target_cpu_utilization")]
        public double TargetCpuUtilization { get; set; } = 70.0;

        [JsonPropertyName("scale_up_cooldown")]
        public int ScaleUpCooldown { get; set; } = 300;

        [JsonPropertyName("scale_down_cooldown")]
        public int ScaleDownCooldown { get; set; } = 600;
    }

    public class HealthCheckConfig
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "/health";

        [JsonPropertyName("interval_seconds")]
        public int IntervalSeconds { get; set; } = 30;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 10;

        [JsonPropertyName("failure_threshold")]
        public int FailureThreshold { get; set; } = 3;
    }

    public class TuningJobListRequest
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } // "pending", "running", "completed", "failed", "cancelled"

        [JsonPropertyName("model_name")]
        public string? ModelName { get; set; }

        [JsonPropertyName("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [JsonPropertyName("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }
}
