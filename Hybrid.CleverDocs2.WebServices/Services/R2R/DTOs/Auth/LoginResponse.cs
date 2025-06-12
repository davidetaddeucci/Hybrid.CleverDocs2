using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth
{
    public class LoginResponse
    {
        [JsonPropertyName("results")]
        public LoginResults Results { get; set; } = new();
    }

    public class LoginResults
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserResponse User { get; set; } = new();
    }
}