using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.Shared.Contracts;
/// <summary>
/// Workflow service interface
/// Manages workflow definitions and executions
//// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Create a new workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Created workflow</returns>
    Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow);
    /// <summary>
    /// Update an existing workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Updated workflow</returns>
    Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow);
    /// <summary>
    /// Delete a workflow definition
    /// </summary>
    /// <param name="workflowId">Workflow ID
    Task DeleteWorkflowAsync(string workflowId);
    /// <summary>
    /// Get workflow definition by ID
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Workflow definition</returns>
    Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId);
    /// <summary>
    /// Get all workflow definitions
    /// </summary>
    /// <returns>List of workflows</returns>
    Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowsAsync();
    /// <summary>
    /// Execute a workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <param name="inputData">Input data</param>
    /// <param name="executedBy">Executed by user</param>
    /// <returns>Workflow execution</returns>
    Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> inputData, string executedBy);
    /// <summary>
    /// Get workflow execution by ID
    /// </summary>
    /// <param name="executionId">Execution ID
    /// <returns>Workflow execution</returns>
    Task<WorkflowExecution?> GetWorkflowExecutionAsync(string executionId);
    /// <summary>
    /// Get all executions for a workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>List of executions</returns>
    Task<IEnumerable<WorkflowExecution>> GetWorkflowExecutionsAsync(string workflowId);
    /// <summary>
    /// Cancel a running workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    Task CancelWorkflowExecutionAsync(string executionId);
    /// <summary>
    /// Pause a running workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    Task PauseWorkflowExecutionAsync(string executionId);
    /// <summary>
    /// Resume a paused workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    Task ResumeWorkflowExecutionAsync(string executionId);
    /// <summary>
    /// Validate workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Validation result</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow);
}
/// <summary>
/// Workflow validation result
/// </summary>
public class WorkflowValidationResult
{
    /// <summary>
    /// Validation success
    /// </summary>
    public bool IsValid { get; set; }
    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

