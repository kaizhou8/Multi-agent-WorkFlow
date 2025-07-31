using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// AI Text Generation ViewModel - AI
/// Manages AI text generation functionality with model configuration and history
//// </summary>
public partial class AITextGenerationViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILogger<AITextGenerationViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    /// <summary>
    /// Initialize AI Text Generation ViewModelAI
    /// </summary>
    public AITextGenerationViewModel(IApiService apiService, ILogger<AITextGenerationViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        AvailableModels = new ObservableCollection<AIModelInfo>();
        GenerationHistory = new ObservableCollection<TextGenerationHistoryItem>();
        // Initialize default values
        MaxTokens = 1000;
        Temperature = 0.7;
        ModelStatus = "Ready";
        ModelStatusColor = Colors.Green;
        GenerationStatus = "Ready to generate";
        LoadAvailableModels();
    }
    #region Properties /
    /// <summary>
    /// Available AI modelsAI
    /// </summary>
    public ObservableCollection<AIModelInfo> AvailableModels { get; }
    /// <summary>
    /// Selected AI modelAI
    /// </summary>
    [ObservableProperty]
    private AIModelInfo? selectedModel;
    /// <summary>
    /// Maximum tokens for generationtoken
    /// </summary>
    [ObservableProperty]
    private double maxTokens;
    /// <summary>
    /// Temperature for generation
    /// </summary>
    [ObservableProperty]
    private double temperature;
    /// <summary>
    /// Model status
    /// </summary>
    [ObservableProperty]
    private string modelStatus = string.Empty;
    /// <summary>
    /// Model status color
    /// </summary>
    [ObservableProperty]
    private Color modelStatusColor;
    /// <summary>
    /// Model response time
    /// </summary>
    [ObservableProperty]
    private int modelResponseTime;
    /// <summary>
    /// Input prompt
    /// </summary>
    [ObservableProperty]
    private string inputPrompt = string.Empty;
    /// <summary>
    /// Generated text
    /// </summary>
    [ObservableProperty]
    private string generatedText = string.Empty;
    /// <summary>
    /// Generation status
    /// </summary>
    [ObservableProperty]
    private string generationStatus = string.Empty;
    /// <summary>
    /// Is generating
    /// </summary>
    [ObservableProperty]
    private bool isGenerating;
    /// <summary>
    /// Has generated text
    /// </summary>
    [ObservableProperty]
    private bool hasGeneratedText;
    /// <summary>
    /// Word count
    /// </summary>
    [ObservableProperty]
    private int wordCount;
    /// <summary>
    /// Character count
    /// </summary>
    [ObservableProperty]
    private int characterCount;
    /// <summary>
    /// Tokens usedtoken
    /// </summary>
    [ObservableProperty]
    private int tokensUsed;
    /// <summary>
    /// Generation time
    /// </summary>
    [ObservableProperty]
    private int generationTime;
    /// <summary>
    /// Generation history
    /// </summary>
    public ObservableCollection<TextGenerationHistoryItem> GenerationHistory { get; }
    /// <summary>
    /// Can generate
    /// </summary>
    public bool CanGenerate => !IsGenerating && !string.IsNullOrWhiteSpace(InputPrompt) && SelectedModel != null;
    /// <summary>
    /// Is not generating
    /// </summary>
    public bool IsNotGenerating => !IsGenerating;
    #endregion
    #region Commands /
    /// <summary>
    /// Generate text command
    /// </summary>
    [RelayCommand]
    private async Task GenerateTextAsync()
    {
        if (string.IsNullOrWhiteSpace(InputPrompt) || SelectedModel == null)
            return;
        try
        {
            IsGenerating = true;
            GenerationStatus = "Initializing generation...";
            _cancellationTokenSource = new CancellationTokenSource();
            var startTime = DateTime.UtcNow;
            // Call AI serviceAI
            var request = new
            {
                prompt = InputPrompt,
                model = SelectedModel.Id,
                max_tokens = (int)MaxTokens,
                temperature = Temperature
            };
            GenerationStatus = "Sending request to AI model...";
            var response = await _apiService.PostAsync<dynamic>("ai/generate-text", request, _cancellationTokenSource.Token);
            if (response != null)
            {
                var responseJson = JsonSerializer.Serialize(response);
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                if (responseObj.TryGetProperty("text", out var textElement))
                {
                    GeneratedText = textElement.GetString() ?? string.Empty;
                    HasGeneratedText = !string.IsNullOrEmpty(GeneratedText);
                    // Update statistics
                    UpdateTextStatistics();
                    // Calculate generation time
                    GenerationTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    // Get tokens used from responsetoken
                    if (responseObj.TryGetProperty("tokens_used", out var tokensElement))
                    {
                        TokensUsed = tokensElement.GetInt32();
                    }
                    // Add to history
                    AddToHistory();
                    GenerationStatus = "Text generated successfully!";
                    _logger.LogInformation("Text generated successfully for prompt: {Prompt}", InputPrompt.Substring(0, Math.Min(50, InputPrompt.Length)));
                }
                else
                {
                    GenerationStatus = "Failed to extract text from response";
                    _logger.LogWarning("Failed to extract text from AI response");
                }
            }
            else
            {
                GenerationStatus = "No response from AI service";
                _logger.LogWarning("No response received from AI service");
            }
        }
        catch (OperationCanceledException)
        {
            GenerationStatus = "Generation cancelled";
            _logger.LogInformation("Text generation was cancelled");
        }
        catch (Exception ex)
        {
            GenerationStatus = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error generating text");
        }
        finally
        {
            IsGenerating = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            OnPropertyChanged(nameof(CanGenerate));
            OnPropertyChanged(nameof(IsNotGenerating));
        }
    }
    /// <summary>
    /// Cancel generation command
    /// </summary>
    [RelayCommand]
    private void CancelGeneration()
    {
        _cancellationTokenSource?.Cancel();
        GenerationStatus = "Cancelling generation...";
    }
    /// <summary>
    /// Clear prompt command
    /// </summary>
    [RelayCommand]
    private void ClearPrompt()
    {
        InputPrompt = string.Empty;
        OnPropertyChanged(nameof(CanGenerate));
    }
    /// <summary>
    /// Copy text command
    /// </summary>
    [RelayCommand]
    private async Task CopyTextAsync()
    {
        if (!string.IsNullOrEmpty(GeneratedText))
        {
            await Clipboard.SetTextAsync(GeneratedText);
            GenerationStatus = "Text copied to clipboard!";
            _logger.LogInformation("Generated text copied to clipboard");
        }
    }
    /// <summary>
    /// Save text command
    /// </summary>
    [RelayCommand]
    private async Task SaveTextAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(GeneratedText))
                return;
            var fileName = $"generated_text_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            await File.WriteAllTextAsync(filePath, GeneratedText);
            GenerationStatus = $"Text saved to {fileName}";
            _logger.LogInformation("Generated text saved to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            GenerationStatus = $"Error saving file: {ex.Message}";
            _logger.LogError(ex, "Error saving generated text to file");
        }
    }
    /// <summary>
    /// Regenerate command
    /// </summary>
    [RelayCommand]
    private async Task RegenerateAsync()
    {
        await GenerateTextAsync();
    }
    /// <summary>
    /// Use template command
    /// </summary>
    [RelayCommand]
    private void UseTemplate(string templateType)
    {
        var templates = new Dictionary<string, string>
        {
            ["email"] = "Write a professional email about:",
            ["article"] = "Write an informative article about:",
            ["business"] = "Create a business proposal for:",
            ["creative"] = "Write a creative story about:"
        };
        if (templates.TryGetValue(templateType, out var template))
        {
            InputPrompt = template + " ";
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
    /// <summary>
    /// Show templates command
    /// </summary>
    [RelayCommand]
    private async Task ShowTemplatesAsync()
    {
        // TODO: Implement template selection dialog
        await Application.Current!.MainPage!.DisplayAlert(
            "Templates /
            "Template selection feature coming soon! /
            "OK");
    }
    /// <summary>
    /// Show examples command
    /// </summary>
    [RelayCommand]
    private async Task ShowExamplesAsync()
    {
        var examples = new[]
        {
            "Write a summary of the benefits of renewable energy",
            "Create a product description for a smart home device",
            "Explain quantum computing in simple terms",
            "Write a cover letter for a software developer position"
        };
        var selectedExample = await Application.Current!.MainPage!.DisplayActionSheet(
            "Select Example /
            "Cancel /
            null,
            examples);
        if (!string.IsNullOrEmpty(selectedExample) && selectedExample != "Cancel /
        {
            InputPrompt = selectedExample;
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
    /// <summary>
    /// Refresh history command
    /// </summary>
    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        try
        {
            // TODO: Load history from backend
            await Task.Delay(500); // Simulate loading
            _logger.LogInformation("Generation history refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing generation history");
        }
    }
    /// <summary>
    /// Clear history command
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Clear History /
            "Are you sure you want to clear all generation history? /
            "Yes /
            "No /
        if (result)
        {
            GenerationHistory.Clear();
            _logger.LogInformation("Generation history cleared");
        }
    }
    /// <summary>
    /// View history item command
    /// </summary>
    [RelayCommand]
    private async Task ViewHistoryItemAsync(TextGenerationHistoryItem item)
    {
        if (item != null)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Generated Text /
                item.GeneratedText,
                "OK");
        }
    }
    /// <summary>
    /// Reuse prompt command
    /// </summary>
    [RelayCommand]
    private void ReusePrompt(TextGenerationHistoryItem item)
    {
        if (item != null)
        {
            InputPrompt = item.Prompt;
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
    #endregion
    #region Methods /
    /// <summary>
    /// Initialize async
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await LoadAvailableModelsAsync();
            await RefreshHistoryAsync();
            _logger.LogInformation("AI Text Generation page initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing AI Text Generation page");
        }
    }
    /// <summary>
    /// Cleanup resources
    /// </summary>
    public void Cleanup()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
    /// <summary>
    /// Load available models
    /// </summary>
    private void LoadAvailableModels()
    {
        // Add mock models
        AvailableModels.Clear();
        AvailableModels.Add(new AIModelInfo { Id = "gpt-4", DisplayName = "GPT-4", Provider = "OpenAI" });
        AvailableModels.Add(new AIModelInfo { Id = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo", Provider = "OpenAI" });
        AvailableModels.Add(new AIModelInfo { Id = "claude-3", DisplayName = "Claude 3", Provider = "Anthropic" });
        SelectedModel = AvailableModels.FirstOrDefault();
    }
    /// <summary>
    /// Load available models async
    /// </summary>
    private async Task LoadAvailableModelsAsync()
    {
        try
        {
            // TODO: Load from backend APIAPI
            var models = await _apiService.GetAsync<List<AIModelInfo>>("ai/models");
            if (models != null && models.Any())
            {
                AvailableModels.Clear();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }
                SelectedModel = AvailableModels.FirstOrDefault();
                ModelStatus = "Models loaded";
                ModelStatusColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI models");
            ModelStatus = "Error loading models";
            ModelStatusColor = Colors.Red;
            // Fallback to mock models
            LoadAvailableModels();
        }
    }
    /// <summary>
    /// Update text statistics
    /// </summary>
    private void UpdateTextStatistics()
    {
        if (string.IsNullOrEmpty(GeneratedText))
        {
            WordCount = 0;
            CharacterCount = 0;
            return;
        }
        CharacterCount = GeneratedText.Length;
        WordCount = GeneratedText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
    /// <summary>
    /// Add to history
    /// </summary>
    private void AddToHistory()
    {
        var historyItem = new TextGenerationHistoryItem
        {
            Id = Guid.NewGuid().ToString(),
            Prompt = InputPrompt,
            GeneratedText = GeneratedText,
            Model = SelectedModel?.DisplayName ?? "Unknown",
            WordCount = WordCount,
            CharacterCount = CharacterCount,
            TokensUsed = TokensUsed,
            GenerationTime = GenerationTime,
            CreatedAt = DateTime.Now
        };
        GenerationHistory.Insert(0, historyItem);
        // Keep only last 20 items20
        while (GenerationHistory.Count > 20)
        {
            GenerationHistory.RemoveAt(GenerationHistory.Count - 1);
        }
    }
    #endregion
}
/// <summary>
/// AI Model Information
/// </summary>
public class AIModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
}
/// <summary>
/// Text Generation History Item
/// </summary>
public class TextGenerationHistoryItem
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string GeneratedText { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public int TokensUsed { get; set; }
    public int GenerationTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

