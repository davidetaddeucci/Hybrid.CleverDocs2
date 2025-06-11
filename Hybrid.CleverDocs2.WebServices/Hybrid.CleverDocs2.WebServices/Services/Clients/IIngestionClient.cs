using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IIngestionClient
    {
        Task<IngestionResponse> CreateAsync(IngestionRequest request);
        Task<IngestionResponse> GetAsync(string id);
        Task<IEnumerable<IngestionResponse>> ListAsync();
        Task<IngestionResponse> UpdateAsync(string id, IngestionRequest request);
        Task DeleteAsync(string id);
    }
}
