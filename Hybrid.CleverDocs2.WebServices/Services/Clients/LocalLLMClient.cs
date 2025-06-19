using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.LocalLLM;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class LocalLLMClient : ILocalLLMClient
    {
        private readonly HttpClient _httpClient;

        public LocalLLMClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // R2R integrates with vLLM directly, not through REST endpoints
        // These methods provide compatibility with the existing interface
        public async Task<LocalLLMResponse> CreateAsync(LocalLLMRequest request)
        {
            try
            {
                // R2R uses vLLM integration, not separate LLM management endpoints
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new LocalLLMResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    ModelName = request.ModelName ?? "vllm-model",
                    Status = "configured",
                    Message = "R2R uses vLLM integration. Configure models through R2R configuration.",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Provider = "vLLM"
                };
            }
            catch (HttpRequestException)
            {
                return new LocalLLMResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    ModelName = request.ModelName ?? "unknown",
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false,
                    Provider = "vLLM"
                };
            }
        }

        public async Task<LocalLLMResponse> GetAsync(string id)
        {
            try
            {
                // R2R doesn't expose individual LLM model status
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new LocalLLMResponse
                {
                    Id = id,
                    ModelName = "vllm-integrated-model",
                    Status = "active",
                    Message = "R2R vLLM integration is operational",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Provider = "vLLM"
                };
            }
            catch (HttpRequestException)
            {
                return new LocalLLMResponse
                {
                    Id = id,
                    ModelName = "unknown",
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false,
                    Provider = "vLLM"
                };
            }
        }

        public async Task<IEnumerable<LocalLLMResponse>> ListAsync()
        {
            try
            {
                // R2R doesn't expose LLM model listing through REST API
                // This returns available integration status
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new List<LocalLLMResponse>
                {
                    new LocalLLMResponse
                    {
                        Id = "vllm-integration",
                        ModelName = "vLLM Integration",
                        Status = "available",
                        Message = "R2R supports vLLM for local model hosting",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        Provider = "vLLM"
                    },
                    new LocalLLMResponse
                    {
                        Id = "cloud-fallback",
                        ModelName = "Cloud Providers",
                        Status = "available",
                        Message = "OpenAI, Anthropic, and SciPhi API integrations available",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        Provider = "Cloud"
                    }
                };
            }
            catch (HttpRequestException)
            {
                return new List<LocalLLMResponse>();
            }
        }

        public async Task<LocalLLMResponse> UpdateAsync(string id, LocalLLMRequest request)
        {
            try
            {
                // R2R LLM configuration is handled through config files
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new LocalLLMResponse
                {
                    Id = id,
                    ModelName = request.ModelName ?? "vllm-model",
                    Status = "config-required",
                    Message = "R2R LLM configuration is managed through config files, not REST API",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Provider = "vLLM"
                };
            }
            catch (HttpRequestException)
            {
                return new LocalLLMResponse
                {
                    Id = id,
                    ModelName = request.ModelName ?? "unknown",
                    Status = "failed",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false,
                    Provider = "vLLM"
                };
            }
        }

        public async Task DeleteAsync(string id)
        {
            // R2R LLM models cannot be deleted via REST API
            // This is a no-op for compatibility
            await Task.CompletedTask;
        }
    }
}
