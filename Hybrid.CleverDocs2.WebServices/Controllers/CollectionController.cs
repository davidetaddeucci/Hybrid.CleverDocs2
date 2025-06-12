using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Collection;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/collections")]
    public class CollectionController : ControllerBase
    {
        private readonly ICollectionClient _client;
        public CollectionController(ICollectionClient client) => _client = client;

        // Collection CRUD operations
        [HttpPost]
        public async Task<IActionResult> CreateCollection(CollectionRequest request) => Ok(await _client.CreateCollectionAsync(request));

        [HttpGet("{collectionId}")]
        public async Task<IActionResult> GetCollection(string collectionId) => Ok(await _client.GetCollectionAsync(collectionId));

        [HttpGet]
        public async Task<IActionResult> ListCollections([FromQuery] CollectionListRequest? request = null) => Ok(await _client.ListCollectionsAsync(request));

        [HttpPut("{collectionId}")]
        public async Task<IActionResult> UpdateCollection(string collectionId, CollectionUpdateRequest request) => Ok(await _client.UpdateCollectionAsync(collectionId, request));

        [HttpDelete("{collectionId}")]
        public async Task<IActionResult> DeleteCollection(string collectionId)
        {
            await _client.DeleteCollectionAsync(collectionId);
            return NoContent();
        }

        // Document management in collections
        [HttpPost("{collectionId}/documents")]
        public async Task<IActionResult> AddDocumentToCollection(string collectionId, CollectionDocumentRequest request) => Ok(await _client.AddDocumentToCollectionAsync(collectionId, request));

        [HttpGet("{collectionId}/documents")]
        public async Task<IActionResult> ListCollectionDocuments(string collectionId, [FromQuery] int offset = 0, [FromQuery] int limit = 100) => Ok(await _client.ListCollectionDocumentsAsync(collectionId, offset, limit));

        [HttpDelete("{collectionId}/documents/{documentId}")]
        public async Task<IActionResult> RemoveDocumentFromCollection(string collectionId, string documentId)
        {
            await _client.RemoveDocumentFromCollectionAsync(collectionId, documentId);
            return NoContent();
        }

        // User access management
        [HttpPost("{collectionId}/users")]
        public async Task<IActionResult> AddUserToCollection(string collectionId, CollectionUserRequest request) => Ok(await _client.AddUserToCollectionAsync(collectionId, request));

        [HttpGet("{collectionId}/users")]
        public async Task<IActionResult> ListCollectionUsers(string collectionId, [FromQuery] int offset = 0, [FromQuery] int limit = 100) => Ok(await _client.ListCollectionUsersAsync(collectionId, offset, limit));

        [HttpPut("{collectionId}/users/{userId}")]
        public async Task<IActionResult> UpdateUserPermission(string collectionId, string userId, CollectionPermissionRequest request) => Ok(await _client.UpdateUserPermissionAsync(collectionId, userId, request));

        [HttpDelete("{collectionId}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromCollection(string collectionId, string userId)
        {
            await _client.RemoveUserFromCollectionAsync(collectionId, userId);
            return NoContent();
        }

        // Collection statistics and analytics
        [HttpGet("{collectionId}/stats")]
        public async Task<IActionResult> GetCollectionStats(string collectionId) => Ok(await _client.GetCollectionStatsAsync(collectionId));

        // Bulk operations
        [HttpPost("{collectionId}/documents/bulk")]
        public async Task<IActionResult> AddMultipleDocuments(string collectionId, [FromBody] List<string> documentIds) => Ok(await _client.AddMultipleDocumentsAsync(collectionId, documentIds));

        [HttpDelete("{collectionId}/documents/bulk")]
        public async Task<IActionResult> RemoveMultipleDocuments(string collectionId, [FromBody] List<string> documentIds) => Ok(await _client.RemoveMultipleDocumentsAsync(collectionId, documentIds));

        [HttpPost("{collectionId}/users/bulk")]
        public async Task<IActionResult> AddMultipleUsers(string collectionId, [FromBody] List<CollectionUserRequest> users) => Ok(await _client.AddMultipleUsersAsync(collectionId, users));

        [HttpDelete("{collectionId}/users/bulk")]
        public async Task<IActionResult> RemoveMultipleUsers(string collectionId, [FromBody] List<string> userIds) => Ok(await _client.RemoveMultipleUsersAsync(collectionId, userIds));

        // Collection cloning and templates
        [HttpPost("{collectionId}/clone")]
        public async Task<IActionResult> CloneCollection(string collectionId, CollectionRequest request) => Ok(await _client.CloneCollectionAsync(collectionId, request));

        [HttpPost("{collectionId}/export")]
        public async Task<IActionResult> ExportCollection(string collectionId) => Ok(await _client.ExportCollectionAsync(collectionId));

        [HttpPost("import")]
        public async Task<IActionResult> ImportCollection([FromForm] CollectionRequest request, IFormFile dataFile)
        {
            if (dataFile == null || dataFile.Length == 0)
                return BadRequest("Data file is required");

            using var stream = dataFile.OpenReadStream();
            var result = await _client.ImportCollectionAsync(request, stream);
            return Ok(result);
        }
    }
}