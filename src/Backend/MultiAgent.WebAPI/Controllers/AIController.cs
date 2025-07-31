using Microsoft.AspNetCore.Mvc;
using MultiAgent.Shared.Contracts;
namespace MultiAgent.WebAPI.Controllers;
/// <summary>
/// AI services controller
/// Provides REST API endpoints for AI operations
//// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly ILogger<AIController> _logger;
    private readonly IAIService _aiService;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="aiService">AI service
    public AIController(ILogger<AIController> logger, IAIService aiService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }
    /// <summary>
    /// Generate text using AIAI
    /// </summary>
    /// <param name="request">Text generation request</param>
    /// <returns>Generated text</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(AITextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AITextResponse>> GenerateText([FromBody] AITextRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty");
            }
            var result = await _aiService.GenerateTextAsync(
                request.Prompt,
                request.MaxTokens ?? 1000,
                request.Temperature ?? 0.7f);
            return Ok(new AITextResponse
            {
                GeneratedText = result,
                Prompt = request.Prompt,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to generate text");
        }
    }
    /// <summary>
    /// Chat with AIAI
    /// </summary>
    /// <param name="request">Chat request</param>
    /// <returns>Chat response</returns>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AIChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIChatResponse>> Chat([FromBody] AIChatRequest request)
    {
        try
        {
            if (request == null || request.Messages == null || !request.Messages.Any())
            {
                return BadRequest("Messages cannot be null or empty");
            }
            var result = await _aiService.ChatAsync(request.Messages, request.MaxTokens ?? 1000);
            return Ok(new AIChatResponse
            {
                Response = result,
                Messages = request.Messages,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat request");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to process chat request");
        }
    }
    /// <summary>
    /// Execute AI functionAI
    /// </summary>
    /// <param name="request">Function execution request</param>
    /// <returns>Function execution result</returns>
    [HttpPost("function")]
    [ProducesResponseType(typeof(AIFunctionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIFunctionResponse>> ExecuteFunction([FromBody] AIFunctionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FunctionName))
            {
                return BadRequest("Function name cannot be null or empty");
            }
            var result = await _aiService.ExecuteFunctionAsync(
                request.FunctionName,
                request.Parameters ?? new Dictionary<string, object>());
            return Ok(new AIFunctionResponse
            {
                Result = result,
                FunctionName = request.FunctionName,
                Parameters = request.Parameters,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute AI function {FunctionName}", request?.FunctionName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to execute AI function");
        }
    }
    /// <summary>
    /// Analyze text using AIAI
    /// </summary>
    /// <param name="request">Analysis request</param>
    /// <returns>Analysis result</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AIAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAnalysisResponse>> AnalyzeText([FromBody] AIAnalysisRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text cannot be null or empty");
            }
            var result = await _aiService.AnalyzeTextAsync(request.Text, request.AnalysisType ?? "general");
            return Ok(new AIAnalysisResponse
            {
                Analysis = result,
                Text = request.Text,
                AnalysisType = request.AnalysisType ?? "general",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to analyze text");
        }
    }
    /// <summary>
    /// Generate embeddings for text
    /// </summary>
    /// <param name="request">Embeddings request</param>
    /// <returns>Embeddings result</returns>
    [HttpPost("embeddings")]
    [ProducesResponseType(typeof(AIEmbeddingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIEmbeddingsResponse>> GenerateEmbeddings([FromBody] AIEmbeddingsRequest request)
    {
        try
        {
            if (request == null || request.Texts == null || !request.Texts.Any())
            {
                return BadRequest("Texts cannot be null or empty");
            }
            var embeddings = new List<float[]>();
            foreach (var text in request.Texts)
            {
                var embedding = await _aiService.GenerateEmbeddingsAsync(text);
                embeddings.Add(embedding);
            }
            return Ok(new AIEmbeddingsResponse
            {
                Embeddings = embeddings,
                Texts = request.Texts,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to generate embeddings");
        }
    }
    /// <summary>
    /// Get available AI modelsAI
    /// </summary>
    /// <returns>List of available models</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableModels()
    {
        try
        {
            var models = await _aiService.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available models");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get available models");
        }
    }
    /// <summary>
    /// Get current AI modelAI
    /// </summary>
    /// <returns>Current model name</returns>
    [HttpGet("models/current")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetCurrentModel()
    {
        try
        {
            var currentModel = await _aiService.GetCurrentModelAsync();
            return Ok(currentModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current model");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get current model");
        }
    }
    /// <summary>
    /// Set AI modelAI
    /// </summary>
    /// <param name="request">Model selection request</param>
    /// <returns>Action result</returns>
    [HttpPost("models/set")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetModel([FromBody] AIModelRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ModelName))
            {
                return BadRequest("Model name cannot be null or empty");
            }
            await _aiService.SetModelAsync(request.ModelName);
            return Ok($"Model set to {request.ModelName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set model to {ModelName}", request?.ModelName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to set model");
        }
    }
}
#region Request/Response Models
/// <summary>
/// AI text generation request
/// </summary>
public class AITextRequest
{
    /// <summary>
    /// Text prompt
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }
    /// <summary>
    /// Temperature for generation
    /// </summary>
    public float? Temperature { get; set; }
}
/// <summary>
/// AI text generation response
/// </summary>
public class AITextResponse
{
    /// <summary>
    /// Generated text
    /// </summary>
    public string GeneratedText { get; set; } = string.Empty;
    /// <summary>
    /// Original prompt
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Generation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}
/// <summary>
/// AI chat request
/// </summary>
public class AIChatRequest
{
    /// <summary>
    /// Chat messages
    /// </summary>
    public List<string> Messages { get; set; } = new();
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }
}
/// <summary>
/// AI chat response
/// </summary>
public class AIChatResponse
{
    /// <summary>
    /// AI response
    /// </summary>
    public string Response { get; set; } = string.Empty;
    /// <summary>
    /// Original messages
    /// </summary>
    public List<string> Messages { get; set; } = new();
    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}
/// <summary>
/// AI function execution request
/// </summary>
public class AIFunctionRequest
{
    /// <summary>
    /// Function name
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Function parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
/// <summary>
/// AI function execution response
/// </summary>
public class AIFunctionResponse
{
    /// <summary>
    /// Function result
    /// </summary>
    public object? Result { get; set; }
    /// <summary>
    /// Function name
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Function parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}
/// <summary>
/// AI analysis request
/// </summary>
public class AIAnalysisRequest
{
    /// <summary>
    /// Text to analyze
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Analysis type
    /// </summary>
    public string? AnalysisType { get; set; }
}
/// <summary>
/// AI analysis response
/// </summary>
public class AIAnalysisResponse
{
    /// <summary>
    /// Analysis result
    /// </summary>
    public string Analysis { get; set; } = string.Empty;
    /// <summary>
    /// Original text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Analysis type
    /// </summary>
    public string AnalysisType { get; set; } = string.Empty;
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}
/// <summary>
/// AI embeddings request
/// </summary>
public class AIEmbeddingsRequest
{
    /// <summary>
    /// Texts to generate embeddings for
    /// </summary>
    public List<string> Texts { get; set; } = new();
}
/// <summary>
/// AI embeddings response
/// </summary>
public class AIEmbeddingsResponse
{
    /// <summary>
    /// Generated embeddings
    /// </summary>
    public List<float[]> Embeddings { get; set; } = new();
    /// <summary>
    /// Original texts
    /// </summary>
    public List<string> Texts { get; set; } = new();
    /// <summary>
    /// Generation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}
/// <summary>
/// AI model selection request
/// </summary>
public class AIModelRequest
{
    /// <summary>
    /// Model name
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
}
#endregion

