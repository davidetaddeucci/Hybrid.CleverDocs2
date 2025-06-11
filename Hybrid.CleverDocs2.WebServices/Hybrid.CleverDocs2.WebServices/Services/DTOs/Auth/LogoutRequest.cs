namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth
{
    public class LogoutRequest
    {
        // Refresh token to invalidate
        public string RefreshToken { get; set; } = string.Empty;
    }
}