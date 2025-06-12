using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Auth
{
    public class UserRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("profile_picture")]
        public string? ProfilePicture { get; set; }
    }

    public class UserUpdateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("profile_picture")]
        public string? ProfilePicture { get; set; }
    }

    public class PasswordResetRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class PasswordResetConfirmRequest
    {
        [JsonPropertyName("reset_token")]
        public string ResetToken { get; set; } = string.Empty;

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [JsonPropertyName("current_password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class EmailVerificationRequest
    {
        [JsonPropertyName("verification_code")]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class ResendVerificationRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
