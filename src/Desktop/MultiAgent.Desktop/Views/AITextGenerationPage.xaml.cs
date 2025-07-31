using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI Text Generation Page - AI
/// Provides interface for AI-powered text generation with model configuration and history
//// </summary>
public partial class AITextGenerationPage : ContentPage
{
    /// <summary>
    /// Initialize AI Text Generation PageAI
    /// </summary>
    /// <param name="viewModel">AI Text Generation ViewModel
    public AITextGenerationPage(AITextGenerationViewModel viewModel)
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
        if (BindingContext is AITextGenerationViewModel viewModel)
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
        if (BindingContext is AITextGenerationViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

