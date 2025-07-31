using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Settings page
/// Application configuration and preferences
//// </summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Settings view model</param>
    public SettingsPage(SettingsViewModel viewModel)
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
        // Load settings when page appears
        if (BindingContext is SettingsViewModel viewModel)
        {
            await viewModel.LoadSettingsAsync();
        }
    }
}

