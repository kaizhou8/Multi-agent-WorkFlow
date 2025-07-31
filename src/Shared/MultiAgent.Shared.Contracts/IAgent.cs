using MultiAgent.Shared.Models.Agent;
namespace MultiAgent.Shared.Contracts;
/// <summary>
/// Core agent interface
/// Defines the contract for all agents in the Multi-Agent system
//// </summary>
public interface IAgent
{
    /// <summary>
    /// Agent unique identifier
    /// </summary>
    string Id { get; }
    /// <summary>
    /// Agent name
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Agent capabilities
    /// </summary>
    AgentCapabilities Capabilities { get; }
    /// <summary>
    /// Execute a command asynchronously
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    Task<AgentExecutionResult> ExecuteAsync(AgentCommand command);
    /// <summary>
    /// Get agent health status
    /// </summary>
    /// <returns>Health status</returns>
    Task<AgentHealthStatus> GetHealthStatusAsync();
    /// <summary>
    /// Get agent information
    /// </summary>
    /// <returns>Agent information</returns>
    Task<AgentInfo> GetAgentInfoAsync();
    /// <summary>
    /// Start the agent
    /// </summary>
    Task StartAsync();
    /// <summary>
    /// Stop the agent
    /// </summary>
    Task StopAsync();
    /// <summary>
    /// Pause the agent
    /// </summary>
    Task PauseAsync();
    /// <summary>
    /// Resume the agent
    /// </summary>
    Task ResumeAsync();
}
/// <summary>
/// Agent manager interface
/// Manages the lifecycle and coordination of multiple agents
//// </summary>
public interface IAgentManager
{
    /// <summary>
    /// Register an agent
    /// </summary>
    /// <param name="agent">Agent to register</param>
    Task RegisterAgentAsync(IAgent agent);
    /// <summary>
    /// Unregister an agent
    /// </summary>
    /// <param name="agentId">Agent ID to unregisterID</param>
    Task UnregisterAgentAsync(string agentId);
    /// <summary>
    /// Get agent by ID
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Agent instance</returns>
    Task<IAgent?> GetAgentAsync(string agentId);
    /// <summary>
    /// Get all registered agents
    /// </summary>
    /// <returns>List of agents</returns>
    Task<IEnumerable<IAgent>> GetAllAgentsAsync();
    /// <summary>
    /// Execute command on specific agent
    /// </summary>
    /// <param name="agentId">Target agent ID
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    Task<AgentExecutionResult> ExecuteCommandAsync(string agentId, AgentCommand command);
    /// <summary>
    /// Get health status of all agents
    /// </summary>
    /// <returns>Health status list</returns>
    Task<IEnumerable<AgentHealthStatus>> GetAllHealthStatusAsync();
}

