using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Search;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public interface ISearchClient
    {
        // Search operations
        Task<SearchResponse?> SearchAsync(SearchRequest request);

        // RAG operations
        Task<RAGResponse?> RAGAsync(RAGRequest request);
        Task<IAsyncEnumerable<string>?> RAGStreamAsync(RAGRequest request);

        // Agent operations
        Task<AgentResponse?> AgentAsync(AgentRequest request);
        Task<IAsyncEnumerable<string>?> AgentStreamAsync(AgentRequest request);

        // Completion operations
        Task<CompletionResponse?> CompletionAsync(CompletionRequest request);

        // Embedding operations
        Task<EmbeddingResponse?> EmbeddingAsync(EmbeddingRequest request);
    }
}
