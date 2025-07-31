using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Workflows page
/// Workflow management interface
//// </summary>
public partial class WorkflowsPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Workflows view model</param>
    public WorkflowsPage(WorkflowsViewModel viewModel)
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
        // Refresh workflows when page appears
        if (BindingContext is WorkflowsViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}

