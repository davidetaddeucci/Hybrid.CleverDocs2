using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth
{
    public class RefreshTokenResponse
    {
        [JsonPropertyName("results")]
        public TokenResults Results { get; set; } = new();
    }
}