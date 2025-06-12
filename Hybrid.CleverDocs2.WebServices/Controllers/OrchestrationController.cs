using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Orchestration;
using System.Threading.Tasks;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/orchestration")]
    public class OrchestrationController : ControllerBase
    {
        private readonly IOrchestrationClient _client;
        public OrchestrationController(IOrchestrationClient client) => _client = client;

        // Workflow definition management
        [HttpPost("workflows")]
        public async Task<IActionResult> CreateWorkflow(WorkflowDefinitionRequest request) => Ok(await _client.CreateWorkflowAsync(request));

        [HttpGet("workflows/{workflowId}")]
        public async Task<IActionResult> GetWorkflow(string workflowId) => Ok(await _client.GetWorkflowAsync(workflowId));

        [HttpGet("workflows")]
        public async Task<IActionResult> ListWorkflows([FromQuery] WorkflowListRequest request) => Ok(await _client.ListWorkflowsAsync(request));

        [HttpPut("workflows/{workflowId}")]
        public async Task<IActionResult> UpdateWorkflow(string workflowId, WorkflowDefinitionRequest request) => Ok(await _client.UpdateWorkflowAsync(workflowId, request));

        [HttpDelete("workflows/{workflowId}")]
        public async Task<IActionResult> DeleteWorkflow(string workflowId) => Ok(await _client.DeleteWorkflowAsync(workflowId));

        // Workflow execution
        [HttpPost("executions")]
        public async Task<IActionResult> ExecuteWorkflow(WorkflowExecutionRequest request) => Ok(await _client.ExecuteWorkflowAsync(request));

        [HttpGet("executions/{executionId}")]
        public async Task<IActionResult> GetExecutionStatus(string executionId) => Ok(await _client.GetExecutionStatusAsync(executionId));

        [HttpGet("executions")]
        public async Task<IActionResult> ListExecutions([FromQuery] ExecutionListRequest request) => Ok(await _client.ListExecutionsAsync(request));

        [HttpPost("executions/{executionId}/cancel")]
        public async Task<IActionResult> CancelExecution(string executionId) => Ok(await _client.CancelExecutionAsync(executionId));

        [HttpPost("executions/{executionId}/retry")]
        public async Task<IActionResult> RetryExecution(string executionId) => Ok(await _client.RetryExecutionAsync(executionId));

        // Workflow control
        [HttpPost("workflows/{workflowId}/pause")]
        public async Task<IActionResult> PauseWorkflow(string workflowId) => Ok(await _client.PauseWorkflowAsync(workflowId));

        [HttpPost("workflows/{workflowId}/resume")]
        public async Task<IActionResult> ResumeWorkflow(string workflowId) => Ok(await _client.ResumeWorkflowAsync(workflowId));

        [HttpPost("workflows/{workflowId}/enable")]
        public async Task<IActionResult> EnableWorkflow(string workflowId) => Ok(await _client.EnableWorkflowAsync(workflowId));

        [HttpPost("workflows/{workflowId}/disable")]
        public async Task<IActionResult> DisableWorkflow(string workflowId) => Ok(await _client.DisableWorkflowAsync(workflowId));

        // Step execution management
        [HttpGet("executions/{executionId}/steps")]
        public async Task<IActionResult> GetExecutionSteps(string executionId) => Ok(await _client.GetExecutionStepsAsync(executionId));

        [HttpGet("executions/{executionId}/steps/{stepId}")]
        public async Task<IActionResult> GetStepExecution(string executionId, string stepId) => Ok(await _client.GetStepExecutionAsync(executionId, stepId));

        [HttpPost("executions/{executionId}/steps/{stepId}/retry")]
        public async Task<IActionResult> RetryStep(string executionId, string stepId) => Ok(await _client.RetryStepAsync(executionId, stepId));

        [HttpPost("executions/{executionId}/steps/{stepId}/skip")]
        public async Task<IActionResult> SkipStep(string executionId, string stepId) => Ok(await _client.SkipStepAsync(executionId, stepId));

        // Workflow validation
        [HttpPost("workflows/validate")]
        public async Task<IActionResult> ValidateWorkflow(WorkflowDefinitionRequest request) => Ok(await _client.ValidateWorkflowAsync(request));

        [HttpPost("workflows/{workflowId}/validate")]
        public async Task<IActionResult> ValidateWorkflowById(string workflowId) => Ok(await _client.ValidateWorkflowByIdAsync(workflowId));

        // Workflow versioning
        [HttpPost("workflows/{workflowId}/versions")]
        public async Task<IActionResult> CreateWorkflowVersion(string workflowId, WorkflowDefinitionRequest request) => Ok(await _client.CreateWorkflowVersionAsync(workflowId, request));

        [HttpGet("workflows/{workflowId}/versions")]
        public async Task<IActionResult> GetWorkflowVersions(string workflowId) => Ok(await _client.GetWorkflowVersionsAsync(workflowId));

        [HttpPost("workflows/{workflowId}/versions/{version}/promote")]
        public async Task<IActionResult> PromoteWorkflowVersion(string workflowId, string version) => Ok(await _client.PromoteWorkflowVersionAsync(workflowId, version));

        [HttpPost("workflows/{workflowId}/versions/{version}/rollback")]
        public async Task<IActionResult> RollbackWorkflowVersion(string workflowId, string version) => Ok(await _client.RollbackWorkflowVersionAsync(workflowId, version));

        // Workflow triggers
        [HttpPost("workflows/{workflowId}/trigger")]
        public async Task<IActionResult> TriggerWorkflow(string workflowId, [FromBody] TriggerWorkflowRequest request) => Ok(await _client.TriggerWorkflowAsync(workflowId, request.InputData));

        [HttpGet("workflows/{workflowId}/triggers")]
        public async Task<IActionResult> GetWorkflowTriggers(string workflowId) => Ok(await _client.GetWorkflowTriggersAsync(workflowId));

        [HttpPost("workflows/{workflowId}/triggers/{triggerId}/enable")]
        public async Task<IActionResult> EnableTrigger(string workflowId, string triggerId) => Ok(await _client.EnableTriggerAsync(workflowId, triggerId));

        [HttpPost("workflows/{workflowId}/triggers/{triggerId}/disable")]
        public async Task<IActionResult> DisableTrigger(string workflowId, string triggerId) => Ok(await _client.DisableTriggerAsync(workflowId, triggerId));

        // Workflow monitoring and analytics
        [HttpGet("stats")]
        public async Task<IActionResult> GetWorkflowStats() => Ok(await _client.GetWorkflowStatsAsync());

        [HttpGet("workflows/{workflowId}/stats")]
        public async Task<IActionResult> GetWorkflowStatsById(string workflowId) => Ok(await _client.GetWorkflowStatsAsync(workflowId));

        [HttpGet("usage-stats")]
        public async Task<IActionResult> GetWorkflowUsageStats([FromQuery] int days = 30) => Ok(await _client.GetWorkflowUsageStatsAsync(days));

        [HttpGet("executions/{executionId}/metrics")]
        public async Task<IActionResult> GetExecutionMetrics(string executionId) => Ok(await _client.GetExecutionMetricsAsync(executionId));

        // Workflow templates and import/export
        [HttpGet("templates")]
        public async Task<IActionResult> GetWorkflowTemplates() => Ok(await _client.GetWorkflowTemplatesAsync());

        [HttpPost("workflows/from-template")]
        public async Task<IActionResult> CreateWorkflowFromTemplate([FromBody] CreateFromTemplateRequest request) => Ok(await _client.CreateWorkflowFromTemplateAsync(request.TemplateId, request.Parameters));

        [HttpGet("workflows/{workflowId}/export")]
        public async Task<IActionResult> ExportWorkflow(string workflowId) => Ok(await _client.ExportWorkflowAsync(workflowId));

        [HttpPost("workflows/import")]
        public async Task<IActionResult> ImportWorkflow([FromBody] ImportWorkflowRequest request) => Ok(await _client.ImportWorkflowAsync(request.WorkflowDefinition));

        // Workflow scheduling
        [HttpPost("workflows/{workflowId}/schedule")]
        public async Task<IActionResult> ScheduleWorkflow(string workflowId, [FromBody] ScheduleWorkflowRequest request) => Ok(await _client.ScheduleWorkflowAsync(workflowId, request.CronExpression));

        [HttpDelete("workflows/{workflowId}/schedule")]
        public async Task<IActionResult> UnscheduleWorkflow(string workflowId) => Ok(await _client.UnscheduleWorkflowAsync(workflowId));

        [HttpGet("scheduled-workflows")]
        public async Task<IActionResult> GetScheduledWorkflows() => Ok(await _client.GetScheduledWorkflowsAsync());

        // Workflow dependencies
        [HttpGet("workflows/{workflowId}/dependencies")]
        public async Task<IActionResult> GetWorkflowDependencies(string workflowId) => Ok(await _client.GetWorkflowDependenciesAsync(workflowId));

        [HttpGet("workflows/{workflowId}/dependents")]
        public async Task<IActionResult> GetWorkflowDependents(string workflowId) => Ok(await _client.GetWorkflowDependentsAsync(workflowId));

        [HttpPost("workflows/{workflowId}/validate-dependencies")]
        public async Task<IActionResult> ValidateWorkflowDependencies(string workflowId) => Ok(await _client.ValidateWorkflowDependenciesAsync(workflowId));
    }

    // Helper DTOs for controller endpoints
    public class TriggerWorkflowRequest
    {
        public Dictionary<string, object> InputData { get; set; } = new();
    }

    public class CreateFromTemplateRequest
    {
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class ImportWorkflowRequest
    {
        public string WorkflowDefinition { get; set; } = string.Empty;
    }

    public class ScheduleWorkflowRequest
    {
        public string CronExpression { get; set; } = string.Empty;
    }
}
