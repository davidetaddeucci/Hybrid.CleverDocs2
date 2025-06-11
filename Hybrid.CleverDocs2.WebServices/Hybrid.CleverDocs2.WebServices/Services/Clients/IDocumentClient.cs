using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IDocumentClient
    {
        Task<DocumentResponse> CreateAsync(DocumentRequest request);
        Task<DocumentResponse> GetAsync(string id);
        Task<IEnumerable<DocumentResponse>> ListAsync();
        Task<DocumentResponse> UpdateAsync(string id, DocumentRequest request);
        Task DeleteAsync(string id);
    }
}
