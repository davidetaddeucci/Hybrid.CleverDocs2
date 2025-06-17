using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hybrid.CleverDocs2.WebServices.Models.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

/// <summary>
/// Controller for user document management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Admin")]
public class UserDocumentsController : ControllerBase
{
    private readonly IUserDocumentService _documentService;
    private readonly ILogger<UserDocumentsController> _logger;
    private readonly ICorrelationService _correlationService;

    public UserDocumentsController(
        IUserDocumentService documentService,
        ILogger<UserDocumentsController> logger,
        ICorrelationService correlationService)
    {
        _documentService = documentService;
        _logger = logger;
        _correlationService = correlationService;
    }

    /// <summary>
    /// Search documents with advanced filtering and pagination
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchDocuments([FromBody] DocumentQueryDto query, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Searching documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);

            query.UserId = userId;
            var result = await _documentService.SearchDocumentsAsync(query, cancellationToken);

            return this.Success(result, "Documents retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            return this.Error("Search operation was cancelled", statusCode: 408);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to search documents", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get documents for a specific collection
    /// </summary>
    [HttpGet("collections/{collectionId:guid}")]
    public async Task<IActionResult> GetCollectionDocuments(
        Guid collectionId, 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "updated_at",
        [FromQuery] SortDirection sortDirection = SortDirection.Desc,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting documents for collection {CollectionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);

            var query = new DocumentQueryDto
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortDirection = sortDirection,
                CollectionId = collectionId,
                UserId = userId
            };

            var result = await _documentService.GetCollectionDocumentsAsync(collectionId, userId, query, cancellationToken);

            return this.Success(result, "Collection documents retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for collection {CollectionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                collectionId, userId, correlationId);
            
            return this.Error("Failed to get collection documents", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get a specific document by ID
    /// </summary>
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetDocument(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting document {DocumentId} for user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);

            var document = await _documentService.GetDocumentByIdAsync(documentId, userId);

            if (document == null)
            {
                return this.NotFound<UserDocumentDto>("Document not found");
            }

            return this.Success(document, "Document retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId} for user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            return this.Error("Failed to get document", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get multiple documents by IDs
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetDocumentsByIds([FromBody] List<Guid> documentIds)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (!documentIds.Any())
            {
                return this.Error("Document IDs are required");
            }

            if (documentIds.Count > 100)
            {
                return this.Error("Maximum 100 documents can be retrieved at once");
            }

            _logger.LogInformation("Getting {Count} documents for user {UserId}, CorrelationId: {CorrelationId}", 
                documentIds.Count, userId, correlationId);

            var documents = await _documentService.GetDocumentsByIdsAsync(documentIds, userId);

            return this.Success(documents, $"Retrieved {documents.Count} documents");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by IDs for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to get documents", ex.Message, 500);
        }
    }

    /// <summary>
    /// Update document metadata
    /// </summary>
    [HttpPut("{documentId:guid}/metadata")]
    public async Task<IActionResult> UpdateDocumentMetadata(Guid documentId, [FromBody] DocumentMetadataUpdateDto update)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Updating metadata for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);

            var document = await _documentService.UpdateDocumentMetadataAsync(documentId, userId, update);

            if (document == null)
            {
                return this.NotFound<UserDocumentDto>("Document not found or access denied");
            }

            return this.Success(document, "Document metadata updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            return this.Error("Failed to update document metadata", ex.Message, 500);
        }
    }

    /// <summary>
    /// Move document to a different collection
    /// </summary>
    [HttpPut("{documentId:guid}/move")]
    public async Task<IActionResult> MoveDocument(Guid documentId, [FromBody] MoveDocumentRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Moving document {DocumentId} to collection {TargetCollectionId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, request.TargetCollectionId, userId, correlationId);

            var success = await _documentService.MoveDocumentToCollectionAsync(documentId, userId, request.TargetCollectionId);

            if (!success)
            {
                return this.Error("Failed to move document. Document not found or access denied.");
            }

            return this.Success("Document moved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            return this.Error("Failed to move document", ex.Message, 500);
        }
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Deleting document {DocumentId} for user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);

            var success = await _documentService.DeleteDocumentAsync(documentId, userId);

            if (!success)
            {
                return this.Error("Failed to delete document. Document not found or access denied.");
            }

            return this.Success("Document deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            return this.Error("Failed to delete document", ex.Message, 500);
        }
    }

    /// <summary>
    /// Toggle document favorite status
    /// </summary>
    [HttpPost("{documentId:guid}/toggle-favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Toggling favorite for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);

            var success = await _documentService.ToggleFavoriteAsync(documentId, userId);

            if (!success)
            {
                return this.Error("Failed to toggle favorite. Document not found or access denied.");
            }

            return this.Success("Document favorite status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            return this.Error("Failed to toggle favorite", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get user's favorite documents
    /// </summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavoriteDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "updated_at",
        [FromQuery] SortDirection sortDirection = SortDirection.Desc)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting favorite documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);

            var query = new DocumentQueryDto
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortDirection = sortDirection,
                IsFavorite = true
            };

            var result = await _documentService.GetFavoriteDocumentsAsync(userId, query);

            return this.Success(result, "Favorite documents retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to get favorite documents", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get recently viewed documents
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentDocuments([FromQuery] int limit = 10)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting recent documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);

            var documents = await _documentService.GetRecentDocumentsAsync(userId, limit);

            return this.Success(documents, "Recent documents retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent documents for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to get recent documents", ex.Message, 500);
        }
    }

    /// <summary>
    /// Execute batch operations on multiple documents
    /// </summary>
    [HttpPost("batch-operation")]
    public async Task<IActionResult> ExecuteBatchOperation([FromBody] BatchOperationRequestDto request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (!request.DocumentIds.Any())
            {
                return this.Error("Document IDs are required");
            }

            if (request.DocumentIds.Count > 100)
            {
                return this.Error("Maximum 100 documents can be processed at once");
            }

            _logger.LogInformation("Executing batch operation {Operation} on {Count} documents for user {UserId}, CorrelationId: {CorrelationId}", 
                request.Operation, request.DocumentIds.Count, userId, correlationId);

            request.UserId = userId;
            var result = await _documentService.ExecuteBatchOperationAsync(request);

            return this.Success(result, $"Batch operation completed: {result.SuccessCount} successful, {result.FailureCount} failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing batch operation for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to execute batch operation", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get search suggestions
    /// </summary>
    [HttpGet("search-suggestions")]
    public async Task<IActionResult> GetSearchSuggestions([FromQuery] string term, [FromQuery] int limit = 10)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return this.Success(new List<string>(), "Search term too short");
            }

            _logger.LogDebug("Getting search suggestions for term '{Term}', user {UserId}, CorrelationId: {CorrelationId}", 
                term, userId, correlationId);

            var suggestions = await _documentService.GetSearchSuggestionsAsync(term, userId, limit);

            return this.Success(suggestions, "Search suggestions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for user {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);
            
            return this.Error("Failed to get search suggestions", ex.Message, 500);
        }
    }

    /// <summary>
    /// Get document download URL
    /// </summary>
    [HttpGet("{documentId:guid}/download-url")]
    public async Task<IActionResult> GetDocumentDownloadUrl(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Getting download URL for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}",
                documentId, userId, correlationId);

            var downloadUrl = await _documentService.GetDocumentDownloadUrlAsync(documentId, userId);

            if (string.IsNullOrEmpty(downloadUrl))
            {
                return this.NotFound<string>("Document not found or download not available");
            }

            return this.Success(downloadUrl, "Download URL generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URL for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}",
                documentId, userId, correlationId);

            return this.Error("Failed to generate download URL", ex.Message, 500);
        }
    }

    /// <summary>
    /// Download document directly
    /// </summary>
    [HttpGet("{documentId:guid}/download-direct")]
    public async Task<IActionResult> DownloadDocumentDirect(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("Direct download for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}",
                documentId, userId, correlationId);

            var document = await _documentService.GetDocumentByIdAsync(documentId, userId);
            if (document == null)
            {
                return NotFound("Document not found");
            }

            // Track download
            await _documentService.TrackDocumentDownloadAsync(documentId, userId);

            // Get file stream from R2R
            if (string.IsNullOrEmpty(document.R2RDocumentId))
            {
                return BadRequest("Document not available for download");
            }

            // For now, redirect to a placeholder or return file info
            // In a real implementation, you would stream the file content
            return Ok(new {
                message = "Download would start here",
                documentId = documentId,
                fileName = document.Name,
                r2rId = document.R2RDocumentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}",
                documentId, userId, correlationId);

            return StatusCode(500, "Failed to download document");
        }
    }

    /// <summary>
    /// Track document view
    /// </summary>
    [HttpPost("{documentId:guid}/track-view")]
    public async Task<IActionResult> TrackDocumentView(Guid documentId)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var userId = GetCurrentUserId();

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            await _documentService.TrackDocumentViewAsync(documentId, userId, ipAddress, userAgent);

            return this.Success("Document view tracked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking document view for document {DocumentId}, user {UserId}, CorrelationId: {CorrelationId}", 
                documentId, userId, correlationId);
            
            // Don't fail the request for tracking errors
            return this.Success("Document view tracking failed but request completed");
        }
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        // Get the user ID from the NameIdentifier claim (which contains the GUID)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         User.FindFirst("sub")?.Value ??
                         User.Identity?.Name;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Guid.Empty; // Return empty Guid for anonymous users
        }

        return userId;
    }
}

/// <summary>
/// DTO for move document request
/// </summary>
public class MoveDocumentRequestDto
{
    public Guid? TargetCollectionId { get; set; }
}
