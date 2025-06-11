using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IConversationClient
    {
        Task<ConversationResponse> CreateAsync(ConversationRequest request);
        Task<ConversationResponse> GetAsync(string id);
        Task<IEnumerable<ConversationResponse>> ListAsync();
        Task<ConversationResponse> UpdateAsync(string id, ConversationRequest request);
        Task DeleteAsync(string id);
    }
}
