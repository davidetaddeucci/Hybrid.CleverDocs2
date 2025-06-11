using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IAuthClient
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(LogoutRequest request);

        // User management endpoints
        Task<UserResponse> CreateUserAsync(UserRequest request);
        Task<UserResponse> GetUserAsync(string id);
        Task<IEnumerable<UserResponse>> ListUsersAsync();
        Task<UserResponse> UpdateUserAsync(string id, UserRequest request);
        Task DeleteUserAsync(string id);
    }
}
