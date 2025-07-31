using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Workflow Designer ViewModel -
/// Manages workflow design, step configuration, and validation
//// </summary>
public partial class WorkflowDesignerViewModel : ObservableObject, IQueryAttributable
{
    private readonly IApiService _apiService;
    private readonly ILogger<WorkflowDesignerViewModel> _logger;
    private string _workflowId = string.Empty;
    /// <summary>
    /// Initialize Workflow Designer ViewModel
    /// </summary>
    public WorkflowDesignerViewModel(IApiService apiService, ILogger<WorkflowDesignerViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        WorkflowSteps = new ObservableCollection<WorkflowStepDesign>();
        AvailableStepTypes = new ObservableCollection<StepTypeInfo>();
        AvailableAgents = new ObservableCollection<AgentInfo>();
        ExecutionModes = new ObservableCollection<string>();
        ValidationResults = new ObservableCollection<ValidationResult>();
        InitializeDefaults();
    }
    #region Properties /
    /// <summary>
    /// Workflow ID
    /// </summary>
    public string WorkflowId
    {
        get => _workflowId;
        set
        {
            _workflowId = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Workflow name
    /// </summary>
    [ObservableProperty]
    private string workflowName = string.Empty;
    /// <summary>
    /// Workflow description
    /// </summary>
    [ObservableProperty]
    private string workflowDescription = string.Empty;
    /// <summary>
    /// Workflow status
    /// </summary>
    [ObservableProperty]
    private string workflowStatus = "Draft";
    /// <summary>
    /// Execution modes
    /// </summary>
    public ObservableCollection<string> ExecutionModes { get; }
    /// <summary>
    /// Selected execution mode
    /// </summary>
    [ObservableProperty]
    private string selectedExecutionMode = "Sequential";
    /// <summary>
    /// Workflow steps
    /// </summary>
    public ObservableCollection<WorkflowStepDesign> WorkflowSteps { get; }
    /// <summary>
    /// Available step types
    /// </summary>
    public ObservableCollection<StepTypeInfo> AvailableStepTypes { get; }
    /// <summary>
    /// Available agents
    /// </summary>
    public ObservableCollection<AgentInfo> AvailableAgents { get; }
    /// <summary>
    /// Selected step
    /// </summary>
    [ObservableProperty]
    private WorkflowStepDesign? selectedStep;
    /// <summary>
    /// Validation results
    /// </summary>
    public ObservableCollection<ValidationResult> ValidationResults { get; }
    /// <summary>
    /// Is loading
    /// </summary>
    [ObservableProperty]
    private bool isLoading;
    /// <summary>
    /// Has unsaved changes
    /// </summary>
    [ObservableProperty]
    private bool hasUnsavedChanges;
    /// <summary>
    /// Has steps
    /// </summary>
    public bool HasSteps => WorkflowSteps.Count > 0;
    /// <summary>
    /// Has selected step
    /// </summary>
    public bool HasSelectedStep => SelectedStep != null;
    /// <summary>
    /// Has validation results
    /// </summary>
    public bool HasValidationResults => ValidationResults.Count > 0;
    /// <summary>
    /// Has no validation results
    /// </summary>
    public bool HasNoValidationResults => ValidationResults.Count == 0;
    #endregion
    #region Commands /
    /// <summary>
    /// Save workflow command
    /// </summary>
    [RelayCommand]
    private async Task SaveWorkflowAsync()
    {
        try
        {
            IsLoading = true;
            var workflowData = BuildWorkflowData();
            if (string.IsNullOrEmpty(WorkflowId))
            {
                // Create new workflow
                var response = await _apiService.PostAsync<dynamic>("workflows", workflowData);
                if (response != null)
                {
                    var responseJson = JsonSerializer.Serialize(response);
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    if (responseObj.TryGetProperty("id", out var idElement))
                    {
                        WorkflowId = idElement.GetString() ?? string.Empty;
                    }
                }
            }
            else
            {
                // Update existing workflow
                await _apiService.PutAsync($"workflows/{WorkflowId}", workflowData);
            }
            HasUnsavedChanges = false;
            WorkflowStatus = "Saved";
            await Application.Current!.MainPage!.DisplayAlert(
                "Success /
                "Workflow saved successfully! /
                "OK");
            _logger.LogInformation("Workflow saved: {WorkflowId}", WorkflowId);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error /
                $"Failed to save workflow: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error saving workflow: {WorkflowId}", WorkflowId);
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Validate workflow command
    /// </summary>
    [RelayCommand]
    private async Task ValidateWorkflowAsync()
    {
        try
        {
            IsLoading = true;
            ValidationResults.Clear();
            var workflowData = BuildWorkflowData();
            var response = await _apiService.PostAsync<dynamic>("workflows/validate", workflowData);
            if (response != null)
            {
                var responseJson = JsonSerializer.Serialize(response);
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                if (responseObj.TryGetProperty("isValid", out var isValidElement) &&
                    responseObj.TryGetProperty("errors", out var errorsElement))
                {
                    var isValid = isValidElement.GetBoolean();
                    if (isValid)
                    {
                        ValidationResults.Add(new ValidationResult
                        {
                            Type = "Success",
                            Message = "Workflow validation passed",
                            Icon = "✅",
                            MessageColor = Colors.Green
                        });
                    }
                    else
                    {
                        foreach (var error in errorsElement.EnumerateArray())
                        {
                            ValidationResults.Add(new ValidationResult
                            {
                                Type = "Error",
                                Message = error.GetString() ?? "Unknown error",
                                Icon = "❌",
                                MessageColor = Colors.Red
                            });
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(HasValidationResults));
            OnPropertyChanged(nameof(HasNoValidationResults));
            _logger.LogInformation("Workflow validation completed for: {WorkflowId}", WorkflowId);
        }
        catch (Exception ex)
        {
            ValidationResults.Add(new ValidationResult
            {
                Type = "Error",
                Message = $"Validation failed: {ex.Message}",
                Icon = "❌",
                MessageColor = Colors.Red
            });
            _logger.LogError(ex, "Error validating workflow: {WorkflowId}", WorkflowId);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasValidationResults));
            OnPropertyChanged(nameof(HasNoValidationResults));
        }
    }
    /// <summary>
    /// Test run command
    /// </summary>
    [RelayCommand]
    private async Task TestRunAsync()
    {
        try
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Test Run /
                "Do you want to run a test execution of this workflow? /
                "Yes /
                "No /
            if (result)
            {
                IsLoading = true;
                var workflowData = BuildWorkflowData();
                var response = await _apiService.PostAsync<dynamic>("workflows/test-run", workflowData);
                if (response != null)
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Test Started /
                        "Test execution started successfully! /
                        "OK");
                }
                _logger.LogInformation("Test run started for workflow: {WorkflowId}", WorkflowId);
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error /
                $"Failed to start test run: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error starting test run for workflow: {WorkflowId}", WorkflowId);
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Export workflow command
    /// </summary>
    [RelayCommand]
    private async Task ExportWorkflowAsync()
    {
        try
        {
            var workflowData = BuildWorkflowData();
            var json = JsonSerializer.Serialize(workflowData, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"workflow_{WorkflowName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            await File.WriteAllTextAsync(filePath, json);
            await Application.Current!.MainPage!.DisplayAlert(
                "Export Success /
                $"Workflow exported to {fileName}",
                "OK");
            _logger.LogInformation("Workflow exported to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Export Failed /
                $"Failed to export workflow: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error exporting workflow");
        }
    }
    /// <summary>
    /// Add step command
    /// </summary>
    [RelayCommand]
    private void AddStep(StepTypeInfo stepType)
    {
        if (stepType != null)
        {
            var step = new WorkflowStepDesign
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{stepType.Name} Step",
                Type = stepType.Type,
                Icon = stepType.Icon,
                Description = stepType.Description,
                Order = WorkflowSteps.Count + 1,
                Status = "Not Configured",
                StatusColor = Colors.Orange,
                StatusTextColor = Colors.Black,
                Parameters = new ObservableCollection<StepParameter>()
            };
            WorkflowSteps.Add(step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            OnPropertyChanged(nameof(HasSteps));
            _logger.LogInformation("Added step: {StepName} of type: {StepType}", step.Name, step.Type);
        }
    }
    /// <summary>
    /// Show add step dialog command
    /// </summary>
    [RelayCommand]
    private async Task ShowAddStepDialogAsync()
    {
        var stepTypeNames = AvailableStepTypes.Select(st => st.Name).ToArray();
        var selectedStepType = await Application.Current!.MainPage!.DisplayActionSheet(
            "Select Step Type /
            "Cancel /
            null,
            stepTypeNames);
        if (!string.IsNullOrEmpty(selectedStepType) && selectedStepType != "Cancel /
        {
            var stepType = AvailableStepTypes.FirstOrDefault(st => st.Name == selectedStepType);
            if (stepType != null)
            {
                AddStep(stepType);
            }
        }
    }
    /// <summary>
    /// Edit step command
    /// </summary>
    [RelayCommand]
    private void EditStep(WorkflowStepDesign step)
    {
        if (step != null)
        {
            SelectedStep = step;
            _logger.LogInformation("Selected step for editing: {StepName}", step.Name);
        }
    }
    /// <summary>
    /// Remove step command
    /// </summary>
    [RelayCommand]
    private async Task RemoveStepAsync(WorkflowStepDesign step)
    {
        if (step != null)
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Remove Step /
                $"Are you sure you want to remove '{step.Name}'? /
                "Yes /
                "No /
            if (result)
            {
                WorkflowSteps.Remove(step);
                // Reorder remaining steps
                for (int i = 0; i < WorkflowSteps.Count; i++)
                {
                    WorkflowSteps[i].Order = i + 1;
                }
                if (SelectedStep == step)
                {
                    SelectedStep = null;
                }
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(HasSteps));
                OnPropertyChanged(nameof(HasSelectedStep));
                _logger.LogInformation("Removed step: {StepName}", step.Name);
            }
        }
    }
    /// <summary>
    /// Add parameter command
    /// </summary>
    [RelayCommand]
    private void AddParameter()
    {
        if (SelectedStep != null)
        {
            var parameter = new StepParameter
            {
                Key = "parameter_name",
                Value = "parameter_value"
            };
            SelectedStep.Parameters.Add(parameter);
            HasUnsavedChanges = true;
        }
    }
    /// <summary>
    /// Remove parameter command
    /// </summary>
    [RelayCommand]
    private void RemoveParameter(StepParameter parameter)
    {
        if (parameter != null && SelectedStep != null)
        {
            SelectedStep.Parameters.Remove(parameter);
            HasUnsavedChanges = true;
        }
    }
    /// <summary>
    /// Zoom in command
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        // TODO: Implement canvas zoom functionality
        _logger.LogInformation("Zoom in requested");
    }
    /// <summary>
    /// Zoom out command
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        // TODO: Implement canvas zoom functionality
        _logger.LogInformation("Zoom out requested");
    }
    /// <summary>
    /// Fit to screen command
    /// </summary>
    [RelayCommand]
    private void FitToScreen()
    {
        // TODO: Implement fit to screen functionality
        _logger.LogInformation("Fit to screen requested");
    }
    /// <summary>
    /// Clear canvas command
    /// </summary>
    [RelayCommand]
    private async Task ClearCanvasAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Clear Canvas /
            "Are you sure you want to clear all steps? /
            "Yes /
            "No /
        if (result)
        {
            WorkflowSteps.Clear();
            SelectedStep = null;
            ValidationResults.Clear();
            HasUnsavedChanges = true;
            OnPropertyChanged(nameof(HasSteps));
            OnPropertyChanged(nameof(HasSelectedStep));
            OnPropertyChanged(nameof(HasValidationResults));
            OnPropertyChanged(nameof(HasNoValidationResults));
            _logger.LogInformation("Canvas cleared");
        }
    }
    #endregion
    #region Methods /
    /// <summary>
    /// Apply query attributes
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workflowId", out var workflowIdObj))
        {
            WorkflowId = workflowIdObj?.ToString() ?? string.Empty;
        }
    }
    /// <summary>
    /// Initialize async
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await LoadAvailableStepTypesAsync();
            await LoadAvailableAgentsAsync();
            if (!string.IsNullOrEmpty(WorkflowId))
            {
                await LoadWorkflowAsync();
            }
            else
            {
                LoadNewWorkflow();
            }
            _logger.LogInformation("Workflow designer initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing workflow designer");
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Cleanup resources
    /// </summary>
    public void Cleanup()
    {
        WorkflowSteps.Clear();
        AvailableStepTypes.Clear();
        AvailableAgents.Clear();
        ValidationResults.Clear();
        SelectedStep = null;
    }
    /// <summary>
    /// Initialize defaults
    /// </summary>
    private void InitializeDefaults()
    {
        // Execution modes
        ExecutionModes.Add("Sequential");
        ExecutionModes.Add("Parallel");
        ExecutionModes.Add("Conditional");
        ExecutionModes.Add("Mixed");
        SelectedExecutionMode = "Sequential";
    }
    /// <summary>
    /// Load available step types async
    /// </summary>
    private async Task LoadAvailableStepTypesAsync()
    {
        try
        {
            // TODO: Load from backend APIAPI
            await Task.Delay(100); // Simulate loading
            // Add mock step types
            AvailableStepTypes.Clear();
            AvailableStepTypes.Add(new StepTypeInfo { Type = "AgentTask", Name = "Agent Task", Description = "Execute a task using an agent", Icon = "🤖" });
            AvailableStepTypes.Add(new StepTypeInfo { Type = "DataTransform", Name = "Data Transform", Description = "Transform data between steps", Icon = "🔄" });
            AvailableStepTypes.Add(new StepTypeInfo { Type = "Condition", Name = "Condition", Description = "Conditional branching logic", Icon = "❓" });
            AvailableStepTypes.Add(new StepTypeInfo { Type = "Loop", Name = "Loop", Description = "Repeat steps in a loop", Icon = "🔁" });
            AvailableStepTypes.Add(new StepTypeInfo { Type = "Delay", Name = "Delay", Description = "Add a delay between steps", Icon = "⏰" });
            AvailableStepTypes.Add(new StepTypeInfo { Type = "Notification", Name = "Notification", Description = "Send notifications", Icon = "📢" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available step types");
        }
    }
    /// <summary>
    /// Load available agents async
    /// </summary>
    private async Task LoadAvailableAgentsAsync()
    {
        try
        {
            var agents = await _apiService.GetAsync<List<AgentInfo>>("agents");
            if (agents != null && agents.Any())
            {
                AvailableAgents.Clear();
                foreach (var agent in agents)
                {
                    AvailableAgents.Add(agent);
                }
            }
            else
            {
                // Add mock agents
                AvailableAgents.Clear();
                AvailableAgents.Add(new AgentInfo { Id = "1", Name = "FileSystem Agent", Type = "FileSystem" });
                AvailableAgents.Add(new AgentInfo { Id = "2", Name = "Web Scraper Agent", Type = "WebScraper" });
                AvailableAgents.Add(new AgentInfo { Id = "3", Name = "Data Analyzer Agent", Type = "DataAnalyzer" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available agents");
            // Fallback to mock agents
            AvailableAgents.Clear();
            AvailableAgents.Add(new AgentInfo { Id = "1", Name = "FileSystem Agent", Type = "FileSystem" });
        }
    }
    /// <summary>
    /// Load workflow async
    /// </summary>
    private async Task LoadWorkflowAsync()
    {
        try
        {
            var workflow = await _apiService.GetAsync<dynamic>($"workflows/{WorkflowId}");
            if (workflow != null)
            {
                // Parse and populate workflow
                PopulateFromWorkflow(workflow);
                HasUnsavedChanges = false;
                _logger.LogInformation("Workflow loaded: {WorkflowId}", WorkflowId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow: {WorkflowId}", WorkflowId);
            LoadNewWorkflow();
        }
    }
    /// <summary>
    /// Load new workflow
    /// </summary>
    private void LoadNewWorkflow()
    {
        WorkflowName = "New Workflow";
        WorkflowDescription = "Description for new workflow";
        WorkflowStatus = "Draft";
        SelectedExecutionMode = "Sequential";
        WorkflowSteps.Clear();
        ValidationResults.Clear();
        SelectedStep = null;
        HasUnsavedChanges = false;
        OnPropertyChanged(nameof(HasSteps));
        OnPropertyChanged(nameof(HasSelectedStep));
    }
    /// <summary>
    /// Populate from workflow
    /// </summary>
    private void PopulateFromWorkflow(dynamic workflow)
    {
        try
        {
            var workflowJson = JsonSerializer.Serialize(workflow);
            var workflowObj = JsonSerializer.Deserialize<JsonElement>(workflowJson);
            if (workflowObj.TryGetProperty("name", out var nameElement))
                WorkflowName = nameElement.GetString() ?? string.Empty;
            if (workflowObj.TryGetProperty("description", out var descElement))
                WorkflowDescription = descElement.GetString() ?? string.Empty;
            if (workflowObj.TryGetProperty("status", out var statusElement))
                WorkflowStatus = statusElement.GetString() ?? "Draft";
            // TODO: Parse steps and other properties
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing workflow data");
            LoadNewWorkflow();
        }
    }
    /// <summary>
    /// Build workflow data
    /// </summary>
    private object BuildWorkflowData()
    {
        return new
        {
            id = WorkflowId,
            name = WorkflowName,
            description = WorkflowDescription,
            executionMode = SelectedExecutionMode,
            steps = WorkflowSteps.Select(step => new
            {
                id = step.Id,
                name = step.Name,
                type = step.Type,
                description = step.Description,
                order = step.Order,
                agentId = step.Agent?.Id,
                timeoutSeconds = int.TryParse(step.TimeoutSeconds, out var timeout) ? timeout : 30,
                retryCount = step.RetryCount,
                parameters = step.Parameters.ToDictionary(p => p.Key, p => p.Value)
            }).ToArray()
        };
    }
    #endregion
}
/// <summary>
/// Step Type Information
/// </summary>
public class StepTypeInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
/// <summary>
/// Workflow Step Design
/// </summary>
public partial class WorkflowStepDesign : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    [ObservableProperty]
    private string name = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    [ObservableProperty]
    private string description = string.Empty;
    public int Order { get; set; }
    public string Status { get; set; } = string.Empty;
    public Color StatusColor { get; set; } = Colors.Gray;
    public Color StatusTextColor { get; set; } = Colors.Black;
    [ObservableProperty]
    private AgentInfo? agent;
    [ObservableProperty]
    private string timeoutSeconds = "30";
    [ObservableProperty]
    private int retryCount = 3;
    public ObservableCollection<StepParameter> Parameters { get; set; } = new();
}
/// <summary>
/// Step Parameter
/// </summary>
public partial class StepParameter : ObservableObject
{
    [ObservableProperty]
    private string key = string.Empty;
    [ObservableProperty]
    private string value = string.Empty;
}
/// <summary>
/// Validation Result
/// </summary>
public class ValidationResult
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Color MessageColor { get; set; } = Colors.Black;
}
/// <summary>
/// Agent Information
/// </summary>
public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

