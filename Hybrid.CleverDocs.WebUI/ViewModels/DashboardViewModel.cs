using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.ViewModels;

public class DashboardViewModel
{
    public UserInfo? CurrentUser { get; set; }
    public string UserRole => CurrentUser?.Role ?? "User";
    public string UserName => $"{CurrentUser?.FirstName} {CurrentUser?.LastName}".Trim();
    public string CompanyName => CurrentUser?.CompanyName ?? "Unknown Company";
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
    public QuotaUsageDto? QuotaUsage { get; set; }
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
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string StatusBadgeClass => Status.ToLower() switch
    {
        "completed" => "badge-success",
        "processing" => "badge-warning",
        "failed" => "badge-danger",
        _ => "badge-secondary"
    };
}

public class RecentConversationDto
{
    public int ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
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