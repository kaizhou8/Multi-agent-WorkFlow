using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Agents page
/// Agent management interface
//// </summary>
public partial class AgentsPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Agents view model</param>
    public AgentsPage(AgentsViewModel viewModel)
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
        // Refresh agents when page appears
        if (BindingContext is AgentsViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}

