using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Orchestration;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IOrchestrationClient
    {
        // Workflow definition management
        Task<WorkflowDefinitionResponse?> CreateWorkflowAsync(WorkflowDefinitionRequest request);
        Task<WorkflowDefinitionResponse?> GetWorkflowAsync(string workflowId);
        Task<WorkflowListResponse?> ListWorkflowsAsync(WorkflowListRequest request);
        Task<WorkflowDefinitionResponse?> UpdateWorkflowAsync(string workflowId, WorkflowDefinitionRequest request);
        Task<MessageResponse5?> DeleteWorkflowAsync(string workflowId);

        // Workflow execution
        Task<WorkflowExecutionResponse?> ExecuteWorkflowAsync(WorkflowExecutionRequest request);
        Task<OrchestrationResponse?> GetExecutionStatusAsync(string executionId);
        Task<ExecutionListResponse?> ListExecutionsAsync(ExecutionListRequest request);
        Task<MessageResponse5?> CancelExecutionAsync(string executionId);
        Task<MessageResponse5?> RetryExecutionAsync(string executionId);

        // Workflow control
        Task<MessageResponse5?> PauseWorkflowAsync(string workflowId);
        Task<MessageResponse5?> ResumeWorkflowAsync(string workflowId);
        Task<MessageResponse5?> EnableWorkflowAsync(string workflowId);
        Task<MessageResponse5?> DisableWorkflowAsync(string workflowId);

        // Step execution management
        Task<List<StepExecution>?> GetExecutionStepsAsync(string executionId);
        Task<StepExecution?> GetStepExecutionAsync(string executionId, string stepId);
        Task<MessageResponse5?> RetryStepAsync(string executionId, string stepId);
        Task<MessageResponse5?> SkipStepAsync(string executionId, string stepId);

        // Workflow validation
        Task<WorkflowValidationResponse?> ValidateWorkflowAsync(WorkflowDefinitionRequest request);
        Task<WorkflowValidationResponse?> ValidateWorkflowByIdAsync(string workflowId);

        // Workflow versioning
        Task<WorkflowDefinitionResponse?> CreateWorkflowVersionAsync(string workflowId, WorkflowDefinitionRequest request);
        Task<List<WorkflowDefinitionResponse>?> GetWorkflowVersionsAsync(string workflowId);
        Task<MessageResponse5?> PromoteWorkflowVersionAsync(string workflowId, string version);
        Task<MessageResponse5?> RollbackWorkflowVersionAsync(string workflowId, string version);

        // Workflow triggers
        Task<MessageResponse5?> TriggerWorkflowAsync(string workflowId, Dictionary<string, object> inputData);
        Task<List<WorkflowTrigger>?> GetWorkflowTriggersAsync(string workflowId);
        Task<MessageResponse5?> EnableTriggerAsync(string workflowId, string triggerId);
        Task<MessageResponse5?> DisableTriggerAsync(string workflowId, string triggerId);

        // Workflow monitoring and analytics
        Task<WorkflowStatsResponse?> GetWorkflowStatsAsync();
        Task<WorkflowStatsResponse?> GetWorkflowStatsAsync(string workflowId);
        Task<List<WorkflowUsageStats>?> GetWorkflowUsageStatsAsync(int days = 30);
        Task<Dictionary<string, object>?> GetExecutionMetricsAsync(string executionId);

        // Workflow templates and import/export
        Task<List<WorkflowDefinitionResponse>?> GetWorkflowTemplatesAsync();
        Task<WorkflowDefinitionResponse?> CreateWorkflowFromTemplateAsync(string templateId, Dictionary<string, object> parameters);
        Task<string?> ExportWorkflowAsync(string workflowId);
        Task<WorkflowDefinitionResponse?> ImportWorkflowAsync(string workflowDefinition);

        // Workflow scheduling
        Task<MessageResponse5?> ScheduleWorkflowAsync(string workflowId, string cronExpression);
        Task<MessageResponse5?> UnscheduleWorkflowAsync(string workflowId);
        Task<List<Dictionary<string, object>>?> GetScheduledWorkflowsAsync();

        // Workflow dependencies
        Task<List<string>?> GetWorkflowDependenciesAsync(string workflowId);
        Task<List<string>?> GetWorkflowDependentsAsync(string workflowId);
        Task<MessageResponse5?> ValidateWorkflowDependenciesAsync(string workflowId);
    }
}
