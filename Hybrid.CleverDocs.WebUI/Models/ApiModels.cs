using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs.WebUI.Models;

// Authentication Models
public record LoginRequest(string Email, string Password, bool RememberMe = false);

public record LoginResponse(
    string AccessToken,
    UserProfile User,
    DateTime ExpiresAt
);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record VerifyEmailRequest(string Token);

public record ResendVerificationRequest(string Email);

// User Models
public record UserProfile(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    string? CompanyId = null,
    string? CompanyName = null,
    string? AvatarUrl = null,
    bool IsActive = true,
    bool IsEmailVerified = true,
    DateTime? LastLogin = null,
    DateTime CreatedAt = default
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
};

public record CreateUserRequest(
    string Email,
    string Name,
    string Password,
    UserRole Role,
    string? CompanyId = null
);

public record UpdateUserRequest(
    string Name,
    string? LogoUrl = null
);

// Company Models
public record Company(
    string Id,
    string Name,
    string Email,
    string? LogoUrl,
    string? Phone,
    string? Address,
    CompanySettings Settings,
    DateTime CreatedAt,
    int UserCount,
    int CollectionCount,
    int DocumentCount
);

public record CompanySettings(
    int MaxUsers,
    int MaxDocumentsPerUser,
    int MaxQueriesPerHour,
    string DefaultLlmModel,
    string? ApiKey,
    string RetrievalMode
);

public record CreateCompanyRequest(
    string Name,
    string Email,
    string Password,
    string? LogoUrl = null,
    string? Phone = null,
    string? Address = null,
    CompanySettings? Settings = null
);

// Collection Models
public record Collection(
    string Id,
    string Name,
    string? Description,
    string UserId,
    string UserName,
    int DocumentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateCollectionRequest(string Name, string? Description = null);

public record UpdateCollectionRequest(string Name, string? Description = null);

// Document Models
public record Document(
    string Id,
    string Name,
    string FileName,
    long Size,
    string ContentType,
    string CollectionId,
    DocumentStatus Status,
    DateTime UploadedAt,
    string? ErrorMessage = null
);

public enum DocumentStatus
{
    Uploading,
    Processing,
    Completed,
    Failed
}

// Chat Models
public record ChatConversation(
    string Id,
    string Title,
    string UserId,
    string[] CollectionIds,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ChatMessage[] Messages
);

public record ChatMessage(
    string Id,
    string Content,
    bool IsUser,
    DateTime Timestamp,
    string[]? Sources = null
);

public record SendMessageRequest(
    string ConversationId,
    string Message,
    string[] CollectionIds
);

public record ChatResponse(
    string Message,
    string[] Sources,
    string ConversationId
);

// Analytics Models
public record DashboardStats(
    int TotalUsers,
    int TotalCompanies,
    int TotalCollections,
    int TotalDocuments,
    int TotalQueries,
    AnalyticsData[] RecentActivity
);

public record AnalyticsData(
    string Label,
    int Value,
    DateTime Date,
    string Type
);

// API Response Models
public record ApiResponse<T>(
    bool Success,
    T? Data = default,
    string? Message = null,
    string[]? Errors = null
);

public record PaginatedResponse<T>(
    T[] Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// File Upload Models
public record FileUploadResult(
    bool Success,
    string? DocumentId = null,
    string? FileName = null,
    string? ErrorMessage = null
);