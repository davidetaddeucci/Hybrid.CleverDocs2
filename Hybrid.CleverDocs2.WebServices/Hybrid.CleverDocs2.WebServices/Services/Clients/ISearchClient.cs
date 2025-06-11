using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Search;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface ISearchClient
    {
        Task<SearchResponse> CreateAsync(SearchRequest request);
        Task<SearchResponse> GetAsync(string id);
        Task<IEnumerable<SearchResponse>> ListAsync();
        Task<SearchResponse> UpdateAsync(string id, SearchRequest request);
        Task DeleteAsync(string id);
    }
}
