using Microsoft.Extensions.Logging;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Agent;
using System.Collections.Concurrent;
namespace MultiAgent.Agents.Services;
/// <summary>
/// Agent manager implementation
/// Manages the lifecycle and coordination of multiple agents
//// </summary>
public class AgentManager : IAgentManager
{
    private readonly ILogger<AgentManager> _logger;
    private readonly ConcurrentDictionary<string, IAgent> _agents;
    private readonly ConcurrentDictionary<string, DateTime> _lastHealthCheck;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public AgentManager(ILogger<AgentManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agents = new ConcurrentDictionary<string, IAgent>();
        _lastHealthCheck = new ConcurrentDictionary<string, DateTime>();
    }
    /// <summary>
    /// Register an agent
    /// </summary>
    /// <param name="agent">Agent to register</param>
    public async Task RegisterAgentAsync(IAgent agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));
        if (_agents.ContainsKey(agent.Id))
        {
            _logger.LogWarning("Agent {AgentId} is already registered, updating registration", agent.Id);
        }
        _agents.AddOrUpdate(agent.Id, agent, (key, oldAgent) => agent);
        _lastHealthCheck[agent.Id] = DateTime.UtcNow;
        await agent.StartAsync();
        _logger.LogInformation("Agent {AgentId} ({AgentName}) registered successfully", agent.Id, agent.Name);
    }
    /// <summary>
    /// Unregister an agent
    /// </summary>
    /// <param name="agentId">Agent ID to unregisterID</param>
    public async Task UnregisterAgentAsync(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        if (_agents.TryRemove(agentId, out var agent))
        {
            await agent.StopAsync();
            _lastHealthCheck.TryRemove(agentId, out _);
            _logger.LogInformation("Agent {AgentId} ({AgentName}) unregistered successfully", agent.Id, agent.Name);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister non-existent agent {AgentId}", agentId);
        }
    }
    /// <summary>
    /// Get agent by ID
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Agent instance</returns>
    public Task<IAgent?> GetAgentAsync(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            return Task.FromResult<IAgent?>(null);
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }
    /// <summary>
    /// Get all registered agents
    /// </summary>
    /// <returns>List of agents</returns>
    public Task<IEnumerable<IAgent>> GetAllAgentsAsync()
    {
        return Task.FromResult<IEnumerable<IAgent>>(_agents.Values.ToList());
    }
    /// <summary>
    /// Execute command on specific agent
    /// </summary>
    /// <param name="agentId">Target agent ID
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    public async Task<AgentExecutionResult> ExecuteCommandAsync(string agentId, AgentCommand command)
    {
        if (string.IsNullOrEmpty(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        var agent = await GetAgentAsync(agentId);
        if (agent == null)
        {
            _logger.LogError("Agent {AgentId} not found for command execution", agentId);
            return new AgentExecutionResult
            {
                AgentId = agentId,
                CommandId = command.Id,
                Success = false,
                ErrorMessage = $"Agent {agentId} not found"
            };
        }
        try
        {
            _logger.LogInformation("Executing command {CommandId} on agent {AgentId}", command.Id, agentId);
            var result = await agent.ExecuteAsync(command);
            // Update last health check
            _lastHealthCheck[agentId] = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {CommandId} on agent {AgentId}", command.Id, agentId);
            return new AgentExecutionResult
            {
                AgentId = agentId,
                CommandId = command.Id,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    /// <summary>
    /// Get health status of all agents
    /// </summary>
    /// <returns>Health status list</returns>
    public async Task<IEnumerable<AgentHealthStatus>> GetAllHealthStatusAsync()
    {
        var healthStatuses = new List<AgentHealthStatus>();
        foreach (var agent in _agents.Values)
        {
            try
            {
                var healthStatus = await agent.GetHealthStatusAsync();
                healthStatuses.Add(healthStatus);
                // Update last health check
                _lastHealthCheck[agent.Id] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status for agent {AgentId}", agent.Id);
                // Add error status
                healthStatuses.Add(new AgentHealthStatus
                {
                    AgentId = agent.Id,
                    Status = "Error",
                    LastCheckTime = DateTime.UtcNow,
                    Metrics = new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    }
                });
            }
        }
        return healthStatuses;
    }
    /// <summary>
    /// Get agents by capability
    /// </summary>
    /// <param name="operation">Required operation</param>
    /// <returns>Agents that support the operation</returns>
    public async Task<IEnumerable<IAgent>> GetAgentsByCapabilityAsync(string operation)
    {
        if (string.IsNullOrEmpty(operation))
            return Enumerable.Empty<IAgent>();
        var capableAgents = new List<IAgent>();
        foreach (var agent in _agents.Values)
        {
            if (agent.Capabilities.SupportedOperations.Contains(operation))
            {
                capableAgents.Add(agent);
            }
        }
        return capableAgents;
    }
    /// <summary>
    /// Start all agents
    /// </summary>
    public async Task StartAllAgentsAsync()
    {
        _logger.LogInformation("Starting all {AgentCount} registered agents", _agents.Count);
        var startTasks = _agents.Values.Select(agent => agent.StartAsync());
        await Task.WhenAll(startTasks);
        _logger.LogInformation("All agents started successfully");
    }
    /// <summary>
    /// Stop all agents
    /// </summary>
    public async Task StopAllAgentsAsync()
    {
        _logger.LogInformation("Stopping all {AgentCount} registered agents", _agents.Count);
        var stopTasks = _agents.Values.Select(agent => agent.StopAsync());
        await Task.WhenAll(stopTasks);
        _logger.LogInformation("All agents stopped successfully");
    }
    /// <summary>
    /// Get agent statistics
    /// </summary>
    /// <returns>Agent statistics</returns>
    public Task<Dictionary<string, object>> GetStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalAgents"] = _agents.Count,
            ["AgentsByStatus"] = _agents.Values
                .GroupBy(a => a.GetAgentInfoAsync().Result.Status)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["LastHealthCheckTimes"] = _lastHealthCheck.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString("yyyy-MM-dd HH:mm:ss UTC"))
        };
        return Task.FromResult(stats);
    }
}

