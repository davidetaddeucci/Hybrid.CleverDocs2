namespace Hybrid.CleverDocs.WebUI.Models
{
    public class ChatIndexViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public List<ConversationViewModel> Conversations { get; set; } = new();
        public List<CollectionViewModel> AvailableCollections { get; set; } = new();
        public ChatSettingsViewModel Settings { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
    }

    public class ConversationViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
        public bool IsActive { get; set; }
        public List<string> CollectionIds { get; set; } = new();
        public string Status { get; set; } = "active";
    }

    public class ConversationDetailViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? R2RConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> CollectionIds { get; set; } = new();
        public string Status { get; set; } = "Active";
        public int MessageCount { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageViewModel> Messages { get; set; } = new();
        public List<CollectionViewModel> Collections { get; set; } = new();
        public ChatSettingsViewModel Settings { get; set; } = new();
        public ConversationMetadataViewModel Metadata { get; set; } = new();
    }

    public class MessageViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public List<CitationViewModel> Citations { get; set; } = new();
        public bool IsUser => Role == "user";
        public bool IsAssistant => Role == "assistant";
        public bool IsSystem => Role == "system";
        public bool IsTemporary { get; set; }
        public MessageMetadataViewModel Metadata { get; set; } = new();

        // R2R Enhanced Properties
        public string? ParentMessageId { get; set; }
        public List<Dictionary<string, object>> RagContext { get; set; } = new();
        public double? ConfidenceScore { get; set; }
        public int? ProcessingTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = "sent"; // "sent", "processing", "completed", "failed"
        public bool IsEdited { get; set; }
        public bool IsTyping { get; set; }

        // Helper properties for UI
        public bool HasCitations => Citations?.Any() == true;
        public bool HasRagContext => RagContext?.Any() == true;
        public string FormattedProcessingTime => ProcessingTimeMs.HasValue ? $"{ProcessingTimeMs}ms" : "";
        public string FormattedTokenCount => TokenCount.HasValue ? $"{TokenCount} tokens" : "";
        public string FormattedConfidence => ConfidenceScore.HasValue ? $"{ConfidenceScore:P1}" : "";
    }

    public class CitationViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }
        public int PageNumber { get; set; }
        public string CollectionId { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CollectionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#007bff";
        public int DocumentCount { get; set; }
        public bool IsSelected { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChatSettingsViewModel
    {
        public List<string> SelectedCollectionIds { get; set; } = new();
        public double RelevanceThreshold { get; set; } = 0.7;
        public int MaxResults { get; set; } = 10;
        public string SearchMode { get; set; } = "hybrid"; // "basic", "advanced", "custom"
        public bool UseVectorSearch { get; set; } = true;
        public bool UseHybridSearch { get; set; } = true;
        public bool IncludeTitleIfAvailable { get; set; } = true;

        // R2R Advanced Settings
        public double SemanticWeight { get; set; } = 5.0; // R2R semantic search weight
        public double FullTextWeight { get; set; } = 1.0; // R2R keyword search weight
        public int RrfK { get; set; } = 50; // Reciprocal Rank Fusion constant
        public bool UseKnowledgeGraph { get; set; } = false; // GraphRAG integration
        public bool AgenticMode { get; set; } = false; // Agentic RAG capabilities
        public int ThinkingBudget { get; set; } = 4096; // Token budget for agentic reasoning
        public bool MultiStepReasoning { get; set; } = false; // Multi-step reasoning
        public bool ContextualEnrichment { get; set; } = true; // Contextual enrichment
        public bool StreamingEnabled { get; set; } = true; // Streaming responses

        public Dictionary<string, object> RagGenerationConfig { get; set; } = new();
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
    }

    public class ConversationMetadataViewModel
    {
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int AssistantMessages { get; set; }
        public DateTime FirstMessageAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public TimeSpan Duration => LastMessageAt - FirstMessageAt;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    public class MessageMetadataViewModel
    {
        public double? ProcessingTime { get; set; }
        public int? TokensUsed { get; set; }
        public string? Model { get; set; }
        public double? Temperature { get; set; }
        public List<string> SearchFilters { get; set; } = new();
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    public class ChatStreamingViewModel
    {
        public string MessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public List<CitationViewModel> Citations { get; set; } = new();
        public string? Error { get; set; }
        public MessageMetadataViewModel Metadata { get; set; } = new();
    }

    public class ConversationBranchViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string ParentConversationId { get; set; } = string.Empty;
        public string FromMessageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class ChatExportViewModel
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string Format { get; set; } = "json";
        public bool IncludeCitations { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
        public List<MessageViewModel> Messages { get; set; } = new();
        public ConversationMetadataViewModel Metadata { get; set; } = new();
    }

    public class ChatSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<ConversationViewModel> Results { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public ChatSearchFiltersViewModel Filters { get; set; } = new();
        public int TotalResults { get; set; }
        public double SearchTime { get; set; }
    }

    public class ChatSearchFiltersViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> CollectionIds { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string? MessageRole { get; set; } // "user", "assistant", or null for both
        public int? MinMessages { get; set; }
        public int? MaxMessages { get; set; }
    }

    public class ChatAnalyticsViewModel
    {
        public int TotalConversations { get; set; }
        public int TotalMessages { get; set; }
        public double AverageMessagesPerConversation { get; set; }
        public TimeSpan AverageConversationDuration { get; set; }
        public List<CollectionUsageViewModel> CollectionUsage { get; set; } = new();
        public List<DailyUsageViewModel> DailyUsage { get; set; } = new();
        public List<string> PopularTopics { get; set; } = new();
    }

    public class CollectionUsageViewModel
    {
        public string CollectionId { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class DailyUsageViewModel
    {
        public DateTime Date { get; set; }
        public int ConversationCount { get; set; }
        public int MessageCount { get; set; }
    }

    // R2R Enhanced ViewModels
    public class ConversationSearchViewModel
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

    public class ConversationSearchResultViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ConversationStatsViewModel
    {
        public int TotalConversations { get; set; }
        public int ActiveConversations { get; set; }
        public int ArchivedConversations { get; set; }
        public int TotalMessages { get; set; }
        public double AverageMessagesPerConversation { get; set; }
        public DateTime? LastActivity { get; set; }
        public List<CollectionUsageViewModel> TopCollections { get; set; } = new();
    }

    public class MessageEditHistoryViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string OriginalContent { get; set; } = string.Empty;
        public string EditedContent { get; set; } = string.Empty;
        public DateTime EditedAt { get; set; }
        public string EditReason { get; set; } = string.Empty;
    }

    public class ConversationAnalyticsViewModel
    {
        public string ConversationId { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int AssistantMessages { get; set; }
        public TimeSpan Duration { get; set; }
        public double AverageResponseTime { get; set; }
        public double AverageConfidenceScore { get; set; }
        public int TotalTokensUsed { get; set; }
        public List<string> TopTopics { get; set; } = new();
        public List<CollectionUsageViewModel> CollectionUsage { get; set; } = new();
    }

    public class R2RStatusViewModel
    {
        public bool IsOnline { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime LastCheck { get; set; }
        public List<string> AvailableModels { get; set; } = new();
        public Dictionary<string, object> SystemInfo { get; set; } = new();
        public bool SupportsStreaming { get; set; }
        public bool SupportsKnowledgeGraph { get; set; }
        public bool SupportsAgenticRAG { get; set; }
    }
}
