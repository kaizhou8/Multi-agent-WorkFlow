using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MultiAgent.Shared.Models.Workflow;
/// <summary>
/// Workflow status enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowStatus
{
    /// <summary>Draft state</summary>
    Draft,
    /// <summary>Ready to run</summary>
    Ready,
    /// <summary>Running state</summary>
    Running,
    /// <summary>Paused state</summary>
    Paused,
    /// <summary>Completed state</summary>
    Completed,
    /// <summary>Failed state</summary>
    Failed,
    /// <summary>Cancelled state</summary>
    Cancelled
}
/// <summary>
/// Workflow execution mode
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowExecutionMode
{
    /// <summary>Sequential execution</summary>
    Sequential,
    /// <summary>Parallel execution</summary>
    Parallel,
    /// <summary>Conditional execution</summary>
    Conditional,
    /// <summary>Mixed execution</summary>
    Mixed
}
/// <summary>
/// Workflow definition
/// </summary>
public class WorkflowDefinition
{
    /// <summary>Workflow unique identifier</summary>
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Workflow name</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    /// <summary>Workflow description</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Workflow version</summary>
    public string Version { get; set; } = "1.0.0";
    /// <summary>Execution mode</summary>
    public WorkflowExecutionMode ExecutionMode { get; set; } = WorkflowExecutionMode.Sequential;
    /// <summary>Workflow steps</summary>
    public List<WorkflowStep> Steps { get; set; } = new();
    /// <summary>Input parameters</summary>
    public Dictionary<string, object> InputParameters { get; set; } = new();
    /// <summary>Output parameters</summary>
    public Dictionary<string, object> OutputParameters { get; set; } = new();
    /// <summary>Creation time</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Last update time</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Created by user</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Workflow metadata</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
/// <summary>
/// Workflow step
/// </summary>
public class WorkflowStep
{
    /// <summary>Step unique identifier</summary>
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Step name</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    /// <summary>Step description</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Step order</summary>
    public int Order { get; set; }
    /// <summary>Agent ID to execute this stepID</summary>
    [Required]
    public string AgentId { get; set; } = string.Empty;
    /// <summary>Step configuration</summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
    /// <summary>Input mapping</summary>
    public Dictionary<string, string> InputMapping { get; set; } = new();
    /// <summary>Output mapping</summary>
    public Dictionary<string, string> OutputMapping { get; set; } = new();
    /// <summary>Condition for execution</summary>
    public string? ExecutionCondition { get; set; }
    /// <summary>Timeout in seconds</summary>
    public int TimeoutSeconds { get; set; } = 300;
    /// <summary>Retry count</summary>
    public int RetryCount { get; set; } = 0;
    /// <summary>Dependencies</summary>
    public List<string> Dependencies { get; set; } = new();
}
/// <summary>
/// Workflow execution instance
/// </summary>
public class WorkflowExecution
{
    /// <summary>Execution unique identifier</summary>
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Workflow definition ID
    [Required]
    public string WorkflowId { get; set; } = string.Empty;
    /// <summary>Execution status</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Ready;
    /// <summary>Input data</summary>
    public Dictionary<string, object> InputData { get; set; } = new();
    /// <summary>Output data</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
    /// <summary>Step executions</summary>
    public List<WorkflowStepExecution> StepExecutions { get; set; } = new();
    /// <summary>Start time</summary>
    public DateTime? StartTime { get; set; }
    /// <summary>End time</summary>
    public DateTime? EndTime { get; set; }
    /// <summary>Execution duration</summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime ?? DateTime.UtcNow);
    /// <summary>Error message</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Execution logs</summary>
    public List<string> Logs { get; set; } = new();
    /// <summary>Executed by user</summary>
    public string ExecutedBy { get; set; } = string.Empty;
    /// <summary>Execution context</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}
/// <summary>
/// Workflow step execution
/// </summary>
public class WorkflowStepExecution
{
    /// <summary>Step execution unique identifier</summary>
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Workflow execution ID
    [Required]
    public string WorkflowExecutionId { get; set; } = string.Empty;
    /// <summary>Step ID
    [Required]
    public string StepId { get; set; } = string.Empty;
    /// <summary>Agent ID
    [Required]
    public string AgentId { get; set; } = string.Empty;
    /// <summary>Execution status</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Ready;
    /// <summary>Input data</summary>
    public Dictionary<string, object> InputData { get; set; } = new();
    /// <summary>Output data</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
    /// <summary>Start time</summary>
    public DateTime? StartTime { get; set; }
    /// <summary>End time</summary>
    public DateTime? EndTime { get; set; }
    /// <summary>Execution duration</summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime ?? DateTime.UtcNow);
    /// <summary>Error message</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Retry count</summary>
    public int RetryCount { get; set; } = 0;
    /// <summary>Execution logs</summary>
    public List<string> Logs { get; set; } = new();
}

