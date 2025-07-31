using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using MultiAgent.Core.Data;
using MultiAgent.Core.Services;
using MultiAgent.Agents.Services;
using MultiAgent.AI.Services;
using MultiAgent.Security.Services;
using MultiAgent.WebAPI.Hubs;
using MultiAgent.Shared.Contracts;
// Configure SerilogSerilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/multiagent-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    // Use SerilogSerilog
    builder.Host.UseSerilog();
    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    // Configure SwaggerSwagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Multi-Agent API",
            Version = "v1",
            Description = "Multi-Agent Workflow System API /
        });
        // Include XML commentsXML
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
        // Add JWT authenticationJWT
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });
    // Configure Entity FrameworkEntity Framework
    builder.Services.AddDbContext<MultiAgentDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use in-memory database for development
            options.UseInMemoryDatabase("MultiAgentDb");
        }
        else
        {
            options.UseSqlServer(connectionString);
        }
    });
    // Configure CORSCORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
    // Configure SignalRSignalR
    builder.Services.AddSignalR();
    // Configure Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // JWT configuration will be added later / JWT
        });
    // Register application services
    builder.Services.AddScoped<IAgentManager, AgentManager>();
    builder.Services.AddScoped<IWorkflowService, WorkflowService>();
    builder.Services.AddScoped<IAIService, SemanticKernelService>();
    builder.Services.AddScoped<ISecurityService, SecurityService>();
    // Register hosted services
    builder.Services.AddHostedService<AgentHostService>();
    var app = builder.Build();
    // Configure the HTTP request pipelineHTTP
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Agent API v1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        });
    }
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    // Map SignalR hubsSignalR
    app.MapHub<AgentHub>("/hubs/agent");
    app.MapHub<WorkflowHub>("/hubs/workflow");
    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MultiAgentDbContext>();
        context.Database.EnsureCreated();
    }
    Log.Information("Multi-Agent API starting up / Multi-Agent API
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly /
}
finally
{
    Log.CloseAndFlush();
}

