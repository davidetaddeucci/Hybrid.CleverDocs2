using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Collection
{
    public class CollectionResponse
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }
    }

    public class CollectionCreateResponse
    {
        [JsonPropertyName("results")]
        public CollectionResponse Results { get; set; } = new();
    }

    public class CollectionListResponse
    {
        [JsonPropertyName("results")]
        public List<CollectionResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class CollectionDocumentResponse
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("added_at")]
        public DateTime AddedAt { get; set; }
    }

    public class CollectionUserResponse
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("permission_level")]
        public string PermissionLevel { get; set; } = string.Empty;

        [JsonPropertyName("added_at")]
        public DateTime AddedAt { get; set; }
    }

    public class CollectionUsersListResponse
    {
        [JsonPropertyName("results")]
        public List<CollectionUserResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class CollectionDocumentsListResponse
    {
        [JsonPropertyName("results")]
        public List<CollectionDocumentResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class CollectionStatsResponse
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }

        [JsonPropertyName("total_size_bytes")]
        public long TotalSizeBytes { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonPropertyName("document_types")]
        public Dictionary<string, int> DocumentTypes { get; set; } = new();
    }

    public class MessageResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}