using Microsoft.AspNetCore.SignalR;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Agent;
using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.WebAPI.Hubs;
/// <summary>
/// SignalR Hub for real-time communicationSignalR Hub
/// Provides real-time updates for agents and workflows
//// </summary>
public class MultiAgentHub : Hub
{
    private readonly ILogger<MultiAgentHub> _logger;
    private readonly IAgentManager _agentManager;
    private readonly IWorkflowService _workflowService;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="agentManager">Agent manager</param>
    /// <param name="workflowService">Workflow service</param>
    public MultiAgentHub(
        ILogger<MultiAgentHub> logger,
        IAgentManager agentManager,
        IWorkflowService workflowService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    }
    /// <summary>
    /// Called when client connects
    /// </summary>
    /// <returns>Task</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to MultiAgent Hub", Context.ConnectionId);
        // Send current system status to newly connected client
        await SendSystemStatus();
        await base.OnConnectedAsync();
    }
    /// <summary>
    /// Called when client disconnects
    /// </summary>
    /// <param name="exception">Exception if any（
    /// <returns>Task</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from MultiAgent Hub. Exception: {Exception}",
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }
    /// <summary>
    /// Join agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    public async Task JoinAgentGroup(string agentId)
    {
        try
        {
            var groupName = $"agent_{agentId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined agent group {GroupName}",
                Context.ConnectionId, groupName);
            // Send current agent status
            var agent = await _agentManager.GetAgentAsync(agentId);
            if (agent != null)
            {
                var agentInfo = await agent.GetAgentInfoAsync();
                var healthStatus = await agent.GetHealthStatusAsync();
                await Clients.Caller.SendAsync("AgentStatusUpdate", agentInfo);
                await Clients.Caller.SendAsync("AgentHealthUpdate", healthStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join agent group for agent {AgentId}", agentId);
            await Clients.Caller.SendAsync("Error", $"Failed to join agent group: {ex.Message}");
        }
    }
    /// <summary>
    /// Leave agent monitoring group
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Task</returns>
    public async Task LeaveAgentGroup(string agentId)
    {
        try
        {
            var groupName = $"agent_{agentId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left agent group {GroupName}",
                Context.ConnectionId, groupName);
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
    public async Task JoinWorkflowGroup(string workflowId)
    {
        try
        {
            var groupName = $"workflow_{workflowId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined workflow group {GroupName}",
                Context.ConnectionId, groupName);
            // Send current workflow status
            var workflow = await _workflowService.GetWorkflowAsync(workflowId);
            if (workflow != null)
            {
                await Clients.Caller.SendAsync("WorkflowStatusUpdate", workflow);
                // Send active executions
                var executions = await _workflowService.GetWorkflowExecutionsAsync(workflowId);
                var activeExecutions = executions.Where(e =>
                    e.Status == WorkflowStatus.Running ||
                    e.Status == WorkflowStatus.Paused);
                foreach (var execution in activeExecutions)
                {
                    await Clients.Caller.SendAsync("WorkflowExecutionUpdate", execution);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join workflow group for workflow {WorkflowId}", workflowId);
            await Clients.Caller.SendAsync("Error", $"Failed to join workflow group: {ex.Message}");
        }
    }
    /// <summary>
    /// Leave workflow monitoring group
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Task</returns>
    public async Task LeaveWorkflowGroup(string workflowId)
    {
        try
        {
            var groupName = $"workflow_{workflowId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left workflow group {GroupName}",
                Context.ConnectionId, groupName);
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
    public async Task RequestSystemStatus()
    {
        await SendSystemStatus();
    }
    /// <summary>
    /// Send system status to caller
    /// </summary>
    /// <returns>Task</returns>
    private async Task SendSystemStatus()
    {
        try
        {
            // Get all agents status
            var agents = await _agentManager.GetAllAgentsAsync();
            var agentInfos = new List<AgentInfo>();
            foreach (var agent in agents)
            {
                var agentInfo = await agent.GetAgentInfoAsync();
                agentInfos.Add(agentInfo);
            }
            // Get all workflows
            var workflows = await _workflowService.GetAllWorkflowsAsync();
            // Get system health
            var healthStatuses = await _agentManager.GetAllHealthStatusAsync();
            var systemStatus = new
            {
                Timestamp = DateTime.UtcNow,
                Agents = agentInfos,
                Workflows = workflows.Count(),
                HealthStatuses = healthStatuses
            };
            await Clients.Caller.SendAsync("SystemStatusUpdate", systemStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send system status");
            await Clients.Caller.SendAsync("Error", $"Failed to get system status: {ex.Message}");
        }
    }
}
/// <summary>
/// Hub context extensions for broadcasting updates
/// </summary>
public static class MultiAgentHubExtensions
{
    /// <summary>
    /// Broadcast agent status update
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="agentInfo">Agent information</param>
    /// <returns>Task</returns>
    public static async Task BroadcastAgentStatusUpdate(
        this IHubContext<MultiAgentHub> hubContext,
        AgentInfo agentInfo)
    {
        var groupName = $"agent_{agentInfo.Id}";
        await hubContext.Clients.Group(groupName).SendAsync("AgentStatusUpdate", agentInfo);
        await hubContext.Clients.All.SendAsync("SystemAgentUpdate", agentInfo);
    }
    /// <summary>
    /// Broadcast agent health update
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="healthStatus">Health status</param>
    /// <returns>Task</returns>
    public static async Task BroadcastAgentHealthUpdate(
        this IHubContext<MultiAgentHub> hubContext,
        AgentHealthStatus healthStatus)
    {
        var groupName = $"agent_{healthStatus.AgentId}";
        await hubContext.Clients.Group(groupName).SendAsync("AgentHealthUpdate", healthStatus);
        await hubContext.Clients.All.SendAsync("SystemHealthUpdate", healthStatus);
    }
    /// <summary>
    /// Broadcast agent execution result
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="executionResult">Execution result</param>
    /// <returns>Task</returns>
    public static async Task BroadcastAgentExecutionResult(
        this IHubContext<MultiAgentHub> hubContext,
        AgentExecutionResult executionResult)
    {
        var groupName = $"agent_{executionResult.AgentId}";
        await hubContext.Clients.Group(groupName).SendAsync("AgentExecutionResult", executionResult);
    }
    /// <summary>
    /// Broadcast workflow status update
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Task</returns>
    public static async Task BroadcastWorkflowStatusUpdate(
        this IHubContext<MultiAgentHub> hubContext,
        WorkflowDefinition workflow)
    {
        var groupName = $"workflow_{workflow.Id}";
        await hubContext.Clients.Group(groupName).SendAsync("WorkflowStatusUpdate", workflow);
        await hubContext.Clients.All.SendAsync("SystemWorkflowUpdate", workflow);
    }
    /// <summary>
    /// Broadcast workflow execution update
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="execution">Workflow execution</param>
    /// <returns>Task</returns>
    public static async Task BroadcastWorkflowExecutionUpdate(
        this IHubContext<MultiAgentHub> hubContext,
        WorkflowExecution execution)
    {
        var groupName = $"workflow_{execution.WorkflowId}";
        await hubContext.Clients.Group(groupName).SendAsync("WorkflowExecutionUpdate", execution);
        await hubContext.Clients.All.SendAsync("SystemExecutionUpdate", execution);
    }
    /// <summary>
    /// Broadcast workflow step execution update
    /// </summary>
    /// <param name="hubContext">Hub context
    /// <param name="stepExecution">Step execution</param>
    /// <returns>Task</returns>
    public static async Task BroadcastWorkflowStepExecutionUpdate(
        this IHubContext<MultiAgentHub> hubContext,
        WorkflowStepExecution stepExecution)
    {
        // Assuming we can get workflow ID from execution ID
        // This would need to be implemented based on your data structure
        await hubContext.Clients.All.SendAsync("WorkflowStepExecutionUpdate", stepExecution);
    }
}

