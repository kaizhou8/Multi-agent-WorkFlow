using Microsoft.AspNetCore.Mvc;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Agent;
namespace MultiAgent.WebAPI.Controllers;
/// <summary>
/// Agents management controller
/// Provides REST API endpoints for agent operations
//// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgentsController : ControllerBase
{
    private readonly ILogger<AgentsController> _logger;
    private readonly IAgentManager _agentManager;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="agentManager">Agent manager</param>
    public AgentsController(ILogger<AgentsController> logger, IAgentManager agentManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
    }
    /// <summary>
    /// Get all registered agents
    /// </summary>
    /// <returns>List of agents</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentInfo>>> GetAllAgents()
    {
        try
        {
            var agents = await _agentManager.GetAllAgentsAsync();
            var agentInfos = new List<AgentInfo>();
            foreach (var agent in agents)
            {
                var agentInfo = await agent.GetAgentInfoAsync();
                agentInfos.Add(agentInfo);
            }
            return Ok(agentInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all agents");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve agents");
        }
    }
    /// <summary>
    /// Get agent by ID
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Agent information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AgentInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentInfo>> GetAgent(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            var agentInfo = await agent.GetAgentInfoAsync();
            return Ok(agentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve agent");
        }
    }
    /// <summary>
    /// Execute command on agent
    /// </summary>
    /// <param name="id">Agent ID
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(AgentExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentExecutionResult>> ExecuteCommand(string id, [FromBody] AgentCommand command)
    {
        try
        {
            if (command == null)
            {
                return BadRequest("Command cannot be null");
            }
            // Ensure command is targeted to the correct agent
            command.AgentId = id;
            var result = await _agentManager.ExecuteCommandAsync(id, command);
            if (result.AgentId == id && !result.Success && result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command on agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to execute command");
        }
    }
    /// <summary>
    /// Get agent health status
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Health status</returns>
    [HttpGet("{id}/health")]
    [ProducesResponseType(typeof(AgentHealthStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentHealthStatus>> GetAgentHealth(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            var healthStatus = await agent.GetHealthStatusAsync();
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status for agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve health status");
        }
    }
    /// <summary>
    /// Get health status of all agents
    /// </summary>
    /// <returns>List of health statuses</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(IEnumerable<AgentHealthStatus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentHealthStatus>>> GetAllAgentsHealth()
    {
        try
        {
            var healthStatuses = await _agentManager.GetAllHealthStatusAsync();
            return Ok(healthStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status for all agents");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve health statuses");
        }
    }
    /// <summary>
    /// Start agent
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Action result</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartAgent(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            await agent.StartAsync();
            return Ok($"Agent {id} started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to start agent");
        }
    }
    /// <summary>
    /// Stop agent
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Action result</returns>
    [HttpPost("{id}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StopAgent(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            await agent.StopAsync();
            return Ok($"Agent {id} stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to stop agent");
        }
    }
    /// <summary>
    /// Pause agent
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Action result</returns>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PauseAgent(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            await agent.PauseAsync();
            return Ok($"Agent {id} paused successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to pause agent");
        }
    }
    /// <summary>
    /// Resume agent
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Action result</returns>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResumeAgent(string id)
    {
        try
        {
            var agent = await _agentManager.GetAgentAsync(id);
            if (agent == null)
            {
                return NotFound($"Agent with ID {id} not found");
            }
            await agent.ResumeAsync();
            return Ok($"Agent {id} resumed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume agent {AgentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to resume agent");
        }
    }
}

