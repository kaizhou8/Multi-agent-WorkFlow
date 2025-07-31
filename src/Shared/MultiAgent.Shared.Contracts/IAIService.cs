namespace MultiAgent.Shared.Contracts;
/// <summary>
/// AI service interface
/// Provides AI capabilities for agents and workflows
//// </summary>
public interface IAIService
{
    /// <summary>
    /// Generate response from AI model
    /// </summary>
    /// <param name="prompt">Input prompt</param>
    /// <param name="systemMessage">System message</param>
    /// <returns>AI response
    Task<string> GenerateResponseAsync(string prompt, string? systemMessage = null);
    /// <summary>
    /// Generate response with conversation history
    /// </summary>
    /// <param name="messages">Conversation messages</param>
    /// <returns>AI response
    Task<string> GenerateResponseAsync(IEnumerable<ChatMessage> messages);
    /// <summary>
    /// Execute a semantic function
    /// </summary>
    /// <param name="functionName">Function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <returns>Function result</returns>
    Task<string> ExecuteFunctionAsync(string functionName, Dictionary<string, object> parameters);
    /// <summary>
    /// Get available AI models
    /// </summary>
    /// <returns>List of available models</returns>
    Task<IEnumerable<AIModel>> GetAvailableModelsAsync();
    /// <summary>
    /// Set the current AI model
    /// </summary>
    /// <param name="modelId">Model ID
    Task SetCurrentModelAsync(string modelId);
    /// <summary>
    /// Get current AI model
    /// </summary>
    /// <returns>Current model</returns>
    Task<AIModel?> GetCurrentModelAsync();
    /// <summary>
    /// Analyze text content
    /// </summary>
    /// <param name="content">Text content</param>
    /// <param name="analysisType">Analysis type</param>
    /// <returns>Analysis result</returns>
    Task<AIAnalysisResult> AnalyzeTextAsync(string content, string analysisType);
    /// <summary>
    /// Generate embeddings for text
    /// </summary>
    /// <param name="text">Input text</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> GenerateEmbeddingsAsync(string text);
}
/// <summary>
/// Chat message
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Message role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Message timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
/// <summary>
/// AI model information
/// </summary>
public class AIModel
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Model name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Model provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>
    /// Model description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Model capabilities
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
    /// <summary>
    /// Maximum tokens
    /// </summary>
    public int MaxTokens { get; set; }
    /// <summary>
    /// Is available
    /// </summary>
    public bool IsAvailable { get; set; }
}
/// <summary>
/// AI analysis result
/// </summary>
public class AIAnalysisResult
{
    /// <summary>
    /// Analysis type
    /// </summary>
    public string AnalysisType { get; set; } = string.Empty;
    /// <summary>
    /// Analysis result
    /// </summary>
    public string Result { get; set; } = string.Empty;
    /// <summary>
    /// Confidence score
    /// </summary>
    public double Confidence { get; set; }
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

