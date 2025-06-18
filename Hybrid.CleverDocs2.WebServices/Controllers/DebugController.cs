using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ApplicationDbContext context, ILogger<DebugController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("r2r-status")]
        public async Task<IActionResult> GetR2RStatus()
        {
            try
            {
                // Get all users
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Role,
                        u.CompanyId,
                        u.IsActive
                    })
                    .ToListAsync();

                // Get all collections with R2R info
                var collections = await _context.Collections
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.UserId,
                        c.R2RCollectionId,
                        c.LastSyncedAt,
                        c.GraphClusterStatus,
                        c.GraphSyncStatus,
                        c.CreatedAt
                    })
                    .ToListAsync();

                // Get all documents with R2R info
                var documents = await _context.Documents
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.FileName,
                        d.CollectionId,
                        d.UserId,
                        d.R2RDocumentId,
                        d.R2RIngestionJobId,
                        d.R2RProcessedAt,
                        d.Status,
                        d.CreatedAt
                    })
                    .ToListAsync();

                var result = new
                {
                    Users = users,
                    Collections = collections,
                    Documents = documents,
                    Summary = new
                    {
                        TotalUsers = users.Count,
                        TotalCollections = collections.Count,
                        TotalDocuments = documents.Count,
                        CollectionsWithR2RId = collections.Count(c => !string.IsNullOrEmpty(c.R2RCollectionId)),
                        DocumentsWithR2RId = documents.Count(d => !string.IsNullOrEmpty(d.R2RDocumentId)),
                        DocumentsWithIngestionJobId = documents.Count(d => !string.IsNullOrEmpty(d.R2RIngestionJobId))
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting R2R status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("admin-users")]
        public async Task<IActionResult> GetAdminUsers()
        {
            try
            {
                var adminUsers = await _context.Users
                    .Where(u => u.Role == Data.Entities.UserRole.Admin)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Role,
                        u.CompanyId,
                        u.IsActive,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(adminUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin users");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
