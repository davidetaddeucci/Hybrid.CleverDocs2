using Hybrid.CleverDocs.WebUI.Models;
using Microsoft.AspNetCore.Http;

namespace Hybrid.CleverDocs.WebUI.ViewModels;

public class DashboardViewModel
{
    public UserInfo? CurrentUser { get; set; }
    public string UserRole => CurrentUser?.Role ?? "User";
    public string UserName => $"{CurrentUser?.FirstName} {CurrentUser?.LastName}".Trim();
    public string CompanyName => CurrentUser?.CompanyName ?? "Unknown Company";
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; } = false;
}

public class AdminDashboardViewModel : DashboardViewModel
{
    public int TotalCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int TotalDocuments { get; set; }
    public SystemHealthDto? SystemHealth { get; set; }
    public List<CompanyStatsDto> CompanyStats { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class CompanyDashboardViewModel : DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalCollections { get; set; }
    public int DocumentsThisMonth { get; set; }
    public List<UserStatsDto> UserStats { get; set; } = new();
    public List<DocumentStatsDto> DocumentStats { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class UserDashboardViewModel : DashboardViewModel
{
    public int DocumentCount { get; set; }
    public int CollectionCount { get; set; }
    public int ConversationCount { get; set; }
    public int DocumentsThisWeek { get; set; }
    public List<RecentDocumentDto> RecentDocuments { get; set; } = new();
    public List<RecentConversationDto> RecentConversations { get; set; } = new();
    public UserQuotaUsageDto? QuotaUsage { get; set; }
}

// Supporting DTOs
public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, string> Services { get; set; } = new();
    public DateTime LastChecked { get; set; }
}

public class CompanyStatsDto
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
}

public class UserStatsDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class DocumentStatsDto
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentActivityDto
{
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Icon { get; set; } = "fas fa-info-circle";
    public string BadgeClass { get; set; } = "badge-info";
}

public class RecentDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Status { get; set; }

    // Compatibility properties for Views
    public int DocumentId => 0; // Legacy compatibility
    public string FileName => Name;
    public string FileType => ContentType;
    public DateTime CreatedAt => UpdatedAt;

    public string StatusBadgeClass => Status switch
    {
        1 => "badge-warning", // Processing
        2 => "badge-success", // Ready
        3 => "badge-danger",  // Error
        _ => "badge-secondary"
    };
}

public class RecentConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Compatibility properties for Views
    public int ConversationId => 0; // Legacy compatibility
    public DateTime LastMessage => UpdatedAt;
    public DateTime CreatedAt => UpdatedAt;
}

public class QuotaUsageDto
{
    public int DocumentQuotaUsed { get; set; }
    public int DocumentQuotaLimit { get; set; }
    public int QueryQuotaUsed { get; set; }
    public int QueryQuotaLimit { get; set; }
    public DateTime QuotaResetDate { get; set; }
    
    public double DocumentQuotaPercentage => DocumentQuotaLimit > 0 ? 
        (double)DocumentQuotaUsed / DocumentQuotaLimit * 100 : 0;
    
    public double QueryQuotaPercentage => QueryQuotaLimit > 0 ? 
        (double)QueryQuotaUsed / QueryQuotaLimit * 100 : 0;
    
    public string DocumentQuotaClass => DocumentQuotaPercentage switch
    {
        >= 90 => "progress-bar bg-danger",
        >= 75 => "progress-bar bg-warning",
        _ => "progress-bar bg-success"
    };
    
    public string QueryQuotaClass => QueryQuotaPercentage switch
    {
        >= 90 => "progress-bar bg-danger",
        >= 75 => "progress-bar bg-warning",
        _ => "progress-bar bg-success"
    };
}

public class UserQuotaUsageDto
{
    public int DocumentsUsed { get; set; }
    public int DocumentsLimit { get; set; }
    public long StorageUsed { get; set; }
    public long StorageLimit { get; set; }
    public int QueriesUsed { get; set; }
    public int QueriesLimit { get; set; }
    public DateTime QuotaResetDate { get; set; } = DateTime.UtcNow.AddDays(30);

    // Compatibility properties for Views
    public int DocumentQuotaUsed => DocumentsUsed;
    public int DocumentQuotaLimit => DocumentsLimit;
    public int QueryQuotaUsed => QueriesUsed;
    public int QueryQuotaLimit => QueriesLimit;

    // Percentage calculations
    public double DocumentsUsagePercentage => DocumentsLimit > 0 ? (double)DocumentsUsed / DocumentsLimit * 100 : 0;
    public double StorageUsagePercentage => StorageLimit > 0 ? (double)StorageUsed / StorageLimit * 100 : 0;
    public double QueriesUsagePercentage => QueriesLimit > 0 ? (double)QueriesUsed / QueriesLimit * 100 : 0;

    // Compatibility percentage properties for Views
    public double DocumentQuotaPercentage => DocumentsUsagePercentage;
    public double QueryQuotaPercentage => QueriesUsagePercentage;

    // CSS classes for progress bars
    public string DocumentQuotaClass => DocumentQuotaPercentage switch
    {
        >= 90 => "progress-bar bg-danger",
        >= 75 => "progress-bar bg-warning",
        _ => "progress-bar bg-success"
    };

    public string QueryQuotaClass => QueryQuotaPercentage switch
    {
        >= 90 => "progress-bar bg-danger",
        >= 75 => "progress-bar bg-warning",
        _ => "progress-bar bg-success"
    };
}