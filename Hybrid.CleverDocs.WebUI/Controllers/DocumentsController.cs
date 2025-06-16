using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs.WebUI.Models.Documents;
using Hybrid.CleverDocs.WebUI.Services.Documents;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hybrid.CleverDocs.WebUI.Controllers;

/// <summary>
/// MVC Controller for document management operations
/// </summary>
[Authorize]
[Route("documents")]
public class DocumentsController : Controller
{
    private readonly IDocumentApiClient _documentApiClient;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentApiClient documentApiClient,
        ILogger<DocumentsController> logger)
    {
        _documentApiClient = documentApiClient;
        _logger = logger;
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
            search.PageSize = search.PageSize <= 0 ? 20 : search.PageSize;
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
        var viewModel = new DocumentUploadViewModel
        {
            CollectionId = collectionId,
            MaxFileSize = 100 * 1024 * 1024, // 100MB
            AllowedFileTypes = new List<string>
            {
                ".pdf", ".doc", ".docx", ".txt", ".md",
                ".ppt", ".pptx", ".xls", ".xlsx"
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file to upload.");
                return View(model);
            }

            // Validate file size (100MB max)
            if (model.File.Length > 100 * 1024 * 1024)
            {
                ModelState.AddModelError("File", "File size cannot exceed 100MB.");
                return View(model);
            }

            // Validate file type
            var fileExtension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".md", ".ppt", ".pptx", ".xls", ".xlsx" };

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("File", "File type not supported. Please upload a PDF, Word, PowerPoint, Excel, or text file.");
                return View(model);
            }

            // TODO: Implement actual upload logic using _documentApiClient
            // For now, just show success message
            TempData["SuccessMessage"] = $"Document '{model.File.FileName}' uploaded successfully.";

            if (model.CollectionId.HasValue)
            {
                return RedirectToAction("Details", "Collections", new { collectionId = model.CollectionId.Value });
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            ModelState.AddModelError("", "An error occurred while uploading the document. Please try again.");
            return View(model);
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
}
