using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Workflow detail page
/// Detailed view and management of a specific workflow
//// </summary>
[QueryProperty(nameof(WorkflowId), "workflowId")]
public partial class WorkflowDetailPage : ContentPage
{
    /// <summary>
    /// Workflow ID
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Workflow detail view model</param>
    public WorkflowDetailPage(WorkflowDetailViewModel viewModel)
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
        // Load workflow details when page appears
        if (BindingContext is WorkflowDetailViewModel viewModel && !string.IsNullOrEmpty(WorkflowId))
        {
            await viewModel.LoadWorkflowAsync(WorkflowId);
        }
    }
}

