using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Ingestion;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IIngestionClient
    {
        // Ingestion CRUD operations
        Task<IngestionCreateResponse?> CreateIngestionAsync(IngestionRequest request);
        Task<IngestionResponse?> GetIngestionAsync(string ingestionId);
        Task<IngestionListResponse?> ListIngestionsAsync(IngestionListRequest? request = null);
        Task<IngestionResponse?> UpdateIngestionAsync(string ingestionId, IngestionUpdateRequest request);
        Task DeleteIngestionAsync(string ingestionId);

        // Ingestion status and monitoring
        Task<IngestionStatusResponse?> GetIngestionStatusAsync(IngestionStatusRequest request);
        Task<IngestionStatsResponse?> GetIngestionStatsAsync();
        Task<IngestionLogsResponse?> GetIngestionLogsAsync(string ingestionId);

        // Ingestion control operations
        Task<IngestionRetryResponse?> RetryIngestionsAsync(IngestionRetryRequest request);
        Task<IngestionCancelResponse?> CancelIngestionsAsync(IngestionCancelRequest request);

        // Streaming support for real-time updates
        Task<IAsyncEnumerable<IngestionResponse>?> StreamIngestionUpdatesAsync(List<string> ingestionIds);

        // Bulk operations
        Task<MessageResponse2?> DeleteMultipleIngestionsAsync(List<string> ingestionIds);
        Task<IngestionListResponse?> GetIngestionsByDocumentAsync(string documentId);
        Task<IngestionStatsResponse?> GetIngestionStatsByStatusAsync(string status);

        // Pipeline management
        Task<MessageResponse2?> PauseIngestionPipelineAsync();
        Task<MessageResponse2?> ResumeIngestionPipelineAsync();
        Task<MessageResponse2?> GetIngestionPipelineStatusAsync();

        // Configuration management
        Task<MessageResponse2?> UpdateIngestionConfigAsync(IngestionConfig config);
        Task<IngestionConfig?> GetIngestionConfigAsync();
    }
}
