namespace Hybrid.CleverDocs2.WebServices.Models.Conversations
{
    /// <summary>
    /// DTO for conversation information
    /// </summary>
    public class ConversationDto
    {
        public int Id { get; set; }
        public string R2RConversationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> CollectionIds { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public bool IsPinned { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// DTO for detailed conversation with messages
    /// </summary>
    public class ConversationDetailDto : ConversationDto
    {
        public List<MessageDto> Messages { get; set; } = new();
    }

    /// <summary>
    /// DTO for message information
    /// </summary>
    public class MessageDto
    {
        public int Id { get; set; }
        public string? R2RMessageId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int? ParentMessageId { get; set; }
        public List<Dictionary<string, object>> Citations { get; set; } = new();
        public Dictionary<string, object> RagContext { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public double? ConfidenceScore { get; set; }
        public int? ProcessingTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEdited { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request model for creating a new conversation
    /// </summary>
    public class CreateConversationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? CollectionIds { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    /// <summary>
    /// Request model for sending a message
    /// Based on R2R message patterns with advanced RAG features
    /// </summary>
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public int? ParentMessageId { get; set; }
        public Dictionary<string, object>? RagConfig { get; set; }
    }

    /// <summary>
    /// Request model for updating conversation settings
    /// Based on R2R hybrid search and agentic RAG configuration
    /// </summary>
    public class UpdateConversationSettingsRequest
    {
        /// <summary>
        /// Search mode: "basic", "advanced", "custom"
        /// </summary>
        public string SearchMode { get; set; } = "hybrid";

        /// <summary>
        /// Enable vector/semantic search
        /// </summary>
        public bool UseVectorSearch { get; set; } = true;

        /// <summary>
        /// Enable hybrid search (semantic + keyword)
        /// </summary>
        public bool UseHybridSearch { get; set; } = true;

        /// <summary>
        /// Semantic search weight for RRF (default: 5.0)
        /// </summary>
        public double SemanticWeight { get; set; } = 5.0;

        /// <summary>
        /// Full-text search weight for RRF (default: 1.0)
        /// </summary>
        public double FullTextWeight { get; set; } = 1.0;

        /// <summary>
        /// Reciprocal Rank Fusion constant (default: 50)
        /// </summary>
        public int RrfK { get; set; } = 50;

        /// <summary>
        /// Maximum number of search results
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Relevance threshold for filtering results
        /// </summary>
        public double RelevanceThreshold { get; set; } = 0.7;

        /// <summary>
        /// Include document titles in results
        /// </summary>
        public bool IncludeTitleIfAvailable { get; set; } = true;

        /// <summary>
        /// Enable knowledge graph integration (GraphRAG)
        /// </summary>
        public bool UseKnowledgeGraph { get; set; } = false;

        /// <summary>
        /// Enable agentic RAG with multi-step reasoning
        /// </summary>
        public bool AgenticMode { get; set; } = false;

        /// <summary>
        /// Token budget for agentic reasoning (default: 4096)
        /// </summary>
        public int ThinkingBudget { get; set; } = 4096;

        /// <summary>
        /// Enable multi-step reasoning workflows
        /// </summary>
        public bool MultiStepReasoning { get; set; } = false;

        /// <summary>
        /// Enable contextual enrichment
        /// </summary>
        public bool ContextualEnrichment { get; set; } = true;

        /// <summary>
        /// Enable streaming responses
        /// </summary>
        public bool StreamingEnabled { get; set; } = true;

        /// <summary>
        /// Custom RAG generation configuration
        /// </summary>
        public Dictionary<string, object>? RagGenerationConfig { get; set; }
    }

    /// <summary>
    /// Response model for conversation operations
    /// </summary>
    public class ConversationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ConversationDto? Data { get; set; }
    }

    /// <summary>
    /// Response model for message operations
    /// </summary>
    public class MessageResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public MessageDto? Data { get; set; }
    }

    /// <summary>
    /// Request model for conversation search and filtering
    /// </summary>
    public class ConversationSearchRequest
    {
        public string? Query { get; set; }
        public string? Status { get; set; }
        public bool? IsPinned { get; set; }
        public List<string>? CollectionIds { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "LastMessageAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Response model for paginated conversation results
    /// </summary>
    public class ConversationSearchResponse
    {
        public List<ConversationDto> Conversations { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }



    /// <summary>
    /// Request model for editing a message
    /// </summary>
    public class EditMessageRequest
    {
        public string NewContent { get; set; } = string.Empty;
        public string? EditReason { get; set; }
    }

    /// <summary>
    /// DTO for message edit history
    /// </summary>
    public class MessageEditHistoryDto
    {
        public string PreviousContent { get; set; } = string.Empty;
        public DateTime EditedAt { get; set; }
        public Guid EditedByUserId { get; set; }
        public string? EditReason { get; set; }
    }
}
