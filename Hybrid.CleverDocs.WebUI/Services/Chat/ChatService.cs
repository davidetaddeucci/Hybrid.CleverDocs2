using Hybrid.CleverDocs.WebUI.Models;
using Hybrid.CleverDocs.WebUI.Models.Collections;
using Hybrid.CleverDocs.WebUI.Services.Collections;
using System.Text.Json;
using System.Text;

namespace Hybrid.CleverDocs.WebUI.Services.Chat
{
    /// <summary>
    /// Service for chat operations with R2R integration
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ICollectionsApiClient _collectionsClient;
        private readonly IAuthService _authService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(HttpClient httpClient, ICollectionsApiClient collectionsClient, IAuthService authService, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _collectionsClient = collectionsClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<List<ConversationViewModel>> GetConversationsAsync(int page = 1, int pageSize = 20, string? status = null, bool? isPinned = null)
        {
            try
            {
                // CRITICAL FIX: Add JWT authentication like CollectionsApiClient
                await SetAuthorizationHeaderAsync();

                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrEmpty(status))
                    queryParams.Add($"status={status}");

                if (isPinned.HasValue)
                    queryParams.Add($"isPinned={isPinned.Value}");

                var queryString = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/api/conversations?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var conversations = JsonSerializer.Deserialize<List<ConversationDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return conversations?.Select(c => new ConversationViewModel
                    {
                        Id = c.Id.ToString(),
                        Title = c.Title,
                        LastMessage = "", // Will be populated from messages
                        UpdatedAt = c.LastMessageAt,
                        MessageCount = c.MessageCount,
                        IsActive = c.Status == "active",
                        CollectionIds = c.CollectionIds,
                        Status = c.Status
                    }).ToList() ?? new List<ConversationViewModel>();
                }

                return new List<ConversationViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return new List<ConversationViewModel>();
            }
        }

        public async Task<ConversationDetailViewModel?> GetConversationAsync(int conversationId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.GetAsync($"/api/conversations/{conversationId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var conversation = JsonSerializer.Deserialize<ConversationDetailDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (conversation != null)
                    {
                        return new ConversationDetailViewModel
                        {
                            Id = conversation.Id.ToString(),
                            Title = conversation.Title,
                            Description = conversation.Description,
                            CollectionIds = conversation.CollectionIds,
                            Status = conversation.Status,
                            MessageCount = conversation.MessageCount,
                            IsPinned = conversation.IsPinned,
                            LastMessageAt = conversation.LastMessageAt,
                            CreatedAt = conversation.CreatedAt,
                            Settings = MapToSettingsViewModel(conversation.Settings),
                            Messages = conversation.Messages.Select(m => new MessageViewModel
                            {
                                Id = m.Id.ToString(),
                                Role = m.Role,
                                Content = m.Content,
                                ParentMessageId = m.ParentMessageId?.ToString(),
                                Citations = m.Citations?.Select(c => new CitationViewModel
                                {
                                    Id = c.TryGetValue("id", out var id) ? id.ToString() : "",
                                    DocumentId = c.TryGetValue("document_id", out var docId) ? docId.ToString() : "",
                                    DocumentName = c.TryGetValue("document_name", out var docName) ? docName.ToString() : "",
                                    Text = c.TryGetValue("text", out var text) ? text.ToString() : "",
                                    Score = c.TryGetValue("score", out var score) ? Convert.ToDouble(score) : 0.0
                                }).ToList() ?? new(),
                                RagContext = new(),
                                ConfidenceScore = m.ConfidenceScore,
                                ProcessingTimeMs = m.ProcessingTimeMs,
                                TokenCount = m.TokenCount,
                                Status = m.Status,
                                IsEdited = m.IsEdited,
                                Timestamp = m.CreatedAt
                            }).ToList()
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId}", conversationId);
                return null;
            }
        }

        public async Task<ConversationViewModel> CreateConversationAsync(string title, string? description = null, List<string>? collectionIds = null, Dictionary<string, object>? settings = null)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var request = new CreateConversationRequest
                {
                    Title = title,
                    Description = description,
                    CollectionIds = collectionIds,
                    Settings = settings
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/conversations", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var conversation = JsonSerializer.Deserialize<ConversationDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (conversation != null)
                    {
                        return new ConversationViewModel
                        {
                            Id = conversation.Id.ToString(),
                            Title = conversation.Title,
                            LastMessage = "",
                            UpdatedAt = conversation.LastMessageAt,
                            MessageCount = conversation.MessageCount,
                            IsActive = conversation.Status == "active",
                            CollectionIds = conversation.CollectionIds,
                            Status = conversation.Status
                        };
                    }
                }

                throw new Exception("Failed to create conversation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                throw;
            }
        }

        public async Task<MessageViewModel> SendMessageAsync(int conversationId, string content, int? parentMessageId = null, Dictionary<string, object>? ragConfig = null)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var request = new SendMessageRequest
                {
                    Content = content,
                    ParentMessageId = parentMessageId,
                    RagConfig = ragConfig
                };

                var json = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/conversations/{conversationId}/messages", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var message = JsonSerializer.Deserialize<MessageDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (message != null)
                    {
                        return new MessageViewModel
                        {
                            Id = message.Id.ToString(),
                            Role = message.Role,
                            Content = message.Content,
                            ParentMessageId = message.ParentMessageId?.ToString(),
                            Citations = message.Citations?.Select(c => new CitationViewModel
                            {
                                Id = c.TryGetValue("id", out var id) ? id.ToString() : "",
                                DocumentId = c.TryGetValue("document_id", out var docId) ? docId.ToString() : "",
                                DocumentName = c.TryGetValue("document_name", out var docName) ? docName.ToString() : "",
                                Text = c.TryGetValue("text", out var text) ? text.ToString() : "",
                                Score = c.TryGetValue("score", out var score) ? Convert.ToDouble(score) : 0.0
                            }).ToList() ?? new(),
                            RagContext = new(),
                            ConfidenceScore = message.ConfidenceScore,
                            ProcessingTimeMs = message.ProcessingTimeMs,
                            TokenCount = message.TokenCount,
                            Status = message.Status,
                            IsEdited = message.IsEdited,
                            Timestamp = message.CreatedAt
                        };
                    }
                }

                throw new Exception("Failed to send message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<List<Models.CollectionViewModel>> GetAvailableCollectionsAsync()
        {
            try
            {
                var searchRequest = new Models.Collections.CollectionSearchViewModel
                {
                    Page = 1,
                    PageSize = 100
                };
                var result = await _collectionsClient.SearchCollectionsAsync(searchRequest);
                return result.Items.Select(c => new Models.CollectionViewModel
                {
                    Id = c.Id.ToString(),
                    Name = c.Name,
                    Description = c.Description ?? string.Empty,
                    DocumentCount = c.DocumentCount // FIXED: Include document count from API response
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available collections");
                return new List<Models.CollectionViewModel>();
            }
        }

        // Helper methods
        private ChatSettingsViewModel MapToSettingsViewModel(Dictionary<string, object> settings)
        {
            return new ChatSettingsViewModel
            {
                SelectedCollectionIds = new List<string>(),
                RelevanceThreshold = GetSettingValue<double>(settings, "relevanceThreshold", 0.7),
                MaxResults = GetSettingValue<int>(settings, "maxResults", 10),
                SearchMode = GetSettingValue<string>(settings, "searchMode", "hybrid"),
                UseVectorSearch = GetSettingValue<bool>(settings, "useVectorSearch", true),
                UseHybridSearch = GetSettingValue<bool>(settings, "useHybridSearch", true),
                IncludeTitleIfAvailable = GetSettingValue<bool>(settings, "includeTitleIfAvailable", true),
                RagGenerationConfig = settings.ContainsKey("ragGenerationConfig") ? 
                    (Dictionary<string, object>)settings["ragGenerationConfig"] : 
                    new Dictionary<string, object>()
            };
        }

        private T GetSettingValue<T>(Dictionary<string, object> settings, string key, T defaultValue)
        {
            if (settings.ContainsKey(key) && settings[key] is T value)
                return value;
            return defaultValue;
        }

        public async Task<MessageViewModel> EditMessageAsync(int messageId, string newContent)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var request = new EditMessageRequest
                {
                    NewContent = newContent,
                    EditReason = "User edit"
                };

                var json = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                // Note: We need the conversation ID for the endpoint, but it's not provided in the interface
                // This is a limitation that should be addressed in the interface design
                var response = await _httpClient.PutAsync($"/api/conversations/0/messages/{messageId}", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var messageDto = JsonSerializer.Deserialize<MessageDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (messageDto != null)
                    {
                        return new MessageViewModel
                        {
                            Id = messageDto.Id.ToString(),
                            Content = messageDto.Content,
                            Role = messageDto.Role,
                            Timestamp = messageDto.UpdatedAt,
                            IsEdited = messageDto.IsEdited,
                            LastEditedAt = messageDto.LastEditedAt,
                            Citations = messageDto.Citations?.Select(c => new CitationViewModel
                            {
                                DocumentId = c.GetValueOrDefault("document_id")?.ToString() ?? "",
                                ChunkId = c.GetValueOrDefault("chunk_id")?.ToString() ?? "",
                                Score = Convert.ToDouble(c.GetValueOrDefault("score", 0.0)),
                                Text = c.GetValueOrDefault("text")?.ToString() ?? ""
                            }).ToList() ?? new List<CitationViewModel>()
                        };
                    }
                }

                return new MessageViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", messageId);
                return new MessageViewModel();
            }
        }

        public async Task<List<MessageEditHistoryViewModel>> GetMessageEditHistoryAsync(int messageId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                // Note: Same limitation as above - need conversation ID
                var response = await _httpClient.GetAsync($"/api/conversations/0/messages/{messageId}/history");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var historyDtos = JsonSerializer.Deserialize<List<MessageEditHistoryDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return historyDtos?.Select(h => new MessageEditHistoryViewModel
                    {
                        PreviousContent = h.PreviousContent,
                        EditedAt = h.EditedAt,
                        EditedByUserId = h.EditedByUserId.ToString(),
                        EditReason = h.EditReason
                    }).ToList() ?? new List<MessageEditHistoryViewModel>();
                }

                return new List<MessageEditHistoryViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit history for message {MessageId}", messageId);
                return new List<MessageEditHistoryViewModel>();
            }
        }

        // Placeholder implementations for remaining interface methods
        public Task<bool> UpdateConversationSettingsAsync(int conversationId, ChatSettingsViewModel settings) => Task.FromResult(false);
        public Task<bool> TogglePinConversationAsync(int conversationId) => Task.FromResult(false);
        public Task<bool> ArchiveConversationAsync(int conversationId) => Task.FromResult(false);
        public Task<bool> DeleteConversationAsync(int conversationId) => Task.FromResult(false);
        public Task<ConversationSearchResultViewModel> SearchConversationsAsync(ConversationSearchViewModel searchRequest) => Task.FromResult(new ConversationSearchResultViewModel());
        public Task<ConversationStatsViewModel> GetConversationStatsAsync() => Task.FromResult(new ConversationStatsViewModel());
        public Task<byte[]> ExportConversationAsync(int conversationId, string format = "json") => Task.FromResult(Array.Empty<byte>());
        public Task<bool> ValidateConversationAccessAsync(int conversationId) => Task.FromResult(true);
        public Task<List<ConversationBranchViewModel>> GetConversationBranchesAsync(int conversationId) => Task.FromResult(new List<ConversationBranchViewModel>());
        public Task<ConversationViewModel> CreateConversationBranchAsync(int conversationId, int fromMessageId, string title) => Task.FromResult(new ConversationViewModel());
        public Task<List<MessageViewModel>> GetMessageThreadAsync(int messageId) => Task.FromResult(new List<MessageViewModel>());

        public Task<bool> RateMessageAsync(int messageId, int rating, string? feedback = null) => Task.FromResult(false);
        public Task<ConversationAnalyticsViewModel> GetConversationAnalyticsAsync(int conversationId) => Task.FromResult(new ConversationAnalyticsViewModel());
        public Task<R2RStatusViewModel> GetR2RStatusAsync() => Task.FromResult(new R2RStatusViewModel());

        #region Helper Methods

        /// <summary>
        /// Sets the Authorization header with JWT token from authentication service
        /// Same pattern as CollectionsApiClient for consistency
        /// </summary>
        private async Task SetAuthorizationHeaderAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("Authorization header set with JWT token for chat API call");
                }
                else
                {
                    _logger.LogWarning("No JWT token available for chat API call");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting authorization header for chat API call");
            }
        }

        #endregion
    }

    // DTOs for API communication
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> CollectionIds { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public bool IsPinned { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class ConversationDetailDto : ConversationDto
    {
        public List<MessageDto> Messages { get; set; } = new();
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? ParentMessageId { get; set; }
        public List<Dictionary<string, object>> Citations { get; set; } = new();
        public Dictionary<string, object> RagContext { get; set; } = new();
        public double? ConfidenceScore { get; set; }
        public int? ProcessingTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }

    public class CreateConversationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? CollectionIds { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public int? ParentMessageId { get; set; }
        public Dictionary<string, object>? RagConfig { get; set; }
    }

    public class EditMessageRequest
    {
        public string NewContent { get; set; } = string.Empty;
        public string? EditReason { get; set; }
    }

    public class MessageEditHistoryDto
    {
        public string PreviousContent { get; set; } = string.Empty;
        public DateTime EditedAt { get; set; }
        public Guid EditedByUserId { get; set; }
        public string? EditReason { get; set; }
    }
}
