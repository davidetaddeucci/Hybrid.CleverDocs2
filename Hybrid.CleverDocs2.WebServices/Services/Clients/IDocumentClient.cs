using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface IDocumentClient
    {
        // Core CRUD operations
        Task<DocumentResponse?> CreateAsync(DocumentRequest request);
        Task<DocumentResponse?> GetAsync(string id);
        Task<DocumentListResponse?> ListAsync(DocumentListRequest? request = null);
        Task<DocumentResponse?> UpdateAsync(string id, DocumentRequest request);
        Task DeleteAsync(string id);

        // File operations
        Task<Stream?> DownloadAsync(string id);
        Task<Stream?> DownloadZipAsync(List<string> documentIds);

        // Chunk operations
        Task<List<DocumentChunk>?> GetChunksAsync(string id);

        // Metadata operations
        Task<DocumentResponse?> UpdateMetadataAsync(string id, DocumentMetadataRequest request);
        Task<DocumentResponse?> ReplaceMetadataAsync(string id, DocumentMetadataRequest request);

        // Search operations
        Task<List<DocumentResponse>?> SearchAsync(DocumentSearchRequest request);

        // Knowledge graph operations
        Task StartExtractionAsync(string id, DocumentExtractionRequest? request = null);
        Task<List<DocumentEntityResponse>?> GetEntitiesAsync(string id);
        Task<List<DocumentRelationshipResponse>?> GetRelationshipsAsync(string id);

        // Bulk operations
        Task DeleteByFilterAsync(Dictionary<string, object> filters);
        Task<Stream?> ExportAsync(DocumentExportRequest request);
        Task<Stream?> ExportEntitiesAsync(string id);
        Task<Stream?> ExportRelationshipsAsync(string id);

        // Deduplication
        Task StartDeduplicationAsync(string id);

        // Collections
        Task<List<string>?> GetCollectionsAsync(string id);
    }
}
