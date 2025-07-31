using Microsoft.Extensions.Logging;
using MultiAgent.Core.Data;
using MultiAgent.Shared.Contracts;
using MultiAgent.Shared.Models.Agent;
using MultiAgent.Shared.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
namespace MultiAgent.Workflows.Services;
/// <summary>
/// Workflow service implementation
/// Manages workflow definitions and executions
//// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly MultiAgentDbContext _dbContext;
    private readonly IAgentManager _agentManager;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningExecutions;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="dbContext">Database context</param>
    /// <param name="agentManager">Agent manager</param>
    public WorkflowService(
        ILogger<WorkflowService> logger,
        MultiAgentDbContext dbContext,
        IAgentManager agentManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _runningExecutions = new ConcurrentDictionary<string, CancellationTokenSource>();
    }
    /// <summary>
    /// Create a new workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Created workflow</returns>
    public async Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow)
    {
        if (workflow == null)
            throw new ArgumentNullException(nameof(workflow));
        // Validate workflow
        var validationResult = await ValidateWorkflowAsync(workflow);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Workflow validation failed: {string.Join(", ", validationResult.Errors)}");
        }
        // Ensure unique ID
        if (string.IsNullOrEmpty(workflow.Id))
        {
            workflow.Id = Guid.NewGuid().ToString();
        }
        // Check for existing workflow with same ID
        var existingWorkflow = await _dbContext.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == workflow.Id);
        if (existingWorkflow != null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflow.Id} already exists");
        }
        _dbContext.WorkflowDefinitions.Add(workflow);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Created workflow {WorkflowId} ({WorkflowName})", workflow.Id, workflow.Name);
        return workflow;
    }
    /// <summary>
    /// Update an existing workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Updated workflow</returns>
    public async Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow)
    {
        if (workflow == null)
            throw new ArgumentNullException(nameof(workflow));
        // Validate workflow
        var validationResult = await ValidateWorkflowAsync(workflow);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Workflow validation failed: {string.Join(", ", validationResult.Errors)}");
        }
        var existingWorkflow = await _dbContext.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == workflow.Id);
        if (existingWorkflow == null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflow.Id} not found");
        }
        // Update properties
        existingWorkflow.Name = workflow.Name;
        existingWorkflow.Description = workflow.Description;
        existingWorkflow.Version = workflow.Version;
        existingWorkflow.ExecutionMode = workflow.ExecutionMode;
        existingWorkflow.Steps = workflow.Steps;
        existingWorkflow.InputParameters = workflow.InputParameters;
        existingWorkflow.OutputParameters = workflow.OutputParameters;
        existingWorkflow.Metadata = workflow.Metadata;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated workflow {WorkflowId} ({WorkflowName})", workflow.Id, workflow.Name);
        return existingWorkflow;
    }
    /// <summary>
    /// Delete a workflow definition
    /// </summary>
    /// <param name="workflowId">Workflow ID
    public async Task DeleteWorkflowAsync(string workflowId)
    {
        if (string.IsNullOrEmpty(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
        var workflow = await _dbContext.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflowId} not found");
        }
        // Check for running executions
        var runningExecutions = await _dbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflowId && e.Status == WorkflowStatus.Running)
            .CountAsync();
        if (runningExecutions > 0)
        {
            throw new InvalidOperationException($"Cannot delete workflow {workflowId} - it has {runningExecutions} running executions");
        }
        _dbContext.WorkflowDefinitions.Remove(workflow);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Deleted workflow {WorkflowId} ({WorkflowName})", workflowId, workflow.Name);
    }
    /// <summary>
    /// Get workflow definition by ID
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>Workflow definition</returns>
    public async Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId)
    {
        if (string.IsNullOrEmpty(workflowId))
            return null;
        return await _dbContext.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == workflowId);
    }
    /// <summary>
    /// Get all workflow definitions
    /// </summary>
    /// <returns>List of workflows</returns>
    public async Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowsAsync()
    {
        return await _dbContext.WorkflowDefinitions
            .OrderBy(w => w.Name)
            .ToListAsync();
    }
    /// <summary>
    /// Execute a workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <param name="inputData">Input data</param>
    /// <param name="executedBy">Executed by user</param>
    /// <returns>Workflow execution</returns>
    public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> inputData, string executedBy)
    {
        if (string.IsNullOrEmpty(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));
        var workflow = await GetWorkflowAsync(workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflowId} not found");
        }
        // Create execution record
        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowId = workflowId,
            Status = WorkflowStatus.Running,
            InputData = inputData ?? new Dictionary<string, object>(),
            ExecutedBy = executedBy ?? "system",
            StartTime = DateTime.UtcNow
        };
        _dbContext.WorkflowExecutions.Add(execution);
        await _dbContext.SaveChangesAsync();
        // Create cancellation token for this execution
        var cancellationTokenSource = new CancellationTokenSource();
        _runningExecutions.TryAdd(execution.Id, cancellationTokenSource);
        _logger.LogInformation("Starting execution {ExecutionId} for workflow {WorkflowId}", execution.Id, workflowId);
        // Execute workflow asynchronously
        _ = Task.Run(async () => await ExecuteWorkflowInternalAsync(workflow, execution, cancellationTokenSource.Token));
        return execution;
    }
    /// <summary>
    /// Internal workflow execution logic
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <param name="execution">Workflow execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ExecuteWorkflowInternalAsync(WorkflowDefinition workflow, WorkflowExecution execution, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing workflow {WorkflowId} with mode {ExecutionMode}", workflow.Id, workflow.ExecutionMode);
            switch (workflow.ExecutionMode)
            {
                case WorkflowExecutionMode.Sequential:
                    await ExecuteSequentialWorkflowAsync(workflow, execution, cancellationToken);
                    break;
                case WorkflowExecutionMode.Parallel:
                    await ExecuteParallelWorkflowAsync(workflow, execution, cancellationToken);
                    break;
                case WorkflowExecutionMode.Conditional:
                    await ExecuteConditionalWorkflowAsync(workflow, execution, cancellationToken);
                    break;
                case WorkflowExecutionMode.Mixed:
                    await ExecuteMixedWorkflowAsync(workflow, execution, cancellationToken);
                    break;
                default:
                    throw new NotSupportedException($"Execution mode {workflow.ExecutionMode} is not supported");
            }
            // Mark as completed
            execution.Status = WorkflowStatus.Completed;
            execution.EndTime = DateTime.UtcNow;
            execution.Logs.Add($"Workflow execution completed successfully at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            _logger.LogInformation("Workflow execution {ExecutionId} completed successfully", execution.Id);
        }
        catch (OperationCanceledException)
        {
            execution.Status = WorkflowStatus.Cancelled;
            execution.EndTime = DateTime.UtcNow;
            execution.Logs.Add($"Workflow execution cancelled at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            _logger.LogInformation("Workflow execution {ExecutionId} was cancelled", execution.Id);
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowStatus.Failed;
            execution.EndTime = DateTime.UtcNow;
            execution.ErrorMessage = ex.Message;
            execution.Logs.Add($"Workflow execution failed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {ex.Message}");
            _logger.LogError(ex, "Workflow execution {ExecutionId} failed", execution.Id);
        }
        finally
        {
            // Clean up
            _runningExecutions.TryRemove(execution.Id, out _);
            // Update execution in database
            try
            {
                var existingExecution = await _dbContext.WorkflowExecutions
                    .FirstOrDefaultAsync(e => e.Id == execution.Id);
                if (existingExecution != null)
                {
                    existingExecution.Status = execution.Status;
                    existingExecution.EndTime = execution.EndTime;
                    existingExecution.ErrorMessage = execution.ErrorMessage;
                    existingExecution.OutputData = execution.OutputData;
                    existingExecution.Logs = execution.Logs;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update execution {ExecutionId} in database", execution.Id);
            }
        }
    }
    /// <summary>
    /// Execute workflow steps sequentially
    /// </summary>
    private async Task ExecuteSequentialWorkflowAsync(WorkflowDefinition workflow, WorkflowExecution execution, CancellationToken cancellationToken)
    {
        var context = new Dictionary<string, object>(execution.InputData);
        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stepExecution = await ExecuteWorkflowStepAsync(step, execution, context, cancellationToken);
            execution.StepExecutions.Add(stepExecution);
            if (!stepExecution.Status.Equals(WorkflowStatus.Completed))
            {
                throw new InvalidOperationException($"Step {step.Name} failed: {stepExecution.ErrorMessage}");
            }
            // Merge step output into context
            foreach (var output in stepExecution.OutputData)
            {
                context[output.Key] = output.Value;
            }
        }
        execution.OutputData = context;
    }
    /// <summary>
    /// Execute workflow steps in parallel
    /// </summary>
    private async Task ExecuteParallelWorkflowAsync(WorkflowDefinition workflow, WorkflowExecution execution, CancellationToken cancellationToken)
    {
        var context = new Dictionary<string, object>(execution.InputData);
        var stepTasks = workflow.Steps.Select(step =>
            ExecuteWorkflowStepAsync(step, execution, context, cancellationToken)).ToArray();
        var stepExecutions = await Task.WhenAll(stepTasks);
        foreach (var stepExecution in stepExecutions)
        {
            execution.StepExecutions.Add(stepExecution);
            if (!stepExecution.Status.Equals(WorkflowStatus.Completed))
            {
                throw new InvalidOperationException($"Step failed: {stepExecution.ErrorMessage}");
            }
            // Merge step output into context
            foreach (var output in stepExecution.OutputData)
            {
                context[output.Key] = output.Value;
            }
        }
        execution.OutputData = context;
    }
    /// <summary>
    /// Execute workflow with conditional logic
    /// </summary>
    private async Task ExecuteConditionalWorkflowAsync(WorkflowDefinition workflow, WorkflowExecution execution, CancellationToken cancellationToken)
    {
        // For now, implement as sequential with condition checking
        var context = new Dictionary<string, object>(execution.InputData);
        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Check execution condition
            if (!string.IsNullOrEmpty(step.ExecutionCondition))
            {
                var shouldExecute = EvaluateCondition(step.ExecutionCondition, context);
                if (!shouldExecute)
                {
                    execution.Logs.Add($"Skipping step {step.Name} - condition not met: {step.ExecutionCondition}");
                    continue;
                }
            }
            var stepExecution = await ExecuteWorkflowStepAsync(step, execution, context, cancellationToken);
            execution.StepExecutions.Add(stepExecution);
            if (!stepExecution.Status.Equals(WorkflowStatus.Completed))
            {
                throw new InvalidOperationException($"Step {step.Name} failed: {stepExecution.ErrorMessage}");
            }
            // Merge step output into context
            foreach (var output in stepExecution.OutputData)
            {
                context[output.Key] = output.Value;
            }
        }
        execution.OutputData = context;
    }
    /// <summary>
    /// Execute workflow with mixed execution modes
    /// </summary>
    private async Task ExecuteMixedWorkflowAsync(WorkflowDefinition workflow, WorkflowExecution execution, CancellationToken cancellationToken)
    {
        // For now, implement as conditional
        await ExecuteConditionalWorkflowAsync(workflow, execution, cancellationToken);
    }
    /// <summary>
    /// Execute a single workflow step
    /// </summary>
    private async Task<WorkflowStepExecution> ExecuteWorkflowStepAsync(WorkflowStep step, WorkflowExecution execution, Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        var stepExecution = new WorkflowStepExecution
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowExecutionId = execution.Id,
            StepId = step.Id,
            AgentId = step.AgentId,
            Status = WorkflowStatus.Running,
            StartTime = DateTime.UtcNow
        };
        try
        {
            // Prepare input data
            var inputData = new Dictionary<string, object>();
            foreach (var mapping in step.InputMapping)
            {
                if (context.TryGetValue(mapping.Value, out var value))
                {
                    inputData[mapping.Key] = value;
                }
            }
            // Add step configuration
            foreach (var config in step.Configuration)
            {
                inputData[config.Key] = config.Value;
            }
            stepExecution.InputData = inputData;
            // Create agent command
            var command = new AgentCommand
            {
                Id = Guid.NewGuid().ToString(),
                AgentId = step.AgentId,
                Type = step.Configuration.TryGetValue("operation", out var operation) ? operation.ToString() ?? "execute" : "execute",
                Parameters = inputData,
                TimeoutSeconds = step.TimeoutSeconds
            };
            // Execute command
            var result = await _agentManager.ExecuteCommandAsync(step.AgentId, command);
            stepExecution.Status = result.Success ? WorkflowStatus.Completed : WorkflowStatus.Failed;
            stepExecution.OutputData = result.OutputData;
            stepExecution.ErrorMessage = result.ErrorMessage;
            stepExecution.EndTime = DateTime.UtcNow;
            stepExecution.Logs = result.Logs;
            _logger.LogInformation("Step {StepName} executed with result: {Success}", step.Name, result.Success);
        }
        catch (Exception ex)
        {
            stepExecution.Status = WorkflowStatus.Failed;
            stepExecution.ErrorMessage = ex.Message;
            stepExecution.EndTime = DateTime.UtcNow;
            stepExecution.Logs.Add($"Step execution failed: {ex.Message}");
            _logger.LogError(ex, "Failed to execute step {StepName}", step.Name);
        }
        return stepExecution;
    }
    /// <summary>
    /// Evaluate execution condition
    /// </summary>
    private bool EvaluateCondition(string condition, Dictionary<string, object> context)
    {
        // Simple condition evaluation - can be enhanced with expression parser -
        try
        {
            // For now, support simple key existence checks
            if (condition.StartsWith("exists:"))
            {
                var key = condition.Substring(7);
                return context.ContainsKey(key);
            }
            // Default to true for unsupported conditionstrue
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// Get workflow execution by ID
    /// </summary>
    /// <param name="executionId">Execution ID
    /// <returns>Workflow execution</returns>
    public async Task<WorkflowExecution?> GetWorkflowExecutionAsync(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
            return null;
        return await _dbContext.WorkflowExecutions
            .Include(e => e.StepExecutions)
            .FirstOrDefaultAsync(e => e.Id == executionId);
    }
    /// <summary>
    /// Get all executions for a workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>List of executions</returns>
    public async Task<IEnumerable<WorkflowExecution>> GetWorkflowExecutionsAsync(string workflowId)
    {
        if (string.IsNullOrEmpty(workflowId))
            return Enumerable.Empty<WorkflowExecution>();
        return await _dbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflowId)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }
    /// <summary>
    /// Cancel a running workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    public async Task CancelWorkflowExecutionAsync(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
            throw new ArgumentException("Execution ID cannot be null or empty", nameof(executionId));
        if (_runningExecutions.TryGetValue(executionId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            _logger.LogInformation("Cancelled workflow execution {ExecutionId}", executionId);
        }
        else
        {
            _logger.LogWarning("Attempted to cancel non-running execution {ExecutionId}", executionId);
        }
    }
    /// <summary>
    /// Pause a running workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    public async Task PauseWorkflowExecutionAsync(string executionId)
    {
        // Implementation would require more complex state management
        throw new NotImplementedException("Pause functionality is not yet implemented");
    }
    /// <summary>
    /// Resume a paused workflow execution
    /// </summary>
    /// <param name="executionId">Execution ID
    public async Task ResumeWorkflowExecutionAsync(string executionId)
    {
        // Implementation would require more complex state management
        throw new NotImplementedException("Resume functionality is not yet implemented");
    }
    /// <summary>
    /// Validate workflow definition
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Validation result</returns>
    public async Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow)
    {
        var result = new WorkflowValidationResult { IsValid = true };
        if (workflow == null)
        {
            result.IsValid = false;
            result.Errors.Add("Workflow definition cannot be null");
            return result;
        }
        // Validate basic properties
        if (string.IsNullOrEmpty(workflow.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Workflow name is required");
        }
        if (workflow.Steps == null || !workflow.Steps.Any())
        {
            result.IsValid = false;
            result.Errors.Add("Workflow must have at least one step");
        }
        else
        {
            // Validate steps
            var stepIds = new HashSet<string>();
            foreach (var step in workflow.Steps)
            {
                if (string.IsNullOrEmpty(step.Id))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Step {step.Name} must have an ID");
                }
                else if (!stepIds.Add(step.Id))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Duplicate step ID: {step.Id}");
                }
                if (string.IsNullOrEmpty(step.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Step {step.Id} must have a name");
                }
                if (string.IsNullOrEmpty(step.AgentId))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Step {step.Name} must specify an agent ID");
                }
                else
                {
                    // Check if agent exists
                    var agent = await _agentManager.GetAgentAsync(step.AgentId);
                    if (agent == null)
                    {
                        result.Warnings.Add($"Agent {step.AgentId} for step {step.Name} is not currently registered");
                    }
                }
                // Validate dependencies
                foreach (var dependency in step.Dependencies)
                {
                    if (!stepIds.Contains(dependency))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Step {step.Name} depends on non-existent step {dependency}");
                    }
                }
            }
        }
        return result;
    }
}

