using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using MultiAgent.Shared.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Workflows page view model
/// Manages workflow list, filtering, and operations
//// </summary>
public partial class WorkflowsViewModel : ObservableObject
{
    #region Fields /
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<WorkflowsViewModel> _logger;
    [ObservableProperty]
    private ObservableCollection<WorkflowViewModel> _workflows = new();
    [ObservableProperty]
    private ObservableCollection<WorkflowViewModel> _filteredWorkflows = new();
    [ObservableProperty]
    private string _searchText = string.Empty;
    [ObservableProperty]
    private bool _isRefreshing;
    [ObservableProperty]
    private int _workflowCount;
    [ObservableProperty]
    private string _selectedStatusFilter = "All";
    [ObservableProperty]
    private ObservableCollection<string> _statusFilters = new() { "All", "Active", "Completed", "Failed", "Cancelled" };
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="signalRService">SignalR service
    /// <param name="logger">Logger</param>
    public WorkflowsViewModel(
        IApiService apiService,
        ISignalRService signalRService,
        ILogger<WorkflowsViewModel> logger)
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
            _logger.LogInformation("Refreshing workflows list /
            var workflows = await _apiService.GetWorkflowsAsync();
            Workflows.Clear();
            foreach (var workflow in workflows)
            {
                var viewModel = new WorkflowViewModel(workflow);
                // Load recent executions
                try
                {
                    var executions = await _apiService.GetWorkflowExecutionsAsync(workflow.Id, 5);
                    viewModel.LoadRecentExecutions(executions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load executions for workflow {WorkflowId} /
                }
                Workflows.Add(viewModel);
            }
            WorkflowCount = Workflows.Count;
            FilterWorkflows();
            _logger.LogInformation("Successfully refreshed {Count} workflows /
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh workflows /
            // TODO: Show error message to user
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    /// <summary>
    /// View workflow command
    /// </summary>
    [RelayCommand]
    private async Task ViewWorkflowAsync(WorkflowViewModel workflow)
    {
        try
        {
            _logger.LogInformation("Viewing workflow: {WorkflowName} /
            // Navigate to workflow detail page
            await Shell.Current.GoToAsync($"//workflows/detail?workflowId={workflow.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to view workflow {WorkflowId} /
        }
    }
    /// <summary>
    /// Execute workflow command
    /// </summary>
    [RelayCommand]
    private async Task ExecuteWorkflowAsync(WorkflowViewModel workflow)
    {
        try
        {
            _logger.LogInformation("Executing workflow: {WorkflowName} /
            // Show execution dialog or navigate to execution page
            await Shell.Current.GoToAsync($"//workflows/execute?workflowId={workflow.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow {WorkflowId} /
        }
    }
    /// <summary>
    /// Edit workflow command
    /// </summary>
    [RelayCommand]
    private async Task EditWorkflowAsync(WorkflowViewModel workflow)
    {
        try
        {
            _logger.LogInformation("Editing workflow: {WorkflowName} /
            // Navigate to workflow editor page
            await Shell.Current.GoToAsync($"//workflows/edit?workflowId={workflow.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit workflow {WorkflowId} /
        }
    }
    /// <summary>
    /// Create workflow command
    /// </summary>
    [RelayCommand]
    private async Task CreateWorkflowAsync()
    {
        try
        {
            _logger.LogInformation("Creating new workflow /
            // Navigate to workflow creation page
            await Shell.Current.GoToAsync("//workflows/create");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to create workflow page /
        }
    }
    #endregion
    #region Private Methods /
    /// <summary>
    /// Property changed event handler
    /// </summary>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(SelectedStatusFilter))
        {
            FilterWorkflows();
        }
        else if (e.PropertyName == nameof(IsRefreshing))
        {
            OnPropertyChanged(nameof(IsNotRefreshing));
        }
    }
    /// <summary>
    /// Filter workflows based on search text and status
    /// </summary>
    private void FilterWorkflows()
    {
        FilteredWorkflows.Clear();
        var filtered = Workflows.AsEnumerable();
        // Apply text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(w =>
                w.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                w.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        // Apply status filter
        if (SelectedStatusFilter != "All")
        {
            filtered = filtered.Where(w => w.Status == SelectedStatusFilter);
        }
        foreach (var workflow in filtered)
        {
            FilteredWorkflows.Add(workflow);
        }
        _logger.LogDebug("Filtered {FilteredCount} workflows from {TotalCount} /
            FilteredWorkflows.Count, Workflows.Count);
    }
    /// <summary>
    /// Subscribe to SignalR eventsSignalR
    /// </summary>
    private void SubscribeToSignalREvents()
    {
        _signalRService.WorkflowExecutionStarted += OnWorkflowExecutionStarted;
        _signalRService.WorkflowExecutionCompleted += OnWorkflowExecutionCompleted;
        _signalRService.WorkflowExecutionFailed += OnWorkflowExecutionFailed;
        _signalRService.WorkflowStepCompleted += OnWorkflowStepCompleted;
    }
    /// <summary>
    /// Workflow execution started event handler
    /// </summary>
    private async void OnWorkflowExecutionStarted(string workflowId, string executionId)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                workflow.Status = "Executing";
                workflow.IsExecuting = true;
                workflow.ExecutionProgress = 0.0;
                workflow.CurrentStep = "Starting...";
                _logger.LogInformation("Workflow {WorkflowId} execution started /
            }
        });
    }
    /// <summary>
    /// Workflow execution completed event handler
    /// </summary>
    private async void OnWorkflowExecutionCompleted(string workflowId, string executionId)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                workflow.Status = "Completed";
                workflow.IsExecuting = false;
                workflow.ExecutionProgress = 1.0;
                workflow.CurrentStep = "Completed";
                _logger.LogInformation("Workflow {WorkflowId} execution completed /
                // Refresh recent executions
                _ = Task.Run(async () => await RefreshWorkflowExecutionsAsync(workflowId));
            }
        });
    }
    /// <summary>
    /// Workflow execution failed event handler
    /// </summary>
    private async void OnWorkflowExecutionFailed(string workflowId, string executionId, string error)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                workflow.Status = "Failed";
                workflow.IsExecuting = false;
                workflow.CurrentStep = $"Failed: {error}";
                _logger.LogWarning("Workflow {WorkflowId} execution failed: {Error} /
                    workflowId, error);
                // Refresh recent executions
                _ = Task.Run(async () => await RefreshWorkflowExecutionsAsync(workflowId));
            }
        });
    }
    /// <summary>
    /// Workflow step completed event handler
    /// </summary>
    private async void OnWorkflowStepCompleted(string workflowId, string executionId, int stepIndex, int totalSteps)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                workflow.ExecutionProgress = (double)stepIndex / totalSteps;
                workflow.CurrentStep = $"Step {stepIndex + 1} of {totalSteps}";
                // Calculate estimated time remaining
                if (stepIndex > 0)
                {
                    var avgTimePerStep = TimeSpan.FromMinutes(1); // Placeholder
                    var remainingSteps = totalSteps - stepIndex;
                    var eta = avgTimePerStep.Multiply(remainingSteps);
                    workflow.EstimatedTimeRemaining = eta.ToString(@"mm\:ss");
                }
            }
        });
    }
    /// <summary>
    /// Refresh workflow executions
    /// </summary>
    private async Task RefreshWorkflowExecutionsAsync(string workflowId)
    {
        try
        {
            var executions = await _apiService.GetWorkflowExecutionsAsync(workflowId, 5);
            var workflow = Workflows.FirstOrDefault(w => w.Id == workflowId);
            if (workflow != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    workflow.LoadRecentExecutions(executions);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh executions for workflow {WorkflowId} /
        }
    }
    #endregion
}
/// <summary>
/// Workflow view model
/// Represents a workflow in the UI
//// </summary>
public partial class WorkflowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _description = string.Empty;
    [ObservableProperty]
    private string _version = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;
    [ObservableProperty]
    private int _stepCount;
    [ObservableProperty]
    private bool _isExecuting;
    [ObservableProperty]
    private double _executionProgress;
    [ObservableProperty]
    private string _currentStep = string.Empty;
    [ObservableProperty]
    private string _estimatedTimeRemaining = string.Empty;
    [ObservableProperty]
    private ObservableCollection<WorkflowExecutionViewModel> _recentExecutions = new();
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="workflowDefinition">Workflow definition</param>
    public WorkflowViewModel(WorkflowDefinition workflowDefinition)
    {
        Id = workflowDefinition.Id;
        Name = workflowDefinition.Name;
        Description = workflowDefinition.Description;
        Version = workflowDefinition.Version;
        Status = "Ready";
        CreatedAt = workflowDefinition.CreatedAt;
        StepCount = workflowDefinition.Steps?.Count ?? 0;
    }
    /// <summary>
    /// Status color based on workflow status
    /// </summary>
    public Color StatusColor => Status switch
    {
        "Executing" => Colors.Blue,
        "Completed" => Colors.Green,
        "Failed" => Colors.Red,
        "Cancelled" => Colors.Orange,
        "Ready" => Colors.Gray,
        _ => Colors.Gray
    };
    /// <summary>
    /// Can execute workflow
    /// </summary>
    public bool CanExecute => Status != "Executing";
    /// <summary>
    /// Has recent executions
    /// </summary>
    public bool HasRecentExecutions => RecentExecutions.Count > 0;
    /// <summary>
    /// Load recent executions
    /// </summary>
    /// <param name="executions">Workflow executions</param>
    public void LoadRecentExecutions(IEnumerable<WorkflowExecution> executions)
    {
        RecentExecutions.Clear();
        foreach (var execution in executions.Take(5))
        {
            RecentExecutions.Add(new WorkflowExecutionViewModel(execution));
        }
        OnPropertyChanged(nameof(HasRecentExecutions));
    }
}
/// <summary>
/// Workflow execution view model
/// Represents a workflow execution in the UI
//// </summary>
public partial class WorkflowExecutionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private DateTime _executedAt = DateTime.Now;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="execution">Workflow execution</param>
    public WorkflowExecutionViewModel(WorkflowExecution execution)
    {
        Id = execution.Id;
        Status = execution.Status;
        ExecutedAt = execution.StartedAt;
    }
    /// <summary>
    /// Status color based on execution status
    /// </summary>
    public Color StatusColor => Status switch
    {
        "Completed" => Colors.Green,
        "Failed" => Colors.Red,
        "Cancelled" => Colors.Orange,
        "Running" => Colors.Blue,
        _ => Colors.Gray
    };
}

