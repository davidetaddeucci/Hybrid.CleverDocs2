using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth
{
    public class LogoutRequest
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}