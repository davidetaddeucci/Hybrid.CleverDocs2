using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Conversation
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
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("use_vector_search")]
        public bool UseVectorSearch { get; set; } = true;

        [JsonPropertyName("search_filters")]
        public Dictionary<string, object> SearchFilters { get; set; } = new();

        [JsonPropertyName("search_limit")]
        public int SearchLimit { get; set; } = 10;

        [JsonPropertyName("use_hybrid_search")]
        public bool UseHybridSearch { get; set; } = true;

        [JsonPropertyName("rag_generation_config")]
        public Dictionary<string, object> RagGenerationConfig { get; set; } = new();

        [JsonPropertyName("include_title_if_available")]
        public bool IncludeTitleIfAvailable { get; set; } = true;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class ConversationBranchRequest
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("new_message")]
        public string NewMessage { get; set; } = string.Empty;
    }
}
