using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using MultiAgent.Shared.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Workflow detail page view model
/// Manages detailed workflow information, execution, and monitoring
//// </summary>
public partial class WorkflowDetailViewModel : ObservableObject
{
    #region Fields /
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<WorkflowDetailViewModel> _logger;
    [ObservableProperty]
    private string _workflowId = string.Empty;
    [ObservableProperty]
    private string _workflowName = string.Empty;
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
    private string _executionMode = string.Empty;
    [ObservableProperty]
    private int _maxParallelSteps;
    [ObservableProperty]
    private int _timeoutMinutes;
    [ObservableProperty]
    private string _retryPolicy = string.Empty;
    [ObservableProperty]
    private string _errorHandling = string.Empty;
    [ObservableProperty]
    private string _inputData = string.Empty;
    [ObservableProperty]
    private string _outputData = string.Empty;
    [ObservableProperty]
    private string _customInputData = string.Empty;
    [ObservableProperty]
    private ObservableCollection<WorkflowStepViewModel> _workflowSteps = new();
    [ObservableProperty]
    private ObservableCollection<WorkflowExecutionHistoryViewModel> _executionHistory = new();
    #endregion
    #region Constructor /
    /// <summary>
    /// Constructor
    /// </summary>
    public WorkflowDetailViewModel(
        IApiService apiService,
        ISignalRService signalRService,
        ILogger<WorkflowDetailViewModel> logger)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        _logger = logger;
    }
    #endregion
    #region Properties /
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
    public bool CanExecute => !IsExecuting && Status != "Executing";
    /// <summary>
    /// Can execute with custom input
    /// </summary>
    public bool CanExecuteWithCustomInput => CanExecute && !string.IsNullOrWhiteSpace(CustomInputData);
    #endregion
    #region Commands /
    /// <summary>
    /// Load workflow async
    /// </summary>
    public async Task LoadWorkflowAsync(string workflowId)
    {
        try
        {
            WorkflowId = workflowId;
            _logger.LogInformation("Loading workflow details for {WorkflowId}", workflowId);
            var workflow = await _apiService.GetWorkflowAsync(workflowId);
            if (workflow != null)
            {
                UpdateFromWorkflowDefinition(workflow);
            }
            await LoadWorkflowStepsAsync();
            await LoadExecutionHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow details");
        }
    }
    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        try
        {
            var inputData = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(InputData))
            {
                inputData = JsonSerializer.Deserialize<Dictionary<string, object>>(InputData) ?? new();
            }
            var execution = await _apiService.ExecuteWorkflowAsync(WorkflowId, inputData);
            if (execution != null)
            {
                IsExecuting = true;
                Status = "Executing";
                ExecutionProgress = 0.0;
                CurrentStep = "Starting execution...";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow");
            await Shell.Current.DisplayAlert("Error", $"Failed to execute workflow: {ex.Message}", "OK");
        }
    }
    [RelayCommand]
    private async Task ExecuteWithCustomInputAsync()
    {
        try
        {
            var inputData = JsonSerializer.Deserialize<Dictionary<string, object>>(CustomInputData) ?? new();
            var execution = await _apiService.ExecuteWorkflowAsync(WorkflowId, inputData);
            if (execution != null)
            {
                IsExecuting = true;
                Status = "Executing";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow with custom input");
        }
    }
    [RelayCommand]
    private async Task CancelExecutionAsync()
    {
        try
        {
            var result = await Shell.Current.DisplayAlert("Cancel Execution", "Are you sure?", "Yes", "No");
            if (result)
            {
                IsExecuting = false;
                Status = "Cancelled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution");
        }
    }
    [RelayCommand]
    private async Task EditWorkflowAsync()
    {
        await Shell.Current.GoToAsync($"//workflows/editor?workflowId={WorkflowId}");
    }
    [RelayCommand]
    private async Task ValidateWorkflowAsync()
    {
        try
        {
            var result = await _apiService.ValidateWorkflowAsync(WorkflowId);
            var message = result?.IsValid == true ? "Validation successful!" : $"Validation failed: {result?.ErrorMessage}";
            await Shell.Current.DisplayAlert("Validation Result", message, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow");
        }
    }
    [RelayCommand]
    private async Task OpenDesignerAsync()
    {
        await Shell.Current.GoToAsync($"//workflows/designer?workflowId={WorkflowId}");
    }
    [RelayCommand]
    private async Task ExecuteStepAsync(WorkflowStepViewModel step)
    {
        await Shell.Current.DisplayAlert("Execute Step", $"Executing step: {step.Name}", "OK");
    }
    [RelayCommand]
    private async Task ConfigureStepAsync(WorkflowStepViewModel step)
    {
        await Shell.Current.GoToAsync($"//workflows/step-config?workflowId={WorkflowId}&stepId={step.Id}");
    }
    [RelayCommand]
    private async Task EditConfigurationAsync()
    {
        await Shell.Current.GoToAsync($"//workflows/config?workflowId={WorkflowId}");
    }
    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        await LoadExecutionHistoryAsync();
    }
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        var result = await Shell.Current.DisplayAlert("Clear History", "Are you sure?", "Yes", "No");
        if (result)
        {
            ExecutionHistory.Clear();
        }
    }
    [RelayCommand]
    private async Task ViewExecutionDetailsAsync(WorkflowExecutionHistoryViewModel execution)
    {
        await Shell.Current.GoToAsync($"//workflows/execution-details?executionId={execution.ExecutionId}");
    }
    #endregion
    #region Private Methods /
    private void UpdateFromWorkflowDefinition(WorkflowDefinition workflow)
    {
        WorkflowName = workflow.Name;
        Description = workflow.Description;
        Version = workflow.Version;
        Status = "Ready";
        CreatedAt = workflow.CreatedAt;
        StepCount = workflow.Steps?.Count ?? 0;
        ExecutionMode = "Sequential";
        MaxParallelSteps = 3;
        TimeoutMinutes = 30;
        RetryPolicy = "Retry 3 times";
        ErrorHandling = "Stop on error";
        InputData = JsonSerializer.Serialize(workflow.InputData ?? new Dictionary<string, object>(), new JsonSerializerOptions { WriteIndented = true });
        OutputData = "";
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(CanExecute));
        OnPropertyChanged(nameof(CanExecuteWithCustomInput));
    }
    private async Task LoadWorkflowStepsAsync()
    {
        WorkflowSteps.Clear();
        var mockSteps = new[]
        {
            new WorkflowStepViewModel
            {
                Id = "step1", StepNumber = 1, Name = "Initialize Data",
                Description = "Initialize input data", Type = "DataInitialization",
                Status = "Completed", Duration = 150, CanExecute = true
            },
            new WorkflowStepViewModel
            {
                Id = "step2", StepNumber = 2, Name = "Process Records",
                Description = "Process all records", Type = "DataProcessing",
                Status = "Running", Duration = 0, CanExecute = false
            }
        };
        foreach (var step in mockSteps)
        {
            WorkflowSteps.Add(step);
        }
    }
    private async Task LoadExecutionHistoryAsync()
    {
        try
        {
            var executions = await _apiService.GetWorkflowExecutionsAsync(WorkflowId, 10);
            ExecutionHistory.Clear();
            foreach (var execution in executions)
            {
                ExecutionHistory.Add(new WorkflowExecutionHistoryViewModel(execution));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load execution history");
        }
    }
    #endregion
}
/// <summary>
/// Workflow step view model
/// </summary>
public partial class WorkflowStepViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    [ObservableProperty]
    private int _stepNumber;
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _description = string.Empty;
    [ObservableProperty]
    private string _type = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private int _duration;
    [ObservableProperty]
    private bool _canExecute = true;
    public string StatusIcon => Status switch
    {
        "Completed" => "✓",
        "Running" => "⏳",
        "Failed" => "✗",
        "Pending" => "⏸️",
        _ => "⚪"
    };
    public Color StepStatusColor => Status switch
    {
        "Completed" => Colors.Green,
        "Running" => Colors.Blue,
        "Failed" => Colors.Red,
        "Pending" => Colors.Orange,
        _ => Colors.Gray
    };
    public bool HasDuration => Duration > 0;
}
/// <summary>
/// Workflow execution history view model
/// </summary>
public partial class WorkflowExecutionHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _executionId = string.Empty;
    [ObservableProperty]
    private string _status = string.Empty;
    [ObservableProperty]
    private DateTime _startedAt = DateTime.Now;
    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;
    [ObservableProperty]
    private int _completedSteps;
    [ObservableProperty]
    private string _statusIcon = string.Empty;
    [ObservableProperty]
    private Color _statusColor = Colors.Gray;
    public WorkflowExecutionHistoryViewModel()
    {
    }
    public WorkflowExecutionHistoryViewModel(WorkflowExecution execution)
    {
        ExecutionId = execution.Id;
        Status = execution.Status;
        StartedAt = execution.StartedAt;
        Duration = execution.CompletedAt.HasValue ? execution.CompletedAt.Value - execution.StartedAt : TimeSpan.Zero;
        CompletedSteps = execution.CompletedSteps;
        StatusIcon = execution.Status switch
        {
            "Completed" => "✓",
            "Failed" => "✗",
            "Cancelled" => "⏹️",
            "Running" => "⏳",
            _ => "⚪"
        };
        StatusColor = execution.Status switch
        {
            "Completed" => Colors.Green,
            "Failed" => Colors.Red,
            "Cancelled" => Colors.Orange,
            "Running" => Colors.Blue,
            _ => Colors.Gray
        };
    }
}

