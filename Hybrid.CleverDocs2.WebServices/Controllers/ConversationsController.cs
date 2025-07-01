using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Models.Conversations;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Middleware;
using System.Security.Claims;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    /// <summary>
    /// API Controller for managing conversations and chat functionality
    /// Based on R2R Conversations API patterns with advanced RAG features
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConversationClient _conversationClient;
        private readonly IMultiLevelCacheService _cacheService;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            ApplicationDbContext context,
            IConversationClient conversationClient,
            IMultiLevelCacheService cacheService,
            ILogger<ConversationsController> logger)
        {
            _context = context;
            _conversationClient = conversationClient;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] bool? isPinned = null)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                var query = _context.Conversations
                    .Where(c => c.UserId == userId && c.CompanyId == companyId);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.Status == status);

                if (isPinned.HasValue)
                    query = query.Where(c => c.IsPinned == isPinned.Value);

                var conversations = await query
                    .OrderByDescending(c => c.LastMessageAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ConversationDto
                    {
                        Id = c.Id,
                        R2RConversationId = c.R2RConversationId,
                        Title = c.Title,
                        Description = c.Description,
                        CollectionIds = c.GetCollectionIdsList(),
                        Status = c.Status,
                        MessageCount = c.MessageCount,
                        IsPinned = c.IsPinned,
                        LastMessageAt = c.LastMessageAt,
                        CreatedAt = c.CreatedAt,
                        Settings = c.GetSettings()
                    })
                    .ToListAsync();

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific conversation by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversation(int id)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                var conversation = await _context.Conversations
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.CompanyId == companyId);

                if (conversation == null)
                    return NotFound();

                var conversationDto = new ConversationDetailDto
                {
                    Id = conversation.Id,
                    R2RConversationId = conversation.R2RConversationId,
                    Title = conversation.Title,
                    Description = conversation.Description,
                    CollectionIds = conversation.GetCollectionIdsList(),
                    Status = conversation.Status,
                    MessageCount = conversation.MessageCount,
                    IsPinned = conversation.IsPinned,
                    LastMessageAt = conversation.LastMessageAt,
                    CreatedAt = conversation.CreatedAt,
                    Settings = conversation.GetSettings(),
                    Messages = conversation.Messages.Select(m => new MessageDto
                    {
                        Id = m.Id,
                        R2RMessageId = m.R2RMessageId,
                        Role = m.Role,
                        Content = m.Content,
                        ParentMessageId = m.ParentMessageId,
                        Citations = m.GetCitations(),
                        RagContext = m.GetRagContext(),
                        ConfidenceScore = m.ConfidenceScore,
                        ProcessingTimeMs = m.ProcessingTimeMs,
                        TokenCount = m.TokenCount,
                        Status = m.Status,
                        IsEdited = m.IsEdited,
                        CreatedAt = m.CreatedAt
                    }).ToList()
                };

                return Ok(conversationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation {ConversationId} for user {UserId}", id, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new conversation
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ConversationDto>> CreateConversation([FromBody] CreateConversationRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                // Create conversation in R2R first
                var r2rRequest = new Services.DTOs.Conversation.ConversationRequest
                {
                    Name = request.Title,
                    Description = request.Description,
                    Metadata = request.Settings ?? new Dictionary<string, object>()
                };

                var r2rResponse = await _conversationClient.CreateConversationAsync(r2rRequest);
                if (r2rResponse == null)
                {
                    return BadRequest("Failed to create conversation in R2R");
                }

                // Create conversation in our database
                var conversation = new Conversation
                {
                    R2RConversationId = r2rResponse.Results.ConversationId,
                    Title = request.Title,
                    Description = request.Description,
                    UserId = userId,
                    CompanyId = companyId,
                    Status = "active",
                    LastMessageAt = DateTime.UtcNow
                };

                if (request.CollectionIds?.Any() == true)
                {
                    conversation.SetCollectionIdsList(request.CollectionIds);
                }

                if (request.Settings?.Any() == true)
                {
                    conversation.SetSettings(request.Settings);
                }

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                var conversationDto = new ConversationDto
                {
                    Id = conversation.Id,
                    R2RConversationId = conversation.R2RConversationId,
                    Title = conversation.Title,
                    Description = conversation.Description,
                    CollectionIds = conversation.GetCollectionIdsList(),
                    Status = conversation.Status,
                    MessageCount = conversation.MessageCount,
                    IsPinned = conversation.IsPinned,
                    LastMessageAt = conversation.LastMessageAt,
                    CreatedAt = conversation.CreatedAt,
                    Settings = conversation.GetSettings()
                };

                // Cache the new conversation
                await _cacheService.SetAsync($"conversation_{conversation.Id}", conversationDto, new CacheOptions
                {
                    UseL1Cache = true,  // Fast access for active conversations
                    UseL2Cache = true,  // Redis for intensive chat sessions
                    UseL3Cache = false, // Conversations change frequently
                    L1TTL = TimeSpan.FromMinutes(10),
                    L2TTL = TimeSpan.FromMinutes(30),
                    L3TTL = TimeSpan.FromHours(1),
                    TenantId = companyId.ToString()
                });

                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Send a message to a conversation with R2R RAG capabilities
        /// </summary>
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<MessageDto>> SendMessage(int id, [FromBody] SendMessageRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                // Get conversation
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.CompanyId == companyId);

                if (conversation == null)
                    return NotFound();

                // Create user message first
                var userMessage = new Message
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = request.Content,
                    ParentMessageId = request.ParentMessageId,
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(userMessage);
                await _context.SaveChangesAsync();

                // Prepare R2R message request with advanced settings
                var conversationSettings = conversation.GetSettings();
                var messageRequest = new Services.DTOs.Conversation.MessageRequest
                {
                    Message = request.Content,
                    UseVectorSearch = (bool)(conversationSettings.GetValueOrDefault("useVectorSearch", true)),
                    UseHybridSearch = (bool)(conversationSettings.GetValueOrDefault("useHybridSearch", true)),
                    SearchLimit = (int)(conversationSettings.GetValueOrDefault("maxResults", 10)),
                    IncludeTitleIfAvailable = (bool)(conversationSettings.GetValueOrDefault("includeTitleIfAvailable", true)),
                    Stream = (bool)(conversationSettings.GetValueOrDefault("streamingEnabled", true)),
                    RagGenerationConfig = new Dictionary<string, object>
                    {
                        ["model"] = "gpt-4o-mini",
                        ["temperature"] = 0.1,
                        ["max_tokens"] = 1024
                    }
                };

                // Add advanced R2R features based on conversation settings
                if ((bool)(conversationSettings.GetValueOrDefault("agenticMode", false)))
                {
                    messageRequest.RagGenerationConfig["extended_thinking"] = true;
                    messageRequest.RagGenerationConfig["thinking_budget"] = conversationSettings.GetValueOrDefault("thinkingBudget", 4096);
                }

                if ((bool)(conversationSettings.GetValueOrDefault("useKnowledgeGraph", false)))
                {
                    messageRequest.RagGenerationConfig["use_knowledge_graph"] = true;
                }

                // Send message to R2R
                var r2rResponse = await _conversationClient.AddMessageAsync(conversation.R2RConversationId, messageRequest);

                if (r2rResponse == null)
                {
                    return BadRequest("Failed to send message to R2R");
                }

                // Create assistant message with R2R response
                var assistantMessage = new Message
                {
                    ConversationId = conversation.Id,
                    R2RMessageId = r2rResponse.Results.MessageId,
                    Role = "assistant",
                    Content = r2rResponse.Results.Content,
                    ParentMessageId = userMessage.Id,
                    Status = "completed",
                    ProcessingTimeMs = (int)(DateTime.UtcNow - userMessage.CreatedAt).TotalMilliseconds,
                    TokenCount = r2rResponse.Results.Content?.Length / 4, // Rough token estimate
                    CreatedAt = DateTime.UtcNow
                };

                // Store citations and RAG context
                if (r2rResponse.Results.SearchResults?.Any() == true)
                {
                    var citations = r2rResponse.Results.SearchResults.Select(sr => new Dictionary<string, object>
                    {
                        ["document_id"] = sr.DocumentId ?? "",
                        ["chunk_id"] = sr.ChunkId ?? "",
                        ["score"] = sr.Score,
                        ["text"] = sr.Text ?? "",
                        ["metadata"] = sr.Metadata ?? new Dictionary<string, object>()
                    }).ToList();

                    assistantMessage.SetCitations(citations);
                }

                // Store RAG context for analysis
                var ragContext = new Dictionary<string, object>
                {
                    ["search_mode"] = conversationSettings.GetValueOrDefault("searchMode", "hybrid"),
                    ["semantic_weight"] = conversationSettings.GetValueOrDefault("semanticWeight", 5.0),
                    ["full_text_weight"] = conversationSettings.GetValueOrDefault("fullTextWeight", 1.0),
                    ["collection_ids"] = conversation.GetCollectionIdsList(),
                    ["agentic_mode"] = conversationSettings.GetValueOrDefault("agenticMode", false),
                    ["knowledge_graph"] = conversationSettings.GetValueOrDefault("useKnowledgeGraph", false)
                };

                assistantMessage.SetRagContext(ragContext);

                _context.Messages.Add(assistantMessage);

                // Update conversation
                conversation.MessageCount += 2; // User + Assistant messages
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Return assistant message
                var messageDto = new MessageDto
                {
                    Id = assistantMessage.Id,
                    R2RMessageId = assistantMessage.R2RMessageId,
                    Role = assistantMessage.Role,
                    Content = assistantMessage.Content,
                    ParentMessageId = assistantMessage.ParentMessageId,
                    Citations = assistantMessage.GetCitations(),
                    RagContext = assistantMessage.GetRagContext(),
                    ConfidenceScore = assistantMessage.ConfidenceScore,
                    ProcessingTimeMs = assistantMessage.ProcessingTimeMs,
                    TokenCount = assistantMessage.TokenCount,
                    Status = assistantMessage.Status,
                    IsEdited = assistantMessage.IsEdited,
                    CreatedAt = assistantMessage.CreatedAt
                };

                // Invalidate conversation cache
                await _cacheService.RemoveAsync($"conversation_{conversation.Id}");

                return Ok(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to conversation {ConversationId} for user {UserId}", id, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Edit a message in a conversation (with history preservation)
        /// </summary>
        [HttpPut("{conversationId}/messages/{messageId}")]
        public async Task<ActionResult<MessageDto>> EditMessage(int conversationId, int messageId, [FromBody] EditMessageRequest request)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                // Verify conversation access
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.CompanyId == companyId);

                if (conversation == null)
                    return NotFound("Conversation not found");

                // Check if user has access to this conversation
                if (!conversation.HasUserAccess(userId))
                    return Forbid("Access denied to this conversation");

                // Find the message
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);

                if (message == null)
                    return NotFound("Message not found");

                // Only allow editing user's own messages or if user is admin
                if (message.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid("You can only edit your own messages");

                // Store edit history
                message.AddEditRecord(message.Content, userId, request.EditReason);

                // Update message content
                message.Content = request.NewContent;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Clear relevant caches
                await _cacheService.RemoveAsync($"conversation_{conversationId}");
                await _cacheService.InvalidateAsync($"type:conversationdto:*");

                // Return updated message
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    Role = message.Role,
                    UserId = message.UserId,
                    CreatedAt = message.CreatedAt,
                    UpdatedAt = message.UpdatedAt,
                    IsEdited = message.IsEdited,
                    LastEditedAt = message.LastEditedAt,
                    Citations = message.GetCitations(),
                    Metadata = message.GetMetadata()
                };

                return Ok(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId} in conversation {ConversationId}", messageId, conversationId);
                return StatusCode(500, "Error editing message");
            }
        }

        /// <summary>
        /// Get message edit history
        /// </summary>
        [HttpGet("{conversationId}/messages/{messageId}/history")]
        public async Task<ActionResult<List<MessageEditHistoryDto>>> GetMessageEditHistory(int conversationId, int messageId)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            try
            {
                // Verify conversation access
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.CompanyId == companyId);

                if (conversation == null)
                    return NotFound("Conversation not found");

                if (!conversation.HasUserAccess(userId))
                    return Forbid("Access denied to this conversation");

                // Find the message
                var message = await _context.Messages
                    .Include(m => m.LastEditedByUser)
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);

                if (message == null)
                    return NotFound("Message not found");

                var editHistory = message.GetEditHistory().Select(edit => new MessageEditHistoryDto
                {
                    PreviousContent = edit.PreviousContent,
                    EditedAt = edit.EditedAt,
                    EditedByUserId = edit.EditedByUserId,
                    EditReason = edit.EditReason
                }).ToList();

                return Ok(editHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit history for message {MessageId}", messageId);
                return StatusCode(500, "Error getting edit history");
            }
        }

        // Helper methods
        private Guid GetUserId()
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("No valid user ID found in claims");
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId.Value;
        }

        private Guid GetCompanyId()
        {
            var companyId = HttpContext.GetCompanyId();
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No valid company ID found in claims");
                throw new UnauthorizedAccessException("Company not found in user claims");
            }
            return companyId.Value;
        }
    }
}
