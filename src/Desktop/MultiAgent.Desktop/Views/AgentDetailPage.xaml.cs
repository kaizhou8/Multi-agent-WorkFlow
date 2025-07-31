using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Agent detail page
/// Detailed view and management of a specific agent
//// </summary>
[QueryProperty(nameof(AgentId), "agentId")]
public partial class AgentDetailPage : ContentPage
{
    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Agent detail view model</param>
    public AgentDetailPage(AgentDetailViewModel viewModel)
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
        // Load agent details when page appears
        if (BindingContext is AgentDetailViewModel viewModel && !string.IsNullOrEmpty(AgentId))
        {
            await viewModel.LoadAgentAsync(AgentId);
        }
    }
}

