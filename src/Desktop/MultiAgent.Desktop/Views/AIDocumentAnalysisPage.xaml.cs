using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI Document Analysis Page - AI
/// Provides interface for AI-powered document analysis with content extraction and insights
//// </summary>
public partial class AIDocumentAnalysisPage : ContentPage
{
    /// <summary>
    /// Initialize AI Document Analysis PageAI
    /// </summary>
    /// <param name="viewModel">AI Document Analysis ViewModel
    public AIDocumentAnalysisPage(AIDocumentAnalysisViewModel viewModel)
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
        if (BindingContext is AIDocumentAnalysisViewModel viewModel)
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
        if (BindingContext is AIDocumentAnalysisViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

