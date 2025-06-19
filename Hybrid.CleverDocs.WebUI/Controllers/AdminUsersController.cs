using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels.Users;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;
using Hybrid.CleverDocs.WebUI.ViewModels.Companies;
using Hybrid.CleverDocs.WebUI.Extensions;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            IApiService apiService,
            ILogger<AdminUsersController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Admin users index page with search and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(UserSearchViewModel search)
        {
            _logger.LogInformation("AdminUsersController.Index called with search: {@Search}", search);
            
            try
            {
                // Set defaults
                search.Page = search.Page <= 0 ? 1 : search.Page;
                search.PageSize = search.PageSize <= 0 ? 10 : search.PageSize;

                // Build query parameters
                var queryParams = new Dictionary<string, string>
                {
                    ["page"] = search.Page.ToString(),
                    ["pageSize"] = search.PageSize.ToString()
                };

                if (!string.IsNullOrEmpty(search.SearchTerm))
                    queryParams["search"] = search.SearchTerm;

                if (search.IsActive.HasValue)
                    queryParams["isActive"] = search.IsActive.Value.ToString();

                if (search.CompanyId.HasValue)
                    queryParams["companyId"] = search.CompanyId.Value.ToString();

                // Call API
                var response = await _apiService.GetAsync<ApiResponse<List<UserDto>>>(
                    "api/users", queryParams);

                // Get companies for filter dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> { ["pageSize"] = "1000" });

                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new UserListViewModel
                    {
                        Users = response.Data,
                        Search = search,
                        Pagination = new PaginationViewModel
                        {
                            CurrentPage = response.Page,
                            TotalItems = (int)response.TotalItems,
                            PageSize = response.PageSize
                        },
                        HasActiveFilters = !string.IsNullOrEmpty(search.SearchTerm) || 
                                         search.IsActive.HasValue || 
                                         search.CompanyId.HasValue,
                        Companies = companiesResponse?.Data ?? new List<CompanyDto>()
                    };

                    return View(viewModel);
                }
                else
                {
                    _logger.LogWarning("API call failed: {Message}", response?.Message);
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to load users";
                    return View(new UserListViewModel());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin users index page");
                TempData["ErrorMessage"] = "Failed to load users. Please try again.";
                return View(new UserListViewModel());
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
                var response = await _apiService.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                
                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new UserDetailsViewModel
                    {
                        User = response.Data,
                        CanEdit = true, // Admin can always edit
                        CanDelete = response.Data.IsActive // Can only deactivate active users
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
                // Get companies for dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });

                var viewModel = new CreateUserViewModel
                {
                    Companies = companiesResponse?.Data ?? new List<CompanyDto>()
                };

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
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload companies for dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });
                model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                
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
                    Role = model.Role,
                    CompanyId = model.CompanyId
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
                    
                    // Reload companies for dropdown
                    var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                        "api/companies", new Dictionary<string, string> 
                        { 
                            ["pageSize"] = "1000",
                            ["isActive"] = "true"
                        });
                    model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                    
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "Failed to create user. Please try again.");
                
                // Reload companies for dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });
                model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                
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
                var userResponse = await _apiService.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });
                
                if (userResponse?.Success == true && userResponse.Data != null)
                {
                    var viewModel = new EditUserViewModel
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
                        IsActive = userResponse.Data.IsActive,
                        R2RUserId = userResponse.Data.R2RUserId,
                        Companies = companiesResponse?.Data ?? new List<CompanyDto>()
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
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                // Reload companies for dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });
                model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                
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
                    
                    // Reload companies for dropdown
                    var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                        "api/companies", new Dictionary<string, string> 
                        { 
                            ["pageSize"] = "1000",
                            ["isActive"] = "true"
                        });
                    model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                    
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                ModelState.AddModelError("", "Failed to update user. Please try again.");
                
                // Reload companies for dropdown
                var companiesResponse = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", new Dictionary<string, string> 
                    { 
                        ["pageSize"] = "1000",
                        ["isActive"] = "true"
                    });
                model.Companies = companiesResponse?.Data ?? new List<CompanyDto>();
                
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
