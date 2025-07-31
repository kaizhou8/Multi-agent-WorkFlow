using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using MultiAgent.Shared.Models.Agent;
using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.Desktop.Services;
/// <summary>
/// SignalR service interface
/// Provides real-time communication with the backend
//// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Connection state
    /// </summary>
    HubConnectionState ConnectionState { get; }
    /// <summary>
    /// Start connection
    /// </summary>
    /// <param name="hubUrl">Hub URL
    /// <param name="accessToken">Access token</param>
    /// <returns>Task</returns>
    Task StartConnectionAsync(string hubUrl, string? accessToken = null);
    /// <summary>
    /// Stop connection
    /// </summary>
    /// <returns>Task</returns>
    Task StopConnectionAsync();
    /// <summary>
    /// Join agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    Task JoinAgentGroupAsync(string agentId);
    /// <summary>
    /// Leave agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    Task LeaveAgentGroupAsync(string agentId);
    /// <summary>
    /// Join workflow monitoring group
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Task</returns>
    Task JoinWorkflowGroupAsync(string workflowId);
    /// <summary>
    /// Leave workflow monitoring group
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Task</returns>
    Task LeaveWorkflowGroupAsync(string workflowId);
    /// <summary>
    /// Request system status
    /// </summary>
    /// <returns>Task</returns>
    Task RequestSystemStatusAsync();
    // Events
    event EventHandler<AgentInfo>? AgentStatusUpdated;
    event EventHandler<AgentHealthStatus>? AgentHealthUpdated;
    event EventHandler<AgentExecutionResult>? AgentExecutionResult;
    event EventHandler<WorkflowDefinition>? WorkflowStatusUpdated;
    event EventHandler<WorkflowExecution>? WorkflowExecutionUpdated;
    event EventHandler<WorkflowStepExecution>? WorkflowStepExecutionUpdated;
    event EventHandler<object>? SystemStatusUpdated;
    event EventHandler<string>? ErrorReceived;
    event EventHandler<HubConnectionState>? ConnectionStateChanged;
}
/// <summary>
/// SignalR service implementation
/// </summary>
public class SignalRService : ISignalRService, IAsyncDisposable
{
    private readonly ILogger<SignalRService> _logger;
    private HubConnection? _connection;
    private readonly object _connectionLock = new();
    /// <summary>
    /// Connection state
    /// </summary>
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public SignalRService(ILogger<SignalRService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    #region Events /
    public event EventHandler<AgentInfo>? AgentStatusUpdated;
    public event EventHandler<AgentHealthStatus>? AgentHealthUpdated;
    public event EventHandler<AgentExecutionResult>? AgentExecutionResult;
    public event EventHandler<WorkflowDefinition>? WorkflowStatusUpdated;
    public event EventHandler<WorkflowExecution>? WorkflowExecutionUpdated;
    public event EventHandler<WorkflowStepExecution>? WorkflowStepExecutionUpdated;
    public event EventHandler<object>? SystemStatusUpdated;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<HubConnectionState>? ConnectionStateChanged;
    #endregion
    /// <summary>
    /// Start connection
    /// </summary>
    /// <param name="hubUrl">Hub URL
    /// <param name="accessToken">Access token</param>
    /// <returns>Task</returns>
    public async Task StartConnectionAsync(string hubUrl, string? accessToken = null)
    {
        try
        {
            lock (_connectionLock)
            {
                if (_connection?.State == HubConnectionState.Connected)
                {
                    _logger.LogInformation("SignalR connection already established");
                    return;
                }
                // Build connection
                var connectionBuilder = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            options.AccessTokenProvider = () => Task.FromResult(accessToken);
                        }
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .ConfigureLogging(logging =>
                    {
                        logging.SetMinimumLevel(LogLevel.Information);
                    });
                _connection = connectionBuilder.Build();
                // Setup event handlers
                SetupEventHandlers();
            }
            // Start connection
            await _connection.StartAsync();
            _logger.LogInformation("SignalR connection started successfully");
            // Notify connection state change
            ConnectionStateChanged?.Invoke(this, _connection.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection to {HubUrl}", hubUrl);
            throw;
        }
    }
    /// <summary>
    /// Stop connection
    /// </summary>
    /// <returns>Task</returns>
    public async Task StopConnectionAsync()
    {
        try
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                _logger.LogInformation("SignalR connection stopped");
                ConnectionStateChanged?.Invoke(this, _connection.State);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping SignalR connection");
            throw;
        }
    }
    /// <summary>
    /// Join agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    public async Task JoinAgentGroupAsync(string agentId)
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("JoinAgentGroup", agentId);
                _logger.LogDebug("Joined agent group for agent {AgentId}", agentId);
            }
            else
            {
                _logger.LogWarning("Cannot join agent group - SignalR connection not established");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join agent group for agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Leave agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    public async Task LeaveAgentGroupAsync(string agentId)
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("LeaveAgentGroup", agentId);
                _logger.LogDebug("Left agent group for agent {AgentId}", agentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave agent group for agent {AgentId}", agentId);
        }
    }
    /// <summary>
    /// Join workflow monitoring group
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Task</returns>
    public async Task JoinWorkflowGroupAsync(string workflowId)
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("JoinWorkflowGroup", workflowId);
                _logger.LogDebug("Joined workflow group for workflow {WorkflowId}", workflowId);
            }
            else
            {
                _logger.LogWarning("Cannot join workflow group - SignalR connection not established");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join workflow group for workflow {WorkflowId}", workflowId);
            throw;
        }
    }
    /// <summary>
    /// Leave workflow monitoring group
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Task</returns>
    public async Task LeaveWorkflowGroupAsync(string workflowId)
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("LeaveWorkflowGroup", workflowId);
                _logger.LogDebug("Left workflow group for workflow {WorkflowId}", workflowId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave workflow group for workflow {WorkflowId}", workflowId);
        }
    }
    /// <summary>
    /// Request system status
    /// </summary>
    /// <returns>Task</returns>
    public async Task RequestSystemStatusAsync()
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("RequestSystemStatus");
                _logger.LogDebug("Requested system status");
            }
            else
            {
                _logger.LogWarning("Cannot request system status - SignalR connection not established");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request system status");
            throw;
        }
    }
    /// <summary>
    /// Setup event handlers for SignalR messagesSignalR
    /// </summary>
    private void SetupEventHandlers()
    {
        if (_connection == null) return;
        // Connection state events
        _connection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed. Error: {Error}", error?.Message);
            ConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
            // Auto-reconnect logic could be added here
        };
        _connection.Reconnecting += (error) =>
        {
            _logger.LogInformation("SignalR connection reconnecting. Error: {Error}", error?.Message);
            ConnectionStateChanged?.Invoke(this, HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };
        _connection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR connection reconnected. Connection ID: {ConnectionId}", connectionId);
            ConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
            return Task.CompletedTask;
        };
        // Agent events
        _connection.On<AgentInfo>("AgentStatusUpdate", (agentInfo) =>
        {
            _logger.LogDebug("Received agent status update for agent {AgentId}", agentInfo.Id);
            AgentStatusUpdated?.Invoke(this, agentInfo);
        });
        _connection.On<AgentHealthStatus>("AgentHealthUpdate", (healthStatus) =>
        {
            _logger.LogDebug("Received agent health update for agent {AgentId}", healthStatus.AgentId);
            AgentHealthUpdated?.Invoke(this, healthStatus);
        });
        _connection.On<AgentExecutionResult>("AgentExecutionResult", (executionResult) =>
        {
            _logger.LogDebug("Received agent execution result for agent {AgentId}", executionResult.AgentId);
            AgentExecutionResult?.Invoke(this, executionResult);
        });
        // System-wide agent events
        _connection.On<AgentInfo>("SystemAgentUpdate", (agentInfo) =>
        {
            _logger.LogDebug("Received system agent update for agent {AgentId}", agentInfo.Id);
            AgentStatusUpdated?.Invoke(this, agentInfo);
        });
        _connection.On<AgentHealthStatus>("SystemHealthUpdate", (healthStatus) =>
        {
            _logger.LogDebug("Received system health update for agent {AgentId}", healthStatus.AgentId);
            AgentHealthUpdated?.Invoke(this, healthStatus);
        });
        // Workflow events
        _connection.On<WorkflowDefinition>("WorkflowStatusUpdate", (workflow) =>
        {
            _logger.LogDebug("Received workflow status update for workflow {WorkflowId}", workflow.Id);
            WorkflowStatusUpdated?.Invoke(this, workflow);
        });
        _connection.On<WorkflowExecution>("WorkflowExecutionUpdate", (execution) =>
        {
            _logger.LogDebug("Received workflow execution update for execution {ExecutionId}", execution.Id);
            WorkflowExecutionUpdated?.Invoke(this, execution);
        });
        _connection.On<WorkflowStepExecution>("WorkflowStepExecutionUpdate", (stepExecution) =>
        {
            _logger.LogDebug("Received workflow step execution update for step {StepId}", stepExecution.StepId);
            WorkflowStepExecutionUpdated?.Invoke(this, stepExecution);
        });
        // System-wide workflow events
        _connection.On<WorkflowDefinition>("SystemWorkflowUpdate", (workflow) =>
        {
            _logger.LogDebug("Received system workflow update for workflow {WorkflowId}", workflow.Id);
            WorkflowStatusUpdated?.Invoke(this, workflow);
        });
        _connection.On<WorkflowExecution>("SystemExecutionUpdate", (execution) =>
        {
            _logger.LogDebug("Received system execution update for execution {ExecutionId}", execution.Id);
            WorkflowExecutionUpdated?.Invoke(this, execution);
        });
        // System status events
        _connection.On<object>("SystemStatusUpdate", (systemStatus) =>
        {
            _logger.LogDebug("Received system status update");
            SystemStatusUpdated?.Invoke(this, systemStatus);
        });
        // Error events
        _connection.On<string>("Error", (errorMessage) =>
        {
            _logger.LogWarning("Received error from SignalR hub: {ErrorMessage}", errorMessage);
            ErrorReceived?.Invoke(this, errorMessage);
        });
    }
    /// <summary>
    /// Dispose async
    /// </summary>
    /// <returns>ValueTask</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _logger.LogInformation("SignalR service disposed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing SignalR service");
        }
    }
}

