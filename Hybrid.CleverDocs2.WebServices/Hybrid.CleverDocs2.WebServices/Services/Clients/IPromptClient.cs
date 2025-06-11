using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Prompt;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IPromptClient
    {
        Task<PromptResponse> CreateAsync(PromptRequest request);
        Task<PromptResponse> GetAsync(string id);
        Task<IEnumerable<PromptResponse>> ListAsync();
        Task<PromptResponse> UpdateAsync(string id, PromptRequest request);
        Task DeleteAsync(string id);
    }
}
