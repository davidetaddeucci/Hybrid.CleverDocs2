using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;

namespace Hybrid.CleverDocs2.WebServices.Services.LLM
{
    /// <summary>
    /// Implementation of LLM Provider Service for managing user LLM configurations
    /// Handles encryption/decryption of API keys and validation of configurations
    /// </summary>
    public class LLMProviderService : ILLMProviderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LLMProviderService> _logger;
        private readonly ILLMAuditService _auditService;
        private readonly string _encryptionKey;

        public LLMProviderService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<LLMProviderService> logger,
            ILLMAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _auditService = auditService;
            _encryptionKey = _configuration["Encryption:ApiKeyEncryptionKey"] ?? "DefaultKey123456789012345678901234"; // 32 chars for AES-256
        }

        public async Task<UserLLMConfiguration?> GetUserLLMConfigurationAsync(Guid userId)
        {
            try
            {
                var preferences = await _context.UserLLMPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);

                if (preferences == null)
                {
                    _logger.LogDebug("No LLM preferences found for user {UserId}, will use system default", userId);
                    return null;
                }

                // Decrypt API key if present
                string? decryptedApiKey = null;
                if (!string.IsNullOrEmpty(preferences.EncryptedApiKey))
                {
                    decryptedApiKey = DecryptApiKey(preferences.EncryptedApiKey);
                }

                // Parse additional parameters
                Dictionary<string, object>? additionalParams = null;
                if (!string.IsNullOrEmpty(preferences.AdditionalParameters))
                {
                    additionalParams = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.AdditionalParameters);
                }

                var configuration = new UserLLMConfiguration
                {
                    Provider = preferences.Provider,
                    Model = preferences.Model,
                    ApiEndpoint = preferences.ApiEndpoint,
                    ApiKey = decryptedApiKey,
                    Temperature = preferences.Temperature,
                    MaxTokens = preferences.MaxTokens,
                    TopP = preferences.TopP,
                    EnableStreaming = preferences.EnableStreaming,
                    IsActive = preferences.IsActive,
                    AdditionalParameters = additionalParams
                };

                _logger.LogInformation("Retrieved LLM configuration for user {UserId}: {Provider}/{Model}", 
                    userId, preferences.Provider, preferences.Model);

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving LLM configuration for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> SaveUserLLMConfigurationAsync(Guid userId, UserLLMConfiguration configuration, string updatedBy)
        {
            try
            {
                // Validate configuration first
                var validationResult = await ValidateConfigurationAsync(configuration);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid LLM configuration for user {UserId}: {Errors}", 
                        userId, string.Join(", ", validationResult.Errors));
                    return false;
                }

                // Encrypt API key if provided
                string? encryptedApiKey = null;
                if (!string.IsNullOrEmpty(configuration.ApiKey))
                {
                    encryptedApiKey = EncryptApiKey(configuration.ApiKey);
                }

                // Serialize additional parameters
                string? additionalParamsJson = null;
                if (configuration.AdditionalParameters != null)
                {
                    additionalParamsJson = JsonSerializer.Serialize(configuration.AdditionalParameters);
                }

                // Check if preferences already exist
                var existingPreferences = await _context.UserLLMPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingPreferences != null)
                {
                    // Update existing preferences
                    existingPreferences.Provider = configuration.Provider;
                    existingPreferences.Model = configuration.Model;
                    existingPreferences.ApiEndpoint = configuration.ApiEndpoint;
                    existingPreferences.EncryptedApiKey = encryptedApiKey;
                    existingPreferences.Temperature = configuration.Temperature;
                    existingPreferences.MaxTokens = configuration.MaxTokens;
                    existingPreferences.TopP = configuration.TopP;
                    existingPreferences.EnableStreaming = configuration.EnableStreaming;
                    existingPreferences.IsActive = configuration.IsActive;
                    existingPreferences.AdditionalParameters = additionalParamsJson;
                    existingPreferences.UpdatedAt = DateTime.UtcNow;
                    existingPreferences.UpdatedBy = updatedBy;

                    _context.UserLLMPreferences.Update(existingPreferences);
                }
                else
                {
                    // Create new preferences
                    var newPreferences = new UserLLMPreferences
                    {
                        UserId = userId,
                        Provider = configuration.Provider,
                        Model = configuration.Model,
                        ApiEndpoint = configuration.ApiEndpoint,
                        EncryptedApiKey = encryptedApiKey,
                        Temperature = configuration.Temperature,
                        MaxTokens = configuration.MaxTokens,
                        TopP = configuration.TopP,
                        EnableStreaming = configuration.EnableStreaming,
                        IsActive = configuration.IsActive,
                        AdditionalParameters = additionalParamsJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = updatedBy,
                        UpdatedBy = updatedBy
                    };

                    await _context.UserLLMPreferences.AddAsync(newPreferences);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved LLM configuration for user {UserId}: {Provider}/{Model}", 
                    userId, configuration.Provider, configuration.Model);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving LLM configuration for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserLLMConfigurationAsync(Guid userId)
        {
            try
            {
                var preferences = await _context.UserLLMPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences != null)
                {
                    _context.UserLLMPreferences.Remove(preferences);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted LLM configuration for user {UserId}", userId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting LLM configuration for user {UserId}", userId);
                return false;
            }
        }

        public async Task<LLMConfigurationValidationResult> ValidateConfigurationAsync(UserLLMConfiguration configuration)
        {
            var result = new LLMConfigurationValidationResult();

            // Validate provider
            if (!LLMProviders.IsValidProvider(configuration.Provider))
            {
                result.Errors.Add($"Invalid provider: {configuration.Provider}. Supported providers: {string.Join(", ", LLMProviders.All)}");
            }

            // Validate model for provider
            if (!LLMProviders.IsValidModel(configuration.Provider, configuration.Model))
            {
                var supportedModels = await GetSupportedModelsAsync(configuration.Provider);
                result.Errors.Add($"Invalid model '{configuration.Model}' for provider '{configuration.Provider}'. Supported models: {string.Join(", ", supportedModels)}");
            }

            // Validate temperature range
            if (configuration.Temperature < 0 || configuration.Temperature > 2)
            {
                result.Errors.Add("Temperature must be between 0.0 and 2.0");
            }

            // Validate max tokens
            if (configuration.MaxTokens < 1 || configuration.MaxTokens > 32000)
            {
                result.Errors.Add("MaxTokens must be between 1 and 32000");
            }

            // Validate TopP if provided
            if (configuration.TopP.HasValue && (configuration.TopP < 0 || configuration.TopP > 1))
            {
                result.Errors.Add("TopP must be between 0.0 and 1.0");
            }

            // Validate API key format (basic check)
            if (!string.IsNullOrEmpty(configuration.ApiKey))
            {
                if (configuration.Provider == LLMProviders.OpenAI && !configuration.ApiKey.StartsWith("sk-"))
                {
                    result.Warnings.Add("OpenAI API keys typically start with 'sk-'");
                }
                else if (configuration.Provider == LLMProviders.Anthropic && !configuration.ApiKey.StartsWith("sk-ant-"))
                {
                    result.Warnings.Add("Anthropic API keys typically start with 'sk-ant-'");
                }
            }

            // Validate API endpoint format if provided
            if (!string.IsNullOrEmpty(configuration.ApiEndpoint))
            {
                if (!Uri.TryCreate(configuration.ApiEndpoint, UriKind.Absolute, out _))
                {
                    result.Errors.Add("ApiEndpoint must be a valid URL");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return await Task.FromResult(result);
        }

        public async Task<LLMConfigurationTestResult> TestConfigurationAsync(UserLLMConfiguration configuration)
        {
            var result = new LLMConfigurationTestResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // For now, just validate the configuration
                // In a full implementation, you would make an actual API call to test
                var validationResult = await ValidateConfigurationAsync(configuration);
                
                if (!validationResult.IsValid)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = string.Join("; ", validationResult.Errors);
                }
                else
                {
                    result.IsSuccessful = true;
                    result.TestResponse = "Configuration validation passed";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error testing LLM configuration");
            }
            finally
            {
                stopwatch.Stop();
                result.ResponseTime = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<string[]> GetSupportedModelsAsync(string provider)
        {
            if (LLMProviders.SupportedModels.ContainsKey(provider))
            {
                return await Task.FromResult(LLMProviders.SupportedModels[provider]);
            }

            return await Task.FromResult(Array.Empty<string>());
        }

        public async Task<UserLLMConfiguration> GetSystemDefaultConfigurationAsync()
        {
            // Get system default from configuration
            var defaultProvider = _configuration["R2R:DefaultProvider"] ?? "openai";
            var defaultModel = _configuration["R2R:DefaultModel"] ?? "gpt-4o-mini";
            var defaultTemperature = decimal.Parse(_configuration["R2R:DefaultTemperature"] ?? "0.7");
            var defaultMaxTokens = int.Parse(_configuration["R2R:DefaultMaxTokens"] ?? "1000");

            return await Task.FromResult(new UserLLMConfiguration
            {
                Provider = defaultProvider,
                Model = defaultModel,
                Temperature = defaultTemperature,
                MaxTokens = defaultMaxTokens,
                TopP = 1.0m,
                EnableStreaming = false,
                IsActive = true
            });
        }

        public async Task<bool> UpdateUsageStatisticsAsync(Guid userId)
        {
            try
            {
                var preferences = await _context.UserLLMPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences != null)
                {
                    preferences.LastUsedAt = DateTime.UtcNow;
                    preferences.UsageCount++;
                    
                    _context.UserLLMPreferences.Update(preferences);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage statistics for user {UserId}", userId);
                return false;
            }
        }

        public async Task<LLMUsageStatistics?> GetUsageStatisticsAsync(Guid userId)
        {
            try
            {
                var preferences = await _context.UserLLMPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                    return null;

                return new LLMUsageStatistics
                {
                    TotalCalls = preferences.UsageCount,
                    LastUsed = preferences.LastUsedAt,
                    Provider = preferences.Provider,
                    Model = preferences.Model,
                    CreatedAt = preferences.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics for user {UserId}", userId);
                return null;
            }
        }

        #region Private Methods

        private string EncryptApiKey(string apiKey)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainTextBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        private string DecryptApiKey(string encryptedApiKey)
        {
            var fullCipher = Convert.FromBase64String(encryptedApiKey);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));

            // Extract IV
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion
    }
}
