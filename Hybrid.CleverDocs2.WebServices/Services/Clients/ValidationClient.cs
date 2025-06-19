using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ValidationClient : IValidationClient
    {
        private readonly HttpClient _httpClient;

        public ValidationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // R2R validation is handled through user limits endpoints
        // These methods provide compatibility with the existing interface
        public async Task<ValidationResponse> CreateAsync(ValidationRequest request)
        {
            try
            {
                // R2R validation is implicit through user limits
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new ValidationResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "valid",
                    Message = "R2R validation is handled through user limits. Use AuthClient for user operations.",
                    ValidatedAt = DateTime.UtcNow,
                    IsValid = true
                };
            }
            catch (HttpRequestException)
            {
                return new ValidationResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "failed",
                    Message = "R2R service unavailable",
                    ValidatedAt = DateTime.UtcNow,
                    IsValid = false
                };
            }
        }

        public async Task<ValidationResponse> GetAsync(string id)
        {
            try
            {
                // Check if we can get user limits (this validates the user exists and has access)
                var response = await _httpClient.GetAsync($"/v3/users/{id}/limits");

                if (response.IsSuccessStatusCode)
                {
                    return new ValidationResponse
                    {
                        Id = id,
                        Status = "valid",
                        Message = "User validation successful through R2R limits check",
                        ValidatedAt = DateTime.UtcNow,
                        IsValid = true
                    };
                }
                else
                {
                    return new ValidationResponse
                    {
                        Id = id,
                        Status = "invalid",
                        Message = "User validation failed - user not found or no access",
                        ValidatedAt = DateTime.UtcNow,
                        IsValid = false
                    };
                }
            }
            catch (HttpRequestException)
            {
                return new ValidationResponse
                {
                    Id = id,
                    Status = "failed",
                    Message = "R2R service unavailable",
                    ValidatedAt = DateTime.UtcNow,
                    IsValid = false
                };
            }
        }

        public async Task<IEnumerable<ValidationResponse>> ListAsync()
        {
            try
            {
                // R2R doesn't have validation listing, return system status
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new List<ValidationResponse>
                {
                    new ValidationResponse
                    {
                        Id = "system",
                        Status = "active",
                        Message = "R2R validation system is operational. Use AuthClient for user validation.",
                        ValidatedAt = DateTime.UtcNow,
                        IsValid = true
                    }
                };
            }
            catch (HttpRequestException)
            {
                return new List<ValidationResponse>();
            }
        }

        public async Task<ValidationResponse> UpdateAsync(string id, ValidationRequest request)
        {
            try
            {
                // R2R doesn't support validation updates
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new ValidationResponse
                {
                    Id = id,
                    Status = "updated",
                    Message = "R2R validation cannot be updated. Use AuthClient for user operations.",
                    ValidatedAt = DateTime.UtcNow,
                    IsValid = true
                };
            }
            catch (HttpRequestException)
            {
                return new ValidationResponse
                {
                    Id = id,
                    Status = "failed",
                    Message = "R2R service unavailable",
                    ValidatedAt = DateTime.UtcNow,
                    IsValid = false
                };
            }
        }

        public async Task DeleteAsync(string id)
        {
            // R2R doesn't support validation deletion
            // This is a no-op for compatibility
            await Task.CompletedTask;
        }
    }
}
