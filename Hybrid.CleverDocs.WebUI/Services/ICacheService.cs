using System;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public interface ICacheService
    {
        /// <summary>
        /// Get cached value by key
        /// </summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set cached value with expiration
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Remove cached value by key
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove cached values by pattern
        /// </summary>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Get or set cached value with factory function
        /// </summary>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Get multiple cached values by keys
        /// </summary>
        Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;

        /// <summary>
        /// Set multiple cached values
        /// </summary>
        Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Refresh cache entry expiration
        /// </summary>
        Task RefreshAsync(string key);
    }

    public enum CacheLevel
    {
        Memory,
        Distributed,
        Both
    }

    public static class CacheKeys
    {
        // Dashboard Cache Keys
        public const string ADMIN_DASHBOARD = "admin:dashboard";
        public const string COMPANY_DASHBOARD = "company:{0}:dashboard";
        public const string USER_DASHBOARD = "user:{0}:dashboard";

        // Statistics Cache Keys
        public const string ADMIN_COMPANIES_COUNT = "admin:companies:count";
        public const string ADMIN_USERS_COUNT = "admin:users:count";
        public const string ADMIN_DOCUMENTS_COUNT = "admin:documents:count";
        public const string ADMIN_COMPANY_STATS = "admin:companies:stats";
        public const string ADMIN_RECENT_ACTIVITIES = "admin:activities:recent";

        public const string COMPANY_USERS_COUNT = "company:{0}:users:count";
        public const string COMPANY_DOCUMENTS_COUNT = "company:{0}:documents:count";
        public const string COMPANY_COLLECTIONS_COUNT = "company:{0}:collections:count";
        public const string COMPANY_USER_STATS = "company:{0}:users:stats";
        public const string COMPANY_DOCUMENT_STATS = "company:{0}:documents:stats";
        public const string COMPANY_RECENT_ACTIVITIES = "company:{0}:activities:recent";

        public const string USER_DOCUMENTS_COUNT = "user:{0}:documents:count";
        public const string USER_COLLECTIONS_COUNT = "user:{0}:collections:count";
        public const string USER_CONVERSATIONS_COUNT = "user:{0}:conversations:count";
        public const string USER_RECENT_DOCUMENTS = "user:{0}:documents:recent";
        public const string USER_RECENT_CONVERSATIONS = "user:{0}:conversations:recent";
        public const string USER_QUOTA_USAGE = "user:{0}:quota:usage";

        // System Cache Keys
        public const string SYSTEM_HEALTH = "system:health";
        public const string SYSTEM_CONFIG = "system:config";

        // Helper methods for formatted keys
        public static string CompanyDashboard(Guid companyId) => string.Format(COMPANY_DASHBOARD, companyId);
        public static string UserDashboard(Guid userId) => string.Format(USER_DASHBOARD, userId);
        public static string CompanyUsersCount(Guid companyId) => string.Format(COMPANY_USERS_COUNT, companyId);
        public static string CompanyDocumentsCount(Guid companyId) => string.Format(COMPANY_DOCUMENTS_COUNT, companyId);
        public static string CompanyCollectionsCount(Guid companyId) => string.Format(COMPANY_COLLECTIONS_COUNT, companyId);
        public static string CompanyUserStats(Guid companyId) => string.Format(COMPANY_USER_STATS, companyId);
        public static string CompanyDocumentStats(Guid companyId) => string.Format(COMPANY_DOCUMENT_STATS, companyId);
        public static string CompanyRecentActivities(Guid companyId) => string.Format(COMPANY_RECENT_ACTIVITIES, companyId);
        public static string UserDocumentsCount(Guid userId) => string.Format(USER_DOCUMENTS_COUNT, userId);
        public static string UserCollectionsCount(Guid userId) => string.Format(USER_COLLECTIONS_COUNT, userId);
        public static string UserConversationsCount(Guid userId) => string.Format(USER_CONVERSATIONS_COUNT, userId);
        public static string UserRecentDocuments(Guid userId) => string.Format(USER_RECENT_DOCUMENTS, userId);
        public static string UserRecentConversations(Guid userId) => string.Format(USER_RECENT_CONVERSATIONS, userId);
        public static string UserQuotaUsage(Guid userId) => string.Format(USER_QUOTA_USAGE, userId);
    }

    public static class CacheExpiration
    {
        // Fast changing data
        public static readonly TimeSpan VeryShort = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        
        // Medium changing data
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan MediumLong = TimeSpan.FromMinutes(30);
        
        // Slow changing data
        public static readonly TimeSpan Long = TimeSpan.FromHours(1);
        public static readonly TimeSpan VeryLong = TimeSpan.FromHours(6);
        
        // Static data
        public static readonly TimeSpan Static = TimeSpan.FromHours(24);
    }
}
