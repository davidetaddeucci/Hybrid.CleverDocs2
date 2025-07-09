using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    /// <summary>
    /// Stores per-user LLM provider preferences for R2R rag_generation_config
    /// Enables users to select their own LLM providers (OpenAI, Anthropic, Azure) and use personal API keys
    /// </summary>
    public class UserLLMPreferences
    {
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// LLM Provider: 'openai', 'anthropic', 'azure', 'custom'
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Model name for the provider (e.g., 'gpt-4o', 'claude-3-opus', 'gpt-4-turbo')
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Custom API endpoint for the provider (optional)
        /// Used for api_base parameter in rag_generation_config
        /// </summary>
        [MaxLength(500)]
        public string? ApiEndpoint { get; set; }

        /// <summary>
        /// Encrypted API key for the user's personal LLM provider account
        /// Stored encrypted for security
        /// </summary>
        [MaxLength(1000)]
        public string? EncryptedApiKey { get; set; }

        /// <summary>
        /// Temperature setting for LLM generation (0.0 to 2.0)
        /// </summary>
        [Range(0.0, 2.0)]
        public decimal Temperature { get; set; } = 0.7m;

        /// <summary>
        /// Maximum tokens for LLM generation
        /// </summary>
        [Range(1, 32000)]
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// Top-p setting for LLM generation (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public decimal? TopP { get; set; } = 1.0m;

        /// <summary>
        /// Whether to enable streaming for this user's LLM calls
        /// </summary>
        public bool EnableStreaming { get; set; } = false;

        /// <summary>
        /// Whether this configuration is active (allows users to temporarily disable)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON field for additional provider-specific parameters
        /// Maps to add_generation_kwargs in rag_generation_config
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? AdditionalParameters { get; set; }

        /// <summary>
        /// When this configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this configuration was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this configuration (for audit purposes)
        /// </summary>
        [MaxLength(255)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Who last updated this configuration (for audit purposes)
        /// </summary>
        [MaxLength(255)]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// When this configuration was last used (for analytics)
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Number of times this configuration has been used (for analytics)
        /// </summary>
        public int UsageCount { get; set; } = 0;

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        // Computed properties for easy access
        [NotMapped]
        public string FullModelName => $"{Provider}/{Model}";

        [NotMapped]
        public bool HasCustomEndpoint => !string.IsNullOrEmpty(ApiEndpoint);

        [NotMapped]
        public bool HasPersonalApiKey => !string.IsNullOrEmpty(EncryptedApiKey);
    }

    /// <summary>
    /// Supported LLM providers for validation
    /// </summary>
    public static class LLMProviders
    {
        public const string OpenAI = "openai";
        public const string Anthropic = "anthropic";
        public const string Azure = "azure";
        public const string Custom = "custom";

        public static readonly string[] All = { OpenAI, Anthropic, Azure, Custom };

        public static readonly Dictionary<string, string[]> SupportedModels = new()
        {
            [OpenAI] = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo", "o1-mini", "o1-preview" },
            [Anthropic] = new[] { "claude-3-opus", "claude-3-sonnet", "claude-3-haiku", "claude-3-5-sonnet" },
            [Azure] = new[] { "gpt-4o", "gpt-4-turbo", "gpt-35-turbo", "gpt-4" },
            [Custom] = new[] { "custom-model" }
        };

        public static bool IsValidProvider(string provider) => All.Contains(provider);

        public static bool IsValidModel(string provider, string model)
        {
            return SupportedModels.ContainsKey(provider) && SupportedModels[provider].Contains(model);
        }
    }
}
