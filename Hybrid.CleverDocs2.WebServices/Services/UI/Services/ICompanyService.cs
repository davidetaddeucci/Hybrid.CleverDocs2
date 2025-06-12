using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.UI.DTOs.Company;

namespace Hybrid.CleverDocs2.WebServices.Services.UI.Services
{
    /// <summary>
    /// Service interface for WebUI company/tenant management operations
    /// Handles multitenant company operations for the WebUI layer
    /// </summary>
    public interface ICompanyService
    {
        // Company management for WebUI
        Task<CompanyDto?> GetCompanyAsync(int companyId);
        Task<CompanyDto?> GetCompanyByNameAsync(string companyName);
        Task<List<CompanyDto>?> GetAllCompaniesAsync();
        Task<CompanyDto?> CreateCompanyAsync(CreateCompanyRequest request);
        Task<CompanyDto?> UpdateCompanyAsync(int companyId, UpdateCompanyRequest request);
        Task<bool> DeleteCompanyAsync(int companyId);
        
        // Company settings and configuration
        Task<CompanySettingsDto?> GetCompanySettingsAsync(int companyId);
        Task<bool> UpdateCompanySettingsAsync(int companyId, CompanySettingsDto settings);
        
        // Company subscription and limits
        Task<CompanySubscriptionDto?> GetCompanySubscriptionAsync(int companyId);
        Task<bool> UpdateCompanySubscriptionAsync(int companyId, CompanySubscriptionDto subscription);
        Task<CompanyUsageDto?> GetCompanyUsageAsync(int companyId);
        
        // Company analytics and reporting
        Task<CompanyAnalyticsDto?> GetCompanyAnalyticsAsync(int companyId, int days = 30);
        Task<List<CompanyActivityDto>?> GetCompanyActivityAsync(int companyId, int days = 30);
    }
}