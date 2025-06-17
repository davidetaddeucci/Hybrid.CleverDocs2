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
        // JWT Authentication: Clear any existing session data
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

                // JWT Authentication: Store token and user info for client-side access
                ViewBag.AccessToken = result.AccessToken;
                ViewBag.RefreshToken = result.RefreshToken;
                ViewBag.UserInfo = result.User;

                // Store in HttpContext.Items for immediate server-side access
                HttpContext.Items["AccessToken"] = result.AccessToken;
                HttpContext.Items["RefreshToken"] = result.RefreshToken;
                HttpContext.Items["UserInfo"] = result.User;

                // Add redirect tracking to prevent loops
                var redirectCount = HttpContext.Session.GetInt32("RedirectCount") ?? 0;
                if (redirectCount > 3)
                {
                    _logger.LogWarning("Redirect loop detected for user {Email}, redirecting to fallback", model.Email);
                    HttpContext.Session.Remove("RedirectCount");
                    return RedirectToAction("Index", "Dashboard"); // Fallback dashboard
                }

                HttpContext.Session.SetInt32("RedirectCount", redirectCount + 1);

                // JWT Authentication: Return special view that saves token and redirects
                // Don't use RoleRedirect anymore - JavaScript will handle role-based redirect
                ViewBag.ReturnUrl = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                    ? model.ReturnUrl
                    : "";

                return View("LoginSuccess", model);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _authService.LogoutAsync();
            // JWT Authentication: Clear session data
            HttpContext.Session.Clear();
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
    // JWT Authentication: Authorization handled client-side
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    // JWT Authentication: Authorization handled client-side
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