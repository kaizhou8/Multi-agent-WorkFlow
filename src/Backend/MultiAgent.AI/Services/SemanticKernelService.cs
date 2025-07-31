using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MultiAgent.Shared.Contracts;
using System.Collections.Concurrent;
namespace MultiAgent.AI.Services;
/// <summary>
/// Semantic Kernel AI service implementation
/// Provides AI capabilities using Microsoft Semantic Kernel
//// </summary>
public class SemanticKernelService : IAIService
{
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, Kernel> _kernels;
    private readonly ConcurrentDictionary<string, AIModel> _availableModels;
    private string _currentModelId;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="configuration">Configuration</param>
    public SemanticKernelService(ILogger<SemanticKernelService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _kernels = new ConcurrentDictionary<string, Kernel>();
        _availableModels = new ConcurrentDictionary<string, AIModel>();
        _currentModelId = "gpt-3.5-turbo"; // Default model
        InitializeModels();
    }
    /// <summary>
    /// Initialize available AI modelsAI
    /// </summary>
    private void InitializeModels()
    {
        // OpenAI models / OpenAI
        _availableModels.TryAdd("gpt-3.5-turbo", new AIModel
        {
            Id = "gpt-3.5-turbo",
            Name = "GPT-3.5 Turbo",
            Provider = "OpenAI",
            Description = "Fast and efficient model for most tasks",
            Capabilities = new List<string> { "text-generation", "conversation", "analysis" },
            MaxTokens = 4096,
            IsAvailable = !string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"])
        });
        _availableModels.TryAdd("gpt-4", new AIModel
        {
            Id = "gpt-4",
            Name = "GPT-4",
            Provider = "OpenAI",
            Description = "Most capable model for complex reasoning tasks",
            Capabilities = new List<string> { "text-generation", "conversation", "analysis", "reasoning" },
            MaxTokens = 8192,
            IsAvailable = !string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"])
        });
        _availableModels.TryAdd("gpt-4-turbo", new AIModel
        {
            Id = "gpt-4-turbo",
            Name = "GPT-4 Turbo",
            Provider = "OpenAI",
            Description = "Latest GPT-4 model with improved performance",
            Capabilities = new List<string> { "text-generation", "conversation", "analysis", "reasoning", "function-calling" },
            MaxTokens = 128000,
            IsAvailable = !string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"])
        });
        // Mock model for testing
        _availableModels.TryAdd("mock", new AIModel
        {
            Id = "mock",
            Name = "Mock AI Model",
            Provider = "Local",
            Description = "Mock model for testing and development",
            Capabilities = new List<string> { "text-generation", "conversation" },
            MaxTokens = 4096,
            IsAvailable = true
        });
        _logger.LogInformation("Initialized {ModelCount} AI models", _availableModels.Count);
    }
    /// <summary>
    /// Get or create kernel for model
    /// </summary>
    /// <param name="modelId">Model ID
    /// <returns>Kernel instance</returns>
    private async Task<Kernel> GetOrCreateKernelAsync(string modelId)
    {
        if (_kernels.TryGetValue(modelId, out var existingKernel))
        {
            return existingKernel;
        }
        var kernelBuilder = Kernel.CreateBuilder();
        // Configure based on model
        switch (modelId)
        {
            case "mock":
                // For mock model, we'll use a simple implementation，
                break;
            case "gpt-3.5-turbo":
            case "gpt-4":
            case "gpt-4-turbo":
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is not configured");
                }
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: apiKey);
                break;
            default:
                throw new NotSupportedException($"Model {modelId} is not supported");
        }
        var kernel = kernelBuilder.Build();
        _kernels.TryAdd(modelId, kernel);
        _logger.LogInformation("Created kernel for model {ModelId}", modelId);
        return kernel;
    }
    /// <summary>
    /// Generate response from AI modelAI
    /// </summary>
    /// <param name="prompt">Input prompt</param>
    /// <param name="systemMessage">System message</param>
    /// <returns>AI response
    public async Task<string> GenerateResponseAsync(string prompt, string? systemMessage = null)
    {
        if (string.IsNullOrEmpty(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        try
        {
            if (_currentModelId == "mock")
            {
                // Mock response for testing
                await Task.Delay(100); // Simulate processing time
                return $"Mock AI response to: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
            }
            var kernel = await GetOrCreateKernelAsync(_currentModelId);
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(systemMessage))
            {
                messages.Add(new ChatMessage { Role = "system", Content = systemMessage });
            }
            messages.Add(new ChatMessage { Role = "user", Content = prompt });
            return await GenerateResponseAsync(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI response for prompt: {Prompt}", prompt.Substring(0, Math.Min(100, prompt.Length)));
            throw;
        }
    }
    /// <summary>
    /// Generate response with conversation history
    /// </summary>
    /// <param name="messages">Conversation messages</param>
    /// <returns>AI response
    public async Task<string> GenerateResponseAsync(IEnumerable<ChatMessage> messages)
    {
        if (messages == null || !messages.Any())
            throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
        try
        {
            if (_currentModelId == "mock")
            {
                // Mock response for testing
                await Task.Delay(100);
                var lastMessage = messages.Last();
                return $"Mock AI response to: {lastMessage.Content.Substring(0, Math.Min(50, lastMessage.Content.Length))}...";
            }
            var kernel = await GetOrCreateKernelAsync(_currentModelId);
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            // Convert messages to Semantic Kernel formatSemantic Kernel
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            foreach (var message in messages)
            {
                switch (message.Role.ToLowerInvariant())
                {
                    case "system":
                        chatHistory.AddSystemMessage(message.Content);
                        break;
                    case "user":
                        chatHistory.AddUserMessage(message.Content);
                        break;
                    case "assistant":
                        chatHistory.AddAssistantMessage(message.Content);
                        break;
                }
            }
            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
            return result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI response for conversation with {MessageCount} messages", messages.Count());
            throw;
        }
    }
    /// <summary>
    /// Execute a semantic function
    /// </summary>
    /// <param name="functionName">Function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <returns>Function result</returns>
    public async Task<string> ExecuteFunctionAsync(string functionName, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(functionName))
            throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
        try
        {
            if (_currentModelId == "mock")
            {
                // Mock function execution
                await Task.Delay(100);
                return $"Mock function result for {functionName} with {parameters.Count} parameters";
            }
            var kernel = await GetOrCreateKernelAsync(_currentModelId);
            // For now, implement as a prompt-based function
            var prompt = $"Execute function '{functionName}' with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}";
            return await GenerateResponseAsync(prompt, $"You are executing the function '{functionName}'. Provide a concise and accurate result.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute function {FunctionName}", functionName);
            throw;
        }
    }
    /// <summary>
    /// Get available AI modelsAI
    /// </summary>
    /// <returns>List of available models</returns>
    public Task<IEnumerable<AIModel>> GetAvailableModelsAsync()
    {
        return Task.FromResult<IEnumerable<AIModel>>(_availableModels.Values.Where(m => m.IsAvailable));
    }
    /// <summary>
    /// Set the current AI modelAI
    /// </summary>
    /// <param name="modelId">Model ID
    public Task SetCurrentModelAsync(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (!_availableModels.ContainsKey(modelId))
            throw new ArgumentException($"Model {modelId} is not available", nameof(modelId));
        if (!_availableModels[modelId].IsAvailable)
            throw new InvalidOperationException($"Model {modelId} is not currently available");
        _currentModelId = modelId;
        _logger.LogInformation("Switched to AI model {ModelId}", modelId);
        return Task.CompletedTask;
    }
    /// <summary>
    /// Get current AI modelAI
    /// </summary>
    /// <returns>Current model</returns>
    public Task<AIModel?> GetCurrentModelAsync()
    {
        _availableModels.TryGetValue(_currentModelId, out var model);
        return Task.FromResult(model);
    }
    /// <summary>
    /// Analyze text content
    /// </summary>
    /// <param name="content">Text content</param>
    /// <param name="analysisType">Analysis type</param>
    /// <returns>Analysis result</returns>
    public async Task<AIAnalysisResult> AnalyzeTextAsync(string content, string analysisType)
    {
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));
        if (string.IsNullOrEmpty(analysisType))
            throw new ArgumentException("Analysis type cannot be null or empty", nameof(analysisType));
        try
        {
            var prompt = analysisType.ToLowerInvariant() switch
            {
                "sentiment" => $"Analyze the sentiment of the following text and provide a score from -1 (negative) to 1 (positive):\n\n{content}",
                "summary" => $"Provide a concise summary of the following text:\n\n{content}",
                "keywords" => $"Extract the main keywords from the following text:\n\n{content}",
                "language" => $"Detect the language of the following text:\n\n{content}",
                _ => $"Analyze the following text for {analysisType}:\n\n{content}"
            };
            var systemMessage = $"You are an expert text analyst. Provide accurate and concise {analysisType} analysis.";
            var result = await GenerateResponseAsync(prompt, systemMessage);
            return new AIAnalysisResult
            {
                AnalysisType = analysisType,
                Result = result,
                Confidence = 0.85, // Default confidence
                Metadata = new Dictionary<string, object>
                {
                    ["model"] = _currentModelId,
                    ["timestamp"] = DateTime.UtcNow,
                    ["content_length"] = content.Length
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text for {AnalysisType}", analysisType);
            throw;
        }
    }
    /// <summary>
    /// Generate embeddings for text
    /// </summary>
    /// <param name="text">Input text</param>
    /// <returns>Embedding vector</returns>
    public async Task<float[]> GenerateEmbeddingsAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        try
        {
            if (_currentModelId == "mock")
            {
                // Mock embeddings for testing
                await Task.Delay(50);
                var random = new Random(text.GetHashCode());
                return Enumerable.Range(0, 1536).Select(_ => (float)random.NextDouble()).ToArray();
            }
            // For now, return mock embeddings as embedding service requires additional setup，
            var mockRandom = new Random(text.GetHashCode());
            return Enumerable.Range(0, 1536).Select(_ => (float)mockRandom.NextDouble()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for text");
            throw;
        }
    }
    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        foreach (var kernel in _kernels.Values)
        {
            kernel?.Dispose();
        }
        _kernels.Clear();
    }
}

