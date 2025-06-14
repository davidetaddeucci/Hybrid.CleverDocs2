using Hybrid.CleverDocs.WebUI.ViewModels;

namespace Hybrid.CleverDocs.WebUI.Services
{
    public interface IDashboardService
    {
        /// <summary>
        /// Get optimized admin dashboard data with caching
        /// </summary>
        Task<AdminDashboardViewModel> GetAdminDashboardAsync();

        /// <summary>
        /// Get optimized company dashboard data with caching
        /// </summary>
        Task<CompanyDashboardViewModel> GetCompanyDashboardAsync(Guid companyId);

        /// <summary>
        /// Get optimized user dashboard data with caching
        /// </summary>
        Task<UserDashboardViewModel> GetUserDashboardAsync(Guid userId, Guid companyId);

        /// <summary>
        /// Invalidate dashboard cache for specific user/company
        /// </summary>
        Task InvalidateDashboardCacheAsync(Guid? userId = null, Guid? companyId = null);

        /// <summary>
        /// Warm up dashboard cache for better performance
        /// </summary>
        Task WarmUpCacheAsync(Guid? userId = null, Guid? companyId = null);


    }

    public class DashboardService : IDashboardService
    {
        private readonly ICacheService _cacheService;
        private readonly IApiService _apiService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ICacheService cacheService,
            IApiService apiService,
            ILogger<DashboardService> logger)
        {
            _cacheService = cacheService;
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.ADMIN_DASHBOARD,
                async () =>
                {
                    _logger.LogInformation("Loading admin dashboard data from API");

                    // Load data in parallel for better performance
                    var companiesTask = _apiService.GetAsync<int>("admin/companies/count");
                    var usersTask = _apiService.GetAsync<int>("admin/users/count");
                    var documentsTask = _apiService.GetAsync<int>("admin/documents/count");
                    var companyStatsTask = _apiService.GetAsync<List<CompanyStatsDto>>("admin/companies/stats");
                    var activitiesTask = _apiService.GetAsync<List<RecentActivityDto>>("admin/activities/recent");

                    // Wait for all tasks to complete
                    var companies = await companiesTask;
                    var users = await usersTask;
                    var documents = await documentsTask;
                    var companyStats = await companyStatsTask;
                    var activities = await activitiesTask;

                    return new AdminDashboardViewModel
                    {
                        TotalCompanies = companies,
                        TotalUsers = users,
                        TotalDocuments = documents,
                        CompanyStats = companyStats ?? new List<CompanyStatsDto>(),
                        RecentActivities = activities ?? new List<RecentActivityDto>()
                    };
                },
                CacheExpiration.Short
            );
        }

        public async Task<CompanyDashboardViewModel> GetCompanyDashboardAsync(Guid companyId)
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.CompanyDashboard(companyId),
                async () =>
                {
                    _logger.LogInformation("Loading company dashboard data for company {CompanyId}", companyId);

                    // Load data in parallel
                    var usersTask = _apiService.GetAsync<int>($"company/{companyId}/users/count");
                    var documentsTask = _apiService.GetAsync<int>($"company/{companyId}/documents/count");
                    var collectionsTask = _apiService.GetAsync<int>($"company/{companyId}/collections/count");
                    var userStatsTask = _apiService.GetAsync<List<UserStatsDto>>($"company/{companyId}/users/stats");
                    var documentStatsTask = _apiService.GetAsync<List<DocumentStatsDto>>($"company/{companyId}/documents/stats");
                    var activitiesTask = _apiService.GetAsync<List<RecentActivityDto>>($"company/{companyId}/activities/recent");

                    // Wait for all tasks to complete
                    var users = await usersTask;
                    var documents = await documentsTask;
                    var collections = await collectionsTask;
                    var userStats = await userStatsTask;
                    var documentStats = await documentStatsTask;
                    var activities = await activitiesTask;

                    return new CompanyDashboardViewModel
                    {
                        TotalUsers = users,
                        TotalDocuments = documents,
                        TotalCollections = collections,
                        UserStats = userStats ?? new List<UserStatsDto>(),
                        DocumentStats = documentStats ?? new List<DocumentStatsDto>(),
                        RecentActivities = activities ?? new List<RecentActivityDto>()
                    };
                },
                CacheExpiration.Short
            );
        }

        public async Task<UserDashboardViewModel> GetUserDashboardAsync(Guid userId, Guid companyId)
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.UserDashboard(userId),
                async () =>
                {
                    _logger.LogInformation("Loading user dashboard data for user {UserId}", userId);

                    // Load data in parallel
                    var documentsTask = _apiService.GetAsync<int>($"user/{userId}/documents/count");
                    var collectionsTask = _apiService.GetAsync<int>($"user/{userId}/collections/count");
                    var conversationsTask = _apiService.GetAsync<int>($"user/{userId}/conversations/count");
                    var recentDocsTask = _apiService.GetAsync<List<RecentDocumentDto>>($"user/{userId}/documents/recent");
                    var recentConversationsTask = _apiService.GetAsync<List<RecentConversationDto>>($"user/{userId}/conversations/recent");
                    var quotaTask = _apiService.GetAsync<UserQuotaUsageDto>($"user/{userId}/quota/usage");

                    // Wait for all tasks to complete
                    var documents = await documentsTask;
                    var collections = await collectionsTask;
                    var conversations = await conversationsTask;
                    var recentDocs = await recentDocsTask;
                    var recentConversations = await recentConversationsTask;
                    var quota = await quotaTask;

                    return new UserDashboardViewModel
                    {
                        DocumentCount = documents,
                        CollectionCount = collections,
                        ConversationCount = conversations,
                        RecentDocuments = recentDocs ?? new List<RecentDocumentDto>(),
                        RecentConversations = recentConversations ?? new List<RecentConversationDto>(),
                        QuotaUsage = quota ?? new UserQuotaUsageDto()
                    };
                },
                CacheExpiration.VeryShort
            );
        }

        public async Task InvalidateDashboardCacheAsync(Guid? userId = null, Guid? companyId = null)
        {
            var tasks = new List<Task>();

            if (userId.HasValue)
            {
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserDashboard(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserDocumentsCount(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserCollectionsCount(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserConversationsCount(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserRecentDocuments(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserRecentConversations(userId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.UserQuotaUsage(userId.Value)));
            }

            if (companyId.HasValue)
            {
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyDashboard(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyUsersCount(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyDocumentsCount(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyCollectionsCount(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyUserStats(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyDocumentStats(companyId.Value)));
                tasks.Add(_cacheService.RemoveAsync(CacheKeys.CompanyRecentActivities(companyId.Value)));
            }

            // Admin cache
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_DASHBOARD));
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_COMPANIES_COUNT));
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_USERS_COUNT));
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_DOCUMENTS_COUNT));
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_COMPANY_STATS));
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.ADMIN_RECENT_ACTIVITIES));

            await Task.WhenAll(tasks);
            _logger.LogInformation("Dashboard cache invalidated for User: {UserId}, Company: {CompanyId}", userId, companyId);
        }

        public async Task WarmUpCacheAsync(Guid? userId = null, Guid? companyId = null)
        {
            var tasks = new List<Task>();

            // Warm up admin cache
            tasks.Add(GetAdminDashboardAsync());

            if (companyId.HasValue)
            {
                tasks.Add(GetCompanyDashboardAsync(companyId.Value));
            }

            if (userId.HasValue && companyId.HasValue)
            {
                tasks.Add(GetUserDashboardAsync(userId.Value, companyId.Value));
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("Dashboard cache warmed up for User: {UserId}, Company: {CompanyId}", userId, companyId);
        }


    }


}
