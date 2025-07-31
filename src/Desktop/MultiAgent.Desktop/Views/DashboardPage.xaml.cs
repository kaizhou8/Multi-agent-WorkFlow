using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Dashboard page
/// Main overview interface for the application
//// </summary>
public partial class DashboardPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Dashboard view model</param>
    public DashboardPage(DashboardViewModel viewModel)
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
        // Refresh data when page appears
        if (BindingContext is DashboardViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}

