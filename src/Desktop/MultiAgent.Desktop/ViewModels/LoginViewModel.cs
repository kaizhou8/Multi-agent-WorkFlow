using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Login view model
/// Handles user authentication logic
//// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<LoginViewModel> _logger;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authenticationService">Authentication service</param>
    /// <param name="logger">Logger</param>
    public LoginViewModel(IAuthenticationService authenticationService, ILogger<LoginViewModel> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Load saved credentials if remember me was checked，
        LoadSavedCredentials();
    }
    #region Properties /
    /// <summary>
    /// Username
    /// </summary>
    [ObservableProperty]
    private string username = string.Empty;
    /// <summary>
    /// Password
    /// </summary>
    [ObservableProperty]
    private string password = string.Empty;
    /// <summary>
    /// Remember me
    /// </summary>
    [ObservableProperty]
    private bool rememberMe;
    /// <summary>
    /// Is loading
    /// </summary>
    [ObservableProperty]
    private bool isLoading;
    /// <summary>
    /// Error message
    /// </summary>
    [ObservableProperty]
    private string errorMessage = string.Empty;
    /// <summary>
    /// Has error
    /// </summary>
    [ObservableProperty]
    private bool hasError;
    #endregion
    #region Commands /
    /// <summary>
    /// Login command
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowError("Please enter your username /
                return;
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Please enter your password /
                return;
            }
            IsLoading = true;
            ClearError();
            _logger.LogInformation("Attempting login for user {Username}", Username);
            // Attempt login
            var result = await _authenticationService.LoginAsync(Username, Password);
            if (result.Success)
            {
                _logger.LogInformation("Login successful for user {Username}", Username);
                // Save credentials if remember me is checked，
                if (RememberMe)
                {
                    SaveCredentials();
                }
                else
                {
                    ClearSavedCredentials();
                }
                // Navigate to main application
                if (Application.Current is App app)
                {
                    app.NavigateToMainApp();
                }
            }
            else
            {
                _logger.LogWarning("Login failed for user {Username}: {ErrorMessage}", Username, result.ErrorMessage);
                ShowError(result.ErrorMessage ?? "Login failed. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", Username);
            ShowError("An error occurred during login. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Focus password command
    /// </summary>
    [RelayCommand]
    private void FocusPassword()
    {
        // This would be handled by the view
        // The view can subscribe to this command to focus the password field
    }
    /// <summary>
    /// Use admin credentials command
    /// </summary>
    [RelayCommand]
    private void UseAdminCredentials()
    {
        Username = "admin";
        Password = "admin123";
        RememberMe = false;
        ClearError();
        _logger.LogDebug("Admin credentials loaded");
    }
    /// <summary>
    /// Use user credentials command
    /// </summary>
    [RelayCommand]
    private void UseUserCredentials()
    {
        Username = "user";
        Password = "user123";
        RememberMe = false;
        ClearError();
        _logger.LogDebug("User credentials loaded");
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Show error message
    /// </summary>
    /// <param name="message">Error message</param>
    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
    /// <summary>
    /// Clear error message
    /// </summary>
    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
    /// <summary>
    /// Load saved credentials
    /// </summary>
    private void LoadSavedCredentials()
    {
        try
        {
            if (Preferences.ContainsKey("remember_me") && Preferences.Get("remember_me", false))
            {
                Username = Preferences.Get("saved_username", string.Empty);
                // Note: In a real application, you should never save passwords in plain text
                // This is for demo purposes only：
                                Password = Preferences.Get("saved_password", string.Empty);
                RememberMe = true;
                _logger.LogDebug("Loaded saved credentials for user {Username}", Username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading saved credentials");
        }
    }
    /// <summary>
    /// Save credentials
    /// </summary>
    private void SaveCredentials()
    {
        try
        {
            Preferences.Set("remember_me", true);
            Preferences.Set("saved_username", Username);
            // Note: In a real application, you should never save passwords in plain text
            // This is for demo purposes only：
                        Preferences.Set("saved_password", Password);
            _logger.LogDebug("Saved credentials for user {Username}", Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credentials");
        }
    }
    /// <summary>
    /// Clear saved credentials
    /// </summary>
    private void ClearSavedCredentials()
    {
        try
        {
            Preferences.Remove("remember_me");
            Preferences.Remove("saved_username");
            Preferences.Remove("saved_password");
            _logger.LogDebug("Cleared saved credentials");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing saved credentials");
        }
    }
    #endregion
}

