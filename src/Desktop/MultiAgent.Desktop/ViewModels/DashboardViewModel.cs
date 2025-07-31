using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Dashboard view model
/// Manages dashboard data and operations
//// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly Timer _refreshTimer;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="signalRService">SignalR service
    /// <param name="authenticationService">Authentication service</param>
    /// <param name="logger">Logger</param>
    public DashboardViewModel(
        IApiService apiService,
        ISignalRService signalRService,
        IAuthenticationService authenticationService,
        ILogger<DashboardViewModel> logger)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Initialize collections
        RecentActivities = new ObservableCollection<ActivityItem>();
        // Setup auto-refresh timer
        _refreshTimer = new Timer(async _ => await RefreshAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        // Subscribe to SignalR eventsSignalR
        SubscribeToSignalREvents();
        // Initialize data
        Initialize();
    }
    #region Properties /
    /// <summary>
    /// Welcome message
    /// </summary>
    [ObservableProperty]
    private string welcomeMessage = "Welcome back!";
    /// <summary>
    /// Current date time
    /// </summary>
    [ObservableProperty]
    private string currentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
    /// <summary>
    /// Agent count
    /// </summary>
    [ObservableProperty]
    private int agentCount;
    /// <summary>
    /// Active agent count
    /// </summary>
    [ObservableProperty]
    private int activeAgentCount;
    /// <summary>
    /// Workflow count
    /// </summary>
    [ObservableProperty]
    private int workflowCount;
    /// <summary>
    /// Running workflow count
    /// </summary>
    [ObservableProperty]
    private int runningWorkflowCount;
    /// <summary>
    /// Total executions
    /// </summary>
    [ObservableProperty]
    private int totalExecutions;
    /// <summary>
    /// Today executions
    /// </summary>
    [ObservableProperty]
    private int todayExecutions;
    /// <summary>
    /// System health status
    /// </summary>
    [ObservableProperty]
    private string systemHealthStatus = "Good";
    /// <summary>
    /// System health icon
    /// </summary>
    [ObservableProperty]
    private string systemHealthIcon = "💚";
    /// <summary>
    /// System health color
    /// </summary>
    [ObservableProperty]
    private Color systemHealthColor = Colors.Green;
    /// <summary>
    /// System uptime
    /// </summary>
    [ObservableProperty]
    private string systemUptime = "24h 15m";
    /// <summary>
    /// Is refreshing
    /// </summary>
    [ObservableProperty]
    private bool isRefreshing;
    /// <summary>
    /// Recent activities
    /// </summary>
    public ObservableCollection<ActivityItem> RecentActivities { get; }
    #endregion
    #region Commands /
    /// <summary>
    /// Refresh command
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            _logger.LogDebug("Refreshing dashboard data");
            // Update current time
            CurrentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy HH:mm");
            // Load dashboard data
            await LoadDashboardDataAsync();
            _logger.LogDebug("Dashboard data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing dashboard data");
            await Shell.Current.DisplayAlert("Error", "Failed to refresh dashboard data. Please try again.", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    /// <summary>
    /// Navigate to agents command
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAgentsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//agents");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to agents");
        }
    }
    /// <summary>
    /// Navigate to workflows command
    /// </summary>
    [RelayCommand]
    private async Task NavigateToWorkflowsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//workflows");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to workflows");
        }
    }
    /// <summary>
    /// Navigate to AI commandAI
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAIAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ai");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to AI services");
        }
    }
    /// <summary>
    /// Navigate to settings command
    /// </summary>
    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to settings");
        }
    }
    /// <summary>
    /// View all activities command
    /// </summary>
    [RelayCommand]
    private async Task ViewAllActivitiesAsync()
    {
        try
        {
            // TODO: Navigate to activities page
            await Shell.Current.DisplayAlert("Info", "Activities page coming soon!", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing all activities");
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Initialize view model
    /// </summary>
    private void Initialize()
    {
        try
        {
            // Set welcome message based on current user
            var user = _authenticationService.CurrentUser;
            if (user != null)
            {
                WelcomeMessage = $"Welcome back, {user.Username}!";
            }
            // Add some sample activities
            AddSampleActivities();
            _logger.LogDebug("DashboardViewModel initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing DashboardViewModel");
        }
    }
    /// <summary>
    /// Load dashboard data
    /// </summary>
    /// <returns>Task</returns>
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            // Load agents data
            var agents = await _apiService.GetAgentsAsync();
            AgentCount = agents.Count();
            ActiveAgentCount = agents.Count(a => a.Status == MultiAgent.Shared.Models.Agent.AgentStatus.Running);
            // Load workflows data
            var workflows = await _apiService.GetWorkflowsAsync();
            WorkflowCount = workflows.Count();
            // TODO: Get running workflow count from executions
            RunningWorkflowCount = 0;
            // Load health data
            var healthStatuses = await _apiService.GetAllAgentsHealthAsync();
            UpdateSystemHealth(healthStatuses);
            // Update execution counts
            // TODO: Implement execution statistics
            TotalExecutions = 156; // Sample data
            TodayExecutions = 23; // Sample data
            _logger.LogDebug("Dashboard data loaded: {AgentCount} agents, {WorkflowCount} workflows",
                AgentCount, WorkflowCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            // Set default values on error
            AgentCount = 0;
            ActiveAgentCount = 0;
            WorkflowCount = 0;
            RunningWorkflowCount = 0;
            TotalExecutions = 0;
            TodayExecutions = 0;
            UpdateSystemHealth(Enumerable.Empty<MultiAgent.Shared.Models.Agent.AgentHealthStatus>());
        }
    }
    /// <summary>
    /// Update system health status
    /// </summary>
    /// <param name="healthStatuses">Health statuses</param>
    private void UpdateSystemHealth(IEnumerable<MultiAgent.Shared.Models.Agent.AgentHealthStatus> healthStatuses)
    {
        try
        {
            if (!healthStatuses.Any())
            {
                SystemHealthStatus = "Unknown";
                SystemHealthIcon = "❓";
                SystemHealthColor = Colors.Gray;
                SystemUptime = "N/A";
                return;
            }
            var healthyCount = healthStatuses.Count(h => h.IsHealthy);
            var totalCount = healthStatuses.Count();
            var healthPercentage = (double)healthyCount / totalCount;
            if (healthPercentage >= 0.9)
            {
                SystemHealthStatus = "Excellent";
                SystemHealthIcon = "💚";
                SystemHealthColor = Colors.Green;
            }
            else if (healthPercentage >= 0.7)
            {
                SystemHealthStatus = "Good";
                SystemHealthIcon = "💛";
                SystemHealthColor = Colors.Orange;
            }
            else
            {
                SystemHealthStatus = "Poor";
                SystemHealthIcon = "❤️";
                SystemHealthColor = Colors.Red;
            }
            // Calculate average uptime
            var avgUptime = healthStatuses.Where(h => h.Uptime.HasValue)
                                         .Select(h => h.Uptime!.Value)
                                         .DefaultIfEmpty(TimeSpan.Zero)
                                         .Average(ts => ts.TotalSeconds);
            var uptimeSpan = TimeSpan.FromSeconds(avgUptime);
            SystemUptime = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system health");
            SystemHealthStatus = "Error";
            SystemHealthIcon = "⚠️";
            SystemHealthColor = Colors.Red;
        }
    }
    /// <summary>
    /// Subscribe to SignalR eventsSignalR
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        try
        {
            _signalRService.AgentStatusUpdated += OnAgentStatusUpdated;
            _signalRService.AgentHealthUpdated += OnAgentHealthUpdated;
            _signalRService.WorkflowStatusUpdated += OnWorkflowStatusUpdated;
            _signalRService.WorkflowExecutionUpdated += OnWorkflowExecutionUpdated;
            _signalRService.SystemStatusUpdated += OnSystemStatusUpdated;
            _logger.LogDebug("Subscribed to SignalR events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to SignalR events");
        }
    }
    /// <summary>
    /// Handle agent status updated event
    /// </summary>
    private void OnAgentStatusUpdated(object? sender, MultiAgent.Shared.Models.Agent.AgentInfo agentInfo)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddActivity("🤖", "Agent Updated", $"Agent {agentInfo.Name} status changed to {agentInfo.Status}", Colors.Blue);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling agent status update");
        }
    }
    /// <summary>
    /// Handle agent health updated event
    /// </summary>
    private void OnAgentHealthUpdated(object? sender, MultiAgent.Shared.Models.Agent.AgentHealthStatus healthStatus)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var status = healthStatus.IsHealthy ? "healthy" : "unhealthy";
                AddActivity("💚", "Health Check", $"Agent {healthStatus.AgentId} is {status}",
                    healthStatus.IsHealthy ? Colors.Green : Colors.Red);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling agent health update");
        }
    }
    /// <summary>
    /// Handle workflow status updated event
    /// </summary>
    private void OnWorkflowStatusUpdated(object? sender, MultiAgent.Shared.Models.Workflow.WorkflowDefinition workflow)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddActivity("⚡", "Workflow Updated", $"Workflow {workflow.Name} was modified", Colors.Purple);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling workflow status update");
        }
    }
    /// <summary>
    /// Handle workflow execution updated event
    /// </summary>
    private void OnWorkflowExecutionUpdated(object? sender, MultiAgent.Shared.Models.Workflow.WorkflowExecution execution)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var color = execution.Status switch
                {
                    MultiAgent.Shared.Models.Workflow.WorkflowStatus.Running => Colors.Blue,
                    MultiAgent.Shared.Models.Workflow.WorkflowStatus.Completed => Colors.Green,
                    MultiAgent.Shared.Models.Workflow.WorkflowStatus.Failed => Colors.Red,
                    _ => Colors.Gray
                };
                AddActivity("📊", "Workflow Execution", $"Execution {execution.Id} status: {execution.Status}", color);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling workflow execution update");
        }
    }
    /// <summary>
    /// Handle system status updated event
    /// </summary>
    private void OnSystemStatusUpdated(object? sender, object systemStatus)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddActivity("🔄", "System Update", "System status updated", Colors.Orange);
                // Refresh data when system status changes
                _ = Task.Run(async () => await RefreshAsync());
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling system status update");
        }
    }
    /// <summary>
    /// Add activity to recent activities
    /// </summary>
    /// <param name="icon">Activity icon</param>
    /// <param name="title">Activity title</param>
    /// <param name="description">Activity description</param>
    /// <param name="iconBackgroundColor">Icon background color</param>
    private void AddActivity(string icon, string title, string description, Color iconBackgroundColor)
    {
        try
        {
            var activity = new ActivityItem
            {
                Icon = icon,
                Title = title,
                Description = description,
                IconBackgroundColor = iconBackgroundColor,
                Timestamp = DateTime.Now,
                TimeAgo = "Just now"
            };
            RecentActivities.Insert(0, activity);
            // Keep only the latest 10 activities10
            while (RecentActivities.Count > 10)
            {
                RecentActivities.RemoveAt(RecentActivities.Count - 1);
            }
            // Update time ago for all activities
            UpdateActivityTimeAgo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding activity");
        }
    }
    /// <summary>
    /// Add sample activities
    /// </summary>
    private void AddSampleActivities()
    {
        try
        {
            AddActivity("🚀", "System Started", "Multi-Agent system initialized successfully", Colors.Green);
            AddActivity("🤖", "Agent Created", "FileSystemAgent was created and started", Colors.Blue);
            AddActivity("⚡", "Workflow Created", "New data processing workflow was created", Colors.Purple);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sample activities");
        }
    }
    /// <summary>
    /// Update time ago for activities
    /// </summary>
    private void UpdateActivityTimeAgo()
    {
        try
        {
            foreach (var activity in RecentActivities)
            {
                var timeSpan = DateTime.Now - activity.Timestamp;
                activity.TimeAgo = timeSpan.TotalMinutes < 1 ? "Just now" :
                                  timeSpan.TotalHours < 1 ? $"{(int)timeSpan.TotalMinutes}m ago" :
                                  timeSpan.TotalDays < 1 ? $"{(int)timeSpan.TotalHours}h ago" :
                                  $"{(int)timeSpan.TotalDays}d ago";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating activity time ago");
        }
    }
    #endregion
    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        try
        {
            _refreshTimer?.Dispose();
            // Unsubscribe from SignalR eventsSignalR
            _signalRService.AgentStatusUpdated -= OnAgentStatusUpdated;
            _signalRService.AgentHealthUpdated -= OnAgentHealthUpdated;
            _signalRService.WorkflowStatusUpdated -= OnWorkflowStatusUpdated;
            _signalRService.WorkflowExecutionUpdated -= OnWorkflowExecutionUpdated;
            _signalRService.SystemStatusUpdated -= OnSystemStatusUpdated;
            _logger.LogDebug("DashboardViewModel disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing DashboardViewModel");
        }
    }
}
/// <summary>
/// Activity item model
/// </summary>
public partial class ActivityItem : ObservableObject
{
    /// <summary>
    /// Activity icon
    /// </summary>
    [ObservableProperty]
    private string icon = string.Empty;
    /// <summary>
    /// Activity title
    /// </summary>
    [ObservableProperty]
    private string title = string.Empty;
    /// <summary>
    /// Activity description
    /// </summary>
    [ObservableProperty]
    private string description = string.Empty;
    /// <summary>
    /// Icon background color
    /// </summary>
    [ObservableProperty]
    private Color iconBackgroundColor = Colors.Gray;
    /// <summary>
    /// Activity timestamp
    /// </summary>
    [ObservableProperty]
    private DateTime timestamp = DateTime.Now;
    /// <summary>
    /// Time ago text
    /// </summary>
    [ObservableProperty]
    private string timeAgo = "Just now";
}

