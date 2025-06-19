using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation
{
    public class ValidationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; }
        public bool IsValid { get; set; }
    }
}
