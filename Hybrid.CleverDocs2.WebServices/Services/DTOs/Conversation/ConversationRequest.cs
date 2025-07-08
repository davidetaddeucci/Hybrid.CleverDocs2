using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation
{
    public class ConversationRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConversationUpdateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class ConversationListRequest
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("conversation_ids")]
        public List<string>? ConversationIds { get; set; }
    }

    public class MessageRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; } = "advanced";

        [JsonPropertyName("rag_generation_config")]
        public RagGenerationConfig RagGenerationConfig { get; set; } = new();

        [JsonPropertyName("search_settings")]
        public SearchSettings? SearchSettings { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class RagGenerationConfig
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "anthropic/claude-3-haiku-20240307";

        [JsonPropertyName("max_tokens_to_sample")]
        public int MaxTokensToSample { get; set; } = 2048;

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }
    }

    public class SearchSettings
    {
        [JsonPropertyName("filters")]
        public Dictionary<string, object> Filters { get; set; } = new();

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 10;

        [JsonPropertyName("use_vector_search")]
        public bool UseVectorSearch { get; set; } = true;

        [JsonPropertyName("use_hybrid_search")]
        public bool UseHybridSearch { get; set; } = true;
    }

    public class ConversationBranchRequest
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("new_message")]
        public string NewMessage { get; set; } = string.Empty;
    }
}
