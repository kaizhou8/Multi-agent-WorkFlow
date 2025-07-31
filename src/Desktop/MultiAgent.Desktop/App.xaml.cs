using MultiAgent.Desktop.Services;
namespace MultiAgent.Desktop;
/// <summary>
/// Main application class
/// Entry point for the MAUI desktop application
/// MAUI
/// </summary>
public partial class App : Application
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<App> _logger;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authenticationService">Authentication service</param>
    /// <param name="settingsService">Settings service</param>
    /// <param name="logger">Logger</param>
    public App(IAuthenticationService authenticationService, ISettingsService settingsService, ILogger<App> logger)
    {
        InitializeComponent();
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Initialize application
        InitializeApp();
    }
    /// <summary>
    /// Initialize application
    /// </summary>
    private async void InitializeApp()
    {
        try
        {
            _logger.LogInformation("Initializing Multi-Agent Desktop Application");
            // Load settings
            await _settingsService.LoadSettingsAsync();
            // Check authentication status
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                _logger.LogInformation("User is authenticated, navigating to main shell");
                MainPage = new AppShell();
            }
            else
            {
                _logger.LogInformation("User is not authenticated, navigating to login page");
                MainPage = new NavigationPage(new Views.LoginPage());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application initialization");
            // Show error and fallback to login
            await DisplayAlert("Initialization Error",
                "An error occurred while initializing the application. Please try again.",
                "OK");
            MainPage = new NavigationPage(new Views.LoginPage());
        }
    }
    /// <summary>
    /// Called when application starts
    /// </summary>
    protected override void OnStart()
    {
        _logger.LogInformation("Application started");
        base.OnStart();
    }
    /// <summary>
    /// Called when application sleeps
    /// </summary>
    protected override void OnSleep()
    {
        _logger.LogInformation("Application sleeping");
        base.OnSleep();
    }
    /// <summary>
    /// Called when application resumes
    /// </summary>
    protected override void OnResume()
    {
        _logger.LogInformation("Application resumed");
        base.OnResume();
    }
    /// <summary>
    /// Display alert dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="cancel">Cancel button text</param>
    /// <returns>Task</returns>
    public static async Task DisplayAlert(string title, string message, string cancel)
    {
        if (Current?.MainPage != null)
        {
            await Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }
    /// <summary>
    /// Display confirmation dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="accept">Accept button text</param>
    /// <param name="cancel">Cancel button text</param>
    /// <returns>User choice</returns>
    public static async Task<bool> DisplayConfirm(string title, string message, string accept, string cancel)
    {
        if (Current?.MainPage != null)
        {
            return await Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }
    /// <summary>
    /// Navigate to main application
    /// </summary>
    public void NavigateToMainApp()
    {
        try
        {
            _logger.LogInformation("Navigating to main application");
            MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to main application");
        }
    }
    /// <summary>
    /// Navigate to login page
    /// </summary>
    public void NavigateToLogin()
    {
        try
        {
            _logger.LogInformation("Navigating to login page");
            MainPage = new NavigationPage(new Views.LoginPage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to login page");
        }
    }
}

