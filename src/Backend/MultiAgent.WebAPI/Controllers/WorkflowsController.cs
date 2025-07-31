using Microsoft.AspNetCore.Mvc;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.WebAPI.Controllers;
/// <summary>
/// Workflows management controller
/// Provides REST API endpoints for workflow operations
//// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkflowsController : ControllerBase
{
    private readonly ILogger<WorkflowsController> _logger;
    private readonly IWorkflowService _workflowService;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="workflowService">Workflow service</param>
    public WorkflowsController(ILogger<WorkflowsController> logger, IWorkflowService workflowService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    }
    /// <summary>
    /// Get all workflow definitions
    /// </summary>
    /// <returns>List of workflows</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowDefinition>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkflowDefinition>>> GetAllWorkflows()
    {
        try
        {
            var workflows = await _workflowService.GetAllWorkflowsAsync();
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all workflows");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve workflows");
        }
    }
    /// <summary>
    /// Get workflow by ID
    /// </summary>
    /// <param name="id">Workflow ID
    /// <returns>Workflow definition</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDefinition>> GetWorkflow(string id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowAsync(id);
            if (workflow == null)
            {
                return NotFound($"Workflow with ID {id} not found");
            }
            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve workflow");
        }
    }
    /// <summary>
    /// Create new workflow
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Created workflow</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowDefinition>> CreateWorkflow([FromBody] WorkflowDefinition workflow)
    {
        try
        {
            if (workflow == null)
            {
                return BadRequest("Workflow definition cannot be null");
            }
            var createdWorkflow = await _workflowService.CreateWorkflowAsync(workflow);
            return CreatedAtAction(nameof(GetWorkflow), new { id = createdWorkflow.Id }, createdWorkflow);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create workflow due to validation error");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create workflow");
        }
    }
    /// <summary>
    /// Update existing workflow
    /// </summary>
    /// <param name="id">Workflow ID
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Updated workflow</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WorkflowDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDefinition>> UpdateWorkflow(string id, [FromBody] WorkflowDefinition workflow)
    {
        try
        {
            if (workflow == null)
            {
                return BadRequest("Workflow definition cannot be null");
            }
            if (workflow.Id != id)
            {
                workflow.Id = id; // Ensure ID matches route parameterID
            }
            var updatedWorkflow = await _workflowService.UpdateWorkflowAsync(workflow);
            return Ok(updatedWorkflow);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound($"Workflow with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update workflow due to validation error");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update workflow");
        }
    }
    /// <summary>
    /// Delete workflow
    /// </summary>
    /// <param name="id">Workflow ID
    /// <returns>Action result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteWorkflow(string id)
    {
        try
        {
            await _workflowService.DeleteWorkflowAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound($"Workflow with ID {id} not found");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("running executions"))
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete workflow");
        }
    }
    /// <summary>
    /// Execute workflow
    /// </summary>
    /// <param name="id">Workflow ID
    /// <param name="request">Execution request</param>
    /// <returns>Workflow execution</returns>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(WorkflowExecution), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowExecution>> ExecuteWorkflow(string id, [FromBody] WorkflowExecutionRequest request)
    {
        try
        {
            if (request == null)
            {
                request = new WorkflowExecutionRequest();
            }
            var execution = await _workflowService.ExecuteWorkflowAsync(
                id,
                request.InputData ?? new Dictionary<string, object>(),
                request.ExecutedBy ?? "api-user");
            return Accepted(execution);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound($"Workflow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to execute workflow");
        }
    }
    /// <summary>
    /// Get workflow execution
    /// </summary>
    /// <param name="id">Workflow ID
    /// <param name="executionId">Execution ID
    /// <returns>Workflow execution</returns>
    [HttpGet("{id}/executions/{executionId}")]
    [ProducesResponseType(typeof(WorkflowExecution), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowExecution>> GetWorkflowExecution(string id, string executionId)
    {
        try
        {
            var execution = await _workflowService.GetWorkflowExecutionAsync(executionId);
            if (execution == null || execution.WorkflowId != id)
            {
                return NotFound($"Execution {executionId} not found for workflow {id}");
            }
            return Ok(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution {ExecutionId} for workflow {WorkflowId}", executionId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve execution");
        }
    }
    /// <summary>
    /// Get all executions for workflow
    /// </summary>
    /// <param name="id">Workflow ID
    /// <returns>List of executions</returns>
    [HttpGet("{id}/executions")]
    [ProducesResponseType(typeof(IEnumerable<WorkflowExecution>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkflowExecution>>> GetWorkflowExecutions(string id)
    {
        try
        {
            var executions = await _workflowService.GetWorkflowExecutionsAsync(id);
            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get executions for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve executions");
        }
    }
    /// <summary>
    /// Cancel workflow execution
    /// </summary>
    /// <param name="id">Workflow ID
    /// <param name="executionId">Execution ID
    /// <returns>Action result</returns>
    [HttpPost("{id}/executions/{executionId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelWorkflowExecution(string id, string executionId)
    {
        try
        {
            await _workflowService.CancelWorkflowExecutionAsync(executionId);
            return Ok($"Execution {executionId} cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution {ExecutionId}", executionId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to cancel execution");
        }
    }
    /// <summary>
    /// Validate workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(WorkflowValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowValidationResult>> ValidateWorkflow([FromBody] WorkflowDefinition workflow)
    {
        try
        {
            if (workflow == null)
            {
                return BadRequest("Workflow definition cannot be null");
            }
            var validationResult = await _workflowService.ValidateWorkflowAsync(workflow);
            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to validate workflow");
        }
    }
}
/// <summary>
/// Workflow execution request
/// </summary>
public class WorkflowExecutionRequest
{
    /// <summary>
    /// Input data for execution
    /// </summary>
    public Dictionary<string, object>? InputData { get; set; }
    /// <summary>
    /// User executing the workflow
    /// </summary>
    public string? ExecutedBy { get; set; }
}

