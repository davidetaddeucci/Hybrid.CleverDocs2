using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Services.Queue;

namespace Hybrid.CleverDocs2.WebServices.Services.Clients
{
    public class ConversationClient : IConversationClient
    {
        private readonly HttpClient _httpClient;
        private readonly IRateLimitingService _rateLimitingService;

        public ConversationClient(HttpClient httpClient, IRateLimitingService rateLimitingService)
        {
            _httpClient = httpClient;
            _rateLimitingService = rateLimitingService;
        }

        // Conversation CRUD operations
        public async Task<ConversationCreateResponse?> CreateConversationAsync(ConversationRequest request)
        {
            try
            {
                // Apply rate limiting for conversation operations
                await _rateLimitingService.WaitForAvailabilityAsync("r2r_conversation");

                var response = await _httpClient.PostAsJsonAsync("/v3/conversations", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ConversationCreateResponse>();
            }
            catch (HttpRequestException)
            {
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

                var response = await _httpClient.PostAsJsonAsync($"/v3/conversations/{conversationId}/messages", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MessageCreateResponse>();
            }
            catch (HttpRequestException)
            {
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

        // Streaming support
        public async Task<IAsyncEnumerable<string>?> StreamMessageAsync(string conversationId, MessageRequest request)
        {
            try
            {
                request.Stream = true; // Ensure streaming is enabled
                var response = await _httpClient.PostAsJsonAsync($"/v3/conversations/{conversationId}/messages/stream", request);
                response.EnsureSuccessStatusCode();

                return StreamResponseAsync(response);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

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
    }
}
