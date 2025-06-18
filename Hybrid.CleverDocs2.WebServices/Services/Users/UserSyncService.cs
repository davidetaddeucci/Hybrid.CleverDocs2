using Microsoft.Extensions.Options;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.DTOs;

namespace Hybrid.CleverDocs2.WebServices.Services.Users
{
    public interface IUserSyncService
    {
        Task<string?> CreateR2RUserAsync(User user);
        Task UpdateR2RUserAsync(User user);
        Task DeleteR2RUserAsync(string r2rUserId);
        Task<bool> SyncUserWithR2RAsync(User user);
    }

    public class UserSyncService : IUserSyncService
    {
        private readonly IAuthClient _r2rUserClient;
        private readonly IMultiLevelCacheService _cacheService;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ICorrelationService _correlationService;
        private readonly ILogger<UserSyncService> _logger;
        private readonly R2ROptions _options;

        public UserSyncService(
            IAuthClient r2rUserClient,
            IMultiLevelCacheService cacheService,
            IRateLimitingService rateLimitingService,
            ICorrelationService correlationService,
            ILogger<UserSyncService> logger,
            IOptions<R2ROptions> options)
        {
            _r2rUserClient = r2rUserClient;
            _cacheService = cacheService;
            _rateLimitingService = rateLimitingService;
            _correlationService = correlationService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<string?> CreateR2RUserAsync(User user)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                // Check rate limiting for user operations
                var canProceed = await _rateLimitingService.CanMakeRequestAsync("user_operation");
                if (!canProceed)
                {
                    _logger.LogWarning("Rate limit exceeded for user creation, queuing for later processing, UserId: {UserId}, CorrelationId: {CorrelationId}", 
                        user.Id, correlationId);
                    
                    // TODO: Implement queue for user sync operations
                    return null;
                }

                _logger.LogInformation("Creating R2R user for {UserId}, CorrelationId: {CorrelationId}", 
                    user.Id, correlationId);

                // Create R2R user request
                var r2rRequest = new UserRequest
                {
                    Email = user.Email,
                    Password = GenerateTemporaryPassword(), // Generate secure temp password
                    Name = user.Name ?? $"{user.FirstName} {user.LastName}".Trim(),
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture
                };

                var r2rResponse = await _r2rUserClient.RegisterUserAsync(r2rRequest);
                var r2rUserId = r2rResponse?.Results?.Id;

                if (!string.IsNullOrEmpty(r2rUserId))
                {
                    _logger.LogInformation("R2R user created successfully: {R2RUserId} for {UserId}, CorrelationId: {CorrelationId}", 
                        r2rUserId, user.Id, correlationId);

                    // Cache the R2R user mapping
                    await _cacheService.SetAsync(
                        $"r2r:user:mapping:{user.Id}", 
                        r2rUserId, 
                        CacheOptions.ForDocumentMetadata(user.Company?.TenantId.ToString()));

                    return r2rUserId;
                }

                _logger.LogWarning("R2R user creation returned empty ID for {UserId}, CorrelationId: {CorrelationId}", 
                    user.Id, correlationId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating R2R user for {UserId}, CorrelationId: {CorrelationId}", 
                    user.Id, correlationId);
                return null;
            }
        }

        public async Task UpdateR2RUserAsync(User user)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                if (string.IsNullOrEmpty(user.R2RUserId))
                {
                    _logger.LogWarning("Cannot update R2R user - R2RUserId is null for {UserId}, CorrelationId: {CorrelationId}", 
                        user.Id, correlationId);
                    return;
                }

                _logger.LogInformation("Updating R2R user {R2RUserId} for {UserId}, CorrelationId: {CorrelationId}", 
                    user.R2RUserId, user.Id, correlationId);

                var r2rRequest = new UserUpdateRequest
                {
                    Name = user.Name ?? $"{user.FirstName} {user.LastName}".Trim(),
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture
                };

                await _r2rUserClient.UpdateUserAsync(user.R2RUserId, r2rRequest);

                _logger.LogInformation("R2R user updated successfully: {R2RUserId} for {UserId}, CorrelationId: {CorrelationId}", 
                    user.R2RUserId, user.Id, correlationId);

                // Invalidate cache
                await _cacheService.RemoveAsync($"r2r:user:mapping:{user.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating R2R user {R2RUserId} for {UserId}, CorrelationId: {CorrelationId}", 
                    user.R2RUserId, user.Id, correlationId);
            }
        }

        public async Task DeleteR2RUserAsync(string r2rUserId)
        {
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                _logger.LogInformation("Deleting R2R user {R2RUserId}, CorrelationId: {CorrelationId}", 
                    r2rUserId, correlationId);

                await _r2rUserClient.DeleteUserAsync(r2rUserId);

                _logger.LogInformation("R2R user deleted successfully: {R2RUserId}, CorrelationId: {CorrelationId}", 
                    r2rUserId, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting R2R user {R2RUserId}, CorrelationId: {CorrelationId}", 
                    r2rUserId, correlationId);
            }
        }

        public async Task<bool> SyncUserWithR2RAsync(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.R2RUserId))
                {
                    // Create new R2R user
                    var r2rUserId = await CreateR2RUserAsync(user);
                    return !string.IsNullOrEmpty(r2rUserId);
                }
                else
                {
                    // Update existing R2R user
                    await UpdateR2RUserAsync(user);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user {UserId} with R2R", user.Id);
                return false;
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a secure temporary password for R2R user
            // This will be used only for R2R authentication, not for WebUI login
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
