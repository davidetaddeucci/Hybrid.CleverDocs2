using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Search;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchClient _client;
        public SearchController(ISearchClient client) => _client = client;

        [HttpPost("search")]
        public async Task<IActionResult> Search(SearchRequest request) => Ok(await _client.SearchAsync(request));

        [HttpPost("rag")]
        public async Task<IActionResult> RAG(RAGRequest request) => Ok(await _client.RAGAsync(request));

        [HttpPost("rag/stream")]
        public async Task<IActionResult> RAGStream(RAGRequest request)
        {
            var stream = await _client.RAGStreamAsync(request);
            if (stream == null) return BadRequest();
            
            Response.ContentType = "text/event-stream";
            await foreach (var chunk in stream)
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }
            return new EmptyResult();
        }

        [HttpPost("agent")]
        public async Task<IActionResult> Agent(AgentRequest request) => Ok(await _client.AgentAsync(request));

        [HttpPost("agent/stream")]
        public async Task<IActionResult> AgentStream(AgentRequest request)
        {
            var stream = await _client.AgentStreamAsync(request);
            if (stream == null) return BadRequest();
            
            Response.ContentType = "text/event-stream";
            await foreach (var chunk in stream)
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }
            return new EmptyResult();
        }

        [HttpPost("completion")]
        public async Task<IActionResult> Completion(CompletionRequest request) => Ok(await _client.CompletionAsync(request));

        [HttpPost("embedding")]
        public async Task<IActionResult> Embedding(EmbeddingRequest request) => Ok(await _client.EmbeddingAsync(request));
    }
}
