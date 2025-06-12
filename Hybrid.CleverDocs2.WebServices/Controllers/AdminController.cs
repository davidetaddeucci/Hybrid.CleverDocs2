using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("companies/count")]
        public async Task<ActionResult<int>> GetCompaniesCount()
        {
            try
            {
                var count = await _context.Companies.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("users/count")]
        public async Task<ActionResult<int>> GetUsersCount()
        {
            try
            {
                var count = await _context.Users.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("documents/count")]
        public async Task<ActionResult<int>> GetDocumentsCount()
        {
            try
            {
                var count = await _context.Documents.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("collections/count")]
        public async Task<ActionResult<int>> GetCollectionsCount()
        {
            try
            {
                var count = await _context.Collections.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collections count");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("companies/stats")]
        public async Task<ActionResult<object>> GetCompaniesStats()
        {
            try
            {
                var stats = new
                {
                    total = await _context.Companies.CountAsync(),
                    active = await _context.Companies.CountAsync(c => c.IsActive),
                    inactive = await _context.Companies.CountAsync(c => !c.IsActive)
                };
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies stats");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("activities/recent")]
        public async Task<ActionResult<object[]>> GetRecentActivities()
        {
            try
            {
                var activities = await _context.AuditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new
                    {
                        id = a.Id,
                        action = a.Action,
                        entityType = a.EntityType,
                        details = a.Details,
                        createdAt = a.CreatedAt,
                        userId = a.UserId
                    })
                    .ToArrayAsync();
                
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}