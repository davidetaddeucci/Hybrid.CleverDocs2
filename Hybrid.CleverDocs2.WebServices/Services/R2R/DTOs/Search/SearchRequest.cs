using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Search
{
    public class SearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; } = "custom"; // basic, advanced, custom

        [JsonPropertyName("search_settings")]
        public SearchSettings? SearchSettings { get; set; }
    }

    public class SearchSettings
    {
        [JsonPropertyName("filters")]
        public Dictionary<string, object>? Filters { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 10;

        [JsonPropertyName("use_hybrid_search")]
        public bool UseHybridSearch { get; set; } = false;

        [JsonPropertyName("hybrid_settings")]
        public HybridSearchSettings? HybridSettings { get; set; }

        [JsonPropertyName("graph_search_settings")]
        public GraphSearchSettings? GraphSearchSettings { get; set; }
    }

    public class HybridSearchSettings
    {
        [JsonPropertyName("full_text_weight")]
        public double FullTextWeight { get; set; } = 1.0;

        [JsonPropertyName("semantic_weight")]
        public double SemanticWeight { get; set; } = 5.0;

        [JsonPropertyName("full_text_limit")]
        public int FullTextLimit { get; set; } = 200;

        [JsonPropertyName("rrf_k")]
        public int RrfK { get; set; } = 50;
    }

    public class GraphSearchSettings
    {
        [JsonPropertyName("use_graph_search")]
        public bool UseGraphSearch { get; set; } = true;

        [JsonPropertyName("kg_search_type")]
        public string KgSearchType { get; set; } = "local"; // local, global
    }

    public class RAGRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("search_mode")]
        public string SearchMode { get; set; } = "custom";

        [JsonPropertyName("search_settings")]
        public SearchSettings? SearchSettings { get; set; }

        [JsonPropertyName("rag_generation_config")]
        public RAGGenerationConfig? RagGenerationConfig { get; set; }

        [JsonPropertyName("task_prompt")]
        public string? TaskPrompt { get; set; }

        [JsonPropertyName("include_title_if_available")]
        public bool IncludeTitleIfAvailable { get; set; } = false;

        [JsonPropertyName("include_web_search")]
        public bool IncludeWebSearch { get; set; } = false;
    }

    public class RAGGenerationConfig
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "openai/gpt-4o-mini";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1500;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("extended_thinking")]
        public bool? ExtendedThinking { get; set; }

        [JsonPropertyName("thinking_budget")]
        public int? ThinkingBudget { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }
    }

    public class AgentRequest
    {
        [JsonPropertyName("message")]
        public AgentMessage Message { get; set; } = new();

        [JsonPropertyName("search_settings")]
        public SearchSettings? SearchSettings { get; set; }

        [JsonPropertyName("rag_tools")]
        public List<string>? RagTools { get; set; }

        [JsonPropertyName("rag_generation_config")]
        public RAGGenerationConfig? RagGenerationConfig { get; set; }

        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "research"; // research, chat
    }

    public class AgentMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class CompletionRequest
    {
        [JsonPropertyName("messages")]
        public List<CompletionMessage> Messages { get; set; } = new();

        [JsonPropertyName("generation_config")]
        public RAGGenerationConfig? GenerationConfig { get; set; }
    }

    public class CompletionMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class EmbeddingRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }
}
