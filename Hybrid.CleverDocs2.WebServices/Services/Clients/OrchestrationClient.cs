using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class OrchestrationClient : IOrchestrationClient
    {
        private readonly HttpClient _httpClient;

        public OrchestrationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // R2R orchestration is handled through Hatchet workflows
        // These methods provide compatibility with the existing interface
        public async Task<OrchestrationResponse> CreateAsync(OrchestrationRequest request)
        {
            try
            {
                // R2R uses Hatchet for workflow orchestration
                // This simulates workflow creation by checking system health
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new OrchestrationResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "created",
                    WorkflowType = request.WorkflowType ?? "IngestFilesWorkflow",
                    Message = "R2R orchestration uses Hatchet workflows. Monitor at http://localhost:7274",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
            }
            catch (HttpRequestException)
            {
                return new OrchestrationResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "failed",
                    WorkflowType = request.WorkflowType ?? "unknown",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false
                };
            }
        }

        public async Task<OrchestrationResponse> GetAsync(string id)
        {
            try
            {
                // R2R workflow status is monitored through Hatchet dashboard
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new OrchestrationResponse
                {
                    Id = id,
                    Status = "running",
                    WorkflowType = "IngestFilesWorkflow",
                    Message = "R2R workflows are managed by Hatchet. Check dashboard at http://localhost:7274",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
            }
            catch (HttpRequestException)
            {
                return new OrchestrationResponse
                {
                    Id = id,
                    Status = "failed",
                    WorkflowType = "unknown",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false
                };
            }
        }

        public async Task<IEnumerable<OrchestrationResponse>> ListAsync()
        {
            try
            {
                // R2R doesn't expose workflow listing through REST API
                // This returns available workflow types
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new List<OrchestrationResponse>
                {
                    new OrchestrationResponse
                    {
                        Id = "ingest-workflow",
                        Status = "available",
                        WorkflowType = "IngestFilesWorkflow",
                        Message = "Document ingestion workflow",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new OrchestrationResponse
                    {
                        Id = "kg-workflow",
                        Status = "available",
                        WorkflowType = "KgExtractAndStoreWorkflow",
                        Message = "Knowledge graph extraction workflow",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    }
                };
            }
            catch (HttpRequestException)
            {
                return new List<OrchestrationResponse>();
            }
        }

        public async Task<OrchestrationResponse> UpdateAsync(string id, OrchestrationRequest request)
        {
            try
            {
                // R2R workflows cannot be updated once started
                // This returns a compatibility response
                var response = await _httpClient.GetAsync("/v3/health");
                response.EnsureSuccessStatusCode();

                return new OrchestrationResponse
                {
                    Id = id,
                    Status = "immutable",
                    WorkflowType = request.WorkflowType ?? "unknown",
                    Message = "R2R workflows cannot be updated. Create new workflow instead.",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
            }
            catch (HttpRequestException)
            {
                return new OrchestrationResponse
                {
                    Id = id,
                    Status = "failed",
                    WorkflowType = request.WorkflowType ?? "unknown",
                    Message = "R2R service unavailable",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false
                };
            }
        }

        public async Task DeleteAsync(string id)
        {
            // R2R workflows are managed by Hatchet and cannot be deleted via REST API
            // This is a no-op for compatibility
            await Task.CompletedTask;
        }
    }
}
