using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Graph
{
    public class GraphRequest
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("settings")]
        public GraphSettings? Settings { get; set; }
    }

    public class GraphSettings
    {
        [JsonPropertyName("entity_types")]
        public List<string>? EntityTypes { get; set; }

        [JsonPropertyName("relationship_types")]
        public List<string>? RelationshipTypes { get; set; }

        [JsonPropertyName("extraction_prompt")]
        public string? ExtractionPrompt { get; set; }

        [JsonPropertyName("max_knowledge_triples")]
        public int? MaxKnowledgeTriples { get; set; }

        [JsonPropertyName("generation_config")]
        public GraphGenerationConfig? GenerationConfig { get; set; }
    }

    public class GraphGenerationConfig
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "openai/gpt-4o-mini";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.1;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;
    }

    public class GraphPullRequest
    {
        [JsonPropertyName("document_ids")]
        public List<string>? DocumentIds { get; set; }

        [JsonPropertyName("run_type")]
        public string RunType { get; set; } = "run"; // run, estimate

        [JsonPropertyName("settings")]
        public GraphSettings? Settings { get; set; }
    }

    public class GraphCommunityRequest
    {
        [JsonPropertyName("collection_id")]
        public string CollectionId { get; set; } = string.Empty;

        [JsonPropertyName("run_type")]
        public string RunType { get; set; } = "run"; // run, estimate

        [JsonPropertyName("settings")]
        public CommunitySettings? Settings { get; set; }
    }

    public class CommunitySettings
    {
        [JsonPropertyName("generation_config")]
        public GraphGenerationConfig? GenerationConfig { get; set; }

        [JsonPropertyName("max_summary_input_length")]
        public int MaxSummaryInputLength { get; set; } = 65536;

        [JsonPropertyName("leiden_params")]
        public LeidenParams? LeidenParams { get; set; }
    }

    public class LeidenParams
    {
        [JsonPropertyName("max_cluster_size")]
        public int MaxClusterSize { get; set; } = 1000;

        [JsonPropertyName("resolution")]
        public double Resolution { get; set; } = 1.0;

        [JsonPropertyName("randomness")]
        public double Randomness { get; set; } = 0.001;

        [JsonPropertyName("iterations")]
        public int Iterations { get; set; } = 10;
    }

    public class EntityRequest
    {
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
    }

    public class RelationshipRequest
    {
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
    }

    public class CommunityRequest
    {
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
    }

    public class GraphSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("search_type")]
        public string SearchType { get; set; } = "local"; // local, global

        [JsonPropertyName("filters")]
        public Dictionary<string, object>? Filters { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 10;
    }

    public class GraphListRequest
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("entity_names")]
        public List<string>? EntityNames { get; set; }

        [JsonPropertyName("entity_table_name")]
        public string? EntityTableName { get; set; }

        [JsonPropertyName("relationship_types")]
        public List<string>? RelationshipTypes { get; set; }

        [JsonPropertyName("community_numbers")]
        public List<int>? CommunityNumbers { get; set; }
    }
}
