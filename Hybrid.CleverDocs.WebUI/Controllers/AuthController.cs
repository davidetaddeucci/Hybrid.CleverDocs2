using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels;
using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Always clear authentication for fresh login
        await HttpContext.SignOutAsync("Cookies");
        HttpContext.Session.Clear();

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var loginRequest = new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginRequest);

            if (result.Success)
            {
                _logger.LogInformation("User {Email} logged in successfully", model.Email);

                // Map role string to numeric value for consistent authorization
                var roleValue = MapRoleToNumericString(result.User?.Role ?? "User");

                // Create claims for ASP.NET Core authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.Name, result.User?.FullName ?? model.Email),
                    new Claim(ClaimTypes.NameIdentifier, result.User?.Id.ToString() ?? Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, roleValue),
                    new Claim("RoleName", result.User?.Role ?? "User") // Keep original role name for display
                };

                if (result.User?.CompanyId != null)
                {
                    claims.Add(new Claim("CompanyId", result.User.CompanyId.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in the user
                await HttpContext.SignInAsync("Cookies", claimsPrincipal);

                // Add redirect tracking to prevent loops
                var redirectCount = HttpContext.Session.GetInt32("RedirectCount") ?? 0;
                if (redirectCount > 3)
                {
                    _logger.LogWarning("Redirect loop detected for user {Email}, redirecting to fallback", model.Email);
                    HttpContext.Session.Remove("RedirectCount");
                    return RedirectToAction("Index", "Dashboard"); // Fallback dashboard
                }

                HttpContext.Session.SetInt32("RedirectCount", redirectCount + 1);

                // Redirect to return URL or role-based dashboard
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    HttpContext.Session.Remove("RedirectCount");
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "RoleRedirect");
            }
            else
            {
                model.ErrorMessage = result.Message ?? "Login failed. Please check your credentials.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            model.ErrorMessage = "An error occurred during login. Please try again.";
            ModelState.AddModelError(string.Empty, model.ErrorMessage);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        // If user is already authenticated, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var registerRequest = new RegisterRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                CompanyName = model.CompanyName
            };

            var result = await _authService.RegisterAsync(registerRequest);

            if (result.Success)
            {
                _logger.LogInformation("User {Email} registered successfully", model.Email);
                TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
                return RedirectToAction("Login");
            }
            else
            {
                model.ErrorMessage = result.Message ?? "Registration failed. Please try again.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", model.Email);
            model.ErrorMessage = "An error occurred during registration. Please try again.";
            ModelState.AddModelError(string.Empty, model.ErrorMessage);
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _authService.LogoutAsync();
            await HttpContext.SignOutAsync("Cookies");
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }

        return RedirectToAction("Login");
    }



    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new ForgotPasswordRequest { Email = model.Email };
            var result = await _authService.ForgotPasswordAsync(request);

            model.Message = result.Message ?? "If the email exists, a password reset link has been sent.";
            model.IsSuccess = result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", model.Email);
            model.Message = "An error occurred. Please try again.";
            model.IsSuccess = false;
        }

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            };

            var result = await _authService.ChangePasswordAsync(request);

            model.Message = result.Message ?? (result.Success ? "Password changed successfully!" : "Failed to change password.");
            model.IsSuccess = result.Success;

            if (result.Success)
            {
                ModelState.Clear();
                model = new ChangePasswordViewModel
                {
                    Message = model.Message,
                    IsSuccess = model.IsSuccess
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            model.Message = "An error occurred. Please try again.";
            model.IsSuccess = false;
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Maps role names to numeric strings for consistent authorization
    /// Backend enum: Admin=1, Company=2, User=3
    /// </summary>
    private static string MapRoleToNumericString(string roleName)
    {
        return roleName.ToLowerInvariant() switch
        {
            "admin" => "1",
            "company" => "2",
            "user" => "3",
            _ => "3" // Default to User role
        };
    }
}