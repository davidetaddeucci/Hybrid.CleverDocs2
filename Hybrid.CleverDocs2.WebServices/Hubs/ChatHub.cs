using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;

namespace Hybrid.CleverDocs2.WebServices.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IConversationClient _conversationClient;
        private readonly IMultiLevelCacheService _cacheService;
        private readonly ICorrelationService _correlationService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IConversationClient conversationClient,
            IMultiLevelCacheService cacheService,
            ICorrelationService correlationService,
            ILogger<ChatHub> logger)
        {
            _conversationClient = conversationClient;
            _cacheService = cacheService;
            _correlationService = correlationService;
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                _logger.LogInformation("User {UserId} connected to ChatHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                    userId, Context.ConnectionId, correlationId);

                // Join user-specific group for targeted updates
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatHub OnConnectedAsync for user {UserId}, CorrelationId: {CorrelationId}", 
                    userId, correlationId);
            }
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                _logger.LogInformation("User {UserId} disconnected from ChatHub, ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}", 
                    userId, Context.ConnectionId, correlationId);

                // Leave all groups
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatHub OnDisconnectedAsync for user {UserId}, CorrelationId: {CorrelationId}", 
                    userId, correlationId);
            }
        }

        /// <summary>
        /// Send a message in a conversation with streaming support
        /// </summary>
        public async Task SendMessage(object messageData)
        {
            var userId = GetUserId();
            var correlationId = _correlationService.GetCorrelationId();

            try
            {
                _logger.LogDebug("Sending message for user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);

                // Parse message data
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(messageData));

                var content = data.GetValueOrDefault("content")?.ToString() ?? "";
                var conversationId = data.GetValueOrDefault("conversationId")?.ToString();
                var collections = data.GetValueOrDefault("collections") as List<string> ?? new List<string>();

                if (string.IsNullOrEmpty(content))
                {
                    await Clients.Caller.SendAsync("MessageError", "Message content cannot be empty");
                    return;
                }

                // Create message request
                var messageRequest = new MessageRequest
                {
                    Message = content,
                    UseVectorSearch = true,
                    UseHybridSearch = true,
                    SearchLimit = 10,
                    IncludeTitleIfAvailable = true
                };

                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Send message to existing conversation with streaming
                    await SendMessageWithStreaming(conversationId, messageRequest, userId.ToString());
                }
                else
                {
                    // Create new conversation first
                    var newConversation = await CreateNewConversation(userId.ToString(), collections);
                    if (newConversation != null)
                    {
                        await SendMessageWithStreaming(newConversation.Results.ConversationId, messageRequest, userId.ToString());
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("MessageError", "Failed to create new conversation");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
                await Clients.Caller.SendAsync("MessageError", "An error occurred while sending the message");
            }
        }

        /// <summary>
        /// Join a conversation room for real-time updates
        /// </summary>
        public async Task JoinConversation(string conversationId)
        {
            var userId = GetUserId();

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                _logger.LogDebug("User {UserId} joined conversation {ConversationId}", userId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining conversation {ConversationId} for user {UserId}", conversationId, userId);
            }
        }

        /// <summary>
        /// Leave a conversation room
        /// </summary>
        public async Task LeaveConversation(string conversationId)
        {
            var userId = GetUserId();

            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                _logger.LogDebug("User {UserId} left conversation {ConversationId}", userId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving conversation {ConversationId} for user {UserId}", conversationId, userId);
            }
        }

        /// <summary>
        /// Send typing indicator to other users in the conversation
        /// </summary>
        public async Task SendTypingIndicator(string conversationId, bool isTyping)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            try
            {
                await Clients.OthersInGroup($"conversation_{conversationId}")
                    .SendAsync("TypingIndicator", conversationId, userId.ToString(), userName, isTyping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing indicator for conversation {ConversationId}, User: {UserId}", 
                    conversationId, userId);
            }
        }

        /// <summary>
        /// Ping to keep connection alive
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        // Private helper methods
        private async Task SendMessageWithStreaming(string conversationId, MessageRequest messageRequest, string userId)
        {
            try
            {
                // Check if streaming is supported
                var streamingResponse = await _conversationClient.StreamMessageAsync(conversationId, messageRequest);
                
                if (streamingResponse != null)
                {
                    // Handle streaming response
                    await foreach (var chunk in streamingResponse)
                    {
                        await Clients.Group($"conversation_{conversationId}")
                            .SendAsync("MessageStreaming", new
                            {
                                messageId = Guid.NewGuid().ToString(),
                                content = chunk,
                                isComplete = false,
                                conversationId = conversationId
                            });
                    }

                    // Send completion signal
                    await Clients.Group($"conversation_{conversationId}")
                        .SendAsync("MessageStreaming", new
                        {
                            messageId = Guid.NewGuid().ToString(),
                            content = "",
                            isComplete = true,
                            conversationId = conversationId
                        });
                }
                else
                {
                    // Fallback to regular message sending
                    var response = await _conversationClient.AddMessageAsync(conversationId, messageRequest);
                    
                    if (response != null)
                    {
                        await Clients.Group($"conversation_{conversationId}")
                            .SendAsync("ReceiveMessage", new
                            {
                                id = response.Results.MessageId,
                                content = response.Results.Content,
                                role = "assistant",
                                createdAt = DateTime.UtcNow,
                                conversationId = conversationId,
                                citations = response.Results.SearchResults?.Cast<object>().ToList() ?? new List<object>()
                            });
                    }
                }

                // Notify conversation update
                await Clients.Group($"conversation_{conversationId}")
                    .SendAsync("ConversationUpdated", conversationId, "message_added");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming message for conversation {ConversationId}", conversationId);
                await Clients.Caller.SendAsync("MessageError", "Failed to send message");
            }
        }

        private async Task<ConversationCreateResponse?> CreateNewConversation(string userId, List<string> collections)
        {
            try
            {
                var request = new ConversationRequest
                {
                    Name = "New Conversation",
                    Description = "Created from chat interface"
                };

                var response = await _conversationClient.CreateConversationAsync(request);
                
                if (response != null)
                {
                    // Cache the new conversation
                    await _cacheService.SetAsync($"conversation_{response.Results.ConversationId}", response, new Services.Cache.CacheOptions
                    {
                        L1TTL = TimeSpan.FromMinutes(10),
                        L2TTL = TimeSpan.FromMinutes(30),
                        L3TTL = TimeSpan.FromHours(1),
                        UseL3Cache = false
                    });

                    // Notify user about new conversation
                    await Clients.Caller.SendAsync("ConversationCreated", new
                    {
                        conversationId = response.Results.ConversationId,
                        title = request.Name
                    });
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new conversation for user {UserId}", userId);
                return null;
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Guid.Empty; // Return empty Guid for anonymous users
            }
            return userId;
        }

        private string GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        }

        private string GetCompanyId()
        {
            return Context.User?.FindFirst("CompanyId")?.Value ?? "";
        }
    }
}
