using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.McpTuning
{
    public class McpTuningResponse
    {
        public string TuningId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
