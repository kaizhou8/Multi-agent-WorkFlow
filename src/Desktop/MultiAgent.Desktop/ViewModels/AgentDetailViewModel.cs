using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using MultiAgent.Shared.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Agent detail page view model
/// Manages detailed agent information, configuration, and operations
//// </summary>
public partial class AgentDetailViewModel : ObservableObject
{
    #region Fields /
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<AgentDetailViewModel> _logger;
    private Timer? _refreshTimer;
    [ObservableProperty]
    private string _agentId = string.Empty;
    [ObservableProperty]
    private string _agentName = string.Empty;
    [ObservableProperty]
    private string _description = string.Empty;
    [ObservableProperty]
    private string _type = string.Empty;
    [ObservableProperty]
    private string _version = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private DateTime _lastHeartbeat = DateTime.Now;
    [ObservableProperty]
    private int _healthPercentage = 100;
    [ObservableProperty]
    private double _cpuUsage = 0.0;
    [ObservableProperty]
    private double _memoryUsage = 0.0;
    [ObservableProperty]
    private int _memoryUsageMB = 0;
    [ObservableProperty]
    private string _uptime = string.Empty;
    [ObservableProperty]
    private bool _autoScrollLogs = true;
    [ObservableProperty]
    private string _selectedCommand = string.Empty;
    [ObservableProperty]
    private string _commandParameters = string.Empty;
    [ObservableProperty]
    private string _commandResult = string.Empty;
    [ObservableProperty]
    private bool _isExecutingCommand;
    [ObservableProperty]
    private ObservableCollection<AgentCapabilityViewModel> _capabilities = new();
    [ObservableProperty]
    private ObservableCollection<ConfigurationItemViewModel> _configurationItems = new();
    [ObservableProperty]
    private ObservableCollection<ExecutionHistoryViewModel> _executionHistory = new();
    [ObservableProperty]
    private ObservableCollection<LogEntryViewModel> _logEntries = new();
    [ObservableProperty]
    private ObservableCollection<string> _availableCommands = new();
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="signalRService">SignalR service
    /// <param name="logger">Logger</param>
    public AgentDetailViewModel(
        IApiService apiService,
        ISignalRService signalRService,
        ILogger<AgentDetailViewModel> logger)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        _logger = logger;
        // Subscribe to SignalR eventsSignalR
        SubscribeToSignalREvents();
        // Initialize available commands
        InitializeAvailableCommands();
    }
    #endregion
    #region Properties /
    /// <summary>
    /// Status color based on agent status
    /// </summary>
    public Color StatusColor => Status switch
    {
        "Running" => Colors.Green,
        "Stopped" => Colors.Gray,
        "Error" => Colors.Red,
        "Starting" => Colors.Orange,
        "Stopping" => Colors.Orange,
        _ => Colors.Gray
    };
    /// <summary>
    /// Health color based on health percentage
    /// </summary>
    public Color HealthColor => HealthPercentage switch
    {
        >= 80 => Colors.Green,
        >= 60 => Colors.Orange,
        >= 40 => Colors.Red,
        _ => Colors.DarkRed
    };
    /// <summary>
    /// Health icon based on health percentage
    /// </summary>
    public string HealthIcon => HealthPercentage switch
    {
        >= 80 => "✓",
        >= 60 => "⚠",
        >= 40 => "⚠",
        _ => "✗"
    };
    /// <summary>
    /// CPU usage color
    /// </summary>
    public Color CpuUsageColor => CpuUsage switch
    {
        >= 0.8 => Colors.Red,
        >= 0.6 => Colors.Orange,
        _ => Colors.Green
    };
    /// <summary>
    /// Memory usage color
    /// </summary>
    public Color MemoryUsageColor => MemoryUsage switch
    {
        >= 0.8 => Colors.Red,
        >= 0.6 => Colors.Orange,
        _ => Colors.Green
    };
    /// <summary>
    /// Action button text based on status
    /// </summary>
    public string ActionButtonText => Status switch
    {
        "Running" => "Stop",
        "Stopped" => "Start",
        "Error" => "Restart",
        _ => "Start"
    };
    /// <summary>
    /// Action button style based on status
    /// </summary>
    public string ActionButtonStyle => Status switch
    {
        "Running" => "DangerButtonStyle",
        "Error" => "WarningButtonStyle",
        _ => "BaseButtonStyle"
    };
    /// <summary>
    /// Can execute command
    /// </summary>
    public bool CanExecuteCommand => !IsExecutingCommand && !string.IsNullOrWhiteSpace(SelectedCommand) && Status == "Running";
    /// <summary>
    /// Has command result
    /// </summary>
    public bool HasCommandResult => !string.IsNullOrWhiteSpace(CommandResult);
    #endregion
    #region Commands /
    /// <summary>
    /// Load agent async
    /// </summary>
    /// <param name="agentId">Agent ID
    public async Task LoadAgentAsync(string agentId)
    {
        try
        {
            AgentId = agentId;
            _logger.LogInformation("Loading agent details for {AgentId} /
            // Load agent information
            var agentInfo = await _apiService.GetAgentAsync(agentId);
            if (agentInfo != null)
            {
                UpdateFromAgentInfo(agentInfo);
            }
            // Load agent capabilities
            await LoadCapabilitiesAsync();
            // Load configuration
            await LoadConfigurationAsync();
            // Load execution history
            await LoadExecutionHistoryAsync();
            // Start real-time updates
            StartRealTimeUpdates();
            _logger.LogInformation("Agent details loaded successfully /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent details /
        }
    }
    /// <summary>
    /// Toggle agent command
    /// </summary>
    [RelayCommand]
    private async Task ToggleAgentAsync()
    {
        try
        {
            _logger.LogInformation("Toggling agent {AgentId} from {Status} /
                AgentId, Status);
            if (Status == "Running")
            {
                await _apiService.StopAgentAsync(AgentId);
            }
            else
            {
                await _apiService.StartAgentAsync(AgentId);
            }
            // Refresh agent status
            await RefreshAgentStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle agent {AgentId} /
        }
    }
    /// <summary>
    /// Configure agent command
    /// </summary>
    [RelayCommand]
    private async Task ConfigureAgentAsync()
    {
        try
        {
            _logger.LogInformation("Opening agent configuration for {AgentId} /
            await Shell.Current.GoToAsync($"//agents/configure?agentId={AgentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open agent configuration /
        }
    }
    /// <summary>
    /// Restart agent command
    /// </summary>
    [RelayCommand]
    private async Task RestartAgentAsync()
    {
        try
        {
            var result = await Shell.Current.DisplayAlert(
                "Restart Agent",
                $"Are you sure you want to restart {AgentName}? /
                "Yes",
                "No");
            if (result)
            {
                _logger.LogInformation("Restarting agent {AgentId} /
                await _apiService.StopAgentAsync(AgentId);
                await Task.Delay(2000); // Wait 2 seconds2
                await _apiService.StartAgentAsync(AgentId);
                await RefreshAgentStatusAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart agent {AgentId} /
        }
    }
    /// <summary>
    /// Execute command
    /// </summary>
    [RelayCommand]
    private async Task ExecuteCommandAsync()
    {
        try
        {
            IsExecutingCommand = true;
            CommandResult = string.Empty;
            _logger.LogInformation("Executing command {Command} on agent {AgentId} /
                SelectedCommand, AgentId);
            // Parse parameters
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(CommandParameters))
            {
                try
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(CommandParameters) ?? new();
                }
                catch (JsonException)
                {
                    CommandResult = "Invalid JSON parameters /
                    return;
                }
            }
            // Execute command
            var command = new AgentCommand
            {
                Id = Guid.NewGuid().ToString(),
                Type = SelectedCommand,
                Parameters = parameters,
                ExecutedBy = "User",
                ExecutedAt = DateTime.Now
            };
            var result = await _apiService.ExecuteAgentCommandAsync(AgentId, command);
            CommandResult = result?.Data?.ToString() ?? "Command executed successfully /
            // Add to execution history
            var historyItem = new ExecutionHistoryViewModel
            {
                Command = SelectedCommand,
                Result = result?.Success == true ? "Success" : "Failed",
                ExecutedAt = DateTime.Now,
                Duration = (int)(result?.ExecutionTime.TotalMilliseconds ?? 0),
                StatusIcon = result?.Success == true ? "✓" : "✗",
                StatusColor = result?.Success == true ? Colors.Green : Colors.Red
            };
            ExecutionHistory.Insert(0, historyItem);
            _logger.LogInformation("Command executed successfully /
        }
        catch (Exception ex)
        {
            CommandResult = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to execute command /
        }
        finally
        {
            IsExecutingCommand = false;
            OnPropertyChanged(nameof(CanExecuteCommand));
            OnPropertyChanged(nameof(HasCommandResult));
        }
    }
    /// <summary>
    /// Edit configuration command
    /// </summary>
    [RelayCommand]
    private async Task EditConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Opening configuration editor for agent {AgentId} /
            await Shell.Current.GoToAsync($"//agents/config-editor?agentId={AgentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open configuration editor /
        }
    }
    /// <summary>
    /// Refresh history command
    /// </summary>
    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        try
        {
            await LoadExecutionHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh execution history /
        }
    }
    /// <summary>
    /// Clear history command
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        try
        {
            var result = await Shell.Current.DisplayAlert(
                "Clear History",
                "Are you sure you want to clear execution history? /
                "Yes",
                "No");
            if (result)
            {
                ExecutionHistory.Clear();
                _logger.LogInformation("Execution history cleared /
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear execution history /
        }
    }
    /// <summary>
    /// Clear logs command
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
        _logger.LogInformation("Agent logs cleared /
    }
    /// <summary>
    /// View execution details command
    /// </summary>
    [RelayCommand]
    private async Task ViewExecutionDetailsAsync(ExecutionHistoryViewModel execution)
    {
        try
        {
            var details = $"Command: {execution.Command}\n" +
                         $"Result: {execution.Result}\n" +
                         $"Executed At: {execution.ExecutedAt:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Duration: {execution.Duration}ms";
            await Shell.Current.DisplayAlert("Execution Details", details, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show execution details /
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Update from agent info
    /// </summary>
    private void UpdateFromAgentInfo(AgentInfo agentInfo)
    {
        AgentName = agentInfo.Name;
        Description = agentInfo.Description;
        Type = agentInfo.Type;
        Version = agentInfo.Version;
        Status = agentInfo.Status;
        LastHeartbeat = agentInfo.LastHeartbeat;
        // Update health and performance metrics
        if (agentInfo.Metadata != null)
        {
            if (agentInfo.Metadata.TryGetValue("HealthPercentage", out var health) && int.TryParse(health.ToString(), out int healthValue))
                HealthPercentage = healthValue;
            if (agentInfo.Metadata.TryGetValue("CpuUsage", out var cpu) && double.TryParse(cpu.ToString(), out double cpuValue))
                CpuUsage = cpuValue;
            if (agentInfo.Metadata.TryGetValue("MemoryUsage", out var memory) && double.TryParse(memory.ToString(), out double memoryValue))
                MemoryUsage = memoryValue;
            if (agentInfo.Metadata.TryGetValue("MemoryUsageMB", out var memoryMB) && int.TryParse(memoryMB.ToString(), out int memoryMBValue))
                MemoryUsageMB = memoryMBValue;
            if (agentInfo.Metadata.TryGetValue("Uptime", out var uptime))
                Uptime = uptime.ToString() ?? "";
        }
        // Update computed properties
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(HealthColor));
        OnPropertyChanged(nameof(HealthIcon));
        OnPropertyChanged(nameof(CpuUsageColor));
        OnPropertyChanged(nameof(MemoryUsageColor));
        OnPropertyChanged(nameof(ActionButtonText));
        OnPropertyChanged(nameof(ActionButtonStyle));
        OnPropertyChanged(nameof(CanExecuteCommand));
    }
    /// <summary>
    /// Load capabilities
    /// </summary>
    private async Task LoadCapabilitiesAsync()
    {
        try
        {
            // TODO: Load actual capabilities from APIAPI
            Capabilities.Clear();
            // Mock capabilities for demonstration
            var mockCapabilities = new[]
            {
                new AgentCapabilityViewModel { Name = "File Operations", Description = "Read/write files" },
                new AgentCapabilityViewModel { Name = "Data Processing", Description = "Process data" },
                new AgentCapabilityViewModel { Name = "API Calls", Description = "Make HTTP requests" },
                new AgentCapabilityViewModel { Name = "Text Analysis", Description = "Analyze text content" }
            };
            foreach (var capability in mockCapabilities)
            {
                Capabilities.Add(capability);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent capabilities /
        }
    }
    /// <summary>
    /// Load configuration
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
        try
        {
            // TODO: Load actual configuration from APIAPI
            ConfigurationItems.Clear();
            // Mock configuration for demonstration
            var mockConfig = new[]
            {
                new ConfigurationItemViewModel { Key = "Max Concurrent Tasks", Value = "5" },
                new ConfigurationItemViewModel { Key = "Timeout (seconds)", Value = "30" },
                new ConfigurationItemViewModel { Key = "Log Level", Value = "Information" },
                new ConfigurationItemViewModel { Key = "Auto Restart", Value = "True" },
                new ConfigurationItemViewModel { Key = "Working Directory", Value = "/app/data" }
            };
            foreach (var item in mockConfig)
            {
                ConfigurationItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent configuration /
        }
    }
    /// <summary>
    /// Load execution history
    /// </summary>
    private async Task LoadExecutionHistoryAsync()
    {
        try
        {
            // TODO: Load actual execution history from APIAPI
            ExecutionHistory.Clear();
            // Mock execution history for demonstration
            var mockHistory = new[]
            {
                new ExecutionHistoryViewModel
                {
                    Command = "ProcessData",
                    Result = "Processed 1000 records successfully",
                    ExecutedAt = DateTime.Now.AddMinutes(-5),
                    Duration = 2500,
                    StatusIcon = "✓",
                    StatusColor = Colors.Green
                },
                new ExecutionHistoryViewModel
                {
                    Command = "ReadFile",
                    Result = "File read successfully",
                    ExecutedAt = DateTime.Now.AddMinutes(-10),
                    Duration = 150,
                    StatusIcon = "✓",
                    StatusColor = Colors.Green
                },
                new ExecutionHistoryViewModel
                {
                    Command = "AnalyzeText",
                    Result = "Analysis failed: Invalid input",
                    ExecutedAt = DateTime.Now.AddMinutes(-15),
                    Duration = 500,
                    StatusIcon = "✗",
                    StatusColor = Colors.Red
                }
            };
            foreach (var item in mockHistory)
            {
                ExecutionHistory.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load execution history /
        }
    }
    /// <summary>
    /// Initialize available commands
    /// </summary>
    private void InitializeAvailableCommands()
    {
        AvailableCommands.Clear();
        AvailableCommands.Add("GetStatus");
        AvailableCommands.Add("ProcessData");
        AvailableCommands.Add("ReadFile");
        AvailableCommands.Add("WriteFile");
        AvailableCommands.Add("AnalyzeText");
        AvailableCommands.Add("ExecuteTask");
        AvailableCommands.Add("GetMetrics");
        AvailableCommands.Add("ClearCache");
    }
    /// <summary>
    /// Subscribe to SignalR eventsSignalR
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        _signalRService.AgentStatusChanged += OnAgentStatusChanged;
        _signalRService.AgentHealthChanged += OnAgentHealthChanged;
        _signalRService.AgentLogReceived += OnAgentLogReceived;
    }
    /// <summary>
    /// Agent status changed event handler
    /// </summary>
    private async void OnAgentStatusChanged(string agentId, string status)
    {
        if (agentId == AgentId)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Status = status;
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ActionButtonStyle));
                OnPropertyChanged(nameof(CanExecuteCommand));
            });
        }
    }
    /// <summary>
    /// Agent health changed event handler
    /// </summary>
    private async void OnAgentHealthChanged(string agentId, int healthPercentage)
    {
        if (agentId == AgentId)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HealthPercentage = healthPercentage;
                OnPropertyChanged(nameof(HealthColor));
                OnPropertyChanged(nameof(HealthIcon));
            });
        }
    }
    /// <summary>
    /// Agent log received event handler
    /// </summary>
    private async void OnAgentLogReceived(string agentId, string level, string message)
    {
        if (agentId == AgentId)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var logEntry = new LogEntryViewModel
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message,
                    LevelColor = level switch
                    {
                        "Error" => Colors.Red,
                        "Warning" => Colors.Orange,
                        "Information" => Colors.Blue,
                        "Debug" => Colors.Gray,
                        _ => Colors.Black
                    }
                };
                LogEntries.Insert(0, logEntry);
                // Keep only last 100 log entries100
                while (LogEntries.Count > 100)
                {
                    LogEntries.RemoveAt(LogEntries.Count - 1);
                }
            });
        }
    }
    /// <summary>
    /// Start real-time updates
    /// </summary>
    private void StartRealTimeUpdates()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(async _ => await RefreshAgentStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
    /// <summary>
    /// Refresh agent status
    /// </summary>
    private async Task RefreshAgentStatusAsync()
    {
        try
        {
            var agentInfo = await _apiService.GetAgentAsync(AgentId);
            if (agentInfo != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() => UpdateFromAgentInfo(agentInfo));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh agent status /
        }
    }
    #endregion
    #region IDisposable
    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _signalRService.AgentStatusChanged -= OnAgentStatusChanged;
        _signalRService.AgentHealthChanged -= OnAgentHealthChanged;
        _signalRService.AgentLogReceived -= OnAgentLogReceived;
    }
    #endregion
}
/// <summary>
/// Agent capability view model
/// </summary>
public partial class AgentCapabilityViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _description = string.Empty;
}
/// <summary>
/// Configuration item view model
/// </summary>
public partial class ConfigurationItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;
    [ObservableProperty]
    private string _value = string.Empty;
}
/// <summary>
/// Execution history view model
/// </summary>
public partial class ExecutionHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _command = string.Empty;
    [ObservableProperty]
    private string _result = string.Empty;
    [ObservableProperty]
    private DateTime _executedAt = DateTime.Now;
    [ObservableProperty]
    private int _duration;
    [ObservableProperty]
    private string _statusIcon = string.Empty;
    [ObservableProperty]
    private Color _statusColor = Colors.Gray;
}
/// <summary>
/// Log entry view model
/// </summary>
public partial class LogEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;
    [ObservableProperty]
    private string _level = string.Empty;
    [ObservableProperty]
    private string _message = string.Empty;
    [ObservableProperty]
    private Color _levelColor = Colors.Black;
}

