using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Maintenance;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IMaintenanceClient
    {
        Task<MaintenanceResponse> CreateAsync(MaintenanceRequest request);
        Task<MaintenanceResponse> GetAsync(string id);
        Task<IEnumerable<MaintenanceResponse>> ListAsync();
        Task<MaintenanceResponse> UpdateAsync(string id, MaintenanceRequest request);
        Task DeleteAsync(string id);
    }
}
