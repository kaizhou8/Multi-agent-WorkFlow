using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Workflow Designer Page -
/// Provides visual interface for designing and editing workflows
//// </summary>
public partial class WorkflowDesignerPage : ContentPage
{
    /// <summary>
    /// Initialize Workflow Designer Page
    /// </summary>
    /// <param name="viewModel">Workflow Designer ViewModel</param>
    public WorkflowDesignerPage(WorkflowDesignerViewModel viewModel)
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
        if (BindingContext is WorkflowDesignerViewModel viewModel)
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
        if (BindingContext is WorkflowDesignerViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}

