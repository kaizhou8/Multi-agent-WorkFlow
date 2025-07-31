using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace MultiAgent.Desktop.Services;
/// <summary>
/// Settings service interface
/// Manages application settings and preferences
//// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Current application settings
    /// </summary>
    AppSettings Settings { get; }
    /// <summary>
    /// Load settings from storage
    /// </summary>
    /// <returns>Task</returns>
    Task LoadSettingsAsync();
    /// <summary>
    /// Save settings to storage
    /// </summary>
    /// <returns>Task</returns>
    Task SaveSettingsAsync();
    /// <summary>
    /// Reset settings to default
    /// </summary>
    /// <returns>Task</returns>
    Task ResetSettingsAsync();
    /// <summary>
    /// Update API endpointAPI
    /// </summary>
    /// <param name="apiEndpoint">API endpoint URL
    /// <returns>Task</returns>
    Task UpdateApiEndpointAsync(string apiEndpoint);
    /// <summary>
    /// Update theme
    /// </summary>
    /// <param name="theme">Theme name</param>
    /// <returns>Task</returns>
    Task UpdateThemeAsync(string theme);
    /// <summary>
    /// Update language
    /// </summary>
    /// <param name="language">Language code</param>
    /// <returns>Task</returns>
    Task UpdateLanguageAsync(string language);
    // Events
    event EventHandler<AppSettings>? SettingsChanged;
}
/// <summary>
/// Settings service implementation
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private AppSettings _settings;
    private const string SettingsKey = "app_settings";
    /// <summary>
    /// Current application settings
    /// </summary>
    public AppSettings Settings => _settings;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = new AppSettings();
    }
    #region Events /
    public event EventHandler<AppSettings>? SettingsChanged;
    #endregion
    /// <summary>
    /// Load settings from storage
    /// </summary>
    /// <returns>Task</returns>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (Preferences.ContainsKey(SettingsKey))
            {
                var settingsJson = Preferences.Get(SettingsKey, string.Empty);
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(settingsJson);
                    if (loadedSettings != null)
                    {
                        _settings = loadedSettings;
                        _logger.LogDebug("Settings loaded successfully");
                    }
                }
            }
            else
            {
                // First time run, create default settings，
                _settings = new AppSettings();
                await SaveSettingsAsync();
                _logger.LogInformation("Created default settings for first time run");
            }
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings, using defaults");
            _settings = new AppSettings();
        }
    }
    /// <summary>
    /// Save settings to storage
    /// </summary>
    /// <returns>Task</returns>
    public async Task SaveSettingsAsync()
    {
        try
        {
            var settingsJson = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Preferences.Set(SettingsKey, settingsJson);
            _logger.LogDebug("Settings saved successfully");
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
        }
        await Task.CompletedTask;
    }
    /// <summary>
    /// Reset settings to default
    /// </summary>
    /// <returns>Task</returns>
    public async Task ResetSettingsAsync()
    {
        try
        {
            _settings = new AppSettings();
            await SaveSettingsAsync();
            _logger.LogInformation("Settings reset to defaults");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings");
        }
    }
    /// <summary>
    /// Update API endpointAPI
    /// </summary>
    /// <param name="apiEndpoint">API endpoint URL
    /// <returns>Task</returns>
    public async Task UpdateApiEndpointAsync(string apiEndpoint)
    {
        try
        {
            _settings.ApiEndpoint = apiEndpoint;
            await SaveSettingsAsync();
            _logger.LogInformation("API endpoint updated to {ApiEndpoint}", apiEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API endpoint");
        }
    }
    /// <summary>
    /// Update theme
    /// </summary>
    /// <param name="theme">Theme name</param>
    /// <returns>Task</returns>
    public async Task UpdateThemeAsync(string theme)
    {
        try
        {
            _settings.Theme = theme;
            await SaveSettingsAsync();
            _logger.LogInformation("Theme updated to {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating theme");
        }
    }
    /// <summary>
    /// Update language
    /// </summary>
    /// <param name="language">Language code</param>
    /// <returns>Task</returns>
    public async Task UpdateLanguageAsync(string language)
    {
        try
        {
            _settings.Language = language;
            await SaveSettingsAsync();
            _logger.LogInformation("Language updated to {Language}", language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating language");
        }
    }
}
/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://localhost:7001/api/";
    /// <summary>
    /// SignalR hub URL
    /// </summary>
    public string SignalRHubUrl { get; set; } = "https://localhost:7001/hubs/multiagent";
    /// <summary>
    /// Application theme
    /// </summary>
    public string Theme { get; set; } = "Light";
    /// <summary>
    /// Application language
    /// </summary>
    public string Language { get; set; } = "en-US";
    /// <summary>
    /// Auto-connect to SignalR on startupSignalR
    /// </summary>
    public bool AutoConnectSignalR { get; set; } = true;
    /// <summary>
    /// Enable notifications
    /// </summary>
    public bool EnableNotifications { get; set; } = true;
    /// <summary>
    /// Auto-refresh interval in seconds（
    /// </summary>
    public int AutoRefreshInterval { get; set; } = 30;
    /// <summary>
    /// Maximum log entries to keep
    /// </summary>
    public int MaxLogEntries { get; set; } = 1000;
    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;
    /// <summary>
    /// Window settings
    /// </summary>
    public WindowSettings Window { get; set; } = new();
    /// <summary>
    /// Dashboard settings
    /// </summary>
    public DashboardSettings Dashboard { get; set; } = new();
    /// <summary>
    /// Agent settings
    /// </summary>
    public AgentSettings Agents { get; set; } = new();
    /// <summary>
    /// Workflow settings
    /// </summary>
    public WorkflowSettings Workflows { get; set; } = new();
}
/// <summary>
/// Window settings
/// </summary>
public class WindowSettings
{
    /// <summary>
    /// Window width
    /// </summary>
    public double Width { get; set; } = 1200;
    /// <summary>
    /// Window height
    /// </summary>
    public double Height { get; set; } = 800;
    /// <summary>
    /// Window is maximized
    /// </summary>
    public bool IsMaximized { get; set; } = false;
    /// <summary>
    /// Remember window position
    /// </summary>
    public bool RememberPosition { get; set; } = true;
}
/// <summary>
/// Dashboard settings
/// </summary>
public class DashboardSettings
{
    /// <summary>
    /// Show system status
    /// </summary>
    public bool ShowSystemStatus { get; set; } = true;
    /// <summary>
    /// Show agent summary
    /// </summary>
    public bool ShowAgentSummary { get; set; } = true;
    /// <summary>
    /// Show workflow summary
    /// </summary>
    public bool ShowWorkflowSummary { get; set; } = true;
    /// <summary>
    /// Show recent activities
    /// </summary>
    public bool ShowRecentActivities { get; set; } = true;
    /// <summary>
    /// Refresh interval in seconds（
    /// </summary>
    public int RefreshInterval { get; set; } = 10;
}
/// <summary>
/// Agent settings
/// </summary>
public class AgentSettings
{
    /// <summary>
    /// Auto-refresh agent list
    /// </summary>
    public bool AutoRefresh { get; set; } = true;
    /// <summary>
    /// Show health status
    /// </summary>
    public bool ShowHealthStatus { get; set; } = true;
    /// <summary>
    /// Show execution history
    /// </summary>
    public bool ShowExecutionHistory { get; set; } = true;
    /// <summary>
    /// Default view mode
    /// </summary>
    public string DefaultViewMode { get; set; } = "List"; // List, Grid, Details
}
/// <summary>
/// Workflow settings
/// </summary>
public class WorkflowSettings
{
    /// <summary>
    /// Auto-refresh workflow list
    /// </summary>
    public bool AutoRefresh { get; set; } = true;
    /// <summary>
    /// Show execution status
    /// </summary>
    public bool ShowExecutionStatus { get; set; } = true;
    /// <summary>
    /// Show step details
    /// </summary>
    public bool ShowStepDetails { get; set; } = true;
    /// <summary>
    /// Default designer layout
    /// </summary>
    public string DefaultDesignerLayout { get; set; } = "Horizontal"; // Horizontal, Vertical
    /// <summary>
    /// Auto-save interval in seconds（
    /// </summary>
    public int AutoSaveInterval { get; set; } = 60;
}

