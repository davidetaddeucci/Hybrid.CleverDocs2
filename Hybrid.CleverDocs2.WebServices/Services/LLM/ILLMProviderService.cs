using Hybrid.CleverDocs2.WebServices.Data.Entities;

namespace Hybrid.CleverDocs2.WebServices.Services.LLM
{
    /// <summary>
    /// Service interface for managing user LLM provider configurations
    /// Enables per-user LLM provider selection and API key management for R2R rag_generation_config
    /// </summary>
    public interface ILLMProviderService
    {
        /// <summary>
        /// Get user's LLM configuration for R2R rag_generation_config
        /// Returns null if user has no custom configuration (will use system default)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User LLM configuration or null for system default</returns>
        Task<UserLLMConfiguration?> GetUserLLMConfigurationAsync(Guid userId);

        /// <summary>
        /// Save or update user's LLM configuration
        /// Encrypts API key before storing in database
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="configuration">LLM configuration to save</param>
        /// <param name="updatedBy">Who is making the update</param>
        /// <returns>Success status</returns>
        Task<bool> SaveUserLLMConfigurationAsync(Guid userId, UserLLMConfiguration configuration, string updatedBy);

        /// <summary>
        /// Delete user's LLM configuration (will revert to system default)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteUserLLMConfigurationAsync(Guid userId);

        /// <summary>
        /// Validate LLM configuration before saving
        /// Checks provider, model compatibility, and API key format
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result with error messages</returns>
        Task<LLMConfigurationValidationResult> ValidateConfigurationAsync(UserLLMConfiguration configuration);

        /// <summary>
        /// Test user's LLM configuration by making a simple API call
        /// Used to verify API key and endpoint are working
        /// </summary>
        /// <param name="configuration">Configuration to test</param>
        /// <returns>Test result with success/error details</returns>
        Task<LLMConfigurationTestResult> TestConfigurationAsync(UserLLMConfiguration configuration);

        /// <summary>
        /// Get supported models for a specific provider
        /// </summary>
        /// <param name="provider">Provider name (openai, anthropic, azure, custom)</param>
        /// <returns>List of supported model names</returns>
        Task<string[]> GetSupportedModelsAsync(string provider);

        /// <summary>
        /// Get system default LLM configuration
        /// Used as fallback when user has no custom configuration
        /// </summary>
        /// <returns>System default configuration</returns>
        Task<UserLLMConfiguration> GetSystemDefaultConfigurationAsync();

        /// <summary>
        /// Update usage statistics for user's LLM configuration
        /// Called after successful R2R API calls
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateUsageStatisticsAsync(Guid userId);

        /// <summary>
        /// Get usage analytics for user's LLM configuration
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Usage statistics</returns>
        Task<LLMUsageStatistics?> GetUsageStatisticsAsync(Guid userId);
    }

    /// <summary>
    /// User LLM configuration for R2R rag_generation_config parameter
    /// </summary>
    public class UserLLMConfiguration
    {
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public string? ApiKey { get; set; } // Decrypted for use, encrypted in database
        public decimal Temperature { get; set; } = 0.7m;
        public int MaxTokens { get; set; } = 1000;
        public decimal? TopP { get; set; } = 1.0m;
        public bool EnableStreaming { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object>? AdditionalParameters { get; set; }

        /// <summary>
        /// Convert to R2R rag_generation_config format
        /// </summary>
        public Dictionary<string, object> ToRagGenerationConfig()
        {
            var config = new Dictionary<string, object>
            {
                ["model"] = $"{Provider}/{Model}",
                ["temperature"] = (double)Temperature,
                ["max_tokens"] = MaxTokens,
                ["stream"] = EnableStreaming
            };

            if (TopP.HasValue)
                config["top_p"] = (double)TopP.Value;

            if (!string.IsNullOrEmpty(ApiEndpoint))
                config["api_base"] = ApiEndpoint;

            if (AdditionalParameters != null)
            {
                config["add_generation_kwargs"] = AdditionalParameters;
            }

            return config;
        }
    }

    /// <summary>
    /// Result of LLM configuration validation
    /// </summary>
    public class LLMConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Result of LLM configuration testing
    /// </summary>
    public class LLMConfigurationTestResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? TestResponse { get; set; }
    }

    /// <summary>
    /// LLM usage statistics for analytics
    /// </summary>
    public class LLMUsageStatistics
    {
        public int TotalCalls { get; set; }
        public DateTime? LastUsed { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
