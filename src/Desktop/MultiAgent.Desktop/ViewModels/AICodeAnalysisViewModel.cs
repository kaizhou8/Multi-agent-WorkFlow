using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// AI Code Analysis ViewModel - AI
/// Manages AI-powered code analysis functionality with quality assessment and improvement suggestions
//// </summary>
public partial class AICodeAnalysisViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILogger<AICodeAnalysisViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    public AICodeAnalysisViewModel(IApiService apiService, ILogger<AICodeAnalysisViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        SupportedLanguages = new ObservableCollection<ProgrammingLanguage>();
        AnalysisTypes = new ObservableCollection<string>();
        Issues = new ObservableCollection<CodeIssue>();
        Suggestions = new ObservableCollection<ImprovementSuggestion>();
        InitializeDefaults();
    }
    #region Properties
    public ObservableCollection<ProgrammingLanguage> SupportedLanguages { get; }
    public ObservableCollection<string> AnalysisTypes { get; }
    public ObservableCollection<CodeIssue> Issues { get; }
    public ObservableCollection<ImprovementSuggestion> Suggestions { get; }
    [ObservableProperty] private ProgrammingLanguage? selectedLanguage;
    [ObservableProperty] private string selectedAnalysisType = "Comprehensive";
    [ObservableProperty] private string codeInput = string.Empty;
    [ObservableProperty] private string fileName = string.Empty;
    [ObservableProperty] private long fileSize;
    [ObservableProperty] private int lineCount;
    [ObservableProperty] private bool hasFileInfo;
    [ObservableProperty] private bool checkSyntax = true;
    [ObservableProperty] private bool checkSecurity = true;
    [ObservableProperty] private bool checkPerformance = true;
    [ObservableProperty] private bool checkBestPractices = true;
    [ObservableProperty] private bool checkComplexity = true;
    [ObservableProperty] private bool generateSuggestions = true;
    [ObservableProperty] private bool isAnalyzing;
    [ObservableProperty] private string analysisStatus = string.Empty;
    [ObservableProperty] private double analysisProgress;
    [ObservableProperty] private bool hasAnalysisResults;
    [ObservableProperty] private int overallScore;
    [ObservableProperty] private Color overallScoreColor = Colors.Gray;
    [ObservableProperty] private int syntaxScore;
    [ObservableProperty] private Color syntaxScoreColor = Colors.Gray;
    [ObservableProperty] private int securityScore;
    [ObservableProperty] private Color securityScoreColor = Colors.Gray;
    [ObservableProperty] private int performanceScore;
    [ObservableProperty] private Color performanceScoreColor = Colors.Gray;
    [ObservableProperty] private int complexityScore;
    [ObservableProperty] private Color complexityScoreColor = Colors.Gray;
    [ObservableProperty] private int bestPracticesScore;
    [ObservableProperty] private Color bestPracticesScoreColor = Colors.Gray;
    [ObservableProperty] private int maintainabilityScore;
    [ObservableProperty] private Color maintainabilityScoreColor = Colors.Gray;
    public int IssueCount => Issues.Count;
    public bool HasIssues => Issues.Count > 0;
    public bool HasSuggestions => Suggestions.Count > 0;
    public bool CanAnalyze => !IsAnalyzing && !string.IsNullOrWhiteSpace(CodeInput) && SelectedLanguage != null;
    public bool IsNotAnalyzing => !IsAnalyzing;
    #endregion
    #region Commands
    [RelayCommand]
    private async Task AnalyzeCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(CodeInput) || SelectedLanguage == null) return;
        try
        {
            IsAnalyzing = true;
            AnalysisProgress = 0;
            AnalysisStatus = "Analyzing code...";
            _cancellationTokenSource = new CancellationTokenSource();
            Issues.Clear();
            Suggestions.Clear();
            HasAnalysisResults = false;
            await Task.Delay(2000, _cancellationTokenSource.Token); // Simulate analysis
            ShowMockResults();
            AnalysisStatus = "Analysis completed successfully!";
            AnalysisProgress = 1.0;
            HasAnalysisResults = true;
            _logger.LogInformation("Code analysis completed");
        }
        catch (OperationCanceledException)
        {
            AnalysisStatus = "Analysis cancelled";
        }
        catch (Exception ex)
        {
            AnalysisStatus = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error analyzing code");
        }
        finally
        {
            IsAnalyzing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            OnPropertyChanged(nameof(CanAnalyze));
            OnPropertyChanged(nameof(IsNotAnalyzing));
            OnPropertyChanged(nameof(IssueCount));
            OnPropertyChanged(nameof(HasIssues));
            OnPropertyChanged(nameof(HasSuggestions));
        }
    }
    [RelayCommand] private void CancelAnalysis() => _cancellationTokenSource?.Cancel();
    [RelayCommand]
    private async Task LoadFromFileAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert("Load File", "File picker coming soon!", "OK");
    }
    [RelayCommand]
    private async Task PasteFromClipboardAsync()
    {
        try
        {
            var clipboardText = await Clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                CodeInput = clipboardText;
                UpdateCodeStatistics();
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pasting from clipboard");
        }
    }
    [RelayCommand] private void ClearCode() => CodeInput = string.Empty;
    [RelayCommand]
    private async Task ShowAnalysisHistoryAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert("History", "Analysis history coming soon!", "OK");
    }
    [RelayCommand]
    private async Task CopyResultsAsync()
    {
        try
        {
            var results = GenerateResultsText();
            await Clipboard.SetTextAsync(results);
            AnalysisStatus = "Results copied!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying results");
        }
    }
    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        try
        {
            var results = GenerateResultsText();
            var fileName = $"analysis_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            await File.WriteAllTextAsync(filePath, results);
            AnalysisStatus = $"Saved to {fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving results");
        }
    }
    [RelayCommand]
    private async Task ShowFixSuggestionAsync(CodeIssue issue)
    {
        if (issue != null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Fix", issue.SuggestedFix, "OK");
        }
    }
    [RelayCommand]
    private async Task ApplySuggestionAsync(ImprovementSuggestion suggestion)
    {
        if (suggestion != null)
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Apply", $"Apply: {suggestion.Description}", "Yes", "No");
            if (result) AnalysisStatus = $"Applied: {suggestion.Title}";
        }
    }
    #endregion
    #region Methods
    public async Task InitializeAsync()
    {
        try
        {
            LoadDefaultLanguages();
            _logger.LogInformation("AI Code Analysis initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing");
        }
    }
    public void Cleanup()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
    private void InitializeDefaults()
    {
        AnalysisTypes.Add("Quick");
        AnalysisTypes.Add("Standard");
        AnalysisTypes.Add("Comprehensive");
        AnalysisTypes.Add("Security Focus");
        AnalysisTypes.Add("Performance Focus");
        LoadDefaultLanguages();
    }
    private void LoadDefaultLanguages()
    {
        SupportedLanguages.Clear();
        SupportedLanguages.Add(new ProgrammingLanguage { Id = "csharp", DisplayName = "C#" });
        SupportedLanguages.Add(new ProgrammingLanguage { Id = "javascript", DisplayName = "JavaScript" });
        SupportedLanguages.Add(new ProgrammingLanguage { Id = "python", DisplayName = "Python" });
        SupportedLanguages.Add(new ProgrammingLanguage { Id = "java", DisplayName = "Java" });
        SelectedLanguage = SupportedLanguages.FirstOrDefault();
    }
    private void UpdateCodeStatistics()
    {
        if (string.IsNullOrEmpty(CodeInput))
        {
            LineCount = 0;
            FileSize = 0;
            return;
        }
        LineCount = CodeInput.Split('\n').Length;
        FileSize = System.Text.Encoding.UTF8.GetByteCount(CodeInput);
        HasFileInfo = LineCount > 0;
    }
    private void ShowMockResults()
    {
        OverallScore = 78;
        OverallScoreColor = GetScoreColor(OverallScore);
        SyntaxScore = 95;
        SyntaxScoreColor = GetScoreColor(SyntaxScore);
        SecurityScore = 65;
        SecurityScoreColor = GetScoreColor(SecurityScore);
        PerformanceScore = 72;
        PerformanceScoreColor = GetScoreColor(PerformanceScore);
        ComplexityScore = 80;
        ComplexityScoreColor = GetScoreColor(ComplexityScore);
        BestPracticesScore = 85;
        BestPracticesScoreColor = GetScoreColor(BestPracticesScore);
        MaintainabilityScore = 75;
        MaintainabilityScoreColor = GetScoreColor(MaintainabilityScore);
        Issues.Add(new CodeIssue
        {
            Title = "Potential SQL Injection",
            Description = "Direct string concatenation in SQL query.",
            Severity = "High",
            SeverityColor = Colors.Red,
            Location = 42,
            SuggestedFix = "Use parameterized queries."
        });
        Suggestions.Add(new ImprovementSuggestion
        {
            Title = "Use StringBuilder",
            Description = "Replace string concatenation with StringBuilder.",
            Icon = "⚡",
            Impact = "Performance improvement"
        });
        HasAnalysisResults = true;
    }
    private Color GetScoreColor(int score) => score >= 80 ? Colors.Green : score >= 60 ? Colors.Orange : Colors.Red;
    private Color GetSeverityColor(string severity) => severity switch
    {
        "High" => Colors.Red,
        "Medium" => Colors.Orange,
        "Low" => Colors.Yellow,
        _ => Colors.Gray
    };
    private string GenerateResultsText()
    {
        return $"Code Analysis Results\n" +
               $"Overall Score: {OverallScore}/100\n" +
               $"Issues Found: {IssueCount}\n" +
               $"Suggestions: {Suggestions.Count}";
    }
    #endregion
}
public class ProgrammingLanguage
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] FileExtensions { get; set; } = Array.Empty<string>();
}
public class CodeIssue
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Color SeverityColor { get; set; } = Colors.Gray;
    public int Location { get; set; }
    public string SuggestedFix { get; set; } = string.Empty;
}
public class ImprovementSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

