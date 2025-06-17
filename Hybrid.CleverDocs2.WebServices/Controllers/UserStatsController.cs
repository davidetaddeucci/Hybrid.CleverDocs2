using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Extensions;
using Hybrid.CleverDocs2.WebServices.Data;
using Microsoft.EntityFrameworkCore;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

/// <summary>
/// Controller for user statistics and dashboard data
/// </summary>
[ApiController]
[Route("api/user")]
[Authorize(Roles = "User,Admin")]
public class UserStatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserStatsController> _logger;

    public UserStatsController(
        ApplicationDbContext context,
        ILogger<UserStatsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get document count for a user
    /// </summary>
    [HttpGet("{userId:guid}/documents/count")]
    public async Task<IActionResult> GetDocumentCount(Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            // Ensure user can only access their own stats (or admin can access any)
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return this.Error("Access denied", statusCode: 403);
            }

            var count = await _context.Documents
                .Where(d => d.UserId == userId)
                .CountAsync();

            _logger.LogInformation("Retrieved document count {Count} for user {UserId}",
                count, userId);

            return this.Success(count, "Document count retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count for user {UserId}",
                userId);
            return this.Error("Failed to get document count");
        }
    }

    /// <summary>
    /// Get collection count for a user
    /// </summary>
    [HttpGet("{userId:guid}/collections/count")]
    public async Task<IActionResult> GetCollectionCount(Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            // Ensure user can only access their own stats (or admin can access any)
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return this.Error("Access denied", statusCode: 403);
            }

            var count = await _context.Collections
                .Where(c => c.UserId == userId)
                .CountAsync();

            _logger.LogInformation("Retrieved collection count {Count} for user {UserId}",
                count, userId);

            return this.Success(count, "Collection count retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection count for user {UserId}",
                userId);
            return this.Error("Failed to get collection count");
        }
    }

    /// <summary>
    /// Get conversation count for a user
    /// </summary>
    [HttpGet("{userId:guid}/conversations/count")]
    public async Task<IActionResult> GetConversationCount(Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            // Ensure user can only access their own stats (or admin can access any)
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return this.Error("Access denied", statusCode: 403);
            }

            // For now, return 0 as conversations table might not exist yet
            var count = 0;

            // TODO: Implement when conversations table is available
            // var count = await _context.Conversations
            //     .Where(c => c.UserId == userId)
            //     .CountAsync();

            _logger.LogInformation("Retrieved conversation count {Count} for user {UserId}",
                count, userId);

            return this.Success(count, "Conversation count retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation count for user {UserId}",
                userId);
            return this.Error("Failed to get conversation count");
        }
    }

    /// <summary>
    /// Get recent documents for a user
    /// </summary>
    [HttpGet("{userId:guid}/documents/recent")]
    public async Task<IActionResult> GetRecentDocuments(Guid userId, [FromQuery] int limit = 5)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            // Ensure user can only access their own stats (or admin can access any)
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return this.Error("Access denied", statusCode: 403);
            }

            var recentDocuments = await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UpdatedAt)
                .Take(Math.Min(limit, 20)) // Max 20 documents
                .Select(d => new
                {
                    Id = d.Id,
                    Name = d.Name,
                    ContentType = d.ContentType,
                    Size = d.Size,
                    UpdatedAt = d.UpdatedAt,
                    Status = d.Status
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} recent documents for user {UserId}",
                recentDocuments.Count, userId);

            return this.Success(recentDocuments, "Recent documents retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent documents for user {UserId}",
                userId);
            return this.Error("Failed to get recent documents");
        }
    }

    /// <summary>
    /// Get recent conversations for a user
    /// </summary>
    [HttpGet("{userId:guid}/conversations/recent")]
    public async Task<IActionResult> GetRecentConversations(Guid userId, [FromQuery] int limit = 5)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            // Ensure user can only access their own stats (or admin can access any)
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return this.Error("Access denied", statusCode: 403);
            }

            // For now, return empty list as conversations table might not exist yet
            var recentConversations = new List<object>();

            // TODO: Implement when conversations table is available
            // var recentConversations = await _context.Conversations
            //     .Where(c => c.UserId == userId)
            //     .OrderByDescending(c => c.UpdatedAt)
            //     .Take(Math.Min(limit, 20))
            //     .Select(c => new
            //     {
            //         Id = c.Id,
            //         Title = c.Title,
            //         UpdatedAt = c.UpdatedAt,
            //         MessageCount = c.MessageCount
            //     })
            //     .ToListAsync();

            _logger.LogInformation("Retrieved {Count} recent conversations for user {UserId}",
                recentConversations.Count, userId);

            return this.Success(recentConversations, "Recent conversations retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent conversations for user {UserId}",
                userId);
            return this.Error("Failed to get recent conversations");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
