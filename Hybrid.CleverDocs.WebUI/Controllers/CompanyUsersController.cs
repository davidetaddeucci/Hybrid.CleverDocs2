using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels.Users;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;
using Hybrid.CleverDocs.WebUI.ViewModels.Companies;
using Hybrid.CleverDocs.WebUI.Extensions;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyUsersController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<CompanyUsersController> _logger;

        public CompanyUsersController(
            IApiService apiService,
            ILogger<CompanyUsersController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Company users index page with search and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(UserSearchViewModel search)
        {
            _logger.LogInformation("CompanyUsersController.Index called with search: {@Search}", search);
            
            try
            {
                // Get current user's company ID
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(userCompanyId) || !Guid.TryParse(userCompanyId, out var companyId))
                {
                    TempData["ErrorMessage"] = "Unable to determine your company. Please contact administrator.";
                    return RedirectToAction("Index", "Home");
                }

                // Set defaults and force company filter
                search.Page = search.Page <= 0 ? 1 : search.Page;
                search.PageSize = search.PageSize <= 0 ? 10 : search.PageSize;
                search.CompanyId = companyId; // Force company filter

                // Build query parameters
                var queryParams = new Dictionary<string, string>
                {
                    ["page"] = search.Page.ToString(),
                    ["pageSize"] = search.PageSize.ToString(),
                    ["companyId"] = companyId.ToString() // Always filter by company
                };

                if (!string.IsNullOrEmpty(search.SearchTerm))
                    queryParams["search"] = search.SearchTerm;

                if (search.IsActive.HasValue)
                    queryParams["isActive"] = search.IsActive.Value.ToString();

                // Call API
                var response = await _apiService.GetAsync<ApiResponse<List<UserDto>>>(
                    "api/users", queryParams);

                // Get company info
                var companyResponse = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{companyId}");

                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new CompanyUserListViewModel
                    {
                        Users = response.Data,
                        Search = search,
                        Pagination = new PaginationViewModel
                        {
                            CurrentPage = response.Page,
                            TotalItems = (int)response.TotalItems,
                            PageSize = response.PageSize
                        },
                        HasActiveFilters = !string.IsNullOrEmpty(search.SearchTerm) || search.IsActive.HasValue,
                        Company = companyResponse?.Data ?? new CompanyDto { Id = companyId, Name = "Unknown Company" }
                    };

                    ViewBag.PageTitle = $"Users - {viewModel.Company.Name}";
                    return View(viewModel);
                }
                else
                {
                    _logger.LogWarning("API call failed: {Message}", response?.Message);
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to load users";
                    return View(new CompanyUserListViewModel());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company users index page");
                TempData["ErrorMessage"] = "Failed to load users. Please try again.";
                return View(new CompanyUserListViewModel());
            }
        }

        /// <summary>
        /// User details page
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(userCompanyId))
                {
                    return Forbid("Unable to determine your company");
                }

                var response = await _apiService.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                
                if (response?.Success == true && response.Data != null)
                {
                    // Verify user belongs to same company
                    if (response.Data.CompanyId.ToString() != userCompanyId)
                    {
                        return Forbid("Access denied to this user");
                    }

                    var viewModel = new UserDetailsViewModel
                    {
                        User = response.Data,
                        CanEdit = true, // Company managers can edit their users
                        CanDelete = response.Data.IsActive && response.Data.Role != "Company" // Cannot deactivate other company managers
                    };

                    ViewBag.PageTitle = $"{response.Data.FirstName} {response.Data.LastName}";
                    return View(viewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "User not found";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Failed to load user details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create user page
        /// </summary>
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(userCompanyId) || !Guid.TryParse(userCompanyId, out var companyId))
                {
                    TempData["ErrorMessage"] = "Unable to determine your company";
                    return RedirectToAction("Index");
                }

                // Get company info
                var companyResponse = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{companyId}");

                var viewModel = new CreateCompanyUserViewModel
                {
                    CompanyId = companyId,
                    CompanyName = companyResponse?.Data?.Name ?? "Unknown Company"
                };

                ViewBag.PageTitle = $"Create User - {viewModel.CompanyName}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user page");
                TempData["ErrorMessage"] = "Failed to load create user page. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create user POST
        /// </summary>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCompanyUserViewModel model)
        {
            var userCompanyId = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(userCompanyId) || !Guid.TryParse(userCompanyId, out var companyId))
            {
                return Forbid("Unable to determine your company");
            }

            // Ensure user is creating for their own company
            model.CompanyId = companyId;

            if (!ModelState.IsValid)
            {
                // Reload company info
                var companyResponse = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{companyId}");
                model.CompanyName = companyResponse?.Data?.Name ?? "Unknown Company";
                return View(model);
            }

            try
            {
                var createDto = new CreateUserDto
                {
                    Email = model.Email,
                    Password = model.Password,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Name = model.Name,
                    Bio = model.Bio,
                    ProfilePicture = model.ProfilePicture,
                    Role = model.Role, // Company managers can only create User or Company roles
                    CompanyId = companyId
                };

                var response = await _apiService.PostAsync<ApiResponse<UserDto>>(
                    "api/users", createDto);

                if (response?.Success == true && response.Data != null)
                {
                    TempData["SuccessMessage"] = $"User '{response.Data.Email}' created successfully. R2R user sync in progress.";
                    return RedirectToAction("Details", new { id = response.Data.Id });
                }
                else
                {
                    ModelState.AddModelError("", response?.Message ?? "Failed to create user");
                    
                    // Reload company info
                    var companyResponse = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{companyId}");
                    model.CompanyName = companyResponse?.Data?.Name ?? "Unknown Company";
                    
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "Failed to create user. Please try again.");
                
                // Reload company info
                var companyResponse = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{companyId}");
                model.CompanyName = companyResponse?.Data?.Name ?? "Unknown Company";
                
                return View(model);
            }
        }

        /// <summary>
        /// Edit user page
        /// </summary>
        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(userCompanyId) || !Guid.TryParse(userCompanyId, out var companyId))
                {
                    return Forbid("Unable to determine your company");
                }

                var userResponse = await _apiService.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                
                if (userResponse?.Success == true && userResponse.Data != null)
                {
                    // Verify user belongs to same company
                    if (userResponse.Data.CompanyId != companyId)
                    {
                        return Forbid("Access denied to this user");
                    }

                    var viewModel = new EditCompanyUserViewModel
                    {
                        Id = userResponse.Data.Id,
                        Email = userResponse.Data.Email,
                        FirstName = userResponse.Data.FirstName,
                        LastName = userResponse.Data.LastName,
                        Name = userResponse.Data.Name,
                        Bio = userResponse.Data.Bio,
                        ProfilePicture = userResponse.Data.ProfilePicture,
                        Role = userResponse.Data.Role,
                        CompanyId = userResponse.Data.CompanyId,
                        CompanyName = userResponse.Data.CompanyName ?? "Unknown Company",
                        IsActive = userResponse.Data.IsActive,
                        R2RUserId = userResponse.Data.R2RUserId
                    };

                    ViewBag.PageTitle = $"Edit {userResponse.Data.FirstName} {userResponse.Data.LastName}";
                    return View(viewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = userResponse?.Message ?? "User not found";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for edit: {UserId}", id);
                TempData["ErrorMessage"] = "Failed to load user. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Edit user POST
        /// </summary>
        [HttpPost("{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditCompanyUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userCompanyId = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(userCompanyId) || !Guid.TryParse(userCompanyId, out var companyId))
            {
                return Forbid("Unable to determine your company");
            }

            // Verify user belongs to same company
            if (model.CompanyId != companyId)
            {
                return Forbid("Access denied to this user");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var updateDto = new UpdateUserDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Name = model.Name,
                    Bio = model.Bio,
                    ProfilePicture = model.ProfilePicture,
                    Role = model.Role,
                    IsActive = model.IsActive
                };

                var response = await _apiService.PutAsync<ApiResponse<UserDto>>(
                    $"api/users/{id}", updateDto);

                if (response?.Success == true)
                {
                    TempData["SuccessMessage"] = "User updated successfully. R2R user sync in progress.";
                    return RedirectToAction("Details", new { id });
                }
                else
                {
                    ModelState.AddModelError("", response?.Message ?? "Failed to update user");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                ModelState.AddModelError("", "Failed to update user. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                if (string.IsNullOrEmpty(userCompanyId))
                {
                    return Forbid("Unable to determine your company");
                }

                // Verify user belongs to same company before deactivating
                var userResponse = await _apiService.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                if (userResponse?.Success == true && userResponse.Data?.CompanyId.ToString() != userCompanyId)
                {
                    return Forbid("Access denied to this user");
                }

                var response = await _apiService.DeleteAsync<ApiResponse<object>>($"api/users/{id}");

                if (response?.Success == true)
                {
                    TempData["SuccessMessage"] = "User deactivated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to deactivate user";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                TempData["ErrorMessage"] = "Failed to deactivate user. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}
