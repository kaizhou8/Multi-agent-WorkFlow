using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Agent Configuration Page -
/// Provides interface for configuring agent settings, capabilities, and environment
//// </summary>
public partial class AgentConfigurationPage : ContentPage
{
    /// <summary>
    /// Initialize Agent Configuration Page
    /// </summary>
    /// <param name="viewModel">Agent Configuration ViewModel</param>
    public AgentConfigurationPage(AgentConfigurationViewModel viewModel)
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
        if (BindingContext is AgentConfigurationViewModel viewModel)
        {
            await viewModel.LoadConfigurationAsync();
        }
    }
    /// <summary>
    /// Handle page disappearing event
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is AgentConfigurationViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

