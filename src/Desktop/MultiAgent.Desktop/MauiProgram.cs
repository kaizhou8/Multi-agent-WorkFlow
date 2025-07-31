using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MultiAgent.Desktop.Services;
using MultiAgent.Desktop.ViewModels;
using MultiAgent.Desktop.Views;
namespace MultiAgent.Desktop;
/// <summary>
/// MAUI program configuration
/// Configures services and dependencies for the desktop application
//// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Create MAUI appMAUI
    /// </summary>
    /// <returns>MAUI app
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        // Add configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        // Register services
        RegisterServices(builder.Services);
        // Register views and view models
        RegisterViewsAndViewModels(builder.Services);
#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif
        return builder.Build();
    }
    /// <summary>
    /// Register application services
    /// </summary>
    /// <param name="services">Service collection</param>
    private static void RegisterServices(IServiceCollection services)
    {
        // HTTP client for API communicationAPI
        services.AddHttpClient<IApiService, ApiService>(client =>
        {
            // TODO: Read from configuration
            client.BaseAddress = new Uri("https://localhost:7001/api/");
            client.DefaultRequestHeaders.Add("User-Agent", "MultiAgent-Desktop/1.0");
        });
        // SignalR service for real-time communicationSignalR
        services.AddSingleton<ISignalRService, SignalRService>();
        // Application services
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddSingleton<IAIService, AIService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        // Navigation service
        services.AddSingleton<INavigationService, NavigationService>();
        // Logging
        services.AddLogging();
    }
    /// <summary>
    /// Register views and view models
    /// </summary>
    /// <param name="services">Service collection</param>
    private static void RegisterViewsAndViewModels(IServiceCollection services)
    {
        // Main shell and app shell view modelShell
        services.AddSingleton<AppShell>();
        services.AddSingleton<AppShellViewModel>();
        // Dashboard
        services.AddTransient<DashboardPage>();
        services.AddTransient<DashboardViewModel>();
        // Agents
        services.AddTransient<AgentsPage>();
        services.AddTransient<AgentsViewModel>();
        services.AddTransient<AgentDetailPage>();
        services.AddTransient<AgentDetailViewModel>();
        services.AddTransient<AgentConfigurationPage>();
        services.AddTransient<AgentConfigurationViewModel>();
        // Workflows
        services.AddTransient<WorkflowsPage>();
        services.AddTransient<WorkflowsViewModel>();
        services.AddTransient<WorkflowDetailPage>();
        services.AddTransient<WorkflowDetailViewModel>();
        services.AddTransient<WorkflowDesignerPage>();
        services.AddTransient<WorkflowDesignerViewModel>();
        // AI Services / AI
        services.AddTransient<AIPage>();
        services.AddTransient<AIViewModel>();
        services.AddTransient<AITextGenerationPage>();
        services.AddTransient<AITextGenerationViewModel>();
        services.AddTransient<AICodeAnalysisPage>();
        services.AddTransient<AICodeAnalysisViewModel>();
        services.AddTransient<AIDocumentAnalysisPage>();
        services.AddTransient<AIDocumentAnalysisViewModel>();
        services.AddTransient<AIImageAnalysisPage>();
        services.AddTransient<AIImageAnalysisViewModel>();
        services.AddTransient<AIAudioAnalysisPage>();
        services.AddTransient<AIAudioAnalysisViewModel>();
        // Settings
        services.AddTransient<SettingsPage>();
        services.AddTransient<SettingsViewModel>();
        // Authentication
        services.AddTransient<LoginPage>();
        services.AddTransient<LoginViewModel>();
    }
}

