using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation
{
    public class ConversationResponse
    {
        [JsonPropertyName("id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConversationCreateResponse
    {
        [JsonPropertyName("results")]
        public ConversationResponse Results { get; set; } = new();
    }

    public class ConversationListResponse
    {
        [JsonPropertyName("results")]
        public List<ConversationResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class MessageResponse
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty; // user, assistant, system

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("search_results")]
        public List<SearchResult>? SearchResults { get; set; }

        [JsonPropertyName("sources")]
        public List<string>? Sources { get; set; }
    }

    public class MessageCreateResponse
    {
        [JsonPropertyName("results")]
        public MessageResponse Results { get; set; } = new();
    }

    public class MessageListResponse
    {
        [JsonPropertyName("results")]
        public List<MessageResponse> Results { get; set; } = new();

        [JsonPropertyName("total_entries")]
        public int TotalEntries { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("chunk_id")]
        public string ChunkId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConversationBranchResponse
    {
        [JsonPropertyName("new_conversation_id")]
        public string NewConversationId { get; set; } = string.Empty;

        [JsonPropertyName("branch_point_message_id")]
        public string BranchPointMessageId { get; set; } = string.Empty;
    }

    public class ConversationStatsResponse
    {
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("message_count")]
        public int MessageCount { get; set; }

        [JsonPropertyName("user_message_count")]
        public int UserMessageCount { get; set; }

        [JsonPropertyName("assistant_message_count")]
        public int AssistantMessageCount { get; set; }

        [JsonPropertyName("first_message_at")]
        public DateTime? FirstMessageAt { get; set; }

        [JsonPropertyName("last_message_at")]
        public DateTime? LastMessageAt { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("average_response_time_ms")]
        public double AverageResponseTimeMs { get; set; }
    }

    public class MessageResponse2
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
