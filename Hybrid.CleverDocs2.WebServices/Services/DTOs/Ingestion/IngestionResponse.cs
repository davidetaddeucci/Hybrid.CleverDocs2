using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion
{
    public class IngestionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
