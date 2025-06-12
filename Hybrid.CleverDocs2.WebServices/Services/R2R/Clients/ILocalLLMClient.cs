using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.LocalLLM;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface ILocalLLMClient
    {
        Task<LocalLLMResponse> CreateAsync(LocalLLMRequest request);
        Task<LocalLLMResponse> GetAsync(string id);
        Task<IEnumerable<LocalLLMResponse>> ListAsync();
        Task<LocalLLMResponse> UpdateAsync(string id, LocalLLMRequest request);
        Task DeleteAsync(string id);
    }
}
