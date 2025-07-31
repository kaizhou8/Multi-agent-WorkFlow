using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI Code Analysis Page - AI
/// Provides interface for AI-powered code analysis with quality assessment and suggestions
//// </summary>
public partial class AICodeAnalysisPage : ContentPage
{
    /// <summary>
    /// Initialize AI Code Analysis PageAI
    /// </summary>
    /// <param name="viewModel">AI Code Analysis ViewModel
    public AICodeAnalysisPage(AICodeAnalysisViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>
    /// Handle page appearing event
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AICodeAnalysisViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
    /// <summary>
    /// Handle page disappearing event
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is AICodeAnalysisViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

