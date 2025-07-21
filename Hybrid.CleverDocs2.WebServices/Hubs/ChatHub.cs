using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Collections;
using Hybrid.CleverDocs2.WebServices.Services.LLM;
using System.Text.Json;

namespace Hybrid.CleverDocs2.WebServices.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality with rate limiting protection
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IConversationClient _conversationClient;
        private readonly IMultiLevelCacheService _cacheService;
        private readonly ICorrelationService _correlationService;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly IUserCollectionService _collectionService;
        private readonly ILLMProviderService _llmProviderService;
        private readonly ILogger<ChatHub> _logger;
        private readonly ApplicationDbContext _context;

        public ChatHub(
            IConversationClient conversationClient,
            IMultiLevelCacheService cacheService,
            ICorrelationService correlationService,
            IRateLimitingService rateLimitingService,
            IUserCollectionService collectionService,
            ILLMProviderService llmProviderService,
            ILogger<ChatHub> logger,
            ApplicationDbContext context)
        {
            _conversationClient = conversationClient;
            _cacheService = cacheService;
            _correlationService = correlationService;
            _rateLimitingService = rateLimitingService;
            _collectionService = collectionService;
            _llmProviderService = llmProviderService;
            _logger = logger;
            _context = context;
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
        /// Send a message in a conversation with streaming support and rate limiting protection
        /// </summary>
        public async Task SendMessage(object messageData)
        {
            var userId = GetUserId();
            var correlationId = _correlationService.GetCorrelationId();

            // ✅ DEBUG: Add logging to see if method is called
            _logger.LogInformation("🔥 ChatHub.SendMessage called for user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            _logger.LogInformation("🔥 MessageData type: {Type}, Content: {Content}", messageData?.GetType().Name, System.Text.Json.JsonSerializer.Serialize(messageData));

            try
            {
                // Apply rate limiting for conversation operations
                if (!await _rateLimitingService.CanMakeRequestAsync("r2r_conversation"))
                {
                    await Clients.Caller.SendAsync("MessageError", "Rate limit exceeded. Please wait before sending another message.");
                    _logger.LogWarning("Rate limit exceeded for user {UserId} in SendMessage, CorrelationId: {CorrelationId}", userId, correlationId);
                    return;
                }

                // Consume tokens for the operation
                await _rateLimitingService.ConsumeTokensAsync("r2r_conversation");

                _logger.LogDebug("Sending message for user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);

                // Parse message data
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    System.Text.Json.JsonSerializer.Serialize(messageData));

                var content = data.GetValueOrDefault("content")?.ToString() ?? "";
                var conversationId = data.GetValueOrDefault("conversationId")?.ToString();

                // ✅ Fixed: Better collections parsing from JsonElement
                var collections = new List<string>();
                if (data.TryGetValue("collections", out var collectionsObj))
                {
                    if (collectionsObj is JsonElement collectionsElement && collectionsElement.ValueKind == JsonValueKind.Array)
                    {
                        collections = collectionsElement.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }
                }

                // ✅ ENHANCED: If no collections specified, use all user's collections as default
                if (collections.Count == 0)
                {
                    _logger.LogInformation("🔥 No collections specified, fetching all user collections as default");
                    try
                    {
                        var userCollections = await _collectionService.GetUserCollectionsAsync(userId.ToString());
                        if (userCollections?.Any() == true)
                        {
                            collections = userCollections.Select(c => c.Id.ToString()).ToList();
                            _logger.LogInformation("🔥 Using {Count} default collections: {Collections}",
                                collections.Count, string.Join(", ", collections.Take(3)) + (collections.Count > 3 ? "..." : ""));
                        }
                        else
                        {
                            _logger.LogWarning("🔥 No collections found for user {UserId}", userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "🔥 Error fetching user collections for default selection");
                    }
                }

                _logger.LogInformation("🔥 Parsed message data - Content length: {ContentLength}, ConversationId: {ConversationId}, Collections count: {CollectionsCount}",
                    content.Length, conversationId, collections.Count);
                if (collections.Count > 0)
                {
                    _logger.LogInformation("🔥 Selected collections: {Collections}", string.Join(", ", collections));
                }
                else
                {
                    _logger.LogWarning("🔥 NO COLLECTIONS AVAILABLE - R2R will not be able to search documents!");
                }

                if (string.IsNullOrEmpty(content))
                {
                    await Clients.Caller.SendAsync("MessageError", "Message content cannot be empty");
                    return;
                }

                // ✅ GAME CHANGER: Get user's LLM configuration for per-user provider selection
                var userLLMConfig = await _llmProviderService.GetUserLLMConfigurationAsync(userId);
                RagGenerationConfig ragConfig;

                if (userLLMConfig != null)
                {
                    // Use user's custom LLM configuration
                    _logger.LogInformation("🚀 Using user's custom LLM configuration: {Provider}/{Model}",
                        userLLMConfig.Provider, userLLMConfig.Model);

                    ragConfig = new RagGenerationConfig
                    {
                        Model = $"{userLLMConfig.Provider}/{userLLMConfig.Model}",
                        MaxTokens = userLLMConfig.MaxTokens,
                        Temperature = (float)userLLMConfig.Temperature,
                        TopP = userLLMConfig.TopP.HasValue ? (float)userLLMConfig.TopP.Value : null,
                        Stream = userLLMConfig.EnableStreaming,
                        ApiBase = userLLMConfig.ApiEndpoint,
                        AdditionalParameters = userLLMConfig.AdditionalParameters
                    };

                    // Update usage statistics
                    await _llmProviderService.UpdateUsageStatisticsAsync(userId);
                }
                else
                {
                    // Use system default configuration
                    var systemDefault = await _llmProviderService.GetSystemDefaultConfigurationAsync();
                    _logger.LogInformation("🔧 Using system default LLM configuration: {Provider}/{Model}, Temperature: {Temperature}",
                        systemDefault.Provider, systemDefault.Model, systemDefault.Temperature);

                    ragConfig = new RagGenerationConfig
                    {
                        Model = $"{systemDefault.Provider}/{systemDefault.Model}",
                        MaxTokens = systemDefault.MaxTokens,
                        Temperature = (float)systemDefault.Temperature,
                        TopP = systemDefault.TopP.HasValue ? (float)systemDefault.TopP.Value : null,
                        Stream = systemDefault.EnableStreaming
                    };
                }

                // Create message request with user-specific or system default LLM configuration
                var messageRequest = new MessageRequest
                {
                    Content = content,
                    Role = "user",
                    SearchMode = "advanced",
                    Stream = ragConfig.Stream,
                    RagGenerationConfig = ragConfig
                };

                // Add search settings if collections are specified
                if (collections.Count > 0)
                {
                    messageRequest.SearchSettings = new SearchSettings
                    {
                        Filters = new Dictionary<string, object>
                        {
                            { "collection_ids", collections }
                        },
                        Limit = 10,
                        UseVectorSearch = true,
                        UseHybridSearch = true
                    };
                }

                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Get R2R conversation UUID from local database ID
                    var r2rConversationId = await GetR2RConversationIdAsync(conversationId, userId);
                    if (!string.IsNullOrEmpty(r2rConversationId))
                    {
                        // Send message to existing conversation (non-streaming mode)
                        await SendMessageWithStreaming(r2rConversationId, messageRequest, userId);
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("MessageError", "Conversation not found");
                        return;
                    }
                }
                else
                {
                    // Create new conversation first
                    var newConversation = await CreateNewConversation(userId, collections);
                    if (newConversation != null)
                    {
                        await SendMessageWithStreaming(newConversation.Results.ConversationId, messageRequest, userId);
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
        /// Ping to keep connection alive and test hub functionality
        /// </summary>
        public async Task Ping()
        {
            var userId = GetUserId();
            _logger.LogInformation("🔥 ChatHub.Ping called for user {UserId} - Hub is working!", userId);
            await Clients.Caller.SendAsync("Pong", $"PONG from ChatHub at {DateTime.UtcNow:HH:mm:ss} - Hub is working!");
            _logger.LogInformation("🔥 ChatHub.Ping response sent back to caller");
        }

        // Private helper methods
        private async Task SendMessageWithStreaming(string conversationId, MessageRequest messageRequest, Guid userId)
        {
            try
            {
                // ✅ ENHANCED: Log the complete request being sent to R2R for debugging
                _logger.LogInformation("🔥 SENDING TO R2R API:");
                _logger.LogInformation("🔥   Conversation ID: {ConversationId}", conversationId);
                _logger.LogInformation("🔥   Message Content: {Content}", messageRequest.Content);
                _logger.LogInformation("🔥   Search Mode: {SearchMode}", messageRequest.SearchMode);
                _logger.LogInformation("🔥   Stream: {Stream}", messageRequest.Stream);
                _logger.LogInformation("🔥   RAG Config: {RagConfig}", System.Text.Json.JsonSerializer.Serialize(messageRequest.RagGenerationConfig));
                if (messageRequest.SearchSettings != null)
                {
                    _logger.LogInformation("🔥   Search Settings: {SearchSettings}", System.Text.Json.JsonSerializer.Serialize(messageRequest.SearchSettings));
                }

                // ✅ ENHANCED: Ensure message request has all required fields for R2R processing
                if (string.IsNullOrEmpty(messageRequest.Content))
                {
                    _logger.LogError("🔥 ERROR: Message content is empty!");
                    await Clients.Caller.SendAsync("MessageError", "Message content cannot be empty");
                    return;
                }

                // ✅ CRITICAL FIX: Get local conversation and save user message to database
                var localConversation = await GetLocalConversationByR2RIdAsync(conversationId, userId);
                if (localConversation == null)
                {
                    _logger.LogError("🔥 CRITICAL ERROR: Local conversation not found for R2R ID {ConversationId}", conversationId);
                    await Clients.Caller.SendAsync("MessageError", "Conversation not found in local database");
                    return;
                }

                // ✅ CRITICAL FIX: Save user message to database
                var userMessage = new Data.Entities.Message
                {
                    ConversationId = localConversation.Id,
                    Role = "user",
                    Content = messageRequest.Content,
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId
                };

                _context.Messages.Add(userMessage);
                await _context.SaveChangesAsync();
                _logger.LogInformation("🔥 SAVED USER MESSAGE to database: ConversationId={ConversationId}, MessageId={MessageId}", localConversation.Id, userMessage.Id);

                // ✅ ENHANCED: Ensure RAG generation config is properly set for AI processing
                if (messageRequest.RagGenerationConfig == null)
                {
                    _logger.LogWarning("🔥 WARNING: RAG generation config is null, setting defaults");
                    messageRequest.RagGenerationConfig = new RagGenerationConfig
                    {
                        Model = "gpt-4o-mini",
                        MaxTokensToSample = 1000,
                        Temperature = 0.7f
                    };
                }

                // ✅ CORRECT: Use the Agent endpoint that generates AI responses
                _logger.LogInformation("🔥 Calling SendToAgentAsync for conversation {ConversationId}", conversationId);

                // Create agent request with proper structure
                var agentRequest = new AgentRequest
                {
                    Message = new AgentMessage
                    {
                        Role = "user",
                        Content = messageRequest.Content
                    },
                    SearchMode = messageRequest.SearchMode,
                    SearchSettings = messageRequest.SearchSettings,
                    RagGenerationConfig = messageRequest.RagGenerationConfig,
                    ConversationId = conversationId,
                    Mode = "rag",
                    IncludeTitleIfAvailable = true
                };

                var response = await _conversationClient.SendToAgentAsync(agentRequest);

                _logger.LogInformation("🔥 SendToAgentAsync response: {Response}", response != null ? "SUCCESS" : "NULL");
                if (response != null)
                {
                    var agentResponse = response.Results?.AssistantMessage;
                    _logger.LogInformation("🔥 R2R Agent Response Details - ConversationId: {ConversationId}, Messages count: {MessagesCount}, Assistant message found: {HasAssistant}",
                        response.Results?.ConversationId,
                        response.Results?.Messages?.Count ?? 0,
                        agentResponse != null);

                    if (!string.IsNullOrEmpty(agentResponse?.Content))
                    {
                        _logger.LogInformation("🔥 Agent response content preview: {Content}",
                            agentResponse.Content.Substring(0, Math.Min(200, agentResponse.Content.Length)));
                    }
                    else
                    {
                        _logger.LogWarning("🔥 R2R Agent returned empty or null content!");
                    }

                    // Check if R2R agent response is empty and use fallback LLM
                    string finalContent = agentResponse?.Content ?? "";
                    if (string.IsNullOrWhiteSpace(finalContent))
                    {
                        _logger.LogWarning("🔥 R2R Agent returned empty content, using fallback LLM generation");
                        finalContent = await GenerateFallbackResponseAsync(messageRequest.Content, new List<string>());
                    }

                    // ✅ CRITICAL FIX: Save assistant message to database
                    var assistantMessage = new Data.Entities.Message
                    {
                        ConversationId = localConversation.Id,
                        R2RMessageId = Guid.NewGuid().ToString(), // Generate ID since Agent doesn't return MessageId
                        Role = "assistant",
                        Content = finalContent,
                        ParentMessageId = userMessage.Id,
                        Status = "completed",
                        ProcessingTimeMs = (int)(DateTime.UtcNow - userMessage.CreatedAt).TotalMilliseconds,
                        TokenCount = finalContent?.Length / 4, // Rough token estimate
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId
                    };

                    // Set RAG context - Agent endpoint doesn't return SearchResults, but we can set basic context
                    var ragContext = new Dictionary<string, object>
                    {
                        ["search_mode"] = messageRequest.SearchMode,
                        ["collection_filters"] = messageRequest.SearchSettings?.Filters,
                        ["conversation_id"] = response.Results.ConversationId
                    };
                    assistantMessage.SetRagContext(ragContext);

                    _context.Messages.Add(assistantMessage);

                    // ✅ CRITICAL FIX: Update conversation message count and last message time
                    localConversation.MessageCount += 2; // User + Assistant messages
                    localConversation.LastMessageAt = DateTime.UtcNow;
                    localConversation.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("🔥 SAVED ASSISTANT MESSAGE to database: ConversationId={ConversationId}, MessageId={MessageId}, MessageCount={MessageCount}",
                        localConversation.Id, assistantMessage.Id, localConversation.MessageCount);

                    _logger.LogInformation("🔥 Sending ReceiveMessage via SignalR to group conversation_{ConversationId}", conversationId);

                    await Clients.Group($"conversation_{conversationId}")
                        .SendAsync("ReceiveMessage", new
                        {
                            id = assistantMessage.R2RMessageId,
                            content = finalContent,
                            role = "assistant",
                            createdAt = DateTime.UtcNow,
                            conversationId = conversationId,
                            citations = new List<object>() // Agent endpoint doesn't return SearchResults
                        });

                    _logger.LogInformation("🔥 ReceiveMessage sent successfully via SignalR");
                }
                else
                {
                    _logger.LogWarning("🔥 AddMessageAsync returned null response");
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

        private async Task<ConversationCreateResponse?> CreateNewConversation(Guid userId, List<string> collections)
        {
            try
            {
                // Apply rate limiting for conversation creation
                if (!await _rateLimitingService.CanMakeRequestAsync("r2r_conversation"))
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId} in CreateNewConversation", userId);
                    return null;
                }

                // Consume tokens for the operation
                await _rateLimitingService.ConsumeTokensAsync("r2r_conversation");

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
                        UseL1Cache = true,  // ✅ ENABLED - Fast access for active conversations
                        UseL2Cache = true,  // ✅ ENABLED - Redis for intensive chat sessions
                        UseL3Cache = false, // ❌ DISABLED - Conversations change frequently
                        L1TTL = TimeSpan.FromMinutes(10),
                        L2TTL = TimeSpan.FromMinutes(30),
                        L3TTL = TimeSpan.FromHours(1)
                    });

                    // Notify user about new conversation
                    await Clients.Caller.SendAsync("ConversationCreated", new
                    {
                        conversationId = response.Results.ConversationId,
                        title = response.Results.Name
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

        private async Task<string?> GetR2RConversationIdAsync(string conversationId, Guid userId)
        {
            try
            {
                // Try to parse as integer (local database ID)
                if (int.TryParse(conversationId, out var localId))
                {
                    // Look up the conversation in the database to get the R2R UUID
                    var conversation = await _context.Conversations
                        .Where(c => c.Id == localId && c.UserId == userId)
                        .Select(c => c.R2RConversationId)
                        .FirstOrDefaultAsync();

                    return conversation;
                }
                else
                {
                    // If it's already a UUID format, assume it's the R2R conversation ID
                    if (Guid.TryParse(conversationId, out _))
                    {
                        return conversationId;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting R2R conversation ID for conversation {ConversationId}", conversationId);
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

        private Guid GetCompanyId()
        {
            var companyIdClaim = Context.User?.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
            {
                return Guid.Empty; // Return empty Guid for users without company
            }
            return companyId;
        }

        /// <summary>
        /// Get local conversation entity by R2R conversation ID
        /// </summary>
        private async Task<Data.Entities.Conversation?> GetLocalConversationByR2RIdAsync(string r2rConversationId, Guid userId)
        {
            try
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.R2RConversationId == r2rConversationId && c.UserId == userId);

                if (conversation == null)
                {
                    _logger.LogWarning("🔥 Local conversation not found for R2R ID {R2RConversationId} and user {UserId}", r2rConversationId, userId);
                }

                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting local conversation by R2R ID {R2RConversationId}", r2rConversationId);
                return null;
            }
        }

        /// <summary>
        /// Generate fallback response using CleverDocs LLM provider when R2R fails
        /// </summary>
        private async Task<string> GenerateFallbackResponseAsync(string userMessage, List<string> collectionIds)
        {
            try
            {
                _logger.LogInformation("🔥 Generating fallback response for message: {Message}", userMessage.Substring(0, Math.Min(50, userMessage.Length)));

                // Simple fallback response for now
                var fallbackResponse = $"I understand you're asking: \"{userMessage}\". " +
                    "I'm currently experiencing some technical difficulties with my knowledge base integration. " +
                    "Please try your question again in a moment, or contact support if the issue persists.";

                _logger.LogInformation("🔥 Generated fallback response: {Response}", fallbackResponse.Substring(0, Math.Min(100, fallbackResponse.Length)));

                return fallbackResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error generating fallback response");
                return "I'm sorry, I'm currently experiencing technical difficulties. Please try again later.";
            }
        }
    }
}
