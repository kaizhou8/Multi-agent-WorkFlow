using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiAgent.Shared.Models.Agent;
using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.Desktop.Services;
/// <summary>
/// API service interface
/// Provides HTTP communication with the backend Web API
//// </summary>
public interface IApiService
{
    /// <summary>
    /// Set authentication token
    /// </summary>
    /// <param name="token">JWT token
    void SetAuthToken(string token);
    /// <summary>
    /// Clear authentication token
    /// </summary>
    void ClearAuthToken();
    // Agent operations
    Task<IEnumerable<AgentInfo>> GetAgentsAsync();
    Task<AgentInfo?> GetAgentAsync(string id);
    Task<AgentExecutionResult> ExecuteAgentCommandAsync(string agentId, AgentCommand command);
    Task<AgentHealthStatus> GetAgentHealthAsync(string agentId);
    Task<IEnumerable<AgentHealthStatus>> GetAllAgentsHealthAsync();
    Task StartAgentAsync(string agentId);
    Task StopAgentAsync(string agentId);
    Task PauseAgentAsync(string agentId);
    Task ResumeAgentAsync(string agentId);
    // Workflow operations
    Task<IEnumerable<WorkflowDefinition>> GetWorkflowsAsync();
    Task<WorkflowDefinition?> GetWorkflowAsync(string id);
    Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow);
    Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow);
    Task DeleteWorkflowAsync(string id);
    Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object>? inputData = null);
    Task<WorkflowExecution?> GetWorkflowExecutionAsync(string workflowId, string executionId);
    Task<IEnumerable<WorkflowExecution>> GetWorkflowExecutionsAsync(string workflowId);
    Task CancelWorkflowExecutionAsync(string workflowId, string executionId);
    // AI operations / AI
    Task<string> GenerateTextAsync(string prompt, int maxTokens = 1000, float temperature = 0.7f);
    Task<string> ChatAsync(List<string> messages, int maxTokens = 1000);
    Task<object?> ExecuteAIFunctionAsync(string functionName, Dictionary<string, object>? parameters = null);
    Task<string> AnalyzeTextAsync(string text, string analysisType = "general");
    Task<IEnumerable<string>> GetAvailableModelsAsync();
    Task<string> GetCurrentModelAsync();
    Task SetModelAsync(string modelName);
    // Authentication
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
}
/// <summary>
/// API service implementation
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client
    /// <param name="logger">Logger</param>
    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
    /// <summary>
    /// Set authentication token
    /// </summary>
    /// <param name="token">JWT token
    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _logger.LogDebug("Authentication token set");
    }
    /// <summary>
    /// Clear authentication token
    /// </summary>
    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _logger.LogDebug("Authentication token cleared");
    }
    #region Agent Operations /
    /// <summary>
    /// Get all agents
    /// </summary>
    /// <returns>List of agents</returns>
    public async Task<IEnumerable<AgentInfo>> GetAgentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("agents");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AgentInfo>>(json, _jsonOptions) ?? Enumerable.Empty<AgentInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agents");
            throw;
        }
    }
    /// <summary>
    /// Get agent by ID
    /// </summary>
    /// <param name="id">Agent ID
    /// <returns>Agent information</returns>
    public async Task<AgentInfo?> GetAgentAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"agents/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentInfo>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent {AgentId}", id);
            throw;
        }
    }
    /// <summary>
    /// Execute agent command
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    public async Task<AgentExecutionResult> ExecuteAgentCommandAsync(string agentId, AgentCommand command)
    {
        try
        {
            var json = JsonSerializer.Serialize(command, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"agents/{agentId}/execute", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentExecutionResult>(responseJson, _jsonOptions) ?? new AgentExecutionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command on agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Get agent health status
    /// </summary>
    /// <param name="agentId">Agent ID
    /// <returns>Health status</returns>
    public async Task<AgentHealthStatus> GetAgentHealthAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"agents/{agentId}/health");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentHealthStatus>(json, _jsonOptions) ?? new AgentHealthStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status for agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Get health status of all agents
    /// </summary>
    /// <returns>List of health statuses</returns>
    public async Task<IEnumerable<AgentHealthStatus>> GetAllAgentsHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("agents/health");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AgentHealthStatus>>(json, _jsonOptions) ?? Enumerable.Empty<AgentHealthStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status for all agents");
            throw;
        }
    }
    /// <summary>
    /// Start agent
    /// </summary>
    /// <param name="agentId">Agent ID
    public async Task StartAgentAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"agents/{agentId}/start", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Stop agent
    /// </summary>
    /// <param name="agentId">Agent ID
    public async Task StopAgentAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"agents/{agentId}/stop", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Pause agent
    /// </summary>
    /// <param name="agentId">Agent ID
    public async Task PauseAgentAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"agents/{agentId}/pause", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause agent {AgentId}", agentId);
            throw;
        }
    }
    /// <summary>
    /// Resume agent
    /// </summary>
    /// <param name="agentId">Agent ID
    public async Task ResumeAgentAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"agents/{agentId}/resume", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume agent {AgentId}", agentId);
            throw;
        }
    }
    #endregion
    #region Workflow Operations /
    /// <summary>
    /// Get all workflows
    /// </summary>
    /// <returns>List of workflows</returns>
    public async Task<IEnumerable<WorkflowDefinition>> GetWorkflowsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("workflows");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<WorkflowDefinition>>(json, _jsonOptions) ?? Enumerable.Empty<WorkflowDefinition>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflows");
            throw;
        }
    }
    /// <summary>
    /// Get workflow by ID
    /// </summary>
    /// <param name="id">Workflow ID
    /// <returns>Workflow definition</returns>
    public async Task<WorkflowDefinition?> GetWorkflowAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"workflows/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow {WorkflowId}", id);
            throw;
        }
    }
    /// <summary>
    /// Create new workflow
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Created workflow</returns>
    public async Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            var json = JsonSerializer.Serialize(workflow, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("workflows", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkflowDefinition>(responseJson, _jsonOptions) ?? workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            throw;
        }
    }
    /// <summary>
    /// Update existing workflow
    /// </summary>
    /// <param name="workflow">Workflow definition</param>
    /// <returns>Updated workflow</returns>
    public async Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            var json = JsonSerializer.Serialize(workflow, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"workflows/{workflow.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkflowDefinition>(responseJson, _jsonOptions) ?? workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }
    /// <summary>
    /// Delete workflow
    /// </summary>
    /// <param name="id">Workflow ID
    public async Task DeleteWorkflowAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"workflows/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow {WorkflowId}", id);
            throw;
        }
    }
    /// <summary>
    /// Execute workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <param name="inputData">Input data</param>
    /// <returns>Workflow execution</returns>
    public async Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object>? inputData = null)
    {
        try
        {
            var request = new { InputData = inputData, ExecutedBy = "desktop-user" };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"workflows/{workflowId}/execute", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkflowExecution>(responseJson, _jsonOptions) ?? new WorkflowExecution();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow {WorkflowId}", workflowId);
            throw;
        }
    }
    /// <summary>
    /// Get workflow execution
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <param name="executionId">Execution ID
    /// <returns>Workflow execution</returns>
    public async Task<WorkflowExecution?> GetWorkflowExecutionAsync(string workflowId, string executionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"workflows/{workflowId}/executions/{executionId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkflowExecution>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution {ExecutionId} for workflow {WorkflowId}", executionId, workflowId);
            throw;
        }
    }
    /// <summary>
    /// Get all executions for workflow
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <returns>List of executions</returns>
    public async Task<IEnumerable<WorkflowExecution>> GetWorkflowExecutionsAsync(string workflowId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"workflows/{workflowId}/executions");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<WorkflowExecution>>(json, _jsonOptions) ?? Enumerable.Empty<WorkflowExecution>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get executions for workflow {WorkflowId}", workflowId);
            throw;
        }
    }
    /// <summary>
    /// Cancel workflow execution
    /// </summary>
    /// <param name="workflowId">Workflow ID
    /// <param name="executionId">Execution ID
    public async Task CancelWorkflowExecutionAsync(string workflowId, string executionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"workflows/{workflowId}/executions/{executionId}/cancel", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution {ExecutionId} for workflow {WorkflowId}", executionId, workflowId);
            throw;
        }
    }
    #endregion
    #region AI Operations / AI
    /// <summary>
    /// Generate text using AIAI
    /// </summary>
    /// <param name="prompt">Text prompt</param>
    /// <param name="maxTokens">Maximum tokens</param>
    /// <param name="temperature">Temperature</param>
    /// <returns>Generated text</returns>
    public async Task<string> GenerateTextAsync(string prompt, int maxTokens = 1000, float temperature = 0.7f)
    {
        try
        {
            var request = new { Prompt = prompt, MaxTokens = maxTokens, Temperature = temperature };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("ai/generate", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson, _jsonOptions);
            return result.GetProperty("generatedText").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text");
            throw;
        }
    }
    /// <summary>
    /// Chat with AIAI
    /// </summary>
    /// <param name="messages">Chat messages</param>
    /// <param name="maxTokens">Maximum tokens</param>
    /// <returns>AI response
    public async Task<string> ChatAsync(List<string> messages, int maxTokens = 1000)
    {
        try
        {
            var request = new { Messages = messages, MaxTokens = maxTokens };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("ai/chat", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson, _jsonOptions);
            return result.GetProperty("response").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to chat with AI");
            throw;
        }
    }
    /// <summary>
    /// Execute AI functionAI
    /// </summary>
    /// <param name="functionName">Function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <returns>Function result</returns>
    public async Task<object?> ExecuteAIFunctionAsync(string functionName, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var request = new { FunctionName = functionName, Parameters = parameters };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("ai/function", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson, _jsonOptions);
            return result.GetProperty("result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute AI function {FunctionName}", functionName);
            throw;
        }
    }
    /// <summary>
    /// Analyze text using AIAI
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="analysisType">Analysis type</param>
    /// <returns>Analysis result</returns>
    public async Task<string> AnalyzeTextAsync(string text, string analysisType = "general")
    {
        try
        {
            var request = new { Text = text, AnalysisType = analysisType };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("ai/analyze", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson, _jsonOptions);
            return result.GetProperty("analysis").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text");
            throw;
        }
    }
    /// <summary>
    /// Get available AI modelsAI
    /// </summary>
    /// <returns>List of available models</returns>
    public async Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("ai/models");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions) ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available models");
            throw;
        }
    }
    /// <summary>
    /// Get current AI modelAI
    /// </summary>
    /// <returns>Current model name</returns>
    public async Task<string> GetCurrentModelAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("ai/models/current");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current model");
            throw;
        }
    }
    /// <summary>
    /// Set AI modelAI
    /// </summary>
    /// <param name="modelName">Model name</param>
    public async Task SetModelAsync(string modelName)
    {
        try
        {
            var request = new { ModelName = modelName };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("ai/models/set", content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set model to {ModelName}", modelName);
            throw;
        }
    }
    #endregion
    #region Authentication /
    /// <summary>
    /// Authenticate user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            var request = new { Username = username, Password = password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("auth/login", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<AuthenticationResult>(responseJson, _jsonOptions) ?? new AuthenticationResult { Success = false };
            }
            else
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Authentication failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate user {Username}", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    #endregion
}
/// <summary>
/// Authentication result
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Authentication success
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// JWT token
    /// </summary>
    public string? Token { get; set; }
    /// <summary>
    /// User information
    /// </summary>
    public object? User { get; set; }
    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}

