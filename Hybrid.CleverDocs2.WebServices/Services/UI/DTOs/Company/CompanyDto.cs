using System;
using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.UI.DTOs.Company
{
    /// <summary>
    /// Company DTO for WebUI operations
    /// Represents a company/tenant in the multitenant WebUI system
    /// </summary>
    public class CompanyDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }

        [JsonPropertyName("subscription_plan")]
        public string SubscriptionPlan { get; set; } = "free";

        [JsonPropertyName("contact_email")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string? ContactPhone { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";
    }

    public class CreateCompanyRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("contact_email")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string? ContactPhone { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";

        [JsonPropertyName("subscription_plan")]
        public string SubscriptionPlan { get; set; } = "free";
    }

    public class UpdateCompanyRequest
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("contact_email")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string? ContactPhone { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    public class CompanySettingsDto
    {
        [JsonPropertyName("max_users")]
        public int MaxUsers { get; set; } = 10;

        [JsonPropertyName("max_documents")]
        public int MaxDocuments { get; set; } = 1000;

        [JsonPropertyName("max_storage_gb")]
        public int MaxStorageGb { get; set; } = 5;

        [JsonPropertyName("features_enabled")]
        public string[] FeaturesEnabled { get; set; } = Array.Empty<string>();

        [JsonPropertyName("api_access_enabled")]
        public bool ApiAccessEnabled { get; set; } = false;

        [JsonPropertyName("sso_enabled")]
        public bool SsoEnabled { get; set; } = false;

        [JsonPropertyName("custom_branding")]
        public bool CustomBranding { get; set; } = false;
    }

    public class CompanySubscriptionDto
    {
        [JsonPropertyName("plan")]
        public string Plan { get; set; } = "free";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("billing_cycle")]
        public string BillingCycle { get; set; } = "monthly";

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";

        [JsonPropertyName("next_billing_date")]
        public DateTime? NextBillingDate { get; set; }

        [JsonPropertyName("trial_ends_at")]
        public DateTime? TrialEndsAt { get; set; }
    }

    public class CompanyUsageDto
    {
        [JsonPropertyName("users_count")]
        public int UsersCount { get; set; }

        [JsonPropertyName("documents_count")]
        public int DocumentsCount { get; set; }

        [JsonPropertyName("storage_used_gb")]
        public decimal StorageUsedGb { get; set; }

        [JsonPropertyName("api_calls_count")]
        public int ApiCallsCount { get; set; }

        [JsonPropertyName("last_activity")]
        public DateTime? LastActivity { get; set; }
    }

    public class CompanyAnalyticsDto
    {
        [JsonPropertyName("total_users")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("active_users")]
        public int ActiveUsers { get; set; }

        [JsonPropertyName("total_documents")]
        public int TotalDocuments { get; set; }

        [JsonPropertyName("documents_processed")]
        public int DocumentsProcessed { get; set; }

        [JsonPropertyName("api_calls")]
        public int ApiCalls { get; set; }

        [JsonPropertyName("storage_used")]
        public decimal StorageUsed { get; set; }

        [JsonPropertyName("period_start")]
        public DateTime PeriodStart { get; set; }

        [JsonPropertyName("period_end")]
        public DateTime PeriodEnd { get; set; }
    }

    public class CompanyActivityDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("company_id")]
        public int CompanyId { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string Details { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}