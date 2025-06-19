using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration
{
    public class OrchestrationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
