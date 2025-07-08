using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Microsoft.Extensions.Logging;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ConversationClient : IConversationClient
    {
        private readonly HttpClient _httpClient;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ILogger<ConversationClient> _logger;

        public ConversationClient(HttpClient httpClient, IRateLimitingService rateLimitingService, ILogger<ConversationClient> logger)
        {
            _httpClient = httpClient;
            _rateLimitingService = rateLimitingService;
            _logger = logger;
        }

        // Conversation CRUD operations
        public async Task<ConversationCreateResponse?> CreateConversationAsync(ConversationRequest request)
        {
            try
            {
                // Apply rate limiting for conversation operations
                await _rateLimitingService.WaitForAvailabilityAsync("r2r_conversation");

                var response = await _httpClient.PostAsJsonAsync("/v3/conversations", request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleR2RErrorAsync(response, "CreateConversation");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ConversationCreateResponse>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception in CreateConversationAsync");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout in CreateConversationAsync");
                return null;
            }
        }

        public async Task<ConversationResponse?> GetConversationAsync(string conversationId)
        {
            try
            {
                // Apply rate limiting for conversation operations
                await _rateLimitingService.WaitForAvailabilityAsync("r2r_conversation");

                var response = await _httpClient.GetAsync($"/v3/conversations/{conversationId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ConversationListResponse?> ListConversationsAsync(ConversationListRequest? request = null)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (request != null)
                {
                    queryParams.Add($"offset={request.Offset}");
                    queryParams.Add($"limit={request.Limit}");
                    
                    if (request.ConversationIds?.Any() == true)
                        queryParams.Add($"conversation_ids={string.Join(",", request.ConversationIds)}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/v3/conversations{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ConversationResponse?> UpdateConversationAsync(string conversationId, ConversationUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/conversations/{conversationId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteConversationAsync(string conversationId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/conversations/{conversationId}");
            response.EnsureSuccessStatusCode();
        }

        // Message operations
        public async Task<MessageCreateResponse?> AddMessageAsync(string conversationId, MessageRequest request)
        {
            try
            {
                // Apply rate limiting for message operations (higher rate for real-time chat)
                await _rateLimitingService.WaitForAvailabilityAsync("r2r_conversation");

                // ✅ ENHANCED: Log the complete request for debugging R2R issues
                _logger.LogInformation("🚀 R2R API CALL: POST /v3/conversations/{ConversationId}/messages", conversationId);
                _logger.LogInformation("🚀 Request payload: {Payload}", System.Text.Json.JsonSerializer.Serialize(request));

                var response = await _httpClient.PostAsJsonAsync($"/v3/conversations/{conversationId}/messages", request);

                // ✅ ENHANCED: Log response details for debugging
                _logger.LogInformation("🚀 R2R API Response: Status={StatusCode}, Headers={Headers}",
                    response.StatusCode, string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));

                if (!response.IsSuccessStatusCode)
                {
                    // ✅ ENHANCED: Log response body for failed requests
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("🚀 R2R API ERROR: Status={StatusCode}, Body={ErrorBody}", response.StatusCode, errorBody);

                    await HandleR2RErrorAsync(response, "AddMessage");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("🚀 R2R API SUCCESS: Response body length={Length}, Preview={Preview}",
                    responseBody.Length, responseBody.Substring(0, Math.Min(500, responseBody.Length)));

                // Parse the response
                var result = System.Text.Json.JsonSerializer.Deserialize<MessageCreateResponse>(responseBody);

                // ✅ ENHANCED: Validate that R2R actually generated content
                if (result?.Results?.Content == null || string.IsNullOrWhiteSpace(result.Results.Content))
                {
                    _logger.LogWarning("🚀 R2R API WARNING: Response received but content is empty or null!");
                    _logger.LogWarning("🚀 This indicates R2R processed the request but didn't generate AI content");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "🚀 HTTP request exception in AddMessageAsync for conversation {ConversationId}", conversationId);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "🚀 Request timeout in AddMessageAsync for conversation {ConversationId}", conversationId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚀 Unexpected error in AddMessageAsync for conversation {ConversationId}", conversationId);
                return null;
            }
        }

        public async Task<MessageResponse?> GetMessageAsync(string conversationId, string messageId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/conversations/{conversationId}/messages/{messageId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageListResponse?> ListMessagesAsync(string conversationId, int offset = 0, int limit = 100)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/conversations/{conversationId}/messages?offset={offset}&limit={limit}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse?> UpdateMessageAsync(string conversationId, string messageId, MessageRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/v3/conversations/{conversationId}/messages/{messageId}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task DeleteMessageAsync(string conversationId, string messageId)
        {
            var response = await _httpClient.DeleteAsync($"/v3/conversations/{conversationId}/messages/{messageId}");
            response.EnsureSuccessStatusCode();
        }

        // Conversation branching
        public async Task<ConversationBranchResponse?> BranchConversationAsync(string conversationId, ConversationBranchRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"/v3/conversations/{conversationId}/branch", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationBranchResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Conversation analytics
        public async Task<ConversationStatsResponse?> GetConversationStatsAsync(string conversationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/conversations/{conversationId}/stats");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationStatsResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Streaming support - DISABLED: R2R streaming endpoint returns 422 errors
        // Use AddMessageAsync instead which works correctly

        private async IAsyncEnumerable<string> StreamResponseAsync(HttpResponseMessage response)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data != "[DONE]")
                    {
                        yield return data;
                    }
                }
            }
        }

        // Bulk operations
        public async Task<MessageResponse2?> DeleteMultipleMessagesAsync(string conversationId, List<string> messageIds)
        {
            try
            {
                var request = new { message_ids = messageIds };
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v3/conversations/{conversationId}/messages/bulk")
                {
                    Content = JsonContent.Create(request)
                });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<MessageResponse2?> ExportConversationAsync(string conversationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/conversations/{conversationId}/export", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageResponse2>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ConversationCreateResponse?> ImportConversationAsync(ConversationRequest request, Stream dataStream)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(JsonContent.Create(request), "conversation");
                content.Add(new StreamContent(dataStream), "data", "conversation_data.json");

                var response = await _httpClient.PostAsync("/v3/conversations/import", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationCreateResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Search within conversation
        public async Task<MessageListResponse?> SearchMessagesAsync(string conversationId, string query, int offset = 0, int limit = 100)
        {
            try
            {
                var searchRequest = new { query, offset, limit };
                var response = await _httpClient.PostAsJsonAsync($"/v3/conversations/{conversationId}/search", searchRequest);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageListResponse>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>
        /// Enhanced error handling for R2R API responses with specific error code handling
        /// </summary>
        private async Task HandleR2RErrorAsync(HttpResponseMessage response, string operation)
        {
            var statusCode = response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();

            switch (statusCode)
            {
                case HttpStatusCode.InternalServerError: // 500
                    if (content.Contains("timeout") || content.Contains("Request timed out"))
                    {
                        _logger.LogWarning("R2R API timeout in {Operation}: {Content}", operation, content);
                    }
                    else
                    {
                        _logger.LogError("R2R API internal server error in {Operation}: {Content}", operation, content);
                    }
                    break;

                case HttpStatusCode.TooManyRequests: // 429
                    _logger.LogWarning("R2R API rate limit exceeded in {Operation}: {Content}", operation, content);
                    break;

                case HttpStatusCode.UnprocessableEntity: // 422
                    _logger.LogWarning("R2R API validation error in {Operation}: {Content}", operation, content);
                    break;

                case HttpStatusCode.Unauthorized: // 401
                    _logger.LogError("R2R API authentication failed in {Operation}: {Content}", operation, content);
                    break;

                case HttpStatusCode.Forbidden: // 403
                    _logger.LogError("R2R API authorization failed in {Operation}: {Content}", operation, content);
                    break;

                case HttpStatusCode.NotFound: // 404
                    _logger.LogWarning("R2R API resource not found in {Operation}: {Content}", operation, content);
                    break;

                default:
                    _logger.LogError("R2R API error {StatusCode} in {Operation}: {Content}",
                        (int)statusCode, operation, content);
                    break;
            }
        }
    }
}
