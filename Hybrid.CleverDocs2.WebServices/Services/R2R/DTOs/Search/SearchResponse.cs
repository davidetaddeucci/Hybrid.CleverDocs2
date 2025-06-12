using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Search
{
    public class SearchResponse
    {
        [JsonPropertyName("results")]
        public SearchResults Results { get; set; } = new();
    }

    public class SearchResults
    {
        [JsonPropertyName("chunk_search_results")]
        public List<ChunkSearchResult> ChunkSearchResults { get; set; } = new();

        [JsonPropertyName("graph_search_results")]
        public List<GraphSearchResult> GraphSearchResults { get; set; } = new();

        [JsonPropertyName("web_search_results")]
        public List<WebSearchResult> WebSearchResults { get; set; } = new();

        [JsonPropertyName("document_search_results")]
        public List<DocumentSearchResult> DocumentSearchResults { get; set; } = new();
    }

    public class ChunkSearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("collection_ids")]
        public List<string> CollectionIds { get; set; } = new();

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }

    public class GraphSearchResult
    {
        [JsonPropertyName("content")]
        public GraphContent Content { get; set; } = new();

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("result_type")]
        public string ResultType { get; set; } = string.Empty;

        [JsonPropertyName("chunk_ids")]
        public List<string> ChunkIds { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class GraphContent
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

    public class WebSearchResult
    {
        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("sitelinks")]
        public List<SiteLink> SiteLinks { get; set; } = new();
    }

    public class SiteLink
    {
        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    public class DocumentSearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("collection_ids")]
        public List<string> CollectionIds { get; set; } = new();

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; set; } = string.Empty;

        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    public class RAGResponse
    {
        [JsonPropertyName("generated_answer")]
        public string GeneratedAnswer { get; set; } = string.Empty;

        [JsonPropertyName("search_results")]
        public SearchResults? SearchResults { get; set; }

        [JsonPropertyName("citations")]
        public List<Citation> Citations { get; set; } = new();
    }

    public class Citation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("raw_index")]
        public int RawIndex { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }

        [JsonPropertyName("end_index")]
        public int EndIndex { get; set; }

        [JsonPropertyName("source_type")]
        public string SourceType { get; set; } = string.Empty;

        [JsonPropertyName("source_id")]
        public string SourceId { get; set; } = string.Empty;

        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("source_title")]
        public string SourceTitle { get; set; } = string.Empty;
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
    }

    public class AgentResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public AgentMetadata? Metadata { get; set; }
    }

    public class AgentMetadata
    {
        [JsonPropertyName("aggregated_search_results")]
        public SearchResults? AggregatedSearchResults { get; set; }

        [JsonPropertyName("citations")]
        public List<Citation> Citations { get; set; } = new();
    }

    public class CompletionResponse
    {
        [JsonPropertyName("results")]
        public CompletionResults Results { get; set; } = new();
    }

    public class CompletionResults
    {
        [JsonPropertyName("choices")]
        public List<CompletionChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public CompletionUsage? Usage { get; set; }
    }

    public class CompletionChoice
    {
        [JsonPropertyName("message")]
        public CompletionResponseMessage Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class CompletionResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class CompletionUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class EmbeddingResponse
    {
        [JsonPropertyName("results")]
        public List<double> Results { get; set; } = new();
    }
}
