namespace Hybrid.CleverDocs2.WebServices.Models.R2R
{
    /// <summary>
    /// R2R Conversation request model
    /// </summary>
    public class ConversationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// R2R Conversation response model
    /// </summary>
    public class ConversationResponse
    {
        public ConversationResult Results { get; set; } = new();
    }

    public class ConversationResult
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// R2R Message request model with advanced RAG features
    /// </summary>
    public class MessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public bool UseVectorSearch { get; set; } = true;
        public bool UseHybridSearch { get; set; } = true;
        public int SearchLimit { get; set; } = 10;
        public bool IncludeTitleIfAvailable { get; set; } = true;
        public Dictionary<string, object>? RagGenerationConfig { get; set; }
        public Dictionary<string, object>? SearchFilters { get; set; }
        public List<string>? CollectionIds { get; set; }
    }

    /// <summary>
    /// R2R Message response model
    /// </summary>
    public class MessageResponse
    {
        public MessageResult Results { get; set; } = new();
    }

    public class MessageResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<SearchResult> SearchResults { get; set; } = new();
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// R2R Search result model
    /// </summary>
    public class SearchResult
    {
        public string? DocumentId { get; set; }
        public string? ChunkId { get; set; }
        public double Score { get; set; }
        public string? Text { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// R2R Error response model
    /// </summary>
    public class R2RErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// R2R System status model
    /// </summary>
    public class R2RSystemStatus
    {
        public bool IsOnline { get; set; }
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> SystemInfo { get; set; } = new();
        public List<string> AvailableModels { get; set; } = new();
        public bool SupportsStreaming { get; set; }
        public bool SupportsKnowledgeGraph { get; set; }
        public bool SupportsAgenticRAG { get; set; }
    }
}
