using System;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion
{
    public class IngestionRequest
    {
        public Guid JobId { get; set; }
        public int Sequence { get; set; }
        public string? Data { get; set; }
    }
}
