using MultiAgent.Desktop.ViewModels;
namespace MultiAgent.Desktop.Views;
/// <summary>
/// Login page
/// User authentication interface
//// </summary>
public partial class LoginPage : ContentPage
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="viewModel">Login view model</param>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>
    /// Page appearing event
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Focus on username entry
        UsernameEntry.Focus();
    }
}

