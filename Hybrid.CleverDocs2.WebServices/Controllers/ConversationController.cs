using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Conversation;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationClient _client;
        public ConversationController(IConversationClient client) => _client = client;

        // Conversation CRUD operations
        [HttpPost]
        public async Task<IActionResult> CreateConversation(ConversationRequest request) => Ok(await _client.CreateConversationAsync(request));

        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetConversation(string conversationId) => Ok(await _client.GetConversationAsync(conversationId));

        [HttpGet]
        public async Task<IActionResult> ListConversations([FromQuery] ConversationListRequest? request = null) => Ok(await _client.ListConversationsAsync(request));

        [HttpPut("{conversationId}")]
        public async Task<IActionResult> UpdateConversation(string conversationId, ConversationUpdateRequest request) => Ok(await _client.UpdateConversationAsync(conversationId, request));

        [HttpDelete("{conversationId}")]
        public async Task<IActionResult> DeleteConversation(string conversationId)
        {
            await _client.DeleteConversationAsync(conversationId);
            return NoContent();
        }

        // Message operations
        [HttpPost("{conversationId}/messages")]
        public async Task<IActionResult> AddMessage(string conversationId, MessageRequest request) => Ok(await _client.AddMessageAsync(conversationId, request));

        [HttpGet("{conversationId}/messages/{messageId}")]
        public async Task<IActionResult> GetMessage(string conversationId, string messageId) => Ok(await _client.GetMessageAsync(conversationId, messageId));

        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> ListMessages(string conversationId, [FromQuery] int offset = 0, [FromQuery] int limit = 100) => Ok(await _client.ListMessagesAsync(conversationId, offset, limit));

        [HttpPut("{conversationId}/messages/{messageId}")]
        public async Task<IActionResult> UpdateMessage(string conversationId, string messageId, MessageRequest request) => Ok(await _client.UpdateMessageAsync(conversationId, messageId, request));

        [HttpDelete("{conversationId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(string conversationId, string messageId)
        {
            await _client.DeleteMessageAsync(conversationId, messageId);
            return NoContent();
        }

        // Conversation branching
        [HttpPost("{conversationId}/branch")]
        public async Task<IActionResult> BranchConversation(string conversationId, ConversationBranchRequest request) => Ok(await _client.BranchConversationAsync(conversationId, request));

        // Conversation analytics
        [HttpGet("{conversationId}/stats")]
        public async Task<IActionResult> GetConversationStats(string conversationId) => Ok(await _client.GetConversationStatsAsync(conversationId));

        // Streaming support
        [HttpPost("{conversationId}/messages/stream")]
        public async Task<IActionResult> StreamMessage(string conversationId, MessageRequest request)
        {
            var stream = await _client.StreamMessageAsync(conversationId, request);
            if (stream == null)
                return BadRequest("Failed to start streaming");

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            await foreach (var chunk in stream)
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }

            return new EmptyResult();
        }

        // Bulk operations
        [HttpDelete("{conversationId}/messages/bulk")]
        public async Task<IActionResult> DeleteMultipleMessages(string conversationId, [FromBody] List<string> messageIds) => Ok(await _client.DeleteMultipleMessagesAsync(conversationId, messageIds));

        [HttpPost("{conversationId}/export")]
        public async Task<IActionResult> ExportConversation(string conversationId) => Ok(await _client.ExportConversationAsync(conversationId));

        [HttpPost("import")]
        public async Task<IActionResult> ImportConversation([FromForm] ConversationRequest request, IFormFile dataFile)
        {
            if (dataFile == null || dataFile.Length == 0)
                return BadRequest("Data file is required");

            using var stream = dataFile.OpenReadStream();
            var result = await _client.ImportConversationAsync(request, stream);
            return Ok(result);
        }

        // Search within conversation
        [HttpPost("{conversationId}/search")]
        public async Task<IActionResult> SearchMessages(string conversationId, [FromBody] dynamic searchRequest)
        {
            var query = searchRequest.query?.ToString() ?? "";
            var offset = searchRequest.offset ?? 0;
            var limit = searchRequest.limit ?? 100;
            
            return Ok(await _client.SearchMessagesAsync(conversationId, query, offset, limit));
        }
    }
}
