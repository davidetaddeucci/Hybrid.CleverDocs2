using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IConversationClient
    {
        // Conversation CRUD operations
        Task<ConversationCreateResponse?> CreateConversationAsync(ConversationRequest request);
        Task<ConversationResponse?> GetConversationAsync(string conversationId);
        Task<ConversationListResponse?> ListConversationsAsync(ConversationListRequest? request = null);
        Task<ConversationResponse?> UpdateConversationAsync(string conversationId, ConversationUpdateRequest request);
        Task DeleteConversationAsync(string conversationId);

        // Message operations
        Task<MessageCreateResponse?> AddMessageAsync(string conversationId, MessageRequest request);
        Task<MessageResponse?> GetMessageAsync(string conversationId, string messageId);
        Task<MessageListResponse?> ListMessagesAsync(string conversationId, int offset = 0, int limit = 100);
        Task<MessageResponse?> UpdateMessageAsync(string conversationId, string messageId, MessageRequest request);
        Task DeleteMessageAsync(string conversationId, string messageId);

        // Conversation branching
        Task<ConversationBranchResponse?> BranchConversationAsync(string conversationId, ConversationBranchRequest request);

        // Conversation analytics
        Task<ConversationStatsResponse?> GetConversationStatsAsync(string conversationId);

        // Streaming support - DISABLED: R2R streaming endpoint returns 422 errors

        // Bulk operations
        Task<MessageResponse2?> DeleteMultipleMessagesAsync(string conversationId, List<string> messageIds);
        Task<MessageResponse2?> ExportConversationAsync(string conversationId);
        Task<ConversationCreateResponse?> ImportConversationAsync(ConversationRequest request, Stream dataStream);

        // Search within conversation
        Task<MessageListResponse?> SearchMessagesAsync(string conversationId, string query, int offset = 0, int limit = 100);
    }
}
