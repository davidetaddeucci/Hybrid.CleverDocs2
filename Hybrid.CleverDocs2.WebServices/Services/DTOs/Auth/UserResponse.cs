using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth
{
    public class UserResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("profile_picture")]
        public string? ProfilePicture { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("is_superuser")]
        public bool IsSuperuser { get; set; } = false;

        [JsonPropertyName("is_verified")]
        public bool IsVerified { get; set; } = false;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("collection_ids")]
        public List<string> CollectionIds { get; set; } = new();
    }

    public class UserListResponse
    {
        [JsonPropertyName("results")]
        public List<UserResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class UserCreateResponse
    {
        [JsonPropertyName("results")]
        public UserResponse Results { get; set; } = new();
    }

    public class MessageResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        [JsonPropertyName("results")]
        public TokenResults Results { get; set; } = new();
    }

    public class TokenResults
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
