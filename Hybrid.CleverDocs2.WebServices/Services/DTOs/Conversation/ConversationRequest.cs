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
        public string Model { get; set; } = "openai/gpt-4o-mini";

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; } = 0.7f;

        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("api_base")]
        public string? ApiBase { get; set; }

        [JsonPropertyName("add_generation_kwargs")]
        public Dictionary<string, object>? AdditionalParameters { get; set; }

        // Legacy support for max_tokens_to_sample
        [JsonPropertyName("max_tokens_to_sample")]
        public int MaxTokensToSample
        {
            get => MaxTokens;
            set => MaxTokens = value;
        }
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

    // ✅ CORRECT R2R Agent API Models - Based on official R2R documentation
    public class AgentRequest
    {
        [JsonPropertyName("message")]
        public AgentMessage Message { get; set; } = new();

        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; } = "advanced";

        [JsonPropertyName("search_settings")]
        public SearchSettings? SearchSettings { get; set; }

        [JsonPropertyName("rag_generation_config")]
        public RagGenerationConfig? RagGenerationConfig { get; set; }

        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "rag";

        [JsonPropertyName("include_title_if_available")]
        public bool IncludeTitleIfAvailable { get; set; } = true;
    }

    public class AgentMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class AgentResponse
    {
        [JsonPropertyName("results")]
        public AgentResults Results { get; set; } = new();
    }

    public class AgentResults
    {
        [JsonPropertyName("messages")]
        public List<AgentResponseMessage> Messages { get; set; } = new();

        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; set; }

        // Helper property to get the assistant message
        public AgentResponseMessage? AssistantMessage => Messages?.FirstOrDefault(m => m.Role == "assistant");
    }

    public class AgentResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
