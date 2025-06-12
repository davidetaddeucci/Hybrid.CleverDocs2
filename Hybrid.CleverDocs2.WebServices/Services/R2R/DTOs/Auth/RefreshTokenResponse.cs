using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth
{
    public class RefreshTokenResponse
    {
        [JsonPropertyName("results")]
        public TokenResults Results { get; set; } = new();
    }
}