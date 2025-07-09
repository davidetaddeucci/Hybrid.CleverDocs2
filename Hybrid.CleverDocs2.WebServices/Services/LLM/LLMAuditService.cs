using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Services.LLM
{
    /// <summary>
    /// Audit service for LLM configuration changes
    /// Provides comprehensive logging and tracking of user LLM preferences
    /// </summary>
    public interface ILLMAuditService
    {
        Task LogConfigurationChangeAsync(Guid userId, string action, object? oldConfig, object? newConfig, string changedBy);
        Task LogConfigurationUsageAsync(Guid userId, string provider, string model, bool success, string? errorMessage = null);
        Task<List<LLMAuditLogEntry>> GetUserAuditLogAsync(Guid userId, int limit = 50);
        Task<List<LLMAuditLogEntry>> GetSystemAuditLogAsync(DateTime? fromDate = null, int limit = 100);
    }

    public class LLMAuditService : ILLMAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LLMAuditService> _logger;

        public LLMAuditService(ApplicationDbContext context, ILogger<LLMAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogConfigurationChangeAsync(Guid userId, string action, object? oldConfig, object? newConfig, string changedBy)
        {
            try
            {
                var auditEntry = new LLMAuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = action,
                    OldConfiguration = oldConfig != null ? JsonSerializer.Serialize(oldConfig) : null,
                    NewConfiguration = newConfig != null ? JsonSerializer.Serialize(newConfig) : null,
                    ChangedBy = changedBy,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetCurrentIpAddress(),
                    UserAgent = GetCurrentUserAgent()
                };

                await _context.LLMAuditLogs.AddAsync(auditEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("LLM configuration audit logged: User {UserId}, Action {Action}, ChangedBy {ChangedBy}", 
                    userId, action, changedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging LLM configuration audit for user {UserId}", userId);
            }
        }

        public async Task LogConfigurationUsageAsync(Guid userId, string provider, string model, bool success, string? errorMessage = null)
        {
            try
            {
                var usageEntry = new LLMUsageLogEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Provider = provider,
                    Model = model,
                    Success = success,
                    ErrorMessage = errorMessage,
                    Timestamp = DateTime.UtcNow,
                    ResponseTimeMs = null // Could be populated if we track response times
                };

                await _context.LLMUsageLogs.AddAsync(usageEntry);
                await _context.SaveChangesAsync();

                if (!success)
                {
                    _logger.LogWarning("LLM usage failed: User {UserId}, Provider {Provider}, Model {Model}, Error {Error}", 
                        userId, provider, model, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging LLM usage for user {UserId}", userId);
            }
        }

        public async Task<List<LLMAuditLogEntry>> GetUserAuditLogAsync(Guid userId, int limit = 50)
        {
            try
            {
                return await _context.LLMAuditLogs
                    .Where(log => log.UserId == userId)
                    .OrderByDescending(log => log.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log for user {UserId}", userId);
                return new List<LLMAuditLogEntry>();
            }
        }

        public async Task<List<LLMAuditLogEntry>> GetSystemAuditLogAsync(DateTime? fromDate = null, int limit = 100)
        {
            try
            {
                var query = _context.LLMAuditLogs.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp >= fromDate.Value);
                }

                return await query
                    .OrderByDescending(log => log.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system audit log");
                return new List<LLMAuditLogEntry>();
            }
        }

        private string? GetCurrentIpAddress()
        {
            // In a real implementation, you would get this from HttpContext
            // For now, return a placeholder
            return "127.0.0.1";
        }

        private string? GetCurrentUserAgent()
        {
            // In a real implementation, you would get this from HttpContext
            // For now, return a placeholder
            return "HybridCleverDocs2.0";
        }
    }

    /// <summary>
    /// Audit log entry for LLM configuration changes
    /// </summary>
    public class LLMAuditLogEntry
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, TEST
        public string? OldConfiguration { get; set; }
        public string? NewConfiguration { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }

    /// <summary>
    /// Usage log entry for LLM API calls
    /// </summary>
    public class LLMUsageLogEntry
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public int? ResponseTimeMs { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }

    /// <summary>
    /// Audit actions for LLM configuration
    /// </summary>
    public static class LLMAuditActions
    {
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string Test = "TEST";
        public const string Use = "USE";
        public const string Validate = "VALIDATE";
    }
}
