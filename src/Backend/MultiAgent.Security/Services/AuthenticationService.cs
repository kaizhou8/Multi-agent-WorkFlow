using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
namespace MultiAgent.Security.Services;
/// <summary>
/// Authentication service
/// Provides JWT token generation and validation
//// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    /// <summary>
    /// Generate JWT tokenJWT
    /// </summary>
    /// <param name="user">User information</param>
    /// <returns>JWT token
    string GenerateJwtToken(UserInfo user);
    /// <summary>
    /// Validate JWT tokenJWT
    /// </summary>
    /// <param name="token">JWT token
    /// <returns>Token validation result</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    /// <summary>
    /// Hash password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);
    /// <summary>
    /// Verify password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Hashed password</param>
    /// <returns>Verification result</returns>
    bool VerifyPassword(string password, string hash);
}
/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="configuration">Configuration</param>
    public AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _jwtSecret = _configuration["Jwt:Secret"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "MultiAgent";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "MultiAgent";
        _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }
    /// <summary>
    /// Authenticate user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Username and password are required"
                };
            }
            // TODO: Replace with actual user store lookup
            // For demo purposes, using hardcoded admin user，
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for user {Username}: User not found", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password"
                };
            }
            if (!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed for user {Username}: Invalid password", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password"
                };
            }
            var token = GenerateJwtToken(user);
            _logger.LogInformation("User {Username} authenticated successfully", username);
            return new AuthenticationResult
            {
                Success = true,
                Token = token,
                User = user,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Authentication failed due to internal error"
            };
        }
    }
    /// <summary>
    /// Generate JWT tokenJWT
    /// </summary>
    /// <param name="user">User information</param>
    /// <returns>JWT token
    public string GenerateJwtToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("user_id", user.Id),
            new("username", user.Username)
        };
        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        // Add permission claims
        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    /// <summary>
    /// Validate JWT tokenJWT
    /// </summary>
    /// <param name="token">JWT token
    /// <returns>Token validation result</returns>
    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token is required"
                };
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(username))
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token claims"
                };
            }
            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                UserId = userId,
                Username = username
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }
    /// <summary>
    /// Hash password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }
    /// <summary>
    /// Verify password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Hashed password</param>
    /// <returns>Verification result</returns>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password verification");
            return false;
        }
    }
    /// <summary>
    /// Get user by username (demo implementation)（
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User information</returns>
    private async Task<UserInfo?> GetUserByUsernameAsync(string username)
    {
        // TODO: Replace with actual database lookup
        // Demo implementation with hardcoded admin user，
        await Task.Delay(10); // Simulate async operation
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return new UserInfo
            {
                Id = "admin-001",
                Username = "admin",
                Email = "admin@multiagent.local",
                PasswordHash = HashPassword("admin123"), // Default password for demo
                Roles = new List<string> { "Administrator", "User" },
                Permissions = new List<string> { "agent:read", "agent:write", "workflow:read", "workflow:write", "system:admin" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow
            };
        }
        if (username.Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            return new UserInfo
            {
                Id = "user-001",
                Username = "user",
                Email = "user@multiagent.local",
                PasswordHash = HashPassword("user123"), // Default password for demo
                Roles = new List<string> { "User" },
                Permissions = new List<string> { "agent:read", "workflow:read" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                LastLoginAt = DateTime.UtcNow
            };
        }
        return null;
    }
}
#region Models
/// <summary>
/// Authentication result
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Authentication success
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// JWT token
    /// </summary>
    public string? Token { get; set; }
    /// <summary>
    /// User information
    /// </summary>
    public UserInfo? User { get; set; }
    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Token is valid
    /// </summary>
    public bool IsValid { get; set; }
    /// <summary>
    /// Claims principal
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
/// <summary>
/// User information
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Password hash
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>
    /// User is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Account creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Last login time
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
#endregion

