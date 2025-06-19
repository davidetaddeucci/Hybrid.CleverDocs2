namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.LocalLLM
{
    public class LocalLLMRequest
    {
        public string? ModelName { get; set; }
        public string? Prompt { get; set; }
        public double? Temperature { get; set; }
        public string? Description { get; set; }
    }
}
