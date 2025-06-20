using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs.WebUI.Models.Documents;
using Hybrid.CleverDocs.WebUI.Services.Documents;
using Microsoft.AspNetCore.Mvc.Rendering;
using Hybrid.CleverDocs.WebUI.Services;

namespace Hybrid.CleverDocs.WebUI.Controllers;

/// <summary>
/// MVC Controller for document management operations
/// </summary>
// JWT Authentication: Authorization handled client-side with JWT tokens
[Route("documents")]
public class DocumentsController : Controller
{
    private readonly IDocumentApiClient _documentApiClient;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IAuthService _authService;

    public DocumentsController(
        IDocumentApiClient documentApiClient,
        ILogger<DocumentsController> logger,
        IAuthService authService)
    {
        _documentApiClient = documentApiClient;
        _logger = logger;
        _authService = authService;
        _logger.LogInformation("DocumentsController constructor called");
    }

    /// <summary>
    /// Main documents page with search and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(DocumentSearchViewModel search)
    {
        _logger.LogInformation("DocumentsController.Index called");
        try
        {
            // Set defaults if not provided
            search.Page = search.Page <= 0 ? 1 : search.Page;
            search.PageSize = search.PageSize <= 0 ? 10 : search.PageSize; // Default to 10 per page
            search.SortBy = string.IsNullOrEmpty(search.SortBy) ? "updated_at" : search.SortBy;

            // Get documents
            var documentsResult = await _documentApiClient.SearchDocumentsAsync(search);

            // Build view model
            var viewModel = new DocumentListViewModel
            {
                Documents = documentsResult.Items,
                Search = search,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = documentsResult.Page,
                    TotalPages = documentsResult.TotalPages,
                    TotalItems = documentsResult.TotalCount,
                    PageSize = documentsResult.PageSize
                },
                Statistics = await BuildStatisticsViewModel(documentsResult.Items),
                HasActiveFilters = HasActiveFilters(search)
            };

            // Populate filter options
            await PopulateFilterOptions(viewModel.Search);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading documents index page");
            TempData["ErrorMessage"] = "Failed to load documents. Please try again.";
            return View(new DocumentListViewModel());
        }
    }

    /// <summary>
    /// Document upload page
    /// </summary>
    [HttpGet("upload")]
    public IActionResult Upload(Guid? collectionId)
    {
        _logger.LogInformation("DocumentsController.Upload GET called with collectionId: {CollectionId}", collectionId);
        var viewModel = new DocumentUploadViewModel
        {
            CollectionId = collectionId,
            MaxFileSize = 100 * 1024 * 1024, // 100MB
            AllowedFileTypes = new List<string>
            {
                // R2R Supported file types only
                ".pdf", ".txt", ".md", ".docx", ".doc", ".xlsx", ".xls",
                ".pptx", ".ppt", ".html", ".csv", ".rtf", ".epub", ".odt",
                ".rst", ".org", ".tsv", ".eml", ".msg", ".png", ".jpeg",
                ".jpg", ".bmp", ".tiff", ".heic", ".mp3", ".py", ".js",
                ".ts", ".css"
            }
        };

        ViewBag.PageTitle = "Upload Document";
        ViewBag.PageDescription = collectionId.HasValue
            ? "Upload a document to the selected collection"
            : "Upload a document";

        return View(viewModel);
    }

    /// <summary>
    /// Process document upload
    /// </summary>
    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model)
    {
        try
        {
            _logger.LogInformation("DocumentsController.Upload POST called with file: {FileName}, CollectionId: {CollectionId}",
                model.File?.FileName, model.CollectionId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for upload");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {ErrorMessage}", error.ErrorMessage);
                }
                // Repopulate UI properties before returning view
                PopulateUploadViewModelProperties(model);
                return View(model);
            }

            if (model.File == null || model.File.Length == 0)
            {
                _logger.LogWarning("No file provided for upload");
                ModelState.AddModelError("File", "Please select a file to upload.");
                // Repopulate UI properties before returning view
                PopulateUploadViewModelProperties(model);
                return View(model);
            }

            // Validate file size (100MB max)
            if (model.File.Length > 100 * 1024 * 1024)
            {
                ModelState.AddModelError("File", "File size cannot exceed 100MB.");
                // Repopulate UI properties before returning view
                PopulateUploadViewModelProperties(model);
                return View(model);
            }

            // Validate file type - R2R supported extensions only
            var fileExtension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            var allowedExtensions = new[] {
                ".pdf", ".txt", ".md", ".docx", ".doc", ".xlsx", ".xls",
                ".pptx", ".ppt", ".html", ".csv", ".rtf", ".epub", ".odt",
                ".rst", ".org", ".tsv", ".eml", ".msg", ".png", ".jpeg",
                ".jpg", ".bmp", ".tiff", ".heic", ".mp3", ".py", ".js",
                ".ts", ".css"
            };

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("File", "File type not supported by R2R. Please upload a supported file type.");
                // Repopulate UI properties before returning view
                PopulateUploadViewModelProperties(model);
                return View(model);
            }

            // Implement actual upload logic using _documentApiClient
            try
            {
                _logger.LogInformation("Starting document upload process for file: {FileName} ({FileSize} bytes)",
                    model.File.FileName, model.File.Length);

                // Create upload request
                var uploadRequest = new DocumentUploadViewModel
                {
                    Title = model.Title,
                    Description = model.Description,
                    File = model.File,
                    CollectionId = model.CollectionId,
                    TagsInput = model.TagsInput,
                    IsFavorite = model.IsFavorite
                };

                _logger.LogInformation("Calling DocumentApiClient.UploadDocumentAsync...");

                // Upload document to backend
                var uploadedDocument = await _documentApiClient.UploadDocumentAsync(uploadRequest);

                _logger.LogInformation("DocumentApiClient.UploadDocumentAsync completed. Result: {IsNull}",
                    uploadedDocument == null ? "NULL" : "SUCCESS");

                if (uploadedDocument != null)
                {
                    _logger.LogInformation("Upload successful! Document ID: {DocumentId}", uploadedDocument.Id);
                    TempData["SuccessMessage"] = $"Document '{model.File.FileName}' uploaded successfully and added to the database.";

                    if (model.CollectionId.HasValue)
                    {
                        _logger.LogInformation("Redirecting to collection details: {CollectionId}", model.CollectionId.Value);
                        // Use relative URL for consistency with bulk upload
                        return Redirect($"/Collections/{model.CollectionId.Value}");
                    }

                    _logger.LogInformation("Redirecting to document details: {DocumentId}", uploadedDocument.Id);
                    return Redirect($"/Documents/{uploadedDocument.Id}");
                }
                else
                {
                    _logger.LogWarning("Upload failed - DocumentApiClient returned null");
                    ModelState.AddModelError("", "Failed to upload document. Please try again.");
                    // Repopulate UI properties before returning view
                    PopulateUploadViewModelProperties(model);
                    return View(model);
                }
            }
            catch (Exception uploadEx)
            {
                _logger.LogError(uploadEx, "Error uploading document to backend");
                ModelState.AddModelError("", "Failed to upload document to the system. Please try again.");
                // Repopulate UI properties before returning view
                PopulateUploadViewModelProperties(model);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            ModelState.AddModelError("", "An error occurred while uploading the document. Please try again.");
            // Repopulate UI properties before returning view
            PopulateUploadViewModelProperties(model);
            return View(model);
        }
    }

    /// <summary>
    /// Bulk upload endpoint - proxies to WebServices bulk upload API
    /// </summary>
    [HttpPost("BulkUpload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpload()
    {
        try
        {
            _logger.LogInformation("🚀 BULK UPLOAD: Starting bulk upload proxy");

            // Get all files from the request
            var files = Request.Form.Files;
            if (files.Count == 0)
            {
                _logger.LogWarning("❌ BULK UPLOAD: No files received");
                return BadRequest(new { success = false, message = "No files provided" });
            }

            _logger.LogInformation("📊 BULK UPLOAD: Received {FileCount} files", files.Count);

            // Get other form data
            var collectionId = Request.Form["CollectionId"].FirstOrDefault();

            // Create HttpClient for calling WebServices
            using var httpClient = new HttpClient();

            // Create multipart form data
            using var formData = new MultipartFormDataContent();

            // Add collection ID if provided
            if (!string.IsNullOrEmpty(collectionId))
            {
                formData.Add(new StringContent(collectionId), "CollectionId");
                _logger.LogInformation("📁 BULK UPLOAD: Collection ID: {CollectionId}", collectionId);
            }

            // Add upload options
            formData.Add(new StringContent("true"), "Options.ExtractMetadata");
            formData.Add(new StringContent("true"), "Options.PerformOCR");
            formData.Add(new StringContent("true"), "Options.AutoDetectLanguage");
            formData.Add(new StringContent("true"), "Options.GenerateThumbnails");
            formData.Add(new StringContent("true"), "Options.EnableVersioning");
            formData.Add(new StringContent("10"), "Options.MaxParallelUploads");

            // Add all files
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                formData.Add(fileContent, "Files", file.FileName);
                _logger.LogInformation("📄 BULK UPLOAD: Added file: {FileName} ({Size} bytes)", file.FileName, file.Length);
            }

            // Add anti-forgery token
            var token = Request.Form["__RequestVerificationToken"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token))
            {
                formData.Add(new StringContent(token), "__RequestVerificationToken");
            }

            // Get JWT token using AuthService (same as DocumentApiClient)
            var jwtToken = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(jwtToken))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
                _logger.LogInformation("🔐 BULK UPLOAD: Added JWT authentication via AuthService. Token length: {TokenLength}", jwtToken.Length);
                _logger.LogInformation("🔐 BULK UPLOAD: Token starts with: {TokenStart}", jwtToken.Substring(0, Math.Min(20, jwtToken.Length)));
            }
            else
            {
                _logger.LogWarning("🔐 BULK UPLOAD: No JWT token available from AuthService");

                // Try fallback to cookie-based token
                var cookieToken = Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cookieToken);
                    _logger.LogInformation("🔐 BULK UPLOAD: Using fallback cookie token. Length: {TokenLength}", cookieToken.Length);
                }
                else
                {
                    _logger.LogError("🔐 BULK UPLOAD: No authentication token available from either AuthService or cookies!");
                }
            }

            _logger.LogInformation("🎯 BULK UPLOAD: Calling WebServices bulk upload API");

            // Call WebServices bulk upload endpoint
            var response = await httpClient.PostAsync("http://localhost:5252/api/DocumentUpload/batch", formData);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ BULK UPLOAD: Success! Response: {Response}", responseContent);

                // Get collection ID from form data for redirect
                var uploadCollectionId = Request.Form["CollectionId"].FirstOrDefault();

                // Parse the response to get more details
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<dynamic>(responseContent);

                // Return JSON response for AJAX call with collection ID for proper redirect
                return Json(new {
                    success = true,
                    message = $"Successfully uploaded {files.Count} files! Processing R2R ingestion...",
                    collectionId = uploadCollectionId,
                    fileCount = files.Count,
                    timestamp = DateTime.UtcNow,
                    details = "Files are being processed for R2R ingestion. Status updates will appear in real-time."
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ BULK UPLOAD: Failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                return BadRequest(new { success = false, message = $"Upload failed: {response.StatusCode} {response.ReasonPhrase}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ BULK UPLOAD: Exception occurred");
            return BadRequest(new { success = false, message = $"Upload failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Document details page
    /// </summary>
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Details(Guid documentId)
    {
        try
        {
            var document = await _documentApiClient.GetDocumentAsync(documentId);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }

            // Track view
            _ = Task.Run(async () => await _documentApiClient.TrackDocumentViewAsync(documentId));

            var viewModel = new DocumentDetailsViewModel
            {
                Document = document,
                RelatedDocuments = new List<DocumentViewModel>(),
                CanEdit = document.Permissions.CanEdit,
                CanDelete = document.Permissions.CanDelete,
                CanShare = document.Permissions.CanShare
            };

            ViewBag.PageTitle = document.Name;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading document details for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "Failed to load document details. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Download document
    /// </summary>
    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId)
    {
        try
        {
            var document = await _documentApiClient.GetDocumentAsync(documentId);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }

            // Track download
            _ = Task.Run(async () => await _documentApiClient.TrackDocumentViewAsync(documentId));

            // Get download URL from the API
            var downloadUrl = await _documentApiClient.GetDocumentDownloadUrlAsync(documentId);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                TempData["ErrorMessage"] = "Unable to generate download link for this document.";
                return RedirectToAction("Details", new { documentId });
            }

            // Redirect to the download URL
            return Redirect(downloadUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while downloading the document.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Edit document metadata
    /// </summary>
    [HttpGet("{documentId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid documentId)
    {
        try
        {
            var document = await _documentApiClient.GetDocumentAsync(documentId);
            if (document == null || !document.Permissions.CanEdit)
            {
                TempData["ErrorMessage"] = "Document not found or you don't have permission to edit it.";
                return RedirectToAction("Index");
            }

            var viewModel = new DocumentEditViewModel
            {
                Id = document.Id,
                Name = document.Name,
                Description = document.Description,
                CollectionId = document.CollectionId,
                TagsInput = string.Join(", ", document.Tags),
                IsFavorite = document.IsFavorite,
                AvailableCollections = new List<SelectListItem>()
            };

            ViewBag.PageTitle = $"Edit {document.Name}";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit page for document {DocumentId}", documentId);
            TempData["ErrorMessage"] = "Failed to load document for editing. Please try again.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Save document metadata changes
    /// </summary>
    [HttpPost("{documentId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid documentId, DocumentEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updatedDocument = await _documentApiClient.UpdateDocumentMetadataAsync(documentId, model);
            if (updatedDocument == null)
            {
                TempData["ErrorMessage"] = "Failed to update document. Please try again.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Document updated successfully.";
            return RedirectToAction("Details", new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", documentId);
            TempData["ErrorMessage"] = "Failed to update document. Please try again.";
            return View(model);
        }
    }

    /// <summary>
    /// Delete document
    /// </summary>
    [HttpPost("{documentId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid documentId)
    {
        try
        {
            var success = await _documentApiClient.DeleteDocumentAsync(documentId);
            if (success)
            {
                TempData["SuccessMessage"] = "Document deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete document. Please try again.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            TempData["ErrorMessage"] = "Failed to delete document. Please try again.";
            return RedirectToAction("Details", new { documentId });
        }
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    [HttpPost("{documentId:guid}/toggle-favorite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(Guid documentId)
    {
        try
        {
            var success = await _documentApiClient.ToggleFavoriteAsync(documentId);
            if (success)
            {
                TempData["SuccessMessage"] = "Favorite status updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update favorite status.";
            }

            return RedirectToAction("Details", new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for document {DocumentId}", documentId);
            TempData["ErrorMessage"] = "Failed to update favorite status.";
            return RedirectToAction("Details", new { documentId });
        }
    }

    /// <summary>
    /// Favorite documents page
    /// </summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> Favorites(DocumentSearchViewModel search)
    {
        try
        {
            // Set defaults
            search.Page = search.Page <= 0 ? 1 : search.Page;
            search.PageSize = search.PageSize <= 0 ? 20 : search.PageSize;
            search.SortBy = string.IsNullOrEmpty(search.SortBy) ? "updated_at" : search.SortBy;

            // Get favorite documents
            var documentsResult = await _documentApiClient.GetFavoriteDocumentsAsync(search);

            // Build view model
            var viewModel = new DocumentListViewModel
            {
                Documents = documentsResult.Items,
                Search = search,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = documentsResult.Page,
                    TotalPages = documentsResult.TotalPages,
                    TotalItems = documentsResult.TotalCount,
                    PageSize = documentsResult.PageSize
                },
                Statistics = await BuildStatisticsViewModel(documentsResult.Items),
                HasActiveFilters = HasActiveFilters(search)
            };

            // Populate filter options
            await PopulateFilterOptions(viewModel.Search);

            ViewBag.PageTitle = "Favorite Documents";

            return View("Favorites", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading favorite documents");
            TempData["ErrorMessage"] = "Failed to load favorite documents. Please try again.";
            return View(new DocumentListViewModel());
        }
    }

    /// <summary>
    /// AJAX endpoint for search suggestions
    /// </summary>
    [HttpGet("search-suggestions")]
    public async Task<IActionResult> GetSearchSuggestions(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            var suggestions = await _documentApiClient.GetSearchSuggestionsAsync(term, 10);
            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
            return Json(new List<string>());
        }
    }

    /// <summary>
    /// R2R Processing Test page
    /// </summary>
    [HttpGet("r2r-test")]
    public IActionResult R2RTest()
    {
        _logger.LogInformation("DocumentsController.R2RTest called");
        ViewBag.PageTitle = "R2R Processing Test";
        ViewBag.PageDescription = "Test real-time R2R document processing status updates";
        return View();
    }

    // Helper methods
    private static bool HasActiveFilters(DocumentSearchViewModel search)
    {
        return !string.IsNullOrEmpty(search.SearchTerm) ||
               search.SelectedContentTypes.Any() ||
               search.SelectedTags.Any() ||
               search.CreatedAfter.HasValue ||
               search.CreatedBefore.HasValue ||
               search.MinSize.HasValue ||
               search.MaxSize.HasValue ||
               search.IsFavorite.HasValue;
    }

    private static async Task<DocumentStatisticsViewModel> BuildStatisticsViewModel(List<DocumentViewModel> documents)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var stats = new DocumentStatisticsViewModel
        {
            TotalDocuments = documents.Count,
            TotalSize = documents.Sum(d => d.Size),
            FavoriteCount = documents.Count(d => d.IsFavorite),
            ProcessingCount = documents.Count(d => d.IsProcessing),
            ContentTypeDistribution = documents
                .GroupBy(d => d.ContentType)
                .ToDictionary(g => g.Key, g => g.Count()),
            StatusDistribution = documents
                .GroupBy(d => d.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    private async Task PopulateFilterOptions(DocumentSearchViewModel search)
    {
        // Content types
        search.AvailableContentTypes = new List<SelectListItem>
        {
            new() { Value = "application/pdf", Text = "PDF" },
            new() { Value = "application/msword", Text = "Word Document" },
            new() { Value = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Text = "Word Document (DOCX)" },
            new() { Value = "application/vnd.ms-excel", Text = "Excel Spreadsheet" },
            new() { Value = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Text = "Excel Spreadsheet (XLSX)" },
            new() { Value = "text/plain", Text = "Text File" },
            new() { Value = "image/jpeg", Text = "JPEG Image" },
            new() { Value = "image/png", Text = "PNG Image" }
        };

        // Sort options
        search.SortOptions = new List<SelectListItem>
        {
            new() { Value = "updated_at", Text = "Date Modified" },
            new() { Value = "created_at", Text = "Date Created" },
            new() { Value = "name", Text = "Name" },
            new() { Value = "size", Text = "File Size" },
            new() { Value = "view_count", Text = "View Count" }
        };

        await Task.CompletedTask;
    }

    private static void PopulateUploadViewModelProperties(DocumentUploadViewModel model)
    {
        // Ensure UI properties are populated for view rendering
        model.MaxFileSize = 100 * 1024 * 1024; // 100MB
        model.AllowedFileTypes = new List<string>
        {
            // R2R Supported file types only
            ".pdf", ".txt", ".md", ".docx", ".doc", ".xlsx", ".xls",
            ".pptx", ".ppt", ".html", ".csv", ".rtf", ".epub", ".odt",
            ".rst", ".org", ".tsv", ".eml", ".msg", ".png", ".jpeg",
            ".jpg", ".bmp", ".tiff", ".heic", ".mp3", ".py", ".js",
            ".ts", ".css"
        };
    }
}
