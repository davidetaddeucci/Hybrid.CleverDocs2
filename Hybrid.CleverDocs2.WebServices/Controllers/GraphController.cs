using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Graph;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/graphs")]
    public class GraphController : ControllerBase
    {
        private readonly IGraphClient _client;
        public GraphController(IGraphClient client) => _client = client;

        // Graph operations
        [HttpGet("{collectionId}")]
        public async Task<IActionResult> GetGraph(string collectionId) => Ok(await _client.GetGraphAsync(collectionId));

        [HttpPost("{collectionId}/pull")]
        public async Task<IActionResult> PullGraph(string collectionId, GraphPullRequest request) => Ok(await _client.PullGraphAsync(collectionId, request));

        [HttpPost("{collectionId}/reset")]
        public async Task<IActionResult> ResetGraph(string collectionId) => Ok(await _client.ResetGraphAsync(collectionId));

        [HttpGet("{collectionId}/status")]
        public async Task<IActionResult> GetGraphStatus(string collectionId) => Ok(await _client.GetGraphStatusAsync(collectionId));

        // Community operations
        [HttpPost("{collectionId}/communities/build")]
        public async Task<IActionResult> BuildCommunities(string collectionId, GraphCommunityRequest request) => Ok(await _client.BuildCommunitiesAsync(collectionId, request));

        // Entity operations
        [HttpPost("{collectionId}/entities")]
        public async Task<IActionResult> CreateEntity(string collectionId, EntityRequest request) => Ok(await _client.CreateEntityAsync(collectionId, request));

        [HttpGet("{collectionId}/entities/{entityId}")]
        public async Task<IActionResult> GetEntity(string collectionId, string entityId) => Ok(await _client.GetEntityAsync(collectionId, entityId));

        [HttpGet("{collectionId}/entities")]
        public async Task<IActionResult> ListEntities(string collectionId, [FromQuery] GraphListRequest? request = null) => Ok(await _client.ListEntitiesAsync(collectionId, request));

        [HttpPut("{collectionId}/entities/{entityId}")]
        public async Task<IActionResult> UpdateEntity(string collectionId, string entityId, EntityRequest request) => Ok(await _client.UpdateEntityAsync(collectionId, entityId, request));

        [HttpDelete("{collectionId}/entities/{entityId}")]
        public async Task<IActionResult> DeleteEntity(string collectionId, string entityId)
        {
            await _client.DeleteEntityAsync(collectionId, entityId);
            return NoContent();
        }

        // Relationship operations
        [HttpPost("{collectionId}/relationships")]
        public async Task<IActionResult> CreateRelationship(string collectionId, RelationshipRequest request) => Ok(await _client.CreateRelationshipAsync(collectionId, request));

        [HttpGet("{collectionId}/relationships/{relationshipId}")]
        public async Task<IActionResult> GetRelationship(string collectionId, string relationshipId) => Ok(await _client.GetRelationshipAsync(collectionId, relationshipId));

        [HttpGet("{collectionId}/relationships")]
        public async Task<IActionResult> ListRelationships(string collectionId, [FromQuery] GraphListRequest? request = null) => Ok(await _client.ListRelationshipsAsync(collectionId, request));

        [HttpPut("{collectionId}/relationships/{relationshipId}")]
        public async Task<IActionResult> UpdateRelationship(string collectionId, string relationshipId, RelationshipRequest request) => Ok(await _client.UpdateRelationshipAsync(collectionId, relationshipId, request));

        [HttpDelete("{collectionId}/relationships/{relationshipId}")]
        public async Task<IActionResult> DeleteRelationship(string collectionId, string relationshipId)
        {
            await _client.DeleteRelationshipAsync(collectionId, relationshipId);
            return NoContent();
        }

        // Community operations
        [HttpPost("{collectionId}/communities")]
        public async Task<IActionResult> CreateCommunity(string collectionId, CommunityRequest request) => Ok(await _client.CreateCommunityAsync(collectionId, request));

        [HttpGet("{collectionId}/communities/{communityId}")]
        public async Task<IActionResult> GetCommunity(string collectionId, string communityId) => Ok(await _client.GetCommunityAsync(collectionId, communityId));

        [HttpGet("{collectionId}/communities")]
        public async Task<IActionResult> ListCommunities(string collectionId, [FromQuery] GraphListRequest? request = null) => Ok(await _client.ListCommunitiesAsync(collectionId, request));

        [HttpPut("{collectionId}/communities/{communityId}")]
        public async Task<IActionResult> UpdateCommunity(string collectionId, string communityId, CommunityRequest request) => Ok(await _client.UpdateCommunityAsync(collectionId, communityId, request));

        [HttpDelete("{collectionId}/communities/{communityId}")]
        public async Task<IActionResult> DeleteCommunity(string collectionId, string communityId)
        {
            await _client.DeleteCommunityAsync(collectionId, communityId);
            return NoContent();
        }

        // Search operations
        [HttpPost("{collectionId}/search")]
        public async Task<IActionResult> SearchGraph(string collectionId, GraphSearchRequest request) => Ok(await _client.SearchGraphAsync(collectionId, request));

        // Bulk operations
        [HttpDelete("{collectionId}/entities/by-filter")]
        public async Task<IActionResult> DeleteEntitiesByFilter(string collectionId, Dictionary<string, object> filters)
        {
            await _client.DeleteEntitiesByFilterAsync(collectionId, filters);
            return NoContent();
        }

        [HttpDelete("{collectionId}/relationships/by-filter")]
        public async Task<IActionResult> DeleteRelationshipsByFilter(string collectionId, Dictionary<string, object> filters)
        {
            await _client.DeleteRelationshipsByFilterAsync(collectionId, filters);
            return NoContent();
        }

        [HttpDelete("{collectionId}/communities/by-filter")]
        public async Task<IActionResult> DeleteCommunitiesByFilter(string collectionId, Dictionary<string, object> filters)
        {
            await _client.DeleteCommunitiesByFilterAsync(collectionId, filters);
            return NoContent();
        }

        // Export operations
        [HttpPost("{collectionId}/entities/export")]
        public async Task<IActionResult> ExportEntities(string collectionId)
        {
            var stream = await _client.ExportEntitiesAsync(collectionId);
            if (stream == null) return BadRequest();
            return File(stream, "application/json", $"entities-{collectionId}.json");
        }

        [HttpPost("{collectionId}/relationships/export")]
        public async Task<IActionResult> ExportRelationships(string collectionId)
        {
            var stream = await _client.ExportRelationshipsAsync(collectionId);
            if (stream == null) return BadRequest();
            return File(stream, "application/json", $"relationships-{collectionId}.json");
        }

        [HttpPost("{collectionId}/communities/export")]
        public async Task<IActionResult> ExportCommunities(string collectionId)
        {
            var stream = await _client.ExportCommunitiesAsync(collectionId);
            if (stream == null) return BadRequest();
            return File(stream, "application/json", $"communities-{collectionId}.json");
        }

        [HttpPost("{collectionId}/export")]
        public async Task<IActionResult> ExportGraph(string collectionId)
        {
            var stream = await _client.ExportGraphAsync(collectionId);
            if (stream == null) return BadRequest();
            return File(stream, "application/json", $"graph-{collectionId}.json");
        }
    }
}
