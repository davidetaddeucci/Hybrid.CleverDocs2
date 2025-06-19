using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.ViewModels.Companies;
using Hybrid.CleverDocs.WebUI.ViewModels.Common;
using Hybrid.CleverDocs.WebUI.Extensions;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CompaniesController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(
            IApiService apiService,
            ILogger<CompaniesController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Companies index page with search and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(CompanySearchViewModel search)
        {
            _logger.LogInformation("CompaniesController.Index called with search: {@Search}", search);
            
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

                // Call API
                var response = await _apiService.GetAsync<ApiResponse<List<CompanyDto>>>(
                    "api/companies", queryParams);

                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new CompanyListViewModel
                    {
                        Companies = response.Data,
                        Search = search,
                        Pagination = new PaginationViewModel
                        {
                            CurrentPage = response.Page,
                            TotalItems = (int)response.TotalItems,
                            PageSize = response.PageSize
                        },
                        HasActiveFilters = !string.IsNullOrEmpty(search.SearchTerm) || search.IsActive.HasValue
                    };

                    return View(viewModel);
                }
                else
                {
                    _logger.LogWarning("API call failed: {Message}", response?.Message);
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to load companies";
                    return View(new CompanyListViewModel());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies index page");
                TempData["ErrorMessage"] = "Failed to load companies. Please try again.";
                return View(new CompanyListViewModel());
            }
        }

        /// <summary>
        /// Company details page
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{id}");
                
                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new CompanyDetailsViewModel
                    {
                        Company = response.Data,
                        CanEdit = true, // Admin can always edit
                        CanDelete = response.Data.IsActive // Can only deactivate active companies
                    };

                    ViewBag.PageTitle = response.Data.Name;
                    return View(viewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Company not found";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company details for ID: {CompanyId}", id);
                TempData["ErrorMessage"] = "Failed to load company details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create company page
        /// </summary>
        [HttpGet("create")]
        public IActionResult Create()
        {
            var viewModel = new CreateCompanyViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// Create company POST
        /// </summary>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCompanyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var createDto = new CreateCompanyDto
                {
                    Name = model.Name,
                    Description = model.Description,
                    Website = model.Website,
                    ContactEmail = model.ContactEmail,
                    ContactPhone = model.ContactPhone,
                    Address = model.Address,
                    MaxUsers = model.MaxUsers,
                    MaxDocuments = model.MaxDocuments,
                    MaxStorageBytes = model.MaxStorageBytes,
                    MaxCollections = model.MaxCollections,
                    R2RApiKey = model.R2RApiKey,
                    R2RConfiguration = model.R2RConfiguration
                };

                var response = await _apiService.PostAsync<ApiResponse<CompanyDto>>(
                    "api/companies", createDto);

                if (response?.Success == true && response.Data != null)
                {
                    TempData["SuccessMessage"] = $"Company '{response.Data.Name}' created successfully. R2R tenant sync in progress.";
                    return RedirectToAction("Details", new { id = response.Data.Id });
                }
                else
                {
                    ModelState.AddModelError("", response?.Message ?? "Failed to create company");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                ModelState.AddModelError("", "Failed to create company. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Edit company page
        /// </summary>
        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{id}");
                
                if (response?.Success == true && response.Data != null)
                {
                    var viewModel = new EditCompanyViewModel
                    {
                        Id = response.Data.Id,
                        Name = response.Data.Name,
                        Description = response.Data.Description,
                        Website = response.Data.Website,
                        ContactEmail = response.Data.ContactEmail,
                        ContactPhone = response.Data.ContactPhone,
                        Address = response.Data.Address,
                        IsActive = response.Data.IsActive,
                        MaxUsers = response.Data.MaxUsers,
                        MaxDocuments = response.Data.MaxDocuments,
                        MaxStorageBytes = response.Data.MaxStorageBytes,
                        MaxCollections = response.Data.MaxCollections,
                        R2RApiKey = response.Data.R2RApiKey,
                        R2RConfiguration = response.Data.R2RConfiguration,
                        R2RTenantId = response.Data.R2RTenantId
                    };

                    ViewBag.PageTitle = $"Edit {response.Data.Name}";
                    return View(viewModel);
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Company not found";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company for edit: {CompanyId}", id);
                TempData["ErrorMessage"] = "Failed to load company. Please try again.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Edit company POST
        /// </summary>
        [HttpPost("{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditCompanyViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var updateDto = new UpdateCompanyDto
                {
                    Name = model.Name,
                    Description = model.Description,
                    Website = model.Website,
                    ContactEmail = model.ContactEmail,
                    ContactPhone = model.ContactPhone,
                    Address = model.Address,
                    IsActive = model.IsActive,
                    MaxUsers = model.MaxUsers,
                    MaxDocuments = model.MaxDocuments,
                    MaxStorageBytes = model.MaxStorageBytes,
                    MaxCollections = model.MaxCollections,
                    R2RApiKey = model.R2RApiKey,
                    R2RConfiguration = model.R2RConfiguration
                };

                var response = await _apiService.PutAsync<ApiResponse<CompanyDto>>(
                    $"api/companies/{id}", updateDto);

                if (response?.Success == true)
                {
                    TempData["SuccessMessage"] = "Company updated successfully. R2R tenant sync in progress.";
                    return RedirectToAction("Details", new { id });
                }
                else
                {
                    ModelState.AddModelError("", response?.Message ?? "Failed to update company");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", id);
                ModelState.AddModelError("", "Failed to update company. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Deactivate company
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                var response = await _apiService.DeleteAsync<ApiResponse<object>>($"api/companies/{id}");

                if (response?.Success == true)
                {
                    TempData["SuccessMessage"] = "Company deactivated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Failed to deactivate company";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company: {CompanyId}", id);
                TempData["ErrorMessage"] = "Failed to deactivate company. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}
