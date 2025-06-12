namespace Hybrid.CleverDocs2.WebServices.Services.DTOs
{
    public class R2ROptions
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ConfigPath { get; set; } = string.Empty;
        public int DefaultTimeout { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
    }
}