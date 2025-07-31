using Microsoft.Extensions.Logging;
using System.Security.Claims;
namespace MultiAgent.Security.Services;
/// <summary>
/// Authorization service interface
/// Provides role-based and permission-based authorization
//// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if user has required role
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="requiredRole">Required role</param>
    /// <returns>Authorization result</returns>
    Task<bool> HasRoleAsync(ClaimsPrincipal user, string requiredRole);
    /// <summary>
    /// Check if user has required permission
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="requiredPermission">Required permission</param>
    /// <returns>Authorization result</returns>
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string requiredPermission);
    /// <summary>
    /// Check if user can access agent
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="agentId">Agent ID
    /// <param name="operation">Operation type</param>
    /// <returns>Authorization result</returns>
    Task<bool> CanAccessAgentAsync(ClaimsPrincipal user, string agentId, string operation);
    /// <summary>
    /// Check if user can access workflow
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="workflowId">Workflow ID
    /// <param name="operation">Operation type</param>
    /// <returns>Authorization result</returns>
    Task<bool> CanAccessWorkflowAsync(ClaimsPrincipal user, string workflowId, string operation);
    /// <summary>
    /// Get user permissions
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <returns>List of permissions</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(ClaimsPrincipal user);
    /// <summary>
    /// Get user roles
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <returns>List of roles</returns>
    Task<IEnumerable<string>> GetUserRolesAsync(ClaimsPrincipal user);
}
/// <summary>
/// Authorization service implementation
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    // Permission constants
    public static class Permissions
    {
        public const string AgentRead = "agent:read";
        public const string AgentWrite = "agent:write";
        public const string AgentExecute = "agent:execute";
        public const string AgentDelete = "agent:delete";
        public const string WorkflowRead = "workflow:read";
        public const string WorkflowWrite = "workflow:write";
        public const string WorkflowExecute = "workflow:execute";
        public const string WorkflowDelete = "workflow:delete";
        public const string SystemAdmin = "system:admin";
        public const string SystemMonitor = "system:monitor";
        public const string AIAccess = "ai:access";
        public const string AIAdmin = "ai:admin";
    }
    // Role constants
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string User = "User";
        public const string Viewer = "Viewer";
        public const string Developer = "Developer";
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public AuthorizationService(ILogger<AuthorizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Check if user has required role
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="requiredRole">Required role</param>
    /// <returns>Authorization result</returns>
    public async Task<bool> HasRoleAsync(ClaimsPrincipal user, string requiredRole)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            var userRoles = await GetUserRolesAsync(user);
            var hasRole = userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Role check for user {UserId}: Required={RequiredRole}, HasRole={HasRole}",
                GetUserId(user), requiredRole, hasRole);
            return hasRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role {RequiredRole} for user {UserId}",
                requiredRole, GetUserId(user));
            return false;
        }
    }
    /// <summary>
    /// Check if user has required permission
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="requiredPermission">Required permission</param>
    /// <returns>Authorization result</returns>
    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string requiredPermission)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            // Administrators have all permissions
            if (await HasRoleAsync(user, Roles.Administrator))
            {
                return true;
            }
            var userPermissions = await GetUserPermissionsAsync(user);
            var hasPermission = userPermissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Permission check for user {UserId}: Required={RequiredPermission}, HasPermission={HasPermission}",
                GetUserId(user), requiredPermission, hasPermission);
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {RequiredPermission} for user {UserId}",
                requiredPermission, GetUserId(user));
            return false;
        }
    }
    /// <summary>
    /// Check if user can access agent
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="agentId">Agent ID
    /// <param name="operation">Operation type</param>
    /// <returns>Authorization result</returns>
    public async Task<bool> CanAccessAgentAsync(ClaimsPrincipal user, string agentId, string operation)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            var requiredPermission = operation.ToLowerInvariant() switch
            {
                "read" or "get" or "view" => Permissions.AgentRead,
                "write" or "create" or "update" => Permissions.AgentWrite,
                "execute" or "run" => Permissions.AgentExecute,
                "delete" => Permissions.AgentDelete,
                _ => Permissions.AgentRead
            };
            var hasPermission = await HasPermissionAsync(user, requiredPermission);
            _logger.LogDebug("Agent access check for user {UserId}: AgentId={AgentId}, Operation={Operation}, HasAccess={HasAccess}",
                GetUserId(user), agentId, operation, hasPermission);
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking agent access for user {UserId}, agent {AgentId}, operation {Operation}",
                GetUserId(user), agentId, operation);
            return false;
        }
    }
    /// <summary>
    /// Check if user can access workflow
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <param name="workflowId">Workflow ID
    /// <param name="operation">Operation type</param>
    /// <returns>Authorization result</returns>
    public async Task<bool> CanAccessWorkflowAsync(ClaimsPrincipal user, string workflowId, string operation)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            var requiredPermission = operation.ToLowerInvariant() switch
            {
                "read" or "get" or "view" => Permissions.WorkflowRead,
                "write" or "create" or "update" => Permissions.WorkflowWrite,
                "execute" or "run" => Permissions.WorkflowExecute,
                "delete" => Permissions.WorkflowDelete,
                _ => Permissions.WorkflowRead
            };
            var hasPermission = await HasPermissionAsync(user, requiredPermission);
            _logger.LogDebug("Workflow access check for user {UserId}: WorkflowId={WorkflowId}, Operation={Operation}, HasAccess={HasAccess}",
                GetUserId(user), workflowId, operation, hasPermission);
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking workflow access for user {UserId}, workflow {WorkflowId}, operation {Operation}",
                GetUserId(user), workflowId, operation);
            return false;
        }
    }
    /// <summary>
    /// Get user permissions
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <returns>List of permissions</returns>
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(ClaimsPrincipal user)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Enumerable.Empty<string>();
            }
            var permissions = user.FindAll("permission").Select(c => c.Value).ToList();
            // Add role-based permissions
            var roles = await GetUserRolesAsync(user);
            foreach (var role in roles)
            {
                permissions.AddRange(GetRolePermissions(role));
            }
            return permissions.Distinct(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", GetUserId(user));
            return Enumerable.Empty<string>();
        }
    }
    /// <summary>
    /// Get user roles
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <returns>List of roles</returns>
    public async Task<IEnumerable<string>> GetUserRolesAsync(ClaimsPrincipal user)
    {
        try
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Enumerable.Empty<string>();
            }
            await Task.CompletedTask; // For async consistency
            return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", GetUserId(user));
            return Enumerable.Empty<string>();
        }
    }
    /// <summary>
    /// Get permissions for a role
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>List of permissions</returns>
    private static IEnumerable<string> GetRolePermissions(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "administrator" => new[]
            {
                Permissions.AgentRead, Permissions.AgentWrite, Permissions.AgentExecute, Permissions.AgentDelete,
                Permissions.WorkflowRead, Permissions.WorkflowWrite, Permissions.WorkflowExecute, Permissions.WorkflowDelete,
                Permissions.SystemAdmin, Permissions.SystemMonitor,
                Permissions.AIAccess, Permissions.AIAdmin
            },
            "developer" => new[]
            {
                Permissions.AgentRead, Permissions.AgentWrite, Permissions.AgentExecute,
                Permissions.WorkflowRead, Permissions.WorkflowWrite, Permissions.WorkflowExecute,
                Permissions.SystemMonitor,
                Permissions.AIAccess
            },
            "user" => new[]
            {
                Permissions.AgentRead, Permissions.AgentExecute,
                Permissions.WorkflowRead, Permissions.WorkflowExecute,
                Permissions.AIAccess
            },
            "viewer" => new[]
            {
                Permissions.AgentRead,
                Permissions.WorkflowRead,
                Permissions.SystemMonitor
            },
            _ => Enumerable.Empty<string>()
        };
    }
    /// <summary>
    /// Get user ID from claimsID
    /// </summary>
    /// <param name="user">User claims principal</param>
    /// <returns>User ID
    private static string GetUserId(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user?.FindFirst("user_id")?.Value ??
               "unknown";
    }
}

