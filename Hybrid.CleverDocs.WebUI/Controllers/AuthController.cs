using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    public IActionResult Login(string? returnUrl = null)
    {
        // If user is already authenticated, redirect to dashboard
        if (_authService.IsAuthenticated)
        {
            return RedirectToAction("Index", "Dashboard");
        }

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
                
                // Redirect to return URL or dashboard
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                
                return RedirectToAction("Index", "Dashboard");
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
        if (_authService.IsAuthenticated)
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
}