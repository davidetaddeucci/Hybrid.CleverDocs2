using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;
using Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentClient _client;
        public DocumentController(IDocumentClient client) => _client = client;

        [HttpPost]
        public async Task<IActionResult> Create(DocumentRequest request) => Ok(await _client.CreateAsync(request));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) => Ok(await _client.GetAsync(id));

        [HttpGet]
        public async Task<IActionResult> List() => Ok(await _client.ListAsync());

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, DocumentRequest request) => Ok(await _client.UpdateAsync(id, request));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _client.DeleteAsync(id);
            return NoContent();
        }
    }
}
