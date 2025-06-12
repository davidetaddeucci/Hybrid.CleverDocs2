using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/tools")]
    public class ToolsController : ControllerBase
    {
        private readonly IToolsClient _client;
        public ToolsController(IToolsClient client) => _client = client;

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteTool(ToolsRequest request) => Ok(await _client.ExecuteToolAsync(request));

        [HttpPost("register")]
        public async Task<IActionResult> RegisterTool(ToolRegistrationRequest request) => Ok(await _client.RegisterToolAsync(request));

        [HttpGet("{toolId}")]
        public async Task<IActionResult> GetTool(string toolId) => Ok(await _client.GetToolAsync(toolId));

        [HttpGet]
        public async Task<IActionResult> ListTools([FromQuery] ToolListRequest request) => Ok(await _client.ListToolsAsync(request));

        [HttpPut("{toolId}")]
        public async Task<IActionResult> UpdateTool(string toolId, ToolRegistrationRequest request) => Ok(await _client.UpdateToolAsync(toolId, request));

        [HttpDelete("{toolId}")]
        public async Task<IActionResult> DeleteTool(string toolId) => Ok(await _client.DeleteToolAsync(toolId));
    }
}
