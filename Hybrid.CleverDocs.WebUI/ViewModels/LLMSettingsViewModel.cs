using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.ViewModels
{
    public class LLMSettingsViewModel
    {
        public UserLLMConfigurationViewModel Configuration { get; set; } = new();
        public LLMUsageStatisticsViewModel UsageStatistics { get; set; } = new();
        public List<LLMProviderViewModel> AvailableProviders { get; set; } = new();
    }

    public class UserLLMConfigurationViewModel
    {
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public decimal Temperature { get; set; } = 0.7m;
        public int MaxTokens { get; set; } = 1000;
        public decimal? TopP { get; set; } = 1.0m;
        public bool EnableStreaming { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool HasApiKey { get; set; } = false;
        public bool IsSystemDefault { get; set; } = false;
        public Dictionary<string, object>? AdditionalParameters { get; set; }
    }

    public class LLMUsageStatisticsViewModel
    {
        public int TotalCalls { get; set; }
        public DateTime? LastUsed { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class LLMProviderViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SaveLLMConfigurationRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public string? ApiKey { get; set; }
        public decimal Temperature { get; set; } = 0.7m;
        public int MaxTokens { get; set; } = 1000;
        public decimal? TopP { get; set; } = 1.0m;
        public bool EnableStreaming { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object>? AdditionalParameters { get; set; }
    }

    public class LLMConfigurationTestResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? TestResponse { get; set; }
    }
}
