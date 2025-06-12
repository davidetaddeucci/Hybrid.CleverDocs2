using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Collection;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface ICollectionClient
    {
        // Collection CRUD operations
        Task<CollectionCreateResponse?> CreateCollectionAsync(CollectionRequest request);
        Task<CollectionResponse?> GetCollectionAsync(string collectionId);
        Task<CollectionListResponse?> ListCollectionsAsync(CollectionListRequest? request = null);
        Task<CollectionResponse?> UpdateCollectionAsync(string collectionId, CollectionUpdateRequest request);
        Task DeleteCollectionAsync(string collectionId);

        // Document management in collections
        Task<CollectionDocumentResponse?> AddDocumentToCollectionAsync(string collectionId, CollectionDocumentRequest request);
        Task<CollectionDocumentsListResponse?> ListCollectionDocumentsAsync(string collectionId, int offset = 0, int limit = 100);
        Task RemoveDocumentFromCollectionAsync(string collectionId, string documentId);

        // User access management
        Task<CollectionUserResponse?> AddUserToCollectionAsync(string collectionId, CollectionUserRequest request);
        Task<CollectionUsersListResponse?> ListCollectionUsersAsync(string collectionId, int offset = 0, int limit = 100);
        Task<CollectionUserResponse?> UpdateUserPermissionAsync(string collectionId, string userId, CollectionPermissionRequest request);
        Task RemoveUserFromCollectionAsync(string collectionId, string userId);

        // Collection statistics and analytics
        Task<CollectionStatsResponse?> GetCollectionStatsAsync(string collectionId);

        // Bulk operations
        Task<MessageResponse?> AddMultipleDocumentsAsync(string collectionId, List<string> documentIds);
        Task<MessageResponse?> RemoveMultipleDocumentsAsync(string collectionId, List<string> documentIds);
        Task<MessageResponse?> AddMultipleUsersAsync(string collectionId, List<CollectionUserRequest> users);
        Task<MessageResponse?> RemoveMultipleUsersAsync(string collectionId, List<string> userIds);

        // Collection cloning and templates
        Task<CollectionCreateResponse?> CloneCollectionAsync(string collectionId, CollectionRequest request);
        Task<MessageResponse?> ExportCollectionAsync(string collectionId);
        Task<MessageResponse?> ImportCollectionAsync(CollectionRequest request, Stream dataStream);
    }
}