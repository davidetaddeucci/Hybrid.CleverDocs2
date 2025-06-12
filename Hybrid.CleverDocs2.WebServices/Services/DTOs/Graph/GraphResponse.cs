using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Graph
{
    public class GraphResponse
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("entity_count")]
        public int EntityCount { get; set; }

        [JsonPropertyName("relationship_count")]
        public int RelationshipCount { get; set; }

        [JsonPropertyName("community_count")]
        public int CommunityCount { get; set; }
    }

    public class GraphPullResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("estimated_entities")]
        public int? EstimatedEntities { get; set; }

        [JsonPropertyName("estimated_relationships")]
        public int? EstimatedRelationships { get; set; }

        [JsonPropertyName("estimated_cost")]
        public double? EstimatedCost { get; set; }
    }

    public class GraphCommunityResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("community_count")]
        public int CommunityCount { get; set; }

        [JsonPropertyName("estimated_cost")]
        public double? EstimatedCost { get; set; }
    }

    public class EntityResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("chunk_ids")]
        public List<string> ChunkIds { get; set; } = new();

        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class RelationshipResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("predicate")]
        public string Predicate { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("chunk_ids")]
        public List<string> ChunkIds { get; set; } = new();

        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class CommunityResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("community_number")]
        public int CommunityNumber { get; set; }

        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("findings")]
        public List<string> Findings { get; set; } = new();

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("rating_explanation")]
        public string? RatingExplanation { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class GraphListResponse<T>
    {
        [JsonPropertyName("results")]
        public List<T> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class GraphSearchResponse
    {
        [JsonPropertyName("results")]
        public List<GraphSearchResult> Results { get; set; } = new();
    }

    public class GraphSearchResult
    {
        [JsonPropertyName("content")]
        public GraphSearchContent Content { get; set; } = new();

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("result_type")]
        public string ResultType { get; set; } = string.Empty;

        [JsonPropertyName("chunk_ids")]
        public List<string> ChunkIds { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }

    public class GraphSearchContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class GraphResetResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;
    }

    public class GraphStatusResponse
    {
        [JsonPropertyName("graph_cluster_status")]
        public string GraphClusterStatus { get; set; } = string.Empty;

        [JsonPropertyName("graph_creation_status")]
        public string GraphCreationStatus { get; set; } = string.Empty;

        [JsonPropertyName("graph_sync_status")]
        public string GraphSyncStatus { get; set; } = string.Empty;
    }
}
