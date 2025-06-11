using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Validation;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IValidationClient
    {
        Task<ValidationResponse> CreateAsync(ValidationRequest request);
        Task<ValidationResponse> GetAsync(string id);
        Task<IEnumerable<ValidationResponse>> ListAsync();
        Task<ValidationResponse> UpdateAsync(string id, ValidationRequest request);
        Task DeleteAsync(string id);
    }
}
