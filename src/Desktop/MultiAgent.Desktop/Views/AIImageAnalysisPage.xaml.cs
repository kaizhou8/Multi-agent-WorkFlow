using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI Image Analysis Page - AI
/// Provides interface for AI-powered image analysis with object detection, scene recognition, and content understanding
//// </summary>
public partial class AIImageAnalysisPage : ContentPage
{
    /// <summary>
    /// Initialize AI Image Analysis PageAI
    /// </summary>
    /// <param name="viewModel">AI Image Analysis ViewModel
    public AIImageAnalysisPage(AIImageAnalysisViewModel viewModel)
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
        if (BindingContext is AIImageAnalysisViewModel viewModel)
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
        if (BindingContext is AIImageAnalysisViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

