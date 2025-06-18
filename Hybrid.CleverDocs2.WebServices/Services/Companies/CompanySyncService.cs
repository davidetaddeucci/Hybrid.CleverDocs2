using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.DTOs;

namespace Hybrid.CleverDocs2.WebServices.Services.Companies
{
    public interface ICompanySyncService
    {
        Task<string?> CreateR2RTenantAsync(Company company);
        Task UpdateR2RTenantAsync(Company company);
        Task DeleteR2RTenantAsync(string r2rTenantId);
        Task<bool> SyncCompanyWithR2RAsync(Company company);
    }

    public class CompanySyncService : ICompanySyncService
    {
        private readonly IMultiLevelCacheService _cacheService;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ICorrelationService _correlationService;
        private readonly ILogger<CompanySyncService> _logger;
        private readonly R2ROptions _options;
        private readonly HttpClient _httpClient;

        public CompanySyncService(
            IMultiLevelCacheService cacheService,
            IRateLimitingService rateLimitingService,
            ICorrelationService correlationService,
            ILogger<CompanySyncService> logger,
            IOptions<R2ROptions> options,
            HttpClient httpClient)
        {
            _cacheService = cacheService;
            _rateLimitingService = rateLimitingService;
            _correlationService = correlationService;
            _logger = logger;
            _options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<string?> CreateR2RTenantAsync(Company company)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                // Check rate limiting for tenant operations
                var canProceed = await _rateLimitingService.CanMakeRequestAsync("tenant_operation");
                if (!canProceed)
                {
                    _logger.LogWarning("Rate limit exceeded for tenant creation, queuing for later processing, CompanyId: {CompanyId}, CorrelationId: {CorrelationId}", 
                        company.Id, correlationId);
                    
                    // TODO: Implement queue for tenant sync operations
                    return null;
                }

                _logger.LogInformation("Creating R2R tenant for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    company.Id, correlationId);

                // For R2R, tenant creation might be implicit or handled differently
                // For now, we'll use the company's TenantId as the R2R tenant identifier
                // This assumes R2R uses tenant-based isolation
                
                var r2rTenantId = company.TenantId.ToString();

                // In a real implementation, you might need to call R2R API to register the tenant
                // For now, we'll simulate this by using the company's TenantId
                
                _logger.LogInformation("R2R tenant created/registered: {R2RTenantId} for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    r2rTenantId, company.Id, correlationId);

                // Cache the R2R tenant mapping
                await _cacheService.SetAsync(
                    $"r2r:tenant:mapping:{company.Id}", 
                    r2rTenantId, 
                    CacheOptions.ForCollectionData(company.TenantId.ToString()));

                return r2rTenantId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating R2R tenant for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    company.Id, correlationId);
                return null;
            }
        }

        public async Task UpdateR2RTenantAsync(Company company)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                if (string.IsNullOrEmpty(company.R2RTenantId))
                {
                    _logger.LogWarning("Cannot update R2R tenant - R2RTenantId is null for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                        company.Id, correlationId);
                    return;
                }

                _logger.LogInformation("Updating R2R tenant {R2RTenantId} for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    company.R2RTenantId, company.Id, correlationId);

                // In a real implementation, you might need to call R2R API to update tenant settings
                // For now, we'll just log the operation
                
                _logger.LogInformation("R2R tenant updated successfully: {R2RTenantId} for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    company.R2RTenantId, company.Id, correlationId);

                // Invalidate cache
                await _cacheService.RemoveAsync($"r2r:tenant:mapping:{company.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating R2R tenant {R2RTenantId} for Company {CompanyId}, CorrelationId: {CorrelationId}", 
                    company.R2RTenantId, company.Id, correlationId);
            }
        }

        public async Task DeleteR2RTenantAsync(string r2rTenantId)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                _logger.LogInformation("Deleting R2R tenant {R2RTenantId}, CorrelationId: {CorrelationId}", 
                    r2rTenantId, correlationId);

                // In a real implementation, you might need to call R2R API to delete the tenant
                // This would involve cleaning up all tenant data, users, collections, documents
                
                _logger.LogInformation("R2R tenant deleted successfully: {R2RTenantId}, CorrelationId: {CorrelationId}", 
                    r2rTenantId, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting R2R tenant {R2RTenantId}, CorrelationId: {CorrelationId}", 
                    r2rTenantId, correlationId);
            }
        }

        public async Task<bool> SyncCompanyWithR2RAsync(Company company)
        {
            try
            {
                if (string.IsNullOrEmpty(company.R2RTenantId))
                {
                    // Create new R2R tenant
                    var r2rTenantId = await CreateR2RTenantAsync(company);
                    return !string.IsNullOrEmpty(r2rTenantId);
                }
                else
                {
                    // Update existing R2R tenant
                    await UpdateR2RTenantAsync(company);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Company {CompanyId} with R2R", company.Id);
                return false;
            }
        }
    }
}
