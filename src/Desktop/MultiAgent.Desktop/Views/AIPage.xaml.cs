using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// AI services page
/// AI model management and chat interface
/// AI
/// </summary>
public partial class AIPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">AI view model
    public AIPage(AIViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>
    /// Page appearing event
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Initialize AI services when page appearsAI
        if (BindingContext is AIViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}

