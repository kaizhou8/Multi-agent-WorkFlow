using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI Audio Analysis Page - AI
/// Provides interface for AI-powered audio analysis with speech recognition, sentiment analysis, and content understanding
//// </summary>
public partial class AIAudioAnalysisPage : ContentPage
{
    /// <summary>
    /// Initialize AI Audio Analysis PageAI
    /// </summary>
    /// <param name="viewModel">AI Audio Analysis ViewModel
    public AIAudioAnalysisPage(AIAudioAnalysisViewModel viewModel)
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
        if (BindingContext is AIAudioAnalysisViewModel viewModel)
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
        if (BindingContext is AIAudioAnalysisViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

