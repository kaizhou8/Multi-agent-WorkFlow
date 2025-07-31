using Microsoft.Extensions.Logging;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Agent;
using System.Collections.Concurrent;
namespace MultiAgent.Agents;
/// <summary>
/// Base implementation for all agents
/// Provides common functionality and lifecycle management
//// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly ILogger<BaseAgent> _logger;
    private readonly ConcurrentDictionary<string, Task<AgentExecutionResult>> _runningTasks;
    private readonly SemaphoreSlim _executionSemaphore;
    private AgentStatus _status;
    private DateTime _lastHeartbeat;
    /// <summary>
    /// Agent unique identifier
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// Agent name
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Agent capabilities
    /// </summary>
    public AgentCapabilities Capabilities { get; protected set; }
    /// <summary>
    /// Current agent status
    /// </summary>
    protected AgentStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            _lastHeartbeat = DateTime.UtcNow;
        }
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">Agent ID
    /// <param name="name">Agent name</param>
    /// <param name="logger">Logger instance</param>
    protected BaseAgent(string id, string name, ILogger<BaseAgent> logger)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runningTasks = new ConcurrentDictionary<string, Task<AgentExecutionResult>>();
        _executionSemaphore = new SemaphoreSlim(1, 1); // Default to single concurrent execution
        Capabilities = new AgentCapabilities
        {
            MaxConcurrentTasks = 1,
            SupportsAsyncExecution = true,
            RequiresAuthentication = false
        };
        Status = AgentStatus.Idle;
        _lastHeartbeat = DateTime.UtcNow;
        _logger.LogInformation("Agent {AgentId} ({AgentName}) initialized", Id, Name);
    }
    /// <summary>
    /// Execute a command asynchronously
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    public async Task<AgentExecutionResult> ExecuteAsync(AgentCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        if (Status == AgentStatus.Stopped)
        {
            return new AgentExecutionResult
            {
                AgentId = Id,
                CommandId = command.Id,
                Success = false,
                ErrorMessage = "Agent is stopped and cannot execute commands"
            };
        }
        _logger.LogInformation("Agent {AgentId} executing command {CommandId} of type {CommandType}",
            Id, command.Id, command.Type);
        // Wait for execution slot
        await _executionSemaphore.WaitAsync();
        try
        {
            Status = AgentStatus.Running;
            var executionTask = ExecuteCommandAsync(command);
            _runningTasks.TryAdd(command.Id, executionTask);
            var result = await executionTask;
            result.AgentId = Id;
            result.CommandId = command.Id;
            result.EndTime = DateTime.UtcNow;
            _logger.LogInformation("Agent {AgentId} completed command {CommandId} with success: {Success}",
                Id, command.Id, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed to execute command {CommandId}", Id, command.Id);
            return new AgentExecutionResult
            {
                AgentId = Id,
                CommandId = command.Id,
                Success = false,
                ErrorMessage = ex.Message,
                EndTime = DateTime.UtcNow
            };
        }
        finally
        {
            _runningTasks.TryRemove(command.Id, out _);
            Status = AgentStatus.Idle;
            _executionSemaphore.Release();
        }
    }
    /// <summary>
    /// Abstract method for command execution implementation
    /// Must be implemented by derived classes
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    protected abstract Task<AgentExecutionResult> ExecuteCommandAsync(AgentCommand command);
    /// <summary>
    /// Get agent health status
    /// </summary>
    /// <returns>Health status</returns>
    public virtual Task<AgentHealthStatus> GetHealthStatusAsync()
    {
        var healthStatus = new AgentHealthStatus
        {
            AgentId = Id,
            Status = Status.ToString(),
            ActiveTasks = _runningTasks.Count,
            LastCheckTime = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["LastHeartbeat"] = _lastHeartbeat,
                ["MaxConcurrentTasks"] = Capabilities.MaxConcurrentTasks,
                ["SupportsAsyncExecution"] = Capabilities.SupportsAsyncExecution
            }
        };
        return Task.FromResult(healthStatus);
    }
    /// <summary>
    /// Get agent information
    /// </summary>
    /// <returns>Agent information</returns>
    public virtual Task<AgentInfo> GetAgentInfoAsync()
    {
        var agentInfo = new AgentInfo
        {
            Id = Id,
            Name = Name,
            Capabilities = Capabilities,
            Status = Status,
            LastHeartbeat = _lastHeartbeat,
            UpdatedAt = DateTime.UtcNow
        };
        return Task.FromResult(agentInfo);
    }
    /// <summary>
    /// Start the agent
    /// </summary>
    public virtual Task StartAsync()
    {
        _logger.LogInformation("Starting agent {AgentId} ({AgentName})", Id, Name);
        Status = AgentStatus.Idle;
        return Task.CompletedTask;
    }
    /// <summary>
    /// Stop the agent
    /// </summary>
    public virtual async Task StopAsync()
    {
        _logger.LogInformation("Stopping agent {AgentId} ({AgentName})", Id, Name);
        Status = AgentStatus.Stopped;
        // Wait for all running tasks to complete
        var runningTasks = _runningTasks.Values.ToArray();
        if (runningTasks.Length > 0)
        {
            _logger.LogInformation("Waiting for {TaskCount} running tasks to complete", runningTasks.Length);
            await Task.WhenAll(runningTasks);
        }
    }
    /// <summary>
    /// Pause the agent
    /// </summary>
    public virtual Task PauseAsync()
    {
        _logger.LogInformation("Pausing agent {AgentId} ({AgentName})", Id, Name);
        Status = AgentStatus.Paused;
        return Task.CompletedTask;
    }
    /// <summary>
    /// Resume the agent
    /// </summary>
    public virtual Task ResumeAsync()
    {
        _logger.LogInformation("Resuming agent {AgentId} ({AgentName})", Id, Name);
        Status = AgentStatus.Idle;
        return Task.CompletedTask;
    }
    /// <summary>
    /// Dispose resources
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _executionSemaphore?.Dispose();
        }
    }
    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

