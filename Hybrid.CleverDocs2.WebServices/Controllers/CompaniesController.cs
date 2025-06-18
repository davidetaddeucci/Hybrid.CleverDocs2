using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Companies;
using Hybrid.CleverDocs2.WebServices.Models.Companies;
using Hybrid.CleverDocs2.WebServices.Middleware;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompanySyncService _companySyncService;
        private readonly IHubContext<CollectionHub> _hubContext;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(
            ApplicationDbContext context,
            ICompanySyncService companySyncService,
            IHubContext<CollectionHub> hubContext,
            ILogger<CompaniesController> logger)
        {
            _context = context;
            _companySyncService = companySyncService;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Get all companies (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginatedResponse<CompanyDto>>> GetCompanies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Companies.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || 
                                           c.ContactEmail.Contains(search));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == isActive.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var companies = await query
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CompanyDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        Website = c.Website,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone,
                        Address = c.Address,
                        IsActive = c.IsActive,
                        MaxUsers = c.MaxUsers,
                        MaxDocuments = c.MaxDocuments,
                        MaxStorageBytes = c.MaxStorageBytes,
                        MaxCollections = c.MaxCollections,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        CreatedBy = c.CreatedBy,
                        TenantId = c.TenantId,
                        R2RApiKey = c.R2RApiKey,
                        R2RConfiguration = c.R2RConfiguration,
                        R2RTenantId = c.R2RTenantId,
                        UserCount = c.Users.Count(),
                        DocumentCount = c.Users.SelectMany(u => u.Documents).Count(),
                        CollectionCount = c.Users.SelectMany(u => u.Collections).Count(),
                        StorageUsed = c.Users.SelectMany(u => u.Documents).Sum(d => d.SizeInBytes)
                    })
                    .ToListAsync();

                return Ok(PaginatedResponse<CompanyDto>.SuccessPaginatedResponse(
                    companies, page, pageSize, totalCount, $"Retrieved {companies.Count} companies"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return BadRequest(PaginatedResponse<CompanyDto>.ErrorResponse("Failed to retrieve companies"));
            }
        }

        /// <summary>
        /// Get company by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CompanyDto>>> GetCompany(Guid id)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;

                // Admin can access any company, others only their own
                if (userRole != "Admin" && userCompanyId != id.ToString())
                {
                    return Forbid("Access denied to this company");
                }

                var company = await _context.Companies
                    .Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                var companyDto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Description = company.Description,
                    Website = company.Website,
                    ContactEmail = company.ContactEmail,
                    ContactPhone = company.ContactPhone,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    MaxUsers = company.MaxUsers,
                    MaxDocuments = company.MaxDocuments,
                    MaxStorageBytes = company.MaxStorageBytes,
                    MaxCollections = company.MaxCollections,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    CreatedBy = company.CreatedBy,
                    TenantId = company.TenantId,
                    R2RApiKey = company.R2RApiKey,
                    R2RConfiguration = company.R2RConfiguration,
                    R2RTenantId = company.R2RTenantId,
                    UserCount = company.Users.Count(),
                    DocumentCount = company.Users.SelectMany(u => u.Documents).Count(),
                    CollectionCount = company.Users.SelectMany(u => u.Collections).Count(),
                    StorageUsed = company.Users.SelectMany(u => u.Documents).Sum(d => d.SizeInBytes)
                };

                return Ok(new ApiResponse<CompanyDto>
                {
                    Success = true,
                    Data = companyDto,
                    Message = "Company retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
                return BadRequest(ApiResponse<CompanyDto>.ErrorResponse($"Failed to retrieve company {id}"));
            }
        }

        /// <summary>
        /// Create new company (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateCompany(CreateCompanyDto request)
        {
            try
            {
                // Check if company name already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name == request.Name);

                if (existingCompany != null)
                {
                    return BadRequest($"Company with name '{request.Name}' already exists");
                }

                var companyId = Guid.NewGuid();
                var company = new Company
                {
                    Id = companyId,
                    TenantId = companyId, // TenantId = Company.Id for consistency
                    Name = request.Name,
                    Description = request.Description,
                    Website = request.Website,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    IsActive = true,
                    MaxUsers = request.MaxUsers,
                    MaxDocuments = request.MaxDocuments,
                    MaxStorageBytes = request.MaxStorageBytes,
                    MaxCollections = request.MaxCollections,
                    R2RApiKey = request.R2RApiKey,
                    R2RConfiguration = request.R2RConfiguration,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin"
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // Create in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var r2rTenantId = await _companySyncService.CreateR2RTenantAsync(company);
                        if (!string.IsNullOrEmpty(r2rTenantId))
                        {
                            // Update database with R2R tenant ID
                            company.R2RTenantId = r2rTenantId;
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("R2R tenant ID {R2RTenantId} saved to database for company {CompanyId}",
                                r2rTenantId, company.Id);

                            // Notify via SignalR
                            await _hubContext.Clients.All.SendAsync("CompanyCreated", new { 
                                CompanyId = company.Id, 
                                R2RTenantId = r2rTenantId,
                                Name = company.Name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create R2R tenant for company {CompanyId}", company.Id);
                    }
                });

                var companyDto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Description = company.Description,
                    Website = company.Website,
                    ContactEmail = company.ContactEmail,
                    ContactPhone = company.ContactPhone,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    MaxUsers = company.MaxUsers,
                    MaxDocuments = company.MaxDocuments,
                    MaxStorageBytes = company.MaxStorageBytes,
                    MaxCollections = company.MaxCollections,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    CreatedBy = company.CreatedBy,
                    TenantId = company.TenantId,
                    R2RApiKey = company.R2RApiKey,
                    R2RConfiguration = company.R2RConfiguration,
                    R2RTenantId = company.R2RTenantId,
                    UserCount = 0,
                    DocumentCount = 0,
                    CollectionCount = 0,
                    StorageUsed = 0
                };

                _logger.LogInformation("Company created successfully: {CompanyId} - {Name}", company.Id, company.Name);

                return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, 
                    new ApiResponse<CompanyDto>
                    {
                        Success = true,
                        Data = companyDto,
                        Message = "Company created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return BadRequest(ApiResponse<CompanyDto>.ErrorResponse("Failed to create company"));
            }
        }

        /// <summary>
        /// Update company (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateCompany(Guid id, UpdateCompanyDto request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                // Check if new name conflicts with existing company
                if (!string.IsNullOrEmpty(request.Name) && request.Name != company.Name)
                {
                    var existingCompany = await _context.Companies
                        .FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != id);

                    if (existingCompany != null)
                    {
                        return BadRequest($"Company with name '{request.Name}' already exists");
                    }
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.Name))
                    company.Name = request.Name;
                if (request.Description != null)
                    company.Description = request.Description;
                if (request.Website != null)
                    company.Website = request.Website;
                if (request.ContactEmail != null)
                    company.ContactEmail = request.ContactEmail;
                if (request.ContactPhone != null)
                    company.ContactPhone = request.ContactPhone;
                if (request.Address != null)
                    company.Address = request.Address;
                if (request.IsActive.HasValue)
                    company.IsActive = request.IsActive.Value;
                if (request.MaxUsers.HasValue)
                    company.MaxUsers = request.MaxUsers.Value;
                if (request.MaxDocuments.HasValue)
                    company.MaxDocuments = request.MaxDocuments.Value;
                if (request.MaxStorageBytes.HasValue)
                    company.MaxStorageBytes = request.MaxStorageBytes.Value;
                if (request.MaxCollections.HasValue)
                    company.MaxCollections = request.MaxCollections.Value;
                if (request.R2RApiKey != null)
                    company.R2RApiKey = request.R2RApiKey;
                if (request.R2RConfiguration != null)
                    company.R2RConfiguration = request.R2RConfiguration;

                company.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _companySyncService.UpdateR2RTenantAsync(company);

                        // Notify via SignalR
                        await _hubContext.Clients.All.SendAsync("CompanyUpdated", new {
                            CompanyId = company.Id,
                            R2RTenantId = company.R2RTenantId,
                            Name = company.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update R2R tenant for company {CompanyId}", company.Id);
                    }
                });

                var companyDto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Description = company.Description,
                    Website = company.Website,
                    ContactEmail = company.ContactEmail,
                    ContactPhone = company.ContactPhone,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    MaxUsers = company.MaxUsers,
                    MaxDocuments = company.MaxDocuments,
                    MaxStorageBytes = company.MaxStorageBytes,
                    MaxCollections = company.MaxCollections,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    CreatedBy = company.CreatedBy,
                    TenantId = company.TenantId,
                    R2RApiKey = company.R2RApiKey,
                    R2RConfiguration = company.R2RConfiguration,
                    R2RTenantId = company.R2RTenantId,
                    UserCount = company.Users?.Count() ?? 0,
                    DocumentCount = company.Users?.SelectMany(u => u.Documents).Count() ?? 0,
                    CollectionCount = company.Users?.SelectMany(u => u.Collections).Count() ?? 0,
                    StorageUsed = company.Users?.SelectMany(u => u.Documents).Sum(d => d.SizeInBytes) ?? 0
                };

                _logger.LogInformation("Company updated successfully: {CompanyId} - {Name}", company.Id, company.Name);

                return Ok(new ApiResponse<CompanyDto>
                {
                    Success = true,
                    Data = companyDto,
                    Message = "Company updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return BadRequest(ApiResponse<CompanyDto>.ErrorResponse($"Failed to update company {id}"));
            }
        }

        /// <summary>
        /// Deactivate company (Admin only) - Soft delete
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateCompany(Guid id)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                if (!company.IsActive)
                {
                    return BadRequest("Company is already deactivated");
                }

                // Soft delete - deactivate company and all users
                company.IsActive = false;
                company.UpdatedAt = DateTime.UtcNow;

                // Deactivate all users in the company
                foreach (var user in company.Users)
                {
                    user.IsActive = false;
                }

                await _context.SaveChangesAsync();

                // Note: We don't delete from R2R immediately, just deactivate
                // R2R cleanup can be done separately if needed

                // Notify via SignalR
                await _hubContext.Clients.All.SendAsync("CompanyDeactivated", new {
                    CompanyId = company.Id,
                    Name = company.Name
                });

                _logger.LogInformation("Company deactivated successfully: {CompanyId} - {Name}", company.Id, company.Name);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { CompanyId = company.Id, Name = company.Name },
                    Message = "Company deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company {CompanyId}", id);
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to deactivate company {id}"));
            }
        }
    }
}
