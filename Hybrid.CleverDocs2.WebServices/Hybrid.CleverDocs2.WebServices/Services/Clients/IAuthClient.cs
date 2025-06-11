using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IAuthClient
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(LogoutRequest request);
    }
}