using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.LocalLLM
{
    public class LocalLLMResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Provider { get; set; } = string.Empty;
    }
}
