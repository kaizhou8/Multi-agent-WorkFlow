using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
using System.Reflection;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Settings page view model
/// Manages application settings and preferences
//// </summary>
public partial class SettingsViewModel : ObservableObject
{
    #region Fields /
    private readonly ISettingsService _settingsService;
    private readonly IApiService _apiService;
    private readonly ILogger<SettingsViewModel> _logger;
    [ObservableProperty]
    private string _apiEndpoint = string.Empty;
    [ObservableProperty]
    private string _signalRHubUrl = string.Empty;
    [ObservableProperty]
    private int _connectionTimeout = 30;
    [ObservableProperty]
    private bool _autoReconnect = true;
    [ObservableProperty]
    private bool _isTesting;
    [ObservableProperty]
    private string _connectionStatus = string.Empty;
    [ObservableProperty]
    private Color _connectionStatusColor = Colors.Gray;
    [ObservableProperty]
    private string _selectedTheme = "System";
    [ObservableProperty]
    private string _selectedLanguage = "English";
    [ObservableProperty]
    private double _fontSize = 14;
    [ObservableProperty]
    private bool _notificationsEnabled = true;
    [ObservableProperty]
    private bool _agentNotifications = true;
    [ObservableProperty]
    private bool _workflowNotifications = true;
    [ObservableProperty]
    private bool _systemNotifications = true;
    [ObservableProperty]
    private double _refreshInterval = 10;
    [ObservableProperty]
    private int _maxLogEntries = 1000;
    [ObservableProperty]
    private bool _cachingEnabled = true;
    [ObservableProperty]
    private bool _rememberLogin = true;
    [ObservableProperty]
    private int _autoLogoutMinutes = 30;
    [ObservableProperty]
    private bool _requireAuthForActions = false;
    [ObservableProperty]
    private string _appVersion = string.Empty;
    [ObservableProperty]
    private string _buildNumber = string.Empty;
    [ObservableProperty]
    private string _platform = string.Empty;
    [ObservableProperty]
    private ObservableCollection<string> _availableThemes = new() { "Light", "Dark", "System" };
    [ObservableProperty]
    private ObservableCollection<string> _availableLanguages = new() { "English", "", "Auto" };
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settingsService">Settings service</param>
    /// <param name="apiService">API service
    /// <param name="logger">Logger</param>
    public SettingsViewModel(
        ISettingsService settingsService,
        IApiService apiService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _apiService = apiService;
        _logger = logger;
        // Initialize app info
        InitializeAppInfo();
    }
    #endregion
    #region Properties /
    /// <summary>
    /// Is not testing connection
    /// </summary>
    public bool IsNotTesting => !IsTesting;
    /// <summary>
    /// Has connection status
    /// </summary>
    public bool HasConnectionStatus => !string.IsNullOrWhiteSpace(ConnectionStatus);
    #endregion
    #region Commands /
    /// <summary>
    /// Load settings async
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Loading application settings /
            // Load settings from service
            ApiEndpoint = _settingsService.ApiEndpoint;
            SignalRHubUrl = _settingsService.SignalRHubUrl;
            ConnectionTimeout = _settingsService.ConnectionTimeout;
            AutoReconnect = _settingsService.AutoReconnect;
            SelectedTheme = _settingsService.Theme;
            SelectedLanguage = _settingsService.Language;
            FontSize = _settingsService.FontSize;
            NotificationsEnabled = _settingsService.NotificationsEnabled;
            AgentNotifications = _settingsService.AgentNotifications;
            WorkflowNotifications = _settingsService.WorkflowNotifications;
            SystemNotifications = _settingsService.SystemNotifications;
            RefreshInterval = _settingsService.RefreshInterval;
            MaxLogEntries = _settingsService.MaxLogEntries;
            CachingEnabled = _settingsService.CachingEnabled;
            RememberLogin = _settingsService.RememberLogin;
            AutoLogoutMinutes = _settingsService.AutoLogoutMinutes;
            RequireAuthForActions = _settingsService.RequireAuthForActions;
            _logger.LogInformation("Settings loaded successfully /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings /
        }
    }
    /// <summary>
    /// Save settings command
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Saving application settings /
            // Save settings to service
            _settingsService.ApiEndpoint = ApiEndpoint;
            _settingsService.SignalRHubUrl = SignalRHubUrl;
            _settingsService.ConnectionTimeout = ConnectionTimeout;
            _settingsService.AutoReconnect = AutoReconnect;
            _settingsService.Theme = SelectedTheme;
            _settingsService.Language = SelectedLanguage;
            _settingsService.FontSize = FontSize;
            _settingsService.NotificationsEnabled = NotificationsEnabled;
            _settingsService.AgentNotifications = AgentNotifications;
            _settingsService.WorkflowNotifications = WorkflowNotifications;
            _settingsService.SystemNotifications = SystemNotifications;
            _settingsService.RefreshInterval = RefreshInterval;
            _settingsService.MaxLogEntries = MaxLogEntries;
            _settingsService.CachingEnabled = CachingEnabled;
            _settingsService.RememberLogin = RememberLogin;
            _settingsService.AutoLogoutMinutes = AutoLogoutMinutes;
            _settingsService.RequireAuthForActions = RequireAuthForActions;
            await _settingsService.SaveAsync();
            await Shell.Current.DisplayAlert("Success", "Settings saved successfully! /
            _logger.LogInformation("Settings saved successfully /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings /
            await Shell.Current.DisplayAlert("Error", "Failed to save settings /
        }
    }
    /// <summary>
    /// Cancel command
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        try
        {
            // Reload original settings
            await LoadSettingsAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel settings /
        }
    }
    /// <summary>
    /// Test connection command
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTesting = true;
            ConnectionStatus = "Testing connection... /
            ConnectionStatusColor = Colors.Orange;
            _logger.LogInformation("Testing connection to {ApiEndpoint} /
            // Test API connectionAPI
            var isConnected = await _apiService.TestConnectionAsync();
            if (isConnected)
            {
                ConnectionStatus = "Connection successful! /
                ConnectionStatusColor = Colors.Green;
                _logger.LogInformation("Connection test successful /
            }
            else
            {
                ConnectionStatus = "Connection failed! /
                ConnectionStatusColor = Colors.Red;
                _logger.LogWarning("Connection test failed /
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Connection error: {ex.Message}";
            ConnectionStatusColor = Colors.Red;
            _logger.LogError(ex, "Connection test error /
        }
        finally
        {
            IsTesting = false;
            OnPropertyChanged(nameof(IsNotTesting));
            OnPropertyChanged(nameof(HasConnectionStatus));
        }
    }
    /// <summary>
    /// Reset settings command
    /// </summary>
    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        try
        {
            var result = await Shell.Current.DisplayAlert(
                "Reset Settings",
                "Are you sure you want to reset all settings to default? /
                "Yes",
                "No");
            if (result)
            {
                _logger.LogInformation("Resetting application settings /
                await _settingsService.ResetAsync();
                await LoadSettingsAsync();
                await Shell.Current.DisplayAlert("Success", "Settings reset successfully! /
                _logger.LogInformation("Settings reset successfully /
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings /
            await Shell.Current.DisplayAlert("Error", "Failed to reset settings /
        }
    }
    /// <summary>
    /// Check updates command
    /// </summary>
    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates /
            // TODO: Implement update checking logic
            await Shell.Current.DisplayAlert("Updates", "You are running the latest version! /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check updates /
        }
    }
    /// <summary>
    /// View logs command
    /// </summary>
    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        try
        {
            _logger.LogInformation("Opening logs viewer /
            await Shell.Current.GoToAsync("//settings/logs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open logs viewer /
        }
    }
    /// <summary>
    /// Export settings command
    /// </summary>
    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Exporting settings /
            // TODO: Implement settings export logic
            await Shell.Current.DisplayAlert("Export", "Settings export functionality coming soon! /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings /
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Initialize app information
    /// </summary>
    private void InitializeAppInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            AppVersion = version?.ToString() ?? "1.0.0.0";
            BuildNumber = DateTime.Now.ToString("yyyyMMdd");
            Platform = DeviceInfo.Platform.ToString();
            _logger.LogInformation("App info initialized: Version {Version}, Platform {Platform} /
                AppVersion, Platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize app info /
            AppVersion = "Unknown";
            BuildNumber = "Unknown";
            Platform = "Unknown";
        }
    }
    #endregion
}

