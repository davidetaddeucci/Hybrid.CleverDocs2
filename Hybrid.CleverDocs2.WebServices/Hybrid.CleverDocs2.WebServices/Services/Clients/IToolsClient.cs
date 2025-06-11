using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Tools;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IToolsClient
    {
        Task<ToolsResponse> CreateAsync(ToolsRequest request);
        Task<ToolsResponse> GetAsync(string id);
        Task<IEnumerable<ToolsResponse>> ListAsync();
        Task<ToolsResponse> UpdateAsync(string id, ToolsRequest request);
        Task DeleteAsync(string id);
    }
}
