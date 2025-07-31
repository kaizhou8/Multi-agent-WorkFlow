using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using MultiAgent.Shared.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Agents page view model
/// Manages agent list, filtering, and operations
//// </summary>
public partial class AgentsViewModel : ObservableObject
{
    #region Fields /
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<AgentsViewModel> _logger;
    [ObservableProperty]
    private ObservableCollection<AgentViewModel> _agents = new();
    [ObservableProperty]
    private ObservableCollection<AgentViewModel> _filteredAgents = new();
    [ObservableProperty]
    private string _searchText = string.Empty;
    [ObservableProperty]
    private bool _isRefreshing;
    [ObservableProperty]
    private bool _isListView = true;
    [ObservableProperty]
    private int _agentCount;
    [ObservableProperty]
    private string _viewModeIcon = "📋";
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="signalRService">SignalR service
    /// <param name="logger">Logger</param>
    public AgentsViewModel(
        IApiService apiService,
        ISignalRService signalRService,
        ILogger<AgentsViewModel> logger)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        _logger = logger;
        // Subscribe to property changes
        PropertyChanged += OnPropertyChanged;
        // Subscribe to SignalR eventsSignalR
        SubscribeToSignalREvents();
    }
    #endregion
    #region Properties /
    /// <summary>
    /// Is not refreshing
    /// </summary>
    public bool IsNotRefreshing => !IsRefreshing;
    #endregion
    #region Commands /
    /// <summary>
    /// Refresh command
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            _logger.LogInformation("Refreshing agents list /
            var agents = await _apiService.GetAgentsAsync();
            Agents.Clear();
            foreach (var agent in agents)
            {
                Agents.Add(new AgentViewModel(agent));
            }
            AgentCount = Agents.Count;
            FilterAgents();
            _logger.LogInformation("Successfully refreshed {Count} agents /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh agents /
            // TODO: Show error message to user
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    /// <summary>
    /// Toggle view mode command
    /// </summary>
    [RelayCommand]
    private void ToggleViewMode()
    {
        IsListView = !IsListView;
        ViewModeIcon = IsListView ? "📋" : "🔲";
        _logger.LogInformation("Switched to {ViewMode} view /
    }
    /// <summary>
    /// View agent command
    /// </summary>
    [RelayCommand]
    private async Task ViewAgentAsync(AgentViewModel agent)
    {
        try
        {
            _logger.LogInformation("Viewing agent: {AgentName} /
            // Navigate to agent detail page
            await Shell.Current.GoToAsync($"//agents/detail?agentId={agent.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to view agent {AgentId} /
        }
    }
    /// <summary>
    /// Toggle agent command
    /// </summary>
    [RelayCommand]
    private async Task ToggleAgentAsync(AgentViewModel agent)
    {
        try
        {
            _logger.LogInformation("Toggling agent {AgentName} from {Status} /
                agent.Name, agent.Status);
            if (agent.Status == "Running")
            {
                await _apiService.StopAgentAsync(agent.Id);
            }
            else
            {
                await _apiService.StartAgentAsync(agent.Id);
            }
            // Refresh agent status
            await RefreshAgentStatusAsync(agent.Id);
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
    private async Task ConfigureAgentAsync(AgentViewModel agent)
    {
        try
        {
            _logger.LogInformation("Configuring agent: {AgentName} /
            // Navigate to agent configuration page
            await Shell.Current.GoToAsync($"//agents/configure?agentId={agent.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure agent {AgentId} /
        }
    }
    /// <summary>
    /// Create agent command
    /// </summary>
    [RelayCommand]
    private async Task CreateAgentAsync()
    {
        try
        {
            _logger.LogInformation("Creating new agent /
            // Navigate to agent creation page
            await Shell.Current.GoToAsync("//agents/create");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to create agent page /
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Property changed event handler
    /// </summary>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText))
        {
            FilterAgents();
        }
        else if (e.PropertyName == nameof(IsRefreshing))
        {
            OnPropertyChanged(nameof(IsNotRefreshing));
        }
    }
    /// <summary>
    /// Filter agents based on search text
    /// </summary>
    private void FilterAgents()
    {
        FilteredAgents.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Agents
            : Agents.Where(a =>
                a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Type.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var agent in filtered)
        {
            FilteredAgents.Add(agent);
        }
        _logger.LogDebug("Filtered {FilteredCount} agents from {TotalCount} /
            FilteredAgents.Count, Agents.Count);
    }
    /// <summary>
    /// Subscribe to SignalR eventsSignalR
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        _signalRService.AgentStatusChanged += OnAgentStatusChanged;
        _signalRService.AgentHealthChanged += OnAgentHealthChanged;
        _signalRService.AgentCreated += OnAgentCreated;
        _signalRService.AgentDeleted += OnAgentDeleted;
    }
    /// <summary>
    /// Agent status changed event handler
    /// </summary>
    private async void OnAgentStatusChanged(string agentId, string status)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var agent = Agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null)
            {
                agent.Status = status;
                _logger.LogInformation("Agent {AgentId} status changed to {Status} /
                    agentId, status);
            }
        });
    }
    /// <summary>
    /// Agent health changed event handler
    /// </summary>
    private async void OnAgentHealthChanged(string agentId, int healthPercentage)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var agent = Agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null)
            {
                agent.HealthPercentage = healthPercentage;
                _logger.LogDebug("Agent {AgentId} health changed to {Health}% /
                    agentId, healthPercentage);
            }
        });
    }
    /// <summary>
    /// Agent created event handler
    /// </summary>
    private async void OnAgentCreated(AgentInfo agentInfo)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var existingAgent = Agents.FirstOrDefault(a => a.Id == agentInfo.Id);
            if (existingAgent == null)
            {
                Agents.Add(new AgentViewModel(agentInfo));
                AgentCount = Agents.Count;
                FilterAgents();
                _logger.LogInformation("New agent created: {AgentName} /
            }
        });
    }
    /// <summary>
    /// Agent deleted event handler
    /// </summary>
    private async void OnAgentDeleted(string agentId)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var agent = Agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null)
            {
                Agents.Remove(agent);
                AgentCount = Agents.Count;
                FilterAgents();
                _logger.LogInformation("Agent deleted: {AgentId} /
            }
        });
    }
    /// <summary>
    /// Refresh specific agent status
    /// </summary>
    private async Task RefreshAgentStatusAsync(string agentId)
    {
        try
        {
            var agentInfo = await _apiService.GetAgentAsync(agentId);
            var agent = Agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null && agentInfo != null)
            {
                agent.UpdateFromAgentInfo(agentInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh agent {AgentId} status /
        }
    }
    #endregion
}
/// <summary>
/// Agent view model
/// Represents an agent in the UI
//// </summary>
public partial class AgentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    [ObservableProperty]
    private string _name = string.Empty;
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
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agentInfo">Agent information</param>
    public AgentViewModel(AgentInfo agentInfo)
    {
        UpdateFromAgentInfo(agentInfo);
    }
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
    /// Update from agent info
    /// </summary>
    /// <param name="agentInfo">Agent information</param>
    public void UpdateFromAgentInfo(AgentInfo agentInfo)
    {
        Id = agentInfo.Id;
        Name = agentInfo.Name;
        Description = agentInfo.Description;
        Type = agentInfo.Type;
        Version = agentInfo.Version;
        Status = agentInfo.Status;
        LastHeartbeat = agentInfo.LastHeartbeat;
        // Update health percentage if available
        if (agentInfo.Metadata?.ContainsKey("HealthPercentage") == true)
        {
            if (int.TryParse(agentInfo.Metadata["HealthPercentage"].ToString(), out int health))
            {
                HealthPercentage = health;
            }
        }
    }
}

