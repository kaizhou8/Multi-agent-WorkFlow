using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// App Shell view modelShell
/// Manages the main shell navigation and global state
//// </summary>
public partial class AppShellViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISignalRService _signalRService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AppShellViewModel> _logger;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authenticationService">Authentication service</param>
    /// <param name="signalRService">SignalR service
    /// <param name="settingsService">Settings service</param>
    /// <param name="logger">Logger</param>
    public AppShellViewModel(
        IAuthenticationService authenticationService,
        ISignalRService signalRService,
        ISettingsService settingsService,
        ILogger<AppShellViewModel> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Initialize
        Initialize();
    }
    #region Properties /
    /// <summary>
    /// Current user
    /// </summary>
    [ObservableProperty]
    private UserInfo? currentUser;
    /// <summary>
    /// Connection status text
    /// </summary>
    [ObservableProperty]
    private string connectionStatusText = "Disconnected";
    /// <summary>
    /// Connection status color
    /// </summary>
    [ObservableProperty]
    private Color connectionStatusColor = Colors.Red;
    /// <summary>
    /// Is connecting
    /// </summary>
    [ObservableProperty]
    private bool isConnecting;
    /// <summary>
    /// Is authenticated
    /// </summary>
    [ObservableProperty]
    private bool isAuthenticated;
    #endregion
    #region Commands /
    /// <summary>
    /// Logout command
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("User initiated logout");
            // Stop SignalR connectionSignalR
            await _signalRService.StopConnectionAsync();
            // Logout user
            await _authenticationService.LogoutAsync();
            // Navigate to login page
            if (Application.Current is App app)
            {
                app.NavigateToLogin();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            await Shell.Current.DisplayAlert("Error", "An error occurred during logout. Please try again.", "OK");
        }
    }
    /// <summary>
    /// Connect to SignalR commandSignalR
    /// </summary>
    [RelayCommand]
    private async Task ConnectSignalRAsync()
    {
        try
        {
            if (IsConnecting || _signalRService.ConnectionState == HubConnectionState.Connected)
                return;
            IsConnecting = true;
            UpdateConnectionStatus(HubConnectionState.Connecting);
            var token = await _authenticationService.GetAccessTokenAsync();
            await _signalRService.StartConnectionAsync(_settingsService.Settings.SignalRHubUrl, token);
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR");
            UpdateConnectionStatus(HubConnectionState.Disconnected);
            await Shell.Current.DisplayAlert("Connection Error",
                "Failed to connect to the real-time service. Some features may not work properly.",
                "OK");
        }
        finally
        {
            IsConnecting = false;
        }
    }
    /// <summary>
    /// Disconnect from SignalR commandSignalR
    /// </summary>
    [RelayCommand]
    private async Task DisconnectSignalRAsync()
    {
        try
        {
            await _signalRService.StopConnectionAsync();
            _logger.LogInformation("SignalR connection stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from SignalR");
        }
    }
    #endregion
    /// <summary>
    /// Initialize view model
    /// </summary>
    private void Initialize()
    {
        try
        {
            // Set current user
            CurrentUser = _authenticationService.CurrentUser;
            IsAuthenticated = CurrentUser != null;
            // Subscribe to authentication events
            _authenticationService.UserChanged += OnUserChanged;
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            // Subscribe to SignalR eventsSignalR
            _signalRService.ConnectionStateChanged += OnConnectionStateChanged;
            // Initialize SignalR connection if auto-connect is enabled，
            if (_settingsService.Settings.AutoConnectSignalR && IsAuthenticated)
            {
                _ = Task.Run(async () => await ConnectSignalRAsync());
            }
            _logger.LogDebug("AppShellViewModel initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing AppShellViewModel");
        }
    }
    /// <summary>
    /// Handle user changed event
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="user">User information</param>
    private void OnUserChanged(object? sender, UserInfo? user)
    {
        CurrentUser = user;
        IsAuthenticated = user != null;
        if (user != null)
        {
            _logger.LogInformation("User changed to {Username}", user.Username);
            // Auto-connect to SignalR when user logs inSignalR
            if (_settingsService.Settings.AutoConnectSignalR)
            {
                _ = Task.Run(async () => await ConnectSignalRAsync());
            }
        }
        else
        {
            _logger.LogInformation("User logged out");
            // Disconnect SignalR when user logs outSignalR
            _ = Task.Run(async () => await DisconnectSignalRAsync());
        }
    }
    /// <summary>
    /// Handle authentication state changed event
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="isAuthenticated">Authentication state</param>
    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        IsAuthenticated = isAuthenticated;
        _logger.LogDebug("Authentication state changed to {IsAuthenticated}", isAuthenticated);
    }
    /// <summary>
    /// Handle connection state changed event
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="state">Connection state</param>
    private void OnConnectionStateChanged(object? sender, HubConnectionState state)
    {
        UpdateConnectionStatus(state);
        _logger.LogDebug("SignalR connection state changed to {State}", state);
    }
    /// <summary>
    /// Update connection status display
    /// </summary>
    /// <param name="state">Connection state</param>
    private void UpdateConnectionStatus(HubConnectionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionStatusText = state switch
            {
                HubConnectionState.Connected => "Connected",
                HubConnectionState.Connecting => "Connecting...",
                HubConnectionState.Reconnecting => "Reconnecting...",
                HubConnectionState.Disconnected => "Disconnected",
                _ => "Unknown"
            };
            ConnectionStatusColor = state switch
            {
                HubConnectionState.Connected => Colors.Green,
                HubConnectionState.Connecting => Colors.Orange,
                HubConnectionState.Reconnecting => Colors.Orange,
                HubConnectionState.Disconnected => Colors.Red,
                _ => Colors.Gray
            };
        });
    }
    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Unsubscribe from events
            _authenticationService.UserChanged -= OnUserChanged;
            _authenticationService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            _signalRService.ConnectionStateChanged -= OnConnectionStateChanged;
            _logger.LogDebug("AppShellViewModel disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing AppShellViewModel");
        }
    }
}

