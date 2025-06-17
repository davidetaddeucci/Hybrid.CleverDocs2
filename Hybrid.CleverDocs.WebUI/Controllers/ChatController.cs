using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Models;
using Hybrid.CleverDocs.WebUI.Services;
using System.Security.Claims;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    // JWT Authentication: Authorization handled client-side with JWT tokens
    public class ChatController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IAuthService authService, ILogger<ChatController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var companyId = User.FindFirst("CompanyId")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var model = new ChatIndexViewModel
                {
                    UserId = userId,
                    CompanyId = companyId,
                    Conversations = new List<ConversationViewModel>(),
                    AvailableCollections = new List<CollectionViewModel>(),
                    Settings = new ChatSettingsViewModel
                    {
                        SelectedCollectionIds = new List<string>(),
                        RelevanceThreshold = 0.7,
                        MaxResults = 10,
                        SearchMode = "hybrid"
                    },
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = 1,
                        PageSize = 20,
                        TotalItems = 0,
                        TotalPages = 1
                    }
                };

                // Load initial data via API calls
                await LoadConversationsAsync(model);
                await LoadCollectionsAsync(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat index");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Load conversation details via API
                var conversation = await LoadConversationDetailsAsync(id, userId);
                if (conversation == null)
                {
                    return Json(new { success = false, message = "Conversation not found" });
                }

                return PartialView("_ConversationView", conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation {ConversationId}", id);
                return Json(new { success = false, message = "Error loading conversation" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Send message via API
                var response = await SendMessageToApiAsync(request, userId);
                
                return Json(new { 
                    success = true, 
                    messageId = response.MessageId,
                    content = response.Content,
                    citations = response.Citations,
                    timestamp = response.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var response = await CreateConversationViaApiAsync(request, userId);
                
                return Json(new { 
                    success = true, 
                    conversationId = response.ConversationId,
                    title = response.Title
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                return Json(new { success = false, message = "Error creating conversation" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchConversations(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var results = await SearchConversationsViaApiAsync(query, userId, page, pageSize);
                
                return Json(new { 
                    success = true, 
                    conversations = results.Conversations,
                    pagination = results.Pagination
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching conversations");
                return Json(new { success = false, message = "Error searching conversations" });
            }
        }

        // Private helper methods
        private async Task LoadConversationsAsync(ChatIndexViewModel model)
        {
            // TODO: Implement API call to load conversations
            // This will be implemented when we create the API client
        }

        private async Task LoadCollectionsAsync(ChatIndexViewModel model)
        {
            // TODO: Implement API call to load collections
            // This will be implemented when we create the API client
        }

        private async Task<ConversationDetailViewModel?> LoadConversationDetailsAsync(string conversationId, string userId)
        {
            // TODO: Implement API call to load conversation details
            return null;
        }

        private async Task<SendMessageResponse> SendMessageToApiAsync(SendMessageRequest request, string userId)
        {
            // TODO: Implement API call to send message
            return new SendMessageResponse();
        }

        private async Task<CreateConversationResponse> CreateConversationViaApiAsync(CreateConversationRequest request, string userId)
        {
            // TODO: Implement API call to create conversation
            return new CreateConversationResponse();
        }

        private async Task<SearchConversationsResponse> SearchConversationsViaApiAsync(string query, string userId, int page, int pageSize)
        {
            // TODO: Implement API call to search conversations
            return new SearchConversationsResponse();
        }

        [HttpPost]
        public async Task<IActionResult> Export(string id, [FromBody] ExportRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var exportData = await ExportConversationAsync(id, request, userId);

                var contentType = request.Format switch
                {
                    "json" => "application/json",
                    "txt" => "text/plain",
                    "pdf" => "application/pdf",
                    _ => "application/octet-stream"
                };

                return File(exportData, contentType, $"conversation_{id}.{request.Format}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting conversation {ConversationId}", id);
                return Json(new { success = false, message = "Error exporting conversation" });
            }
        }

        private async Task<byte[]> ExportConversationAsync(string conversationId, ExportRequest request, string userId)
        {
            // TODO: Implement API call to export conversation
            return Array.Empty<byte>();
        }
    }

    // Request/Response models
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
        public List<string> Collections { get; set; } = new();
        public ChatSettingsViewModel Settings { get; set; } = new();
    }

    public class SendMessageResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<CitationViewModel> Citations { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class CreateConversationRequest
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Collections { get; set; } = new();
    }

    public class CreateConversationResponse
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class SearchConversationsResponse
    {
        public List<ConversationViewModel> Conversations { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
    }

    public class ExportRequest
    {
        public string Format { get; set; } = "json";
        public bool IncludeCitations { get; set; } = true;
    }
}
