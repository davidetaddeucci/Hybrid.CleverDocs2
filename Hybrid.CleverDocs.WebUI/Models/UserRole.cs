namespace Hybrid.CleverDocs.WebUI.Models;

public enum UserRole
{
    Admin = 1,
    Company = 2,
    User = 3
}

public static class UserRoleExtensions
{
    public static string ToDisplayString(this UserRole role) => role switch
    {
        UserRole.Admin => "Amministratore",
        UserRole.Company => "Azienda",
        UserRole.User => "Utente",
        _ => role.ToString()
    };

    public static string ToCssClass(this UserRole role) => role switch
    {
        UserRole.Admin => "bg-red-100 text-red-800",
        UserRole.Company => "bg-blue-100 text-blue-800",
        UserRole.User => "bg-green-100 text-green-800",
        _ => "bg-gray-100 text-gray-800"
    };
}