using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.WebDev;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IWebDevClient
    {
        Task<WebDevResponse> CreateAsync(WebDevRequest request);
        Task<WebDevResponse> GetAsync(string id);
        Task<IEnumerable<WebDevResponse>> ListAsync();
        Task<WebDevResponse> UpdateAsync(string id, WebDevRequest request);
        Task DeleteAsync(string id);
    }
}
