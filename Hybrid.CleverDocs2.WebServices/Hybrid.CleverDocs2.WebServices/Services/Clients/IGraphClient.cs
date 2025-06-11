using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Graph;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IGraphClient
    {
        Task<GraphResponse> CreateAsync(GraphRequest request);
        Task<GraphResponse> GetAsync(string id);
        Task<IEnumerable<GraphResponse>> ListAsync();
        Task<GraphResponse> UpdateAsync(string id, GraphRequest request);
        Task DeleteAsync(string id);
    }
}
