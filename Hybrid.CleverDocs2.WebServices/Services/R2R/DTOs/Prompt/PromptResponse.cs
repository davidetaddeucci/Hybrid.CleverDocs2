using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Prompt
{
    public class PromptResponse
    {
        [JsonPropertyName("response_text")]
        public string ResponseText { get; set; } = string.Empty;

        [JsonPropertyName("prompt_id")]
        public string PromptId { get; set; } = string.Empty;

        [JsonPropertyName("model_used")]
        public string ModelUsed { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public UsageInfo Usage { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("processing_time_ms")]
        public long ProcessingTimeMs { get; set; }

        [JsonPropertyName("confidence_score")]
        public double? ConfidenceScore { get; set; }
    }

    public class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("cost")]
        public double? Cost { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";
    }

    public class PromptTemplateResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

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
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("usage_count")]
        public long UsageCount { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }
    }

    public class PromptOptimizationResponse
    {
        [JsonPropertyName("optimization_id")]
        public string OptimizationId { get; set; } = string.Empty;

        [JsonPropertyName("original_prompt")]
        public string OriginalPrompt { get; set; } = string.Empty;

        [JsonPropertyName("optimized_prompt")]
        public string OptimizedPrompt { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "running", "completed", "failed"

        [JsonPropertyName("optimization_results")]
        public OptimizationResults OptimizationResults { get; set; } = new();

        [JsonPropertyName("iterations")]
        public List<OptimizationIteration> Iterations { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public long? DurationSeconds { get; set; }
    }

    public class OptimizationResults
    {
        [JsonPropertyName("improvement_percentage")]
        public double ImprovementPercentage { get; set; }

        [JsonPropertyName("original_score")]
        public double OriginalScore { get; set; }

        [JsonPropertyName("optimized_score")]
        public double OptimizedScore { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, double> Metrics { get; set; } = new();

        [JsonPropertyName("test_results")]
        public List<TestResult> TestResults { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
    }

    public class OptimizationIteration
    {
        [JsonPropertyName("iteration")]
        public int Iteration { get; set; }

        [JsonPropertyName("prompt_variant")]
        public string PromptVariant { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, double> Metrics { get; set; } = new();

        [JsonPropertyName("changes_made")]
        public List<string> ChangesMade { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class TestResult
    {
        [JsonPropertyName("test_case_id")]
        public string TestCaseId { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; } = new();

        [JsonPropertyName("expected_output")]
        public string? ExpectedOutput { get; set; }

        [JsonPropertyName("actual_output")]
        public string ActualOutput { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, double> Metrics { get; set; } = new();

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }
    }

    public class PromptEvaluationResponse
    {
        [JsonPropertyName("evaluation_id")]
        public string EvaluationId { get; set; } = string.Empty;

        [JsonPropertyName("prompt_text")]
        public string PromptText { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("overall_score")]
        public double OverallScore { get; set; }

        [JsonPropertyName("metrics")]
        public EvaluationMetrics Metrics { get; set; } = new();

        [JsonPropertyName("test_results")]
        public List<TestResult> TestResults { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<EvaluationRecommendation> Recommendations { get; set; } = new();

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
        public double Accuracy { get; set; }

        [JsonPropertyName("relevance")]
        public double Relevance { get; set; }

        [JsonPropertyName("clarity")]
        public double Clarity { get; set; }

        [JsonPropertyName("consistency")]
        public double Consistency { get; set; }

        [JsonPropertyName("safety")]
        public double Safety { get; set; }

        [JsonPropertyName("efficiency")]
        public double Efficiency { get; set; }

        [JsonPropertyName("custom_metrics")]
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    public class EvaluationRecommendation
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "improvement", "warning", "optimization"

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("suggested_change")]
        public string? SuggestedChange { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty; // "low", "medium", "high", "critical"

        [JsonPropertyName("impact_score")]
        public double ImpactScore { get; set; }
    }

    public class PromptListResponse
    {
        [JsonPropertyName("templates")]
        public List<PromptTemplateResponse> Templates { get; set; } = new();

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class PromptVersionResponse
    {
        [JsonPropertyName("version_id")]
        public string VersionId { get; set; } = string.Empty;

        [JsonPropertyName("template_id")]
        public string TemplateId { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("template")]
        public string Template { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<TemplateVariable> Variables { get; set; } = new();

        [JsonPropertyName("version_notes")]
        public string VersionNotes { get; set; } = string.Empty;

        [JsonPropertyName("model_config")]
        public ModelConfig? ModelConfig { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("usage_count")]
        public long UsageCount { get; set; }
    }

    public class PromptStatsResponse
    {
        [JsonPropertyName("total_prompts")]
        public long TotalPrompts { get; set; }

        [JsonPropertyName("total_executions")]
        public long TotalExecutions { get; set; }

        [JsonPropertyName("average_response_time")]
        public double AverageResponseTime { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("total_tokens_used")]
        public long TotalTokensUsed { get; set; }

        [JsonPropertyName("total_cost")]
        public double TotalCost { get; set; }

        [JsonPropertyName("most_used_templates")]
        public List<TemplateUsageStats> MostUsedTemplates { get; set; } = new();

        [JsonPropertyName("performance_trends")]
        public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
    }

    public class TemplateUsageStats
    {
        [JsonPropertyName("template_id")]
        public string TemplateId { get; set; } = string.Empty;

        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; } = string.Empty;

        [JsonPropertyName("usage_count")]
        public long UsageCount { get; set; }

        [JsonPropertyName("average_score")]
        public double AverageScore { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("last_used")]
        public DateTime? LastUsed { get; set; }
    }

    public class PerformanceTrend
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("executions")]
        public long Executions { get; set; }

        [JsonPropertyName("average_response_time")]
        public double AverageResponseTime { get; set; }

        [JsonPropertyName("success_rate")]
        public double SuccessRate { get; set; }

        [JsonPropertyName("total_tokens")]
        public long TotalTokens { get; set; }

        [JsonPropertyName("total_cost")]
        public double TotalCost { get; set; }
    }

    public class MessageResponse6
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
