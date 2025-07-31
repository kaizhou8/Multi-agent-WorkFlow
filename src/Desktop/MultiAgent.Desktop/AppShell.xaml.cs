using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop;
/// <summary>
/// Application shellShell
/// Main navigation container for the desktop application
//// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Shell view model
    public AppShell(AppShellViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Register routes for navigation
        RegisterRoutes();
    }
    /// <summary>
    /// Register navigation routes
    /// </summary>
    private void RegisterRoutes()
    {
        // Agent detail page
        Routing.RegisterRoute("agents/detail", typeof(Views.AgentDetailPage));
        // Workflow detail page
        Routing.RegisterRoute("workflows/detail", typeof(Views.WorkflowDetailPage));
        // Workflow designer page
        Routing.RegisterRoute("workflows/designer", typeof(Views.WorkflowDesignerPage));
    }
}

