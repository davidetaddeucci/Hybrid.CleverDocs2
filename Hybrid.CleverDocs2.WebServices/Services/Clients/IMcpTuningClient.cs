using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.McpTuning;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IMcpTuningClient
    {
        Task<McpTuningResponse> CreateAsync(McpTuningRequest request);
        Task<McpTuningResponse> GetAsync(string id);
        Task<IEnumerable<McpTuningResponse>> ListAsync();
        Task<McpTuningResponse> UpdateAsync(string id, McpTuningRequest request);
        Task DeleteAsync(string id);
    }
}
