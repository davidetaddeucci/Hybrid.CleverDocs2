using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.Models;
using Hybrid.CleverDocs.WebUI.ViewModels;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    /// <summary>
    /// Controller for user settings including LLM provider configuration
    /// </summary>
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IApiService apiService, ILogger<SettingsController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Main settings page
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user's LLM configuration
                var llmConfig = await _apiService.GetAsync<UserLLMConfigurationViewModel>("/api/LLMSettings");
                
                var viewModel = new SettingsViewModel
                {
                    LLMConfiguration = llmConfig ?? new UserLLMConfigurationViewModel
                    {
                        Provider = "openai",
                        Model = "gpt-4o-mini",
                        Temperature = 0.7m,
                        MaxTokens = 1000,
                        IsSystemDefault = true
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings page");
                TempData["ErrorMessage"] = "Error loading settings. Please try again.";
                return View(new SettingsViewModel());
            }
        }

        /// <summary>
        /// LLM Settings page
        /// </summary>
        public async Task<IActionResult> LLMSettings()
        {
            try
            {
                // Get current user's LLM configuration
                var llmConfig = await _apiService.GetAsync<UserLLMConfigurationViewModel>("/api/LLMSettings");
                
                // Get usage statistics
                var usageStats = await _apiService.GetAsync<LLMUsageStatisticsViewModel>("/api/LLMSettings/usage");

                var viewModel = new LLMSettingsViewModel
                {
                    Configuration = llmConfig ?? new UserLLMConfigurationViewModel
                    {
                        Provider = "openai",
                        Model = "gpt-4o-mini",
                        Temperature = 0.7m,
                        MaxTokens = 1000,
                        IsSystemDefault = true
                    },
                    UsageStatistics = usageStats ?? new LLMUsageStatisticsViewModel(),
                    AvailableProviders = new List<LLMProviderViewModel>
                    {
                        new() { Id = "openai", Name = "OpenAI", Description = "GPT-4o, GPT-4o-mini, and other OpenAI models" },
                        new() { Id = "anthropic", Name = "Anthropic", Description = "Claude-3 Opus, Sonnet, and Haiku models" },
                        new() { Id = "azure", Name = "Azure OpenAI", Description = "Azure-hosted OpenAI models" },
                        new() { Id = "custom", Name = "Custom", Description = "Custom API endpoint" }
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading LLM settings page");
                TempData["ErrorMessage"] = "Error loading LLM settings. Please try again.";
                return View(new LLMSettingsViewModel());
            }
        }

        /// <summary>
        /// Save LLM configuration
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveLLMSettings([FromBody] SaveLLMConfigurationRequest request)
        {
            try
            {
                var response = await _apiService.PostAsync<object>("/api/LLMSettings", request);
                
                if (response != null)
                {
                    return Json(new { success = true, message = "LLM configuration saved successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to save LLM configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving LLM configuration");
                return Json(new { success = false, message = "Error saving configuration: " + ex.Message });
            }
        }

        /// <summary>
        /// Test LLM configuration
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestLLMConfiguration([FromBody] UserLLMConfigurationViewModel config)
        {
            try
            {
                var testResult = await _apiService.PostAsync<LLMConfigurationTestResult>("/api/LLMSettings/test", config);
                
                if (testResult != null)
                {
                    return Json(new { 
                        success = testResult.IsSuccessful, 
                        message = testResult.IsSuccessful ? "Configuration test successful!" : testResult.ErrorMessage,
                        responseTime = testResult.ResponseTime.TotalMilliseconds
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to test configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing LLM configuration");
                return Json(new { success = false, message = "Error testing configuration: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete LLM configuration (revert to system default)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteLLMConfiguration()
        {
            try
            {
                var response = await _apiService.DeleteAsync("/api/LLMSettings");
                
                if (response)
                {
                    return Json(new { success = true, message = "LLM configuration deleted. Reverted to system default." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete LLM configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting LLM configuration");
                return Json(new { success = false, message = "Error deleting configuration: " + ex.Message });
            }
        }

        /// <summary>
        /// Get supported models for a provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSupportedModels(string provider)
        {
            try
            {
                var models = await _apiService.GetAsync<string[]>($"/api/LLMSettings/providers/{provider}/models");
                return Json(models ?? Array.Empty<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported models for provider {Provider}", provider);
                return Json(Array.Empty<string>());
            }
        }
    }

    #region ViewModels

    public class SettingsViewModel
    {
        public UserLLMConfigurationViewModel LLMConfiguration { get; set; } = new();
    }



    #endregion
}
