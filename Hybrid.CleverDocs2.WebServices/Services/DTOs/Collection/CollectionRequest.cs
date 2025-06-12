using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Collection
{
    public class CollectionRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CollectionUpdateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class CollectionListRequest
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("collection_ids")]
        public List<string>? CollectionIds { get; set; }
    }

    public class CollectionDocumentRequest
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;
    }

    public class CollectionUserRequest
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("permission_level")]
        public string PermissionLevel { get; set; } = "read"; // read, write, admin
    }

    public class CollectionPermissionRequest
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("permission_level")]
        public string PermissionLevel { get; set; } = "read"; // read, write, admin
    }
}