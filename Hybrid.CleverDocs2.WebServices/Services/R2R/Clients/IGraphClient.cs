using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Graph;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IGraphClient
    {
        // Graph operations
        Task<GraphResponse?> GetGraphAsync(string collectionId);
        Task<GraphPullResponse?> PullGraphAsync(string collectionId, GraphPullRequest request);
        Task<GraphResetResponse?> ResetGraphAsync(string collectionId);
        Task<GraphStatusResponse?> GetGraphStatusAsync(string collectionId);

        // Community operations
        Task<GraphCommunityResponse?> BuildCommunitiesAsync(string collectionId, GraphCommunityRequest request);

        // Entity operations
        Task<EntityResponse?> CreateEntityAsync(string collectionId, EntityRequest request);
        Task<EntityResponse?> GetEntityAsync(string collectionId, string entityId);
        Task<GraphListResponse<EntityResponse>?> ListEntitiesAsync(string collectionId, GraphListRequest? request = null);
        Task<EntityResponse?> UpdateEntityAsync(string collectionId, string entityId, EntityRequest request);
        Task DeleteEntityAsync(string collectionId, string entityId);

        // Relationship operations
        Task<RelationshipResponse?> CreateRelationshipAsync(string collectionId, RelationshipRequest request);
        Task<RelationshipResponse?> GetRelationshipAsync(string collectionId, string relationshipId);
        Task<GraphListResponse<RelationshipResponse>?> ListRelationshipsAsync(string collectionId, GraphListRequest? request = null);
        Task<RelationshipResponse?> UpdateRelationshipAsync(string collectionId, string relationshipId, RelationshipRequest request);
        Task DeleteRelationshipAsync(string collectionId, string relationshipId);

        // Community operations
        Task<CommunityResponse?> CreateCommunityAsync(string collectionId, CommunityRequest request);
        Task<CommunityResponse?> GetCommunityAsync(string collectionId, string communityId);
        Task<GraphListResponse<CommunityResponse>?> ListCommunitiesAsync(string collectionId, GraphListRequest? request = null);
        Task<CommunityResponse?> UpdateCommunityAsync(string collectionId, string communityId, CommunityRequest request);
        Task DeleteCommunityAsync(string collectionId, string communityId);

        // Search operations
        Task<GraphSearchResponse?> SearchGraphAsync(string collectionId, GraphSearchRequest request);

        // Bulk operations
        Task DeleteEntitiesByFilterAsync(string collectionId, Dictionary<string, object> filters);
        Task DeleteRelationshipsByFilterAsync(string collectionId, Dictionary<string, object> filters);
        Task DeleteCommunitiesByFilterAsync(string collectionId, Dictionary<string, object> filters);

        // Export operations
        Task<Stream?> ExportEntitiesAsync(string collectionId);
        Task<Stream?> ExportRelationshipsAsync(string collectionId);
        Task<Stream?> ExportCommunitiesAsync(string collectionId);
        Task<Stream?> ExportGraphAsync(string collectionId);
    }
}
