using System.Collections.Generic;
using System.Threading.Tasks;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.Clients
{
    public interface IToolsClient
    {
        // Tool execution
        Task<ToolsResponse?> ExecuteToolAsync(ToolsRequest request);
        Task<ToolsResponse?> ExecuteToolAsyncAsync(ToolsRequest request);
        Task<ToolsResponse?> GetExecutionStatusAsync(string executionId);
        Task<MessageResponse7?> CancelExecutionAsync(string executionId);

        // Tool registration and management
        Task<ToolRegistrationResponse?> RegisterToolAsync(ToolRegistrationRequest request);
        Task<ToolRegistrationResponse?> GetToolAsync(string toolId);
        Task<ToolListResponse?> ListToolsAsync(ToolListRequest request);
        Task<ToolRegistrationResponse?> UpdateToolAsync(string toolId, ToolRegistrationRequest request);
        Task<MessageResponse7?> DeleteToolAsync(string toolId);

        // Tool discovery and search
        Task<ToolListResponse?> SearchToolsAsync(string query, int limit = 20);
        Task<List<ToolRegistrationResponse>?> GetToolsByCategoryAsync(string category);
        Task<List<ToolRegistrationResponse>?> GetToolsByTagAsync(string tag);
        Task<List<ToolRegistrationResponse>?> GetPopularToolsAsync(int limit = 10);
        Task<List<ToolRegistrationResponse>?> GetRecentToolsAsync(int limit = 10);

        // Tool validation and testing
        Task<ToolValidationResponse?> ValidateToolParametersAsync(ToolValidationRequest request);
        Task<ToolTestResponse?> RunToolTestsAsync(ToolTestRequest request);
        Task<ToolTestResponse?> GetTestResultsAsync(string testId);
        Task<MessageResponse7?> CancelTestAsync(string testId);

        // Tool versioning
        Task<ToolRegistrationResponse?> CreateToolVersionAsync(string toolId, ToolRegistrationRequest request);
        Task<List<ToolRegistrationResponse>?> GetToolVersionsAsync(string toolId);
        Task<ToolRegistrationResponse?> GetToolVersionAsync(string toolId, string version);
        Task<MessageResponse7?> ActivateToolVersionAsync(string toolId, string version);
        Task<MessageResponse7?> DeleteToolVersionAsync(string toolId, string version);

        // Tool categories and tags
        Task<List<string>?> GetToolCategoriesAsync();
        Task<List<string>?> GetToolTagsAsync();
        Task<MessageResponse7?> AddToolCategoryAsync(string category);
        Task<MessageResponse7?> AddToolTagAsync(string tag);
        Task<MessageResponse7?> RemoveToolCategoryAsync(string category);
        Task<MessageResponse7?> RemoveToolTagAsync(string tag);

        // Tool access control and permissions
        Task<MessageResponse7?> SetToolPermissionsAsync(string toolId, AccessControl accessControl);
        Task<AccessControl?> GetToolPermissionsAsync(string toolId);
        Task<MessageResponse7?> ShareToolAsync(string toolId, List<string> userIds);
        Task<MessageResponse7?> UnshareToolAsync(string toolId, List<string> userIds);
        Task<List<string>?> GetToolCollaboratorsAsync(string toolId);

        // Tool analytics and monitoring
        Task<ToolStatsResponse?> GetToolStatsAsync(string toolId);
        Task<ToolStatsResponse?> GetGlobalToolStatsAsync();
        Task<List<PerformanceTrend>?> GetToolPerformanceTrendsAsync(string toolId, int days = 30);
        Task<Dictionary<string, object>?> GetToolUsageAnalyticsAsync(string toolId, int days = 30);

        // Tool execution history
        Task<List<ToolsResponse>?> GetToolExecutionHistoryAsync(string toolId, int limit = 50, int offset = 0);
        Task<List<ToolsResponse>?> GetUserExecutionHistoryAsync(int limit = 50, int offset = 0);
        Task<ToolsResponse?> GetExecutionDetailsAsync(string executionId);
        Task<MessageResponse7?> DeleteExecutionAsync(string executionId);

        // Tool configuration and settings
        Task<Dictionary<string, object>?> GetToolConfigurationAsync(string toolId);
        Task<MessageResponse7?> UpdateToolConfigurationAsync(string toolId, Dictionary<string, object> configuration);
        Task<Dictionary<string, object>?> GetDefaultToolConfigurationAsync();
        Task<MessageResponse7?> ResetToolConfigurationAsync(string toolId);

        // Tool dependencies and requirements
        Task<List<string>?> GetToolDependenciesAsync(string toolId);
        Task<MessageResponse7?> ValidateToolDependenciesAsync(string toolId);
        Task<List<ToolRegistrationResponse>?> GetDependentToolsAsync(string toolId);
        Task<Dictionary<string, object>?> CheckToolRequirementsAsync(string toolId);

        // Tool deployment and lifecycle
        Task<MessageResponse7?> DeployToolAsync(string toolId);
        Task<MessageResponse7?> UndeployToolAsync(string toolId);
        Task<string?> GetToolDeploymentStatusAsync(string toolId);
        Task<MessageResponse7?> RestartToolAsync(string toolId);
        Task<Dictionary<string, object>?> GetToolHealthAsync(string toolId);

        // Batch operations
        Task<List<ToolsResponse>?> ExecuteBatchToolsAsync(List<ToolsRequest> requests);
        Task<MessageResponse7?> DeleteMultipleToolsAsync(List<string> toolIds);
        Task<List<ToolRegistrationResponse>?> ImportToolsAsync(List<ToolRegistrationRequest> tools);
        Task<List<ToolRegistrationResponse>?> ExportToolsAsync(List<string> toolIds);

        // Tool marketplace and sharing
        Task<ToolListResponse?> GetMarketplaceToolsAsync(int limit = 50, int offset = 0);
        Task<MessageResponse7?> PublishToolToMarketplaceAsync(string toolId);
        Task<MessageResponse7?> UnpublishToolFromMarketplaceAsync(string toolId);
        Task<MessageResponse7?> InstallMarketplaceToolAsync(string toolId);
        Task<MessageResponse7?> UninstallToolAsync(string toolId);

        // Tool documentation and help
        Task<string?> GetToolDocumentationAsync(string toolId);
        Task<MessageResponse7?> UpdateToolDocumentationAsync(string toolId, string documentation);
        Task<List<FunctionExample>?> GetToolExamplesAsync(string toolId);
        Task<MessageResponse7?> AddToolExampleAsync(string toolId, FunctionExample example);
        Task<MessageResponse7?> RemoveToolExampleAsync(string toolId, string exampleId);
    }
}
