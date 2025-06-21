using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services.Chat
{
    /// <summary>
    /// Interface for chat service operations
    /// Based on R2R Conversations API patterns
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        Task<List<ConversationViewModel>> GetConversationsAsync(int page = 1, int pageSize = 20, string? status = null, bool? isPinned = null);

        /// <summary>
        /// Get a specific conversation with messages
        /// </summary>
        Task<ConversationDetailViewModel?> GetConversationAsync(int conversationId);

        /// <summary>
        /// Create a new conversation
        /// </summary>
        Task<ConversationViewModel> CreateConversationAsync(string title, string? description = null, List<string>? collectionIds = null, Dictionary<string, object>? settings = null);

        /// <summary>
        /// Send a message to a conversation
        /// </summary>
        Task<MessageViewModel> SendMessageAsync(int conversationId, string content, int? parentMessageId = null, Dictionary<string, object>? ragConfig = null);

        /// <summary>
        /// Update conversation settings
        /// </summary>
        Task<bool> UpdateConversationSettingsAsync(int conversationId, ChatSettingsViewModel settings);

        /// <summary>
        /// Pin/unpin a conversation
        /// </summary>
        Task<bool> TogglePinConversationAsync(int conversationId);

        /// <summary>
        /// Archive a conversation
        /// </summary>
        Task<bool> ArchiveConversationAsync(int conversationId);

        /// <summary>
        /// Delete a conversation
        /// </summary>
        Task<bool> DeleteConversationAsync(int conversationId);

        /// <summary>
        /// Search conversations
        /// </summary>
        Task<ConversationSearchResultViewModel> SearchConversationsAsync(ConversationSearchViewModel searchRequest);

        /// <summary>
        /// Get conversation statistics
        /// </summary>
        Task<ConversationStatsViewModel> GetConversationStatsAsync();

        /// <summary>
        /// Export conversation to various formats
        /// </summary>
        Task<byte[]> ExportConversationAsync(int conversationId, string format = "json");

        /// <summary>
        /// Get available collections for conversation context
        /// </summary>
        Task<List<CollectionViewModel>> GetAvailableCollectionsAsync();

        /// <summary>
        /// Validate conversation access for current user
        /// </summary>
        Task<bool> ValidateConversationAccessAsync(int conversationId);

        /// <summary>
        /// Get conversation branches (for threading)
        /// </summary>
        Task<List<ConversationBranchViewModel>> GetConversationBranchesAsync(int conversationId);

        /// <summary>
        /// Create a conversation branch
        /// </summary>
        Task<ConversationViewModel> CreateConversationBranchAsync(int conversationId, int fromMessageId, string title);

        /// <summary>
        /// Get message thread (parent-child relationships)
        /// </summary>
        Task<List<MessageViewModel>> GetMessageThreadAsync(int messageId);

        /// <summary>
        /// Edit a message (with history preservation)
        /// </summary>
        Task<MessageViewModel> EditMessageAsync(int messageId, string newContent);

        /// <summary>
        /// Get message edit history
        /// </summary>
        Task<List<MessageEditHistoryViewModel>> GetMessageEditHistoryAsync(int messageId);

        /// <summary>
        /// Rate a message response
        /// </summary>
        Task<bool> RateMessageAsync(int messageId, int rating, string? feedback = null);

        /// <summary>
        /// Get conversation analytics
        /// </summary>
        Task<ConversationAnalyticsViewModel> GetConversationAnalyticsAsync(int conversationId);

        /// <summary>
        /// Get R2R system status for chat functionality
        /// </summary>
        Task<R2RStatusViewModel> GetR2RStatusAsync();
    }
}
