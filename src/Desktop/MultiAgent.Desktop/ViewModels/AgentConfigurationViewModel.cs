using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MultiAgent.Desktop.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
namespace MultiAgent.Desktop.ViewModels;
/// <summary>
/// Agent Configuration ViewModel -
/// Manages agent configuration settings, capabilities, and environment variables
//// </summary>
public partial class AgentConfigurationViewModel : ObservableObject, IQueryAttributable
{
    private readonly IApiService _apiService;
    private readonly ILogger<AgentConfigurationViewModel> _logger;
    private string _agentId = string.Empty;
    /// <summary>
    /// Initialize Agent Configuration ViewModel
    /// </summary>
    public AgentConfigurationViewModel(IApiService apiService, ILogger<AgentConfigurationViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        Capabilities = new ObservableCollection<AgentCapabilityConfig>();
        EnvironmentVariables = new ObservableCollection<EnvironmentVariable>();
        AvailableAgentTypes = new ObservableCollection<string>();
        AvailableLogLevels = new ObservableCollection<string>();
        InitializeDefaults();
    }
    #region Properties /
    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId
    {
        get => _agentId;
        set
        {
            _agentId = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Agent name
    /// </summary>
    [ObservableProperty]
    private string agentName = string.Empty;
    /// <summary>
    /// Agent description
    /// </summary>
    [ObservableProperty]
    private string agentDescription = string.Empty;
    /// <summary>
    /// Available agent types
    /// </summary>
    public ObservableCollection<string> AvailableAgentTypes { get; }
    /// <summary>
    /// Selected agent type
    /// </summary>
    [ObservableProperty]
    private string selectedAgentType = string.Empty;
    /// <summary>
    /// Auto start
    /// </summary>
    [ObservableProperty]
    private bool autoStart;
    /// <summary>
    /// Priority
    /// </summary>
    [ObservableProperty]
    private double priority = 5;
    /// <summary>
    /// Max concurrent tasks
    /// </summary>
    [ObservableProperty]
    private double maxConcurrentTasks = 5;
    /// <summary>
    /// Timeout seconds
    /// </summary>
    [ObservableProperty]
    private string timeoutSeconds = "30";
    /// <summary>
    /// Retry count
    /// </summary>
    [ObservableProperty]
    private double retryCount = 3;
    /// <summary>
    /// Available log levels
    /// </summary>
    public ObservableCollection<string> AvailableLogLevels { get; }
    /// <summary>
    /// Selected log level
    /// </summary>
    [ObservableProperty]
    private string selectedLogLevel = "Information";
    /// <summary>
    /// Capabilities
    /// </summary>
    public ObservableCollection<AgentCapabilityConfig> Capabilities { get; }
    /// <summary>
    /// Environment variables
    /// </summary>
    public ObservableCollection<EnvironmentVariable> EnvironmentVariables { get; }
    /// <summary>
    /// Configuration JSONJSON
    /// </summary>
    [ObservableProperty]
    private string configurationJson = string.Empty;
    /// <summary>
    /// JSON status
    /// </summary>
    [ObservableProperty]
    private string jsonStatus = string.Empty;
    /// <summary>
    /// JSON status color
    /// </summary>
    [ObservableProperty]
    private Color jsonStatusColor = Colors.Gray;
    /// <summary>
    /// JSON validation message
    /// </summary>
    [ObservableProperty]
    private string jsonValidationMessage = string.Empty;
    /// <summary>
    /// Has JSON statusJSON
    /// </summary>
    [ObservableProperty]
    private bool hasJsonStatus;
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
    #endregion
    #region Commands /
    /// <summary>
    /// Save configuration command
    /// </summary>
    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            IsLoading = true;
            var configuration = BuildConfigurationObject();
            var response = await _apiService.PutAsync($"agents/{AgentId}/configuration", configuration);
            if (response != null)
            {
                HasUnsavedChanges = false;
                await Application.Current!.MainPage!.DisplayAlert(
                    "Success /
                    "Configuration saved successfully! /
                    "OK");
                _logger.LogInformation("Agent configuration saved for agent: {AgentId}", AgentId);
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error /
                $"Failed to save configuration: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error saving agent configuration for agent: {AgentId}", AgentId);
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Reset configuration command
    /// </summary>
    [RelayCommand]
    private async Task ResetConfigurationAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Reset Configuration /
            "Are you sure you want to reset all changes? /
            "Yes /
            "No /
        if (result)
        {
            await LoadConfigurationAsync();
            HasUnsavedChanges = false;
        }
    }
    /// <summary>
    /// Add capability command
    /// </summary>
    [RelayCommand]
    private async Task AddCapabilityAsync()
    {
        var availableCapabilities = new[]
        {
            "File Operations /
            "Web Scraping /
            "Data Analysis /
            "Image Processing /
            "Text Processing /
            "API Integration / API
        };
        var selectedCapability = await Application.Current!.MainPage!.DisplayActionSheet(
            "Select Capability /
            "Cancel /
            null,
            availableCapabilities);
        if (!string.IsNullOrEmpty(selectedCapability) && selectedCapability != "Cancel /
        {
            var capability = new AgentCapabilityConfig
            {
                Id = Guid.NewGuid().ToString(),
                Name = selectedCapability.Split(" / ")[0],
                Description = $"Description for {selectedCapability}",
                Icon = GetCapabilityIcon(selectedCapability),
                IsEnabled = true
            };
            Capabilities.Add(capability);
            HasUnsavedChanges = true;
            UpdateConfigurationJson();
        }
    }
    /// <summary>
    /// Remove capability command
    /// </summary>
    [RelayCommand]
    private void RemoveCapability(AgentCapabilityConfig capability)
    {
        if (capability != null)
        {
            Capabilities.Remove(capability);
            HasUnsavedChanges = true;
            UpdateConfigurationJson();
        }
    }
    /// <summary>
    /// Add environment variable command
    /// </summary>
    [RelayCommand]
    private void AddEnvironmentVariable()
    {
        var envVar = new EnvironmentVariable
        {
            Key = "NEW_VARIABLE",
            Value = "value"
        };
        EnvironmentVariables.Add(envVar);
        HasUnsavedChanges = true;
        UpdateConfigurationJson();
    }
    /// <summary>
    /// Remove environment variable command
    /// </summary>
    [RelayCommand]
    private void RemoveEnvironmentVariable(EnvironmentVariable envVar)
    {
        if (envVar != null)
        {
            EnvironmentVariables.Remove(envVar);
            HasUnsavedChanges = true;
            UpdateConfigurationJson();
        }
    }
    /// <summary>
    /// Format JSON commandJSON
    /// </summary>
    [RelayCommand]
    private void FormatJson()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationJson))
            {
                var jsonDocument = JsonDocument.Parse(ConfigurationJson);
                ConfigurationJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                JsonStatus = "Formatted";
                JsonStatusColor = Colors.Green;
                JsonValidationMessage = "JSON formatted successfully";
                HasJsonStatus = true;
            }
        }
        catch (Exception ex)
        {
            JsonStatus = "Error";
            JsonStatusColor = Colors.Red;
            JsonValidationMessage = $"Format error: {ex.Message}";
            HasJsonStatus = true;
        }
    }
    /// <summary>
    /// Validate JSON commandJSON
    /// </summary>
    [RelayCommand]
    private void ValidateJson()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ConfigurationJson))
            {
                JsonStatus = "Empty";
                JsonStatusColor = Colors.Orange;
                JsonValidationMessage = "JSON is empty";
                HasJsonStatus = true;
                return;
            }
            JsonDocument.Parse(ConfigurationJson);
            JsonStatus = "Valid";
            JsonStatusColor = Colors.Green;
            JsonValidationMessage = "JSON is valid";
            HasJsonStatus = true;
        }
        catch (JsonException ex)
        {
            JsonStatus = "Invalid";
            JsonStatusColor = Colors.Red;
            JsonValidationMessage = $"JSON error: {ex.Message}";
            HasJsonStatus = true;
        }
    }
    /// <summary>
    /// Test configuration command
    /// </summary>
    [RelayCommand]
    private async Task TestConfigurationAsync()
    {
        try
        {
            IsLoading = true;
            var configuration = BuildConfigurationObject();
            var response = await _apiService.PostAsync($"agents/{AgentId}/test-configuration", configuration);
            if (response != null)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Test Result /
                    "Configuration test passed! /
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Test Failed /
                $"Configuration test failed: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
    /// <summary>
    /// Export configuration command
    /// </summary>
    [RelayCommand]
    private async Task ExportConfigurationAsync()
    {
        try
        {
            var configuration = BuildConfigurationObject();
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"agent_config_{AgentName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            await File.WriteAllTextAsync(filePath, json);
            await Application.Current!.MainPage!.DisplayAlert(
                "Export Success /
                $"Configuration exported to {fileName}",
                "OK");
            _logger.LogInformation("Agent configuration exported to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Export Failed /
                $"Failed to export configuration: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error exporting agent configuration");
        }
    }
    /// <summary>
    /// Import configuration command
    /// </summary>
    [RelayCommand]
    private async Task ImportConfigurationAsync()
    {
        try
        {
            // TODO: Implement file picker for configuration import
            await Application.Current!.MainPage!.DisplayAlert(
                "Import /
                "Configuration import feature coming soon! /
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Import Failed /
                $"Failed to import configuration: {ex.Message}",
                "OK");
            _logger.LogError(ex, "Error importing agent configuration");
        }
    }
    #endregion
    #region Methods /
    /// <summary>
    /// Apply query attributes
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("agentId", out var agentIdObj))
        {
            AgentId = agentIdObj?.ToString() ?? string.Empty;
        }
    }
    /// <summary>
    /// Load configuration async
    /// </summary>
    public async Task LoadConfigurationAsync()
    {
        if (string.IsNullOrEmpty(AgentId))
            return;
        try
        {
            IsLoading = true;
            // Load agent configuration from APIAPI
            var agentConfig = await _apiService.GetAsync<dynamic>($"agents/{AgentId}/configuration");
            if (agentConfig != null)
            {
                // Parse and populate configuration
                PopulateFromConfiguration(agentConfig);
                UpdateConfigurationJson();
                HasUnsavedChanges = false;
                _logger.LogInformation("Agent configuration loaded for agent: {AgentId}", AgentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agent configuration for agent: {AgentId}", AgentId);
            // Load default configuration
            LoadDefaultConfiguration();
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
        // Clear collections and reset state
        Capabilities.Clear();
        EnvironmentVariables.Clear();
        HasJsonStatus = false;
    }
    /// <summary>
    /// Initialize defaults
    /// </summary>
    private void InitializeDefaults()
    {
        // Available agent types
        AvailableAgentTypes.Add("FileSystem");
        AvailableAgentTypes.Add("WebScraper");
        AvailableAgentTypes.Add("DataAnalyzer");
        AvailableAgentTypes.Add("APIIntegrator");
        AvailableAgentTypes.Add("Custom");
        // Available log levels
        AvailableLogLevels.Add("Trace");
        AvailableLogLevels.Add("Debug");
        AvailableLogLevels.Add("Information");
        AvailableLogLevels.Add("Warning");
        AvailableLogLevels.Add("Error");
        AvailableLogLevels.Add("Critical");
        SelectedAgentType = AvailableAgentTypes.FirstOrDefault() ?? "Custom";
        SelectedLogLevel = "Information";
    }
    /// <summary>
    /// Load default configuration
    /// </summary>
    private void LoadDefaultConfiguration()
    {
        AgentName = $"Agent_{AgentId}";
        AgentDescription = "Default agent configuration";
        AutoStart = false;
        Priority = 5;
        MaxConcurrentTasks = 5;
        TimeoutSeconds = "30";
        RetryCount = 3;
        // Add default capabilities
        Capabilities.Clear();
        Capabilities.Add(new AgentCapabilityConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Basic Operations",
            Description = "Basic agent operations",
            Icon = "⚙️",
            IsEnabled = true
        });
        UpdateConfigurationJson();
    }
    /// <summary>
    /// Populate from configuration
    /// </summary>
    private void PopulateFromConfiguration(dynamic config)
    {
        try
        {
            var configJson = JsonSerializer.Serialize(config);
            var configObj = JsonSerializer.Deserialize<JsonElement>(configJson);
            if (configObj.TryGetProperty("name", out var nameElement))
                AgentName = nameElement.GetString() ?? string.Empty;
            if (configObj.TryGetProperty("description", out var descElement))
                AgentDescription = descElement.GetString() ?? string.Empty;
            if (configObj.TryGetProperty("autoStart", out var autoStartElement))
                AutoStart = autoStartElement.GetBoolean();
            if (configObj.TryGetProperty("priority", out var priorityElement))
                Priority = priorityElement.GetDouble();
            // TODO: Parse capabilities and environment variables
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing agent configuration");
            LoadDefaultConfiguration();
        }
    }
    /// <summary>
    /// Build configuration object
    /// </summary>
    private object BuildConfigurationObject()
    {
        return new
        {
            name = AgentName,
            description = AgentDescription,
            type = SelectedAgentType,
            autoStart = AutoStart,
            priority = (int)Priority,
            maxConcurrentTasks = (int)MaxConcurrentTasks,
            timeoutSeconds = int.TryParse(TimeoutSeconds, out var timeout) ? timeout : 30,
            retryCount = (int)RetryCount,
            logLevel = SelectedLogLevel,
            capabilities = Capabilities.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                isEnabled = c.IsEnabled
            }).ToArray(),
            environmentVariables = EnvironmentVariables.ToDictionary(ev => ev.Key, ev => ev.Value)
        };
    }
    /// <summary>
    /// Update configuration JSONJSON
    /// </summary>
    private void UpdateConfigurationJson()
    {
        try
        {
            var configuration = BuildConfigurationObject();
            ConfigurationJson = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
            HasJsonStatus = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration JSON");
        }
    }
    /// <summary>
    /// Get capability icon
    /// </summary>
    private string GetCapabilityIcon(string capability)
    {
        return capability switch
        {
            var c when c.Contains("File") => "📁",
            var c when c.Contains("Web") => "🌐",
            var c when c.Contains("Data") => "📊",
            var c when c.Contains("Image") => "🖼️",
            var c when c.Contains("Text") => "📝",
            var c when c.Contains("API") => "🔌",
            _ => "⚙️"
        };
    }
    #endregion
}
/// <summary>
/// Agent Capability Configuration
/// </summary>
public class AgentCapabilityConfig : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    [ObservableProperty]
    private bool isEnabled = true;
}
/// <summary>
/// Environment Variable
/// </summary>
public class EnvironmentVariable : ObservableObject
{
    [ObservableProperty]
    private string key = string.Empty;
    [ObservableProperty]
    private string value = string.Empty;
}

