using Microsoft.EntityFrameworkCore;
using MultiAgent.Shared.Models.Agent;
using MultiAgent.Shared.Models.Workflow;
namespace MultiAgent.Core.Data;
/// <summary>
/// Multi-Agent database context
/// Manages all data entities for the Multi-Agent system
//// </summary>
public class MultiAgentDbContext : DbContext
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Database context options</param>
    public MultiAgentDbContext(DbContextOptions<MultiAgentDbContext> options) : base(options)
    {
    }
    /// <summary>
    /// Agent information entities
    /// </summary>
    public DbSet<AgentInfo> Agents { get; set; } = null!;
    /// <summary>
    /// Agent execution results
    /// </summary>
    public DbSet<AgentExecutionResult> AgentExecutions { get; set; } = null!;
    /// <summary>
    /// Agent health status records
    /// </summary>
    public DbSet<AgentHealthStatus> AgentHealthRecords { get; set; } = null!;
    /// <summary>
    /// Workflow definitions
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = null!;
    /// <summary>
    /// Workflow executions
    /// </summary>
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; } = null!;
    /// <summary>
    /// Workflow step executions
    /// </summary>
    public DbSet<WorkflowStepExecution> WorkflowStepExecutions { get; set; } = null!;
    /// <summary>
    /// Configure entity models
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure AgentInfo entityAgentInfo
        modelBuilder.Entity<AgentInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).HasMaxLength(50);
            // Configure JSON columns for complex propertiesJSON
            entity.Property(e => e.Capabilities)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<AgentCapabilities>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new AgentCapabilities());
            entity.Property(e => e.Metadata)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
        });
        // Configure AgentExecutionResult entityAgentExecutionResult
        modelBuilder.Entity<AgentExecutionResult>(entity =>
        {
            entity.HasKey(e => e.ExecutionId);
            entity.Property(e => e.ExecutionId).HasMaxLength(100);
            entity.Property(e => e.AgentId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CommandId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.OutputData)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.Logs)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Success);
        });
        // Configure AgentHealthStatus entityAgentHealthStatus
        modelBuilder.Entity<AgentHealthStatus>(entity =>
        {
            entity.HasKey(e => new { e.AgentId, e.LastCheckTime });
            entity.Property(e => e.AgentId).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Metrics)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.LastCheckTime);
        });
        // Configure WorkflowDefinition entityWorkflowDefinition
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.Steps)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<WorkflowStep>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<WorkflowStep>());
            entity.Property(e => e.InputParameters)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.OutputParameters)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.Metadata)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);
        });
        // Configure WorkflowExecution entityWorkflowExecution
        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.WorkflowId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ExecutedBy).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.InputData)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.OutputData)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.Context)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.Logs)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            // Configure relationships
            entity.HasMany(e => e.StepExecutions)
                  .WithOne()
                  .HasForeignKey(se => se.WorkflowExecutionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.ExecutedBy);
        });
        // Configure WorkflowStepExecution entityWorkflowStepExecution
        modelBuilder.Entity<WorkflowStepExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.WorkflowExecutionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StepId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AgentId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.InputData)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.OutputData)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            entity.Property(e => e.Logs)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            entity.HasIndex(e => e.WorkflowExecutionId);
            entity.HasIndex(e => e.StepId);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
        });
    }
    /// <summary>
    /// Save changes with automatic timestamp updates
    /// </summary>
    /// <returns>Number of affected records</returns>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }
    /// <summary>
    /// Save changes asynchronously with automatic timestamp updates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// Update timestamps for entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        foreach (var entry in entries)
        {
            if (entry.Entity is AgentInfo agentInfo)
            {
                if (entry.State == EntityState.Added)
                {
                    agentInfo.CreatedAt = DateTime.UtcNow;
                }
                agentInfo.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is WorkflowDefinition workflowDef)
            {
                if (entry.State == EntityState.Added)
                {
                    workflowDef.CreatedAt = DateTime.UtcNow;
                }
                workflowDef.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

