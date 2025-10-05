// ClientPortalLoginService.cs — Secure login service for IBKR Client Portal
// Manages secrets internally and provides authenticated API client

using System.Text.Json;
using AutoRevOption.Shared.Portal;

namespace AutoRevOption.Client;

/// <summary>
/// Provides secure login services for IBKR Client Portal
/// Manages credentials internally and returns authenticated API client
/// </summary>
public class ClientPortalLoginService : IDisposable
{
    private readonly string _secretsPath;
    private ClientPortalBrowserLogin? _browserSession;
    private AutoRevClient? _apiClient;
    private ClientCredentials? _credentials;
    private bool _disposed;

    public ClientPortalLoginService(string? secretsFilePath = null)
    {
        // Default to secrets.json in the Client library folder
        _secretsPath = secretsFilePath ?? Path.Combine(
            Path.GetDirectoryName(typeof(ClientPortalLoginService).Assembly.Location) ?? "",
            "..", "..", "..", "secrets.json"
        );
    }

    /// <summary>
    /// Authenticate and get API client
    /// Handles gateway startup, browser automation and 2FA internally
    /// </summary>
    public async Task<AutoRevClient> LoginAsync(bool headless = true, int twoFactorTimeoutMinutes = 2)
    {
        // Ensure Client Portal Gateway is running (persistent background service)
        var gatewayManager = GatewayProcessManager.Instance;
        var gatewayRunning = await gatewayManager.EnsureGatewayRunningAsync();

        if (!gatewayRunning)
        {
            throw new Exception("Failed to start Client Portal Gateway");
        }

        // Load credentials from secrets file
        if (_credentials == null)
        {
            _credentials = LoadCredentials();
        }

        // Check if already authenticated
        _apiClient = new AutoRevClient("https://localhost:5000/v1/api");
        var authStatus = await _apiClient.GetAuthStatusAsync();

        if (authStatus?.Authenticated == true && authStatus.Connected)
        {
            Console.WriteLine("[LoginService] Already authenticated");
            return _apiClient;
        }

        // Perform automated browser login
        Console.WriteLine("[LoginService] Starting automated browser login...");
        _browserSession = new ClientPortalBrowserLogin();

        var loginSuccess = await _browserSession.LoginAsync(
            _credentials.Username,
            _credentials.Password,
            headless: headless,
            twoFactorTimeoutMinutes: twoFactorTimeoutMinutes,
            keepSessionAlive: true  // Keep browser running for session persistence
        );

        if (!loginSuccess)
        {
            throw new Exception("Browser login failed");
        }

        // Verify API authentication
        await Task.Delay(5000); // Wait for session to propagate

        var verifyAuth = await _apiClient.GetAuthStatusAsync();
        if (verifyAuth?.Authenticated != true || !verifyAuth.Connected)
        {
            throw new Exception("API authentication verification failed");
        }

        Console.WriteLine("[LoginService] ✅ Login successful");
        return _apiClient;
    }

    /// <summary>
    /// Get the authenticated API client (must call LoginAsync first)
    /// </summary>
    public AutoRevClient GetApiClient()
    {
        if (_apiClient == null)
        {
            throw new InvalidOperationException("Not logged in. Call LoginAsync first.");
        }

        return _apiClient;
    }

    /// <summary>
    /// Check if browser session is still active
    /// </summary>
    public bool IsSessionAlive()
    {
        return _browserSession?.IsSessionAlive() ?? false;
    }

    /// <summary>
    /// Load credentials from secrets.json
    /// </summary>
    private ClientCredentials LoadCredentials()
    {
        var resolvedPath = Path.GetFullPath(_secretsPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"secrets.json not found at: {resolvedPath}\n" +
                "Please create secrets.json in AutoRevOption.Client folder with:\n" +
                "{\n" +
                "  \"IBKRCredentials\": {\n" +
                "    \"Username\": \"your_username\",\n" +
                "    \"Password\": \"your_password\"\n" +
                "  }\n" +
                "}"
            );
        }

        var json = File.ReadAllText(resolvedPath);
        var config = JsonSerializer.Deserialize<SecretsConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config?.IBKRCredentials == null)
        {
            throw new InvalidOperationException("IBKRCredentials not found in secrets.json");
        }

        if (string.IsNullOrEmpty(config.IBKRCredentials.Username) ||
            string.IsNullOrEmpty(config.IBKRCredentials.Password))
        {
            throw new InvalidOperationException("Username or Password not configured in secrets.json");
        }

        return config.IBKRCredentials;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _apiClient?.Dispose();
        _browserSession?.Dispose();

        _disposed = true;
    }

    // Internal models for secrets management
    private class SecretsConfig
    {
        public ClientCredentials? IBKRCredentials { get; set; }
    }

    private class ClientCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
