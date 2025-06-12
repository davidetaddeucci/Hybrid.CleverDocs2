using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Search;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class SearchClient : ISearchClient
    {
        private readonly HttpClient _httpClient;

        public SearchClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Search operations
        public async Task<SearchResponse?> SearchAsync(SearchRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/search", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SearchResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // RAG operations
        public async Task<RAGResponse?> RAGAsync(RAGRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/rag", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RAGResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IAsyncEnumerable<string>?> RAGStreamAsync(RAGRequest request)
        {
            try
            {
                // Enable streaming
                request.RagGenerationConfig ??= new RAGGenerationConfig();
                request.RagGenerationConfig.Stream = true;

                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/rag", request);
                response.EnsureSuccessStatusCode();

                return ProcessStreamingResponse(response);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Agent operations
        public async Task<AgentResponse?> AgentAsync(AgentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/agent", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<AgentResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IAsyncEnumerable<string>?> AgentStreamAsync(AgentRequest request)
        {
            try
            {
                // Enable streaming
                request.RagGenerationConfig ??= new RAGGenerationConfig();
                request.RagGenerationConfig.Stream = true;

                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/agent", request);
                response.EnsureSuccessStatusCode();

                return ProcessStreamingResponse(response);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Completion operations
        public async Task<CompletionResponse?> CompletionAsync(CompletionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/completion", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CompletionResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Embedding operations
        public async Task<EmbeddingResponse?> EmbeddingAsync(EmbeddingRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/v3/retrieval/embedding", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Helper method for processing streaming responses
        private async IAsyncEnumerable<string> ProcessStreamingResponse(HttpResponseMessage response, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var jsonData = line.Substring(6); // Remove "data: " prefix
                
                if (jsonData == "[DONE]")
                    break;

                string? delta = null;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonData);
                    var eventType = jsonDoc.RootElement.GetProperty("event").GetString();
                    
                    if (eventType == "message" || eventType == "thinking")
                    {
                        delta = jsonDoc.RootElement
                            .GetProperty("data")
                            .GetProperty("delta")
                            .GetProperty("content")[0]
                            .GetProperty("payload")
                            .GetProperty("value")
                            .GetString();
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON
                    continue;
                }

                if (!string.IsNullOrEmpty(delta))
                    yield return delta;
            }
        }
    }
}
