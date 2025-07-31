using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using MultiAgent.Shared.Models;
using System.Collections.ObjectModel;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// AI services page view model
/// Manages AI models, chat interface, and AI functions
//// </summary>
public partial class AIViewModel : ObservableObject
{
    #region Fields /
    private readonly IApiService _apiService;
    private readonly ILogger<AIViewModel> _logger;
    [ObservableProperty]
    private ObservableCollection<AIModelViewModel> _availableModels = new();
    [ObservableProperty]
    private AIModelViewModel? _selectedModel;
    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _chatMessages = new();
    [ObservableProperty]
    private string _messageText = string.Empty;
    [ObservableProperty]
    private bool _isProcessing;
    [ObservableProperty]
    private int _requestsToday;
    [ObservableProperty]
    private int _tokensUsedToday;
    [ObservableProperty]
    private int _averageResponseTime;
    [ObservableProperty]
    private int _totalTokensUsed;
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="logger">Logger</param>
    public AIViewModel(
        IApiService apiService,
        ILogger<AIViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        // Add welcome message
        ChatMessages.Add(new ChatMessageViewModel
        {
            Sender = "System",
            Content = "Welcome to AI Chat! Select a model and start chatting. /
            Timestamp = DateTime.Now,
            SenderIcon = "🤖",
            SenderColor = Colors.Blue
        });
    }
    #endregion
    #region Properties /
    /// <summary>
    /// Can send message
    /// </summary>
    public bool CanSendMessage => !IsProcessing && SelectedModel != null && !string.IsNullOrWhiteSpace(MessageText);
    #endregion
    #region Commands /
    /// <summary>
    /// Initialize async
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing AI services /
            await LoadAvailableModelsAsync();
            await LoadUsageStatisticsAsync();
            _logger.LogInformation("AI services initialized successfully / AI
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI services /
        }
    }
    /// <summary>
    /// Send message command
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (!CanSendMessage || SelectedModel == null)
            return;
        try
        {
            IsProcessing = true;
            var userMessage = MessageText.Trim();
            MessageText = string.Empty;
            // Add user message to chat
            var userChatMessage = new ChatMessageViewModel
            {
                Sender = "You",
                Content = userMessage,
                Timestamp = DateTime.Now,
                SenderIcon = "👤",
                SenderColor = Colors.Green
            };
            ChatMessages.Add(userChatMessage);
            _logger.LogInformation("Sending message to AI model {ModelName} /
            // Prepare chat messages for APIAPI
            var messages = ChatMessages
                .Where(m => m.Sender != "System")
                .Select(m => $"{m.Sender}: {m.Content}")
                .ToList();
            // Send to AI serviceAI
            var startTime = DateTime.Now;
            var response = await _apiService.ChatAsync(messages, 1000);
            var responseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
            // Add AI response to chatAI
            var aiChatMessage = new ChatMessageViewModel
            {
                Sender = SelectedModel.Name,
                Content = response,
                Timestamp = DateTime.Now,
                SenderIcon = "🤖",
                SenderColor = SelectedModel.StatusColor,
                TokenUsage = EstimateTokenUsage(userMessage + response),
                HasTokenUsage = true
            };
            ChatMessages.Add(aiChatMessage);
            // Update statistics
            RequestsToday++;
            TotalTokensUsed += aiChatMessage.TokenUsage;
            TokensUsedToday += aiChatMessage.TokenUsage;
            AverageResponseTime = (AverageResponseTime + responseTime) / 2;
            _logger.LogInformation("Received AI response in {ResponseTime}ms /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to AI /
            // Add error message
            ChatMessages.Add(new ChatMessageViewModel
            {
                Sender = "System",
                Content = $"Error: Failed to get AI response. {ex.Message}",
                Timestamp = DateTime.Now,
                SenderIcon = "❌",
                SenderColor = Colors.Red
            });
        }
        finally
        {
            IsProcessing = false;
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }
    /// <summary>
    /// Clear chat command
    /// </summary>
    [RelayCommand]
    private void ClearChat()
    {
        ChatMessages.Clear();
        TotalTokensUsed = 0;
        // Add welcome message back
        ChatMessages.Add(new ChatMessageViewModel
        {
            Sender = "System",
            Content = "Chat cleared. Ready for new conversation. /
            Timestamp = DateTime.Now,
            SenderIcon = "🤖",
            SenderColor = Colors.Blue
        });
        _logger.LogInformation("Chat cleared /
    }
    /// <summary>
    /// Export chat command
    /// </summary>
    [RelayCommand]
    private async Task ExportChatAsync()
    {
        try
        {
            _logger.LogInformation("Exporting chat history /
            var chatHistory = string.Join("\n\n", ChatMessages.Select(m =>
                $"[{m.Timestamp:yyyy-MM-dd HH:mm:ss}] {m.Sender}: {m.Content}"));
            // TODO: Implement file saving logic
            await Shell.Current.DisplayAlert("Export", "Chat export functionality coming soon! /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export chat /
        }
    }
    /// <summary>
    /// Attach file command
    /// </summary>
    [RelayCommand]
    private async Task AttachFileAsync()
    {
        try
        {
            _logger.LogInformation("Attaching file to chat /
            // TODO: Implement file picker logic
            await Shell.Current.DisplayAlert("Attach File", "File attachment functionality coming soon! /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach file /
        }
    }
    /// <summary>
    /// Configure models command
    /// </summary>
    [RelayCommand]
    private async Task ConfigureModelsAsync()
    {
        try
        {
            _logger.LogInformation("Opening model configuration /
            await Shell.Current.GoToAsync("//ai/models");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open model configuration /
        }
    }
    /// <summary>
    /// Open text generation command
    /// </summary>
    [RelayCommand]
    private async Task OpenTextGenerationAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/textgeneration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open text generation /
        }
    }
    /// <summary>
    /// Open code analysis command
    /// </summary>
    [RelayCommand]
    private async Task OpenCodeAnalysisAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/codeanalysis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open code analysis /
        }
    }
    /// <summary>
    /// Open document analysis command
    /// </summary>
    [RelayCommand]
    private async Task OpenDocumentAnalysisAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/documentanalysis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open document analysis /
        }
    }
    /// <summary>
    /// Open function calling command
    /// </summary>
    [RelayCommand]
    private async Task OpenFunctionCallingAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/functioncalling");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open function calling /
        }
    }
    /// <summary>
    /// Open embeddings command
    /// </summary>
    [RelayCommand]
    private async Task OpenEmbeddingsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/embeddings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open embeddings /
        }
    }
    /// <summary>
    /// Open model comparison command
    /// </summary>
    [RelayCommand]
    private async Task OpenModelComparisonAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai/modelcomparison");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open model comparison /
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Load available models
    /// </summary>
    private async Task LoadAvailableModelsAsync()
    {
        try
        {
            var models = await _apiService.GetAvailableModelsAsync();
            AvailableModels.Clear();
            foreach (var model in models)
            {
                var modelViewModel = new AIModelViewModel(model);
                // Test model availability
                try
                {
                    var startTime = DateTime.Now;
                    await _apiService.GenerateTextAsync("Test", 10, 0.1f);
                    var responseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    modelViewModel.Status = "Available";
                    modelViewModel.ResponseTime = responseTime;
                }
                catch
                {
                    modelViewModel.Status = "Unavailable";
                    modelViewModel.ResponseTime = 0;
                }
                AvailableModels.Add(modelViewModel);
            }
            // Select first available model
            SelectedModel = AvailableModels.FirstOrDefault(m => m.Status == "Available");
            _logger.LogInformation("Loaded {Count} AI models /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available models /
        }
    }
    /// <summary>
    /// Load usage statistics
    /// </summary>
    private async Task LoadUsageStatisticsAsync()
    {
        try
        {
            // TODO: Load actual statistics from APIAPI
            RequestsToday = 42;
            TokensUsedToday = 15420;
            AverageResponseTime = 850;
            _logger.LogInformation("Loaded usage statistics /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load usage statistics /
        }
    }
    /// <summary>
    /// Estimate token usagetoken
    /// </summary>
    private int EstimateTokenUsage(string text)
    {
        // Simple estimation: ~4 characters per token：
        return (int)Math.Ceiling(text.Length / 4.0);
    }
    #endregion
}
/// <summary>
/// AI model view model
/// Represents an AI model in the UI
//// </summary>
public partial class AIModelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _provider = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private int _responseTime;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="model">AI model
    public AIModelViewModel(string model)
    {
        // Parse model string (e.g., "OpenAI:gpt-4")
        var parts = model.Split(':');
        if (parts.Length >= 2)
        {
            Provider = parts[0];
            Name = parts[1];
        }
        else
        {
            Provider = "Unknown";
            Name = model;
        }
        Status = "Unknown";
    }
    /// <summary>
    /// Display name for picker
    /// </summary>
    public string DisplayName => $"{Provider}: {Name}";
    /// <summary>
    /// Status color based on model status
    /// </summary>
    public Color StatusColor => Status switch
    {
        "Available" => Colors.Green,
        "Unavailable" => Colors.Red,
        "Unknown" => Colors.Gray,
        _ => Colors.Gray
    };
}
/// <summary>
/// Chat message view model
/// Represents a chat message in the UI
//// </summary>
public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _sender = string.Empty;
    [ObservableProperty]
    private string _content = string.Empty;
    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;
    [ObservableProperty]
    private string _senderIcon = string.Empty;
    [ObservableProperty]
    private Color _senderColor = Colors.Gray;
    [ObservableProperty]
    private int _tokenUsage;
    [ObservableProperty]
    private bool _hasTokenUsage;
}

