using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.WebDev;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.WebDev;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/webdev")]
    public class WebDevController : ControllerBase
    {
        private readonly IWebDevClient _client;
        public WebDevController(IWebDevClient client) => _client = client;

        [HttpPost("projects")]
        public async Task<IActionResult> CreateProject(ProjectCreateRequest request) => Ok(await _client.CreateProjectAsync(request));

        [HttpGet("projects/{projectId}")]
        public async Task<IActionResult> GetProject(string projectId) => Ok(await _client.GetProjectAsync(projectId));

        [HttpGet("projects")]
        public async Task<IActionResult> ListProjects(int page = 1, int pageSize = 50, string? filter = null) => Ok(await _client.ListProjectsAsync(page, pageSize, filter));

        [HttpPost("builds")]
        public async Task<IActionResult> StartBuild(BuildRequest request) => Ok(await _client.StartBuildAsync(request));

        [HttpPost("deployments")]
        public async Task<IActionResult> Deploy(DeploymentRequest request) => Ok(await _client.DeployAsync(request));

        [HttpGet("projects/{projectId}/monitoring")]
        public async Task<IActionResult> GetMonitoring(string projectId, MonitoringRequest request) => Ok(await _client.GetMonitoringDataAsync(request));

        [HttpPost("projects/{projectId}/optimization")]
        public async Task<IActionResult> AnalyzeProject(string projectId, OptimizationRequest request) => Ok(await _client.AnalyzeProjectAsync(request));
    }
}
