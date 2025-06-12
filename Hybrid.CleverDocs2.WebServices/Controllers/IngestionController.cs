using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Ingestion;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/ingestion")]
    public class IngestionController : ControllerBase
    {
        private readonly IIngestionClient _client;
        public IngestionController(IIngestionClient client) => _client = client;

        // Ingestion CRUD operations
        [HttpPost]
        public async Task<IActionResult> CreateIngestion(IngestionRequest request) => Ok(await _client.CreateIngestionAsync(request));

        [HttpGet("{ingestionId}")]
        public async Task<IActionResult> GetIngestion(string ingestionId) => Ok(await _client.GetIngestionAsync(ingestionId));

        [HttpGet]
        public async Task<IActionResult> ListIngestions([FromQuery] IngestionListRequest? request = null) => Ok(await _client.ListIngestionsAsync(request));

        [HttpPut("{ingestionId}")]
        public async Task<IActionResult> UpdateIngestion(string ingestionId, IngestionUpdateRequest request) => Ok(await _client.UpdateIngestionAsync(ingestionId, request));

        [HttpDelete("{ingestionId}")]
        public async Task<IActionResult> DeleteIngestion(string ingestionId)
        {
            await _client.DeleteIngestionAsync(ingestionId);
            return NoContent();
        }

        // Ingestion status and monitoring
        [HttpPost("status")]
        public async Task<IActionResult> GetIngestionStatus(IngestionStatusRequest request) => Ok(await _client.GetIngestionStatusAsync(request));

        [HttpGet("stats")]
        public async Task<IActionResult> GetIngestionStats() => Ok(await _client.GetIngestionStatsAsync());

        [HttpGet("{ingestionId}/logs")]
        public async Task<IActionResult> GetIngestionLogs(string ingestionId) => Ok(await _client.GetIngestionLogsAsync(ingestionId));

        // Ingestion control operations
        [HttpPost("retry")]
        public async Task<IActionResult> RetryIngestions(IngestionRetryRequest request) => Ok(await _client.RetryIngestionsAsync(request));

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelIngestions(IngestionCancelRequest request) => Ok(await _client.CancelIngestionsAsync(request));

        // Streaming support for real-time updates
        [HttpPost("stream")]
        public async Task<IActionResult> StreamIngestionUpdates([FromBody] List<string> ingestionIds)
        {
            var stream = await _client.StreamIngestionUpdatesAsync(ingestionIds);
            if (stream == null)
                return BadRequest("Failed to start streaming");

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            await foreach (var ingestionUpdate in stream)
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(ingestionUpdate)}\n\n");
                await Response.Body.FlushAsync();
            }

            return new EmptyResult();
        }

        // Bulk operations
        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultipleIngestions([FromBody] List<string> ingestionIds) => Ok(await _client.DeleteMultipleIngestionsAsync(ingestionIds));

        [HttpGet("document/{documentId}")]
        public async Task<IActionResult> GetIngestionsByDocument(string documentId) => Ok(await _client.GetIngestionsByDocumentAsync(documentId));

        [HttpGet("stats/status/{status}")]
        public async Task<IActionResult> GetIngestionStatsByStatus(string status) => Ok(await _client.GetIngestionStatsByStatusAsync(status));

        // Pipeline management
        [HttpPost("pipeline/pause")]
        public async Task<IActionResult> PauseIngestionPipeline() => Ok(await _client.PauseIngestionPipelineAsync());

        [HttpPost("pipeline/resume")]
        public async Task<IActionResult> ResumeIngestionPipeline() => Ok(await _client.ResumeIngestionPipelineAsync());

        [HttpGet("pipeline/status")]
        public async Task<IActionResult> GetIngestionPipelineStatus() => Ok(await _client.GetIngestionPipelineStatusAsync());

        // Configuration management
        [HttpPut("config")]
        public async Task<IActionResult> UpdateIngestionConfig(IngestionConfig config) => Ok(await _client.UpdateIngestionConfigAsync(config));

        [HttpGet("config")]
        public async Task<IActionResult> GetIngestionConfig() => Ok(await _client.GetIngestionConfigAsync());
    }
}
