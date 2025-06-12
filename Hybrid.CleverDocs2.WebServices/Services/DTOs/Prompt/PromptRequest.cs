using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Prompt
{
    public class PromptRequest
    {
        [JsonPropertyName("prompt_text")]
        public string PromptText { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public Dictionary<string, object> Context { get; set; } = new();

        [JsonPropertyName("variables")]
        public Dictionary<string, object> Variables { get; set; } = new();

        [JsonPropertyName("template_id")]
        public string? TemplateId { get; set; }

        [JsonPropertyName("model_config")]
        public ModelConfig? ModelConfig { get; set; }

        [JsonPropertyName("generation_config")]
        public GenerationConfig? GenerationConfig { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ModelConfig
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("stop_sequences")]
        public List<string> StopSequences { get; set; } = new();
    }

    public class GenerationConfig
    {
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("include_usage")]
        public bool IncludeUsage { get; set; } = true;

        [JsonPropertyName("include_metadata")]
        public bool IncludeMetadata { get; set; } = true;

        [JsonPropertyName("response_format")]
        public string? ResponseFormat { get; set; } // "text", "json", "structured"

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("timeout_seconds")]
        public int? TimeoutSeconds { get; set; }
    }

    public class PromptTemplateRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("template")]
        public string Template { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<TemplateVariable> Variables { get; set; } = new();

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("default_model_config")]
        public ModelConfig? DefaultModelConfig { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; } = false;
    }

    public class TemplateVariable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "string", "number", "boolean", "array", "object"

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;

        [JsonPropertyName("default_value")]
        public object? DefaultValue { get; set; }

        [JsonPropertyName("validation")]
        public VariableValidation? Validation { get; set; }
    }

    public class VariableValidation
    {
        [JsonPropertyName("min_length")]
        public int? MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("allowed_values")]
        public List<object>? AllowedValues { get; set; }

        [JsonPropertyName("min_value")]
        public double? MinValue { get; set; }

        [JsonPropertyName("max_value")]
        public double? MaxValue { get; set; }
    }

    public class PromptOptimizationRequest
    {
        [JsonPropertyName("prompt_text")]
        public string PromptText { get; set; } = string.Empty;

        [JsonPropertyName("objective")]
        public string Objective { get; set; } = string.Empty; // "clarity", "performance", "safety", "efficiency"

        [JsonPropertyName("target_metrics")]
        public List<string> TargetMetrics { get; set; } = new();

        [JsonPropertyName("test_cases")]
        public List<PromptTestCase> TestCases { get; set; } = new();

        [JsonPropertyName("optimization_config")]
        public OptimizationConfig OptimizationConfig { get; set; } = new();
    }

    public class PromptTestCase
    {
        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; } = new();

        [JsonPropertyName("expected_output")]
        public string? ExpectedOutput { get; set; }

        [JsonPropertyName("evaluation_criteria")]
        public List<string> EvaluationCriteria { get; set; } = new();

        [JsonPropertyName("weight")]
        public double Weight { get; set; } = 1.0;
    }

    public class OptimizationConfig
    {
        [JsonPropertyName("max_iterations")]
        public int MaxIterations { get; set; } = 10;

        [JsonPropertyName("optimization_method")]
        public string OptimizationMethod { get; set; } = "genetic"; // "genetic", "gradient", "random", "bayesian"

        [JsonPropertyName("population_size")]
        public int PopulationSize { get; set; } = 20;

        [JsonPropertyName("mutation_rate")]
        public double MutationRate { get; set; } = 0.1;

        [JsonPropertyName("convergence_threshold")]
        public double ConvergenceThreshold { get; set; } = 0.01;
    }

    public class PromptEvaluationRequest
    {
        [JsonPropertyName("prompt_text")]
        public string PromptText { get; set; } = string.Empty;

        [JsonPropertyName("test_cases")]
        public List<PromptTestCase> TestCases { get; set; } = new();

        [JsonPropertyName("evaluation_metrics")]
        public List<string> EvaluationMetrics { get; set; } = new();

        [JsonPropertyName("model_config")]
        public ModelConfig? ModelConfig { get; set; }

        [JsonPropertyName("evaluation_config")]
        public EvaluationConfig EvaluationConfig { get; set; } = new();
    }

    public class EvaluationConfig
    {
        [JsonPropertyName("include_reasoning")]
        public bool IncludeReasoning { get; set; } = true;

        [JsonPropertyName("include_confidence")]
        public bool IncludeConfidence { get; set; } = true;

        [JsonPropertyName("parallel_execution")]
        public bool ParallelExecution { get; set; } = true;

        [JsonPropertyName("max_retries")]
        public int MaxRetries { get; set; } = 3;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class PromptListRequest
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("search_query")]
        public string? SearchQuery { get; set; }

        [JsonPropertyName("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [JsonPropertyName("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50;

        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;
    }

    public class PromptVersionRequest
    {
        [JsonPropertyName("template_id")]
        public string TemplateId { get; set; } = string.Empty;

        [JsonPropertyName("template")]
        public string Template { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<TemplateVariable> Variables { get; set; } = new();

        [JsonPropertyName("version_notes")]
        public string VersionNotes { get; set; } = string.Empty;

        [JsonPropertyName("model_config")]
        public ModelConfig? ModelConfig { get; set; }
    }
}
