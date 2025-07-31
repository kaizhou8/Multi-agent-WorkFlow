using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MultiAgent.Shared.Models.Agent;
/// <summary>
/// Agent status enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentStatus
{
    /// <summary>Idle state</summary>
    Idle,
    /// <summary>Running state</summary>
    Running,
    /// <summary>Paused state</summary>
    Paused,
    /// <summary>Stopped state</summary>
    Stopped,
    /// <summary>Error state</summary>
    Error,
    /// <summary>Completed state</summary>
    Completed
}
/// <summary>
/// Agent priority level
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentPriority
{
    /// <summary>Low priority</summary>
    Low = 1,
    /// <summary>Normal priority</summary>
    Normal = 2,
    /// <summary>High priority</summary>
    High = 3,
    /// <summary>Critical priority</summary>
    Critical = 4
}
/// <summary>
/// Agent capabilities
/// </summary>
public class AgentCapabilities
{
    /// <summary>Supported operations</summary>
    public List<string> SupportedOperations { get; set; } = new();
    /// <summary>Maximum concurrent tasks</summary>
    public int MaxConcurrentTasks { get; set; } = 1;
    /// <summary>Supports async execution</summary>
    public bool SupportsAsyncExecution { get; set; } = true;
    /// <summary>Requires authentication</summary>
    public bool RequiresAuthentication { get; set; } = false;
    /// <summary>Resource requirements</summary>
    public Dictionary<string, object> ResourceRequirements { get; set; } = new();
}
/// <summary>
/// Agent information
/// </summary>
public class AgentInfo
{
    /// <summary>Agent unique identifier</summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    /// <summary>Agent name</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    /// <summary>Agent description</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Agent version</summary>
    public string Version { get; set; } = "1.0.0";
    /// <summary>Agent capabilities</summary>
    public AgentCapabilities Capabilities { get; set; } = new();
    /// <summary>Current status</summary>
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    /// <summary>Creation time</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Last update time</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Last heartbeat time</summary>
    public DateTime? LastHeartbeat { get; set; }
    /// <summary>Agent metadata</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
/// <summary>
/// Agent command
/// </summary>
public class AgentCommand
{
    /// <summary>Command unique identifier</summary>
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Target agent ID
    [Required]
    public string AgentId { get; set; } = string.Empty;
    /// <summary>Command type</summary>
    [Required]
    public string Type { get; set; } = string.Empty;
    /// <summary>Command parameters</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    /// <summary>Command priority</summary>
    public AgentPriority Priority { get; set; } = AgentPriority.Normal;
    /// <summary>Command timeout (seconds)</summary>
    public int TimeoutSeconds { get; set; } = 300;
    /// <summary>Creation time</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Execution context</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}
/// <summary>
/// Agent execution result
/// </summary>
public class AgentExecutionResult
{
    /// <summary>Execution unique identifier</summary>
    [Required]
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Agent ID
    [Required]
    public string AgentId { get; set; } = string.Empty;
    /// <summary>Command ID
    [Required]
    public string CommandId { get; set; } = string.Empty;
    /// <summary>Execution success</summary>
    public bool Success { get; set; }
    /// <summary>Result data</summary>
    public object? Data { get; set; }
    /// <summary>Output data</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
    /// <summary>Error message</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Execution start time</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    /// <summary>Execution end time</summary>
    public DateTime? EndTime { get; set; }
    /// <summary>Execution duration</summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    /// <summary>Execution logs</summary>
    public List<string> Logs { get; set; } = new();
}
/// <summary>
/// Agent health status
/// </summary>
public class AgentHealthStatus
{
    /// <summary>Agent ID
    [Required]
    public string AgentId { get; set; } = string.Empty;
    /// <summary>Health status</summary>
    [Required]
    public string Status { get; set; } = "Healthy";
    /// <summary>CPU usage percentage</summary>
    public double CpuUsage { get; set; }
    /// <summary>Memory usage in MB（MB）</summary>
    public long MemoryUsage { get; set; }
    /// <summary>Active tasks count</summary>
    public int ActiveTasks { get; set; }
    /// <summary>Total processed tasks</summary>
    public long TotalProcessedTasks { get; set; }
    /// <summary>Last check time</summary>
    public DateTime LastCheckTime { get; set; } = DateTime.UtcNow;
    /// <summary>Additional metrics</summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

