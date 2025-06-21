using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Models;
using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.Services.Chat;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    // JWT Authentication: Authorization handled client-side with JWT tokens
    public class ChatController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IAuthService authService, IChatService chatService, ILogger<ChatController> logger)
        {
            _authService = authService;
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("ChatController.Index called");
            try
            {
                // Get user info from AuthService (like Collections does)
                var currentUser = await _authService.GetCurrentUserAsync();
                _logger.LogInformation("Current user: {UserId}", currentUser?.Id);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var model = new ChatIndexViewModel
                {
                    UserId = currentUser.Id.ToString(),
                    CompanyId = currentUser.CompanyId?.ToString() ?? "default",
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
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Load conversation details via API
                var conversation = await LoadConversationDetailsAsync(id, currentUser.Id.ToString());
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
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Send message via API
                var response = await SendMessageToApiAsync(request, currentUser.Id.ToString());

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
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var response = await CreateConversationViaApiAsync(request, currentUser.Id.ToString());

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
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var results = await SearchConversationsViaApiAsync(query, currentUser.Id.ToString(), page, pageSize);

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

        // Private helper methods - Now using ChatService
        private async Task LoadConversationsAsync(ChatIndexViewModel model)
        {
            try
            {
                var conversations = await _chatService.GetConversationsAsync(
                    model.Pagination.CurrentPage,
                    model.Pagination.PageSize);

                model.Conversations = conversations;
                model.Pagination.TotalItems = conversations.Count;
                model.Pagination.TotalPages = (int)Math.Ceiling((double)conversations.Count / model.Pagination.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversations");
                model.Conversations = new List<ConversationViewModel>();
            }
        }

        private async Task LoadCollectionsAsync(ChatIndexViewModel model)
        {
            try
            {
                var collections = await _chatService.GetAvailableCollectionsAsync();
                model.AvailableCollections = collections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading collections");
                model.AvailableCollections = new List<CollectionViewModel>();
            }
        }

        private async Task<ConversationDetailViewModel?> LoadConversationDetailsAsync(string conversationId, string userId)
        {
            try
            {
                if (int.TryParse(conversationId, out var id))
                {
                    return await _chatService.GetConversationAsync(id);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation details for {ConversationId}", conversationId);
                return null;
            }
        }

        private async Task<SendMessageResponse> SendMessageToApiAsync(SendMessageRequest request, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ConversationId) || !int.TryParse(request.ConversationId, out var conversationId))
                {
                    throw new ArgumentException("Invalid conversation ID");
                }

                var message = await _chatService.SendMessageAsync(conversationId, request.Content);

                return new SendMessageResponse
                {
                    MessageId = message.Id,
                    Content = message.Content,
                    Citations = message.Citations,
                    Timestamp = message.Timestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message via API");
                throw;
            }
        }

        private async Task<CreateConversationResponse> CreateConversationViaApiAsync(CreateConversationRequest request, string userId)
        {
            try
            {
                var conversation = await _chatService.CreateConversationAsync(
                    request.Title,
                    null,
                    request.Collections);

                return new CreateConversationResponse
                {
                    ConversationId = conversation.Id,
                    Title = conversation.Title
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation via API");
                throw;
            }
        }

        private async Task<SearchConversationsResponse> SearchConversationsViaApiAsync(string query, string userId, int page, int pageSize)
        {
            try
            {
                var searchRequest = new ConversationSearchViewModel
                {
                    Query = query,
                    Page = page,
                    PageSize = pageSize
                };

                var results = await _chatService.SearchConversationsAsync(searchRequest);

                return new SearchConversationsResponse
                {
                    Conversations = results.Conversations,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = results.Page,
                        PageSize = results.PageSize,
                        TotalItems = results.TotalCount,
                        TotalPages = results.TotalPages
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching conversations via API");
                return new SearchConversationsResponse();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Export(string id, [FromBody] ExportRequest request)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var exportData = await ExportConversationAsync(id, request, currentUser.Id.ToString());

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
            try
            {
                if (int.TryParse(conversationId, out var id))
                {
                    return await _chatService.ExportConversationAsync(id, request.Format);
                }
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting conversation {ConversationId}", conversationId);
                return Array.Empty<byte>();
            }
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
