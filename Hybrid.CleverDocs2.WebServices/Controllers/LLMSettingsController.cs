using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.LLM;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    /// <summary>
    /// Controller for managing user LLM provider settings
    /// Enables per-user LLM configuration for R2R rag_generation_config
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LLMSettingsController : ControllerBase
    {
        private readonly ILLMProviderService _llmProviderService;
        private readonly ILogger<LLMSettingsController> _logger;

        public LLMSettingsController(
            ILLMProviderService llmProviderService,
            ILogger<LLMSettingsController> logger)
        {
            _llmProviderService = llmProviderService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's LLM configuration
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserLLMConfigurationDto>> GetUserConfiguration()
        {
            try
            {
                var userId = GetCurrentUserId();
                var configuration = await _llmProviderService.GetUserLLMConfigurationAsync(userId);

                if (configuration == null)
                {
                    // Return system default if user has no custom configuration
                    var systemDefault = await _llmProviderService.GetSystemDefaultConfigurationAsync();
                    return Ok(new UserLLMConfigurationDto
                    {
                        Provider = systemDefault.Provider,
                        Model = systemDefault.Model,
                        Temperature = systemDefault.Temperature,
                        MaxTokens = systemDefault.MaxTokens,
                        TopP = systemDefault.TopP,
                        EnableStreaming = systemDefault.EnableStreaming,
                        IsActive = false, // Not a custom configuration
                        IsSystemDefault = true
                    });
                }

                return Ok(new UserLLMConfigurationDto
                {
                    Provider = configuration.Provider,
                    Model = configuration.Model,
                    ApiEndpoint = configuration.ApiEndpoint,
                    Temperature = configuration.Temperature,
                    MaxTokens = configuration.MaxTokens,
                    TopP = configuration.TopP,
                    EnableStreaming = configuration.EnableStreaming,
                    IsActive = configuration.IsActive,
                    HasApiKey = !string.IsNullOrEmpty(configuration.ApiKey),
                    AdditionalParameters = configuration.AdditionalParameters,
                    IsSystemDefault = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LLM configuration for user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Save user's LLM configuration
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> SaveUserConfiguration([FromBody] SaveLLMConfigurationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userEmail = GetCurrentUserEmail();

                var configuration = new UserLLMConfiguration
                {
                    Provider = request.Provider,
                    Model = request.Model,
                    ApiEndpoint = request.ApiEndpoint,
                    ApiKey = request.ApiKey,
                    Temperature = request.Temperature,
                    MaxTokens = request.MaxTokens,
                    TopP = request.TopP,
                    EnableStreaming = request.EnableStreaming,
                    IsActive = request.IsActive,
                    AdditionalParameters = request.AdditionalParameters
                };

                // Validate configuration
                var validationResult = await _llmProviderService.ValidateConfigurationAsync(configuration);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { errors = validationResult.Errors, warnings = validationResult.Warnings });
                }

                // Save configuration
                var success = await _llmProviderService.SaveUserLLMConfigurationAsync(userId, configuration, userEmail);
                if (!success)
                {
                    return StatusCode(500, "Failed to save LLM configuration");
                }

                _logger.LogInformation("User {UserId} saved LLM configuration: {Provider}/{Model}", userId, request.Provider, request.Model);
                return Ok(new { message = "LLM configuration saved successfully", warnings = validationResult.Warnings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving LLM configuration for user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Test user's LLM configuration
        /// </summary>
        [HttpPost("test")]
        public async Task<ActionResult<LLMConfigurationTestResult>> TestConfiguration([FromBody] UserLLMConfiguration configuration)
        {
            try
            {
                var testResult = await _llmProviderService.TestConfigurationAsync(configuration);
                return Ok(testResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing LLM configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete user's LLM configuration (revert to system default)
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult> DeleteUserConfiguration()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _llmProviderService.DeleteUserLLMConfigurationAsync(userId);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to delete LLM configuration");
                }

                _logger.LogInformation("User {UserId} deleted their LLM configuration", userId);
                return Ok(new { message = "LLM configuration deleted successfully. Reverted to system default." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting LLM configuration for user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get supported models for a provider
        /// </summary>
        [HttpGet("providers/{provider}/models")]
        public async Task<ActionResult<string[]>> GetSupportedModels(string provider)
        {
            try
            {
                var models = await _llmProviderService.GetSupportedModelsAsync(provider);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported models for provider {Provider}", provider);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get usage statistics for user's LLM configuration
        /// </summary>
        [HttpGet("usage")]
        public async Task<ActionResult<LLMUsageStatistics>> GetUsageStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                var statistics = await _llmProviderService.GetUsageStatisticsAsync(userId);
                
                if (statistics == null)
                {
                    return Ok(new LLMUsageStatistics { TotalCalls = 0 });
                }

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage statistics for user");
                return StatusCode(500, "Internal server error");
            }
        }

        #region Private Methods

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }

        private string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
        }

        #endregion
    }

    #region DTOs

    public class UserLLMConfigurationDto
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

    #endregion
}
