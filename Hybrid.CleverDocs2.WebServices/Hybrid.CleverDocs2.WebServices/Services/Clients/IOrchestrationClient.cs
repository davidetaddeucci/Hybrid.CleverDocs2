using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Orchestration;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IOrchestrationClient
    {
        Task<OrchestrationResponse> CreateAsync(OrchestrationRequest request);
        Task<OrchestrationResponse> GetAsync(string id);
        Task<IEnumerable<OrchestrationResponse>> ListAsync();
        Task<OrchestrationResponse> UpdateAsync(string id, OrchestrationRequest request);
        Task DeleteAsync(string id);
    }
}
