using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace MultiAgent.Desktop.Services;
/// <summary>
/// Authentication service interface for desktop app
/// Manages user authentication and token storage
//// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Current user information
    /// </summary>
    UserInfo? CurrentUser { get; }
    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    /// <returns>Authentication status</returns>
    Task<bool> IsAuthenticatedAsync();
    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Login result</returns>
    Task<LoginResult> LoginAsync(string username, string password);
    /// <summary>
    /// Logout user
    /// </summary>
    /// <returns>Task</returns>
    Task LogoutAsync();
    /// <summary>
    /// Get current access token
    /// </summary>
    /// <returns>Access token</returns>
    Task<string?> GetAccessTokenAsync();
    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <returns>Refresh result</returns>
    Task<bool> RefreshTokenAsync();
    // Events
    event EventHandler<UserInfo?>? UserChanged;
    event EventHandler<bool>? AuthenticationStateChanged;
}
/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _apiService;
    private readonly ILogger<AuthenticationService> _logger;
    private UserInfo? _currentUser;
    private string? _accessToken;
    private DateTime? _tokenExpiration;
    private const string UserPreferenceKey = "current_user";
    private const string TokenPreferenceKey = "access_token";
    private const string TokenExpirationKey = "token_expiration";
    /// <summary>
    /// Current user information
    /// </summary>
    public UserInfo? CurrentUser => _currentUser;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiService">API service
    /// <param name="logger">Logger</param>
    public AuthenticationService(IApiService apiService, ILogger<AuthenticationService> logger)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Load stored authentication data
        LoadStoredAuthenticationData();
    }
    #region Events /
    public event EventHandler<UserInfo?>? UserChanged;
    public event EventHandler<bool>? AuthenticationStateChanged;
    #endregion
    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    /// <returns>Authentication status</returns>
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            // Check if we have a token and it's not expired
            if (string.IsNullOrEmpty(_accessToken) || _tokenExpiration == null)
            {
                return false;
            }
            if (DateTime.UtcNow >= _tokenExpiration.Value.AddMinutes(-5)) // 5 minute buffer / 5
            {
                _logger.LogInformation("Token is expired or about to expire, attempting refresh");
                return await RefreshTokenAsync();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }
    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Login result</returns>
    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Attempting to login user {Username}", username);
            var authResult = await _apiService.AuthenticateAsync(username, password);
            if (authResult.Success && !string.IsNullOrEmpty(authResult.Token))
            {
                // Store authentication data
                _accessToken = authResult.Token;
                _tokenExpiration = authResult.ExpiresAt ?? DateTime.UtcNow.AddHours(1);
                _currentUser = JsonSerializer.Deserialize<UserInfo>(JsonSerializer.Serialize(authResult.User));
                // Set token in API serviceAPI
                _apiService.SetAuthToken(_accessToken);
                // Store in preferences
                await StoreAuthenticationDataAsync();
                // Notify authentication state change
                UserChanged?.Invoke(this, _currentUser);
                AuthenticationStateChanged?.Invoke(this, true);
                _logger.LogInformation("User {Username} logged in successfully", username);
                return new LoginResult
                {
                    Success = true,
                    User = _currentUser,
                    Token = _accessToken
                };
            }
            else
            {
                _logger.LogWarning("Login failed for user {Username}: {ErrorMessage}", username, authResult.ErrorMessage);
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = authResult.ErrorMessage ?? "Login failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "An error occurred during login. Please try again."
            };
        }
    }
    /// <summary>
    /// Logout user
    /// </summary>
    /// <returns>Task</returns>
    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user {Username}", _currentUser?.Username);
            // Clear authentication data
            _currentUser = null;
            _accessToken = null;
            _tokenExpiration = null;
            // Clear token from API serviceAPI
            _apiService.ClearAuthToken();
            // Clear stored preferences
            await ClearStoredAuthenticationDataAsync();
            // Notify authentication state change
            UserChanged?.Invoke(this, null);
            AuthenticationStateChanged?.Invoke(this, false);
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }
    /// <summary>
    /// Get current access token
    /// </summary>
    /// <returns>Access token</returns>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (await IsAuthenticatedAsync())
        {
            return _accessToken;
        }
        return null;
    }
    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <returns>Refresh result</returns>
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            // For now, we'll just check if the current token is still valid，
            // In a real implementation, you would call a refresh endpoint，
            if (string.IsNullOrEmpty(_accessToken) || _currentUser == null)
            {
                _logger.LogWarning("Cannot refresh token - no current authentication data");
                await LogoutAsync();
                return false;
            }
            // TODO: Implement actual token refresh with the backend
            // For demo purposes, we'll extend the current token expiration，
            if (_tokenExpiration != null && DateTime.UtcNow < _tokenExpiration.Value)
            {
                _logger.LogDebug("Token is still valid, no refresh needed");
                return true;
            }
            _logger.LogWarning("Token refresh not implemented, logging out user");
            await LogoutAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            await LogoutAsync();
            return false;
        }
    }
    /// <summary>
    /// Load stored authentication data
    /// </summary>
    private void LoadStoredAuthenticationData()
    {
        try
        {
            // Load user data
            if (Preferences.ContainsKey(UserPreferenceKey))
            {
                var userJson = Preferences.Get(UserPreferenceKey, string.Empty);
                if (!string.IsNullOrEmpty(userJson))
                {
                    _currentUser = JsonSerializer.Deserialize<UserInfo>(userJson);
                }
            }
            // Load token data
            if (Preferences.ContainsKey(TokenPreferenceKey))
            {
                _accessToken = Preferences.Get(TokenPreferenceKey, string.Empty);
            }
            // Load token expiration
            if (Preferences.ContainsKey(TokenExpirationKey))
            {
                var expirationTicks = Preferences.Get(TokenExpirationKey, 0L);
                if (expirationTicks > 0)
                {
                    _tokenExpiration = new DateTime(expirationTicks);
                }
            }
            // Set token in API service if available，
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _apiService.SetAuthToken(_accessToken);
                _logger.LogDebug("Loaded stored authentication data for user {Username}", _currentUser?.Username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stored authentication data");
            // Clear potentially corrupted data
            ClearStoredAuthenticationDataAsync().ConfigureAwait(false);
        }
    }
    /// <summary>
    /// Store authentication data to preferences
    /// </summary>
    /// <returns>Task</returns>
    private async Task StoreAuthenticationDataAsync()
    {
        try
        {
            if (_currentUser != null)
            {
                var userJson = JsonSerializer.Serialize(_currentUser);
                Preferences.Set(UserPreferenceKey, userJson);
            }
            if (!string.IsNullOrEmpty(_accessToken))
            {
                Preferences.Set(TokenPreferenceKey, _accessToken);
            }
            if (_tokenExpiration != null)
            {
                Preferences.Set(TokenExpirationKey, _tokenExpiration.Value.Ticks);
            }
            _logger.LogDebug("Stored authentication data for user {Username}", _currentUser?.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing authentication data");
        }
        await Task.CompletedTask;
    }
    /// <summary>
    /// Clear stored authentication data
    /// </summary>
    /// <returns>Task</returns>
    private async Task ClearStoredAuthenticationDataAsync()
    {
        try
        {
            Preferences.Remove(UserPreferenceKey);
            Preferences.Remove(TokenPreferenceKey);
            Preferences.Remove(TokenExpirationKey);
            _logger.LogDebug("Cleared stored authentication data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing stored authentication data");
        }
        await Task.CompletedTask;
    }
}
#region Models /
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
    /// Last login time
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
/// <summary>
/// Login result
/// </summary>
public class LoginResult
{
    /// <summary>
    /// Login success
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// User information
    /// </summary>
    public UserInfo? User { get; set; }
    /// <summary>
    /// Access token
    /// </summary>
    public string? Token { get; set; }
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
#endregion

