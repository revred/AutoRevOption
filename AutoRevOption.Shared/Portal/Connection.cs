// Connection.cs — IBKR connection using Client Portal API

using AutoRevOption.Shared.Configuration;

namespace AutoRevOption.Shared.Portal;

public class AccountInfo
{
    public string AccountId { get; set; } = string.Empty;
    public decimal NetLiquidation { get; set; }
    public decimal Cash { get; set; }
    public decimal BuyingPower { get; set; }
    public decimal MaintenanceMargin { get; set; }
    public decimal MaintenancePct { get; set; }
    public Dictionary<string, string> AllValues { get; set; } = new();
}

public class PositionInfo
{
    public string Account { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string SecType { get; set; } = string.Empty;
    public string Right { get; set; } = string.Empty;
    public double Strike { get; set; }
    public string Expiry { get; set; } = string.Empty;
    public decimal Position { get; set; }
    public double AvgCost { get; set; }
    public double MarketPrice { get; set; }
    public double MarketValue { get; set; }
    public double UnrealizedPnL { get; set; }
    public double RealizedPnL { get; set; }
}

/// <summary>
/// IBKR connection wrapper using Client Portal API
/// </summary>
public class Connection : IDisposable
{
    private readonly AutoRevClient _client;
    private readonly IBKRCredentials _credentials;
    private bool _isConnected;
    private bool _disposed;

    // Cached data
    private AccountInfo? _accountInfo;
    private List<PositionInfo> _positions = new();
    private string? _accountId;

    // Browser session (kept alive to avoid repeated 2FA)
    private ClientPortalBrowserLogin? _browserSession;

    // Position cache for fast offline access
    private readonly PositionCacheService _positionCache;

    public Connection(IBKRCredentials credentials)
    {
        _credentials = credentials;
        _client = new AutoRevClient("https://localhost:5000/v1/api");
        _positionCache = new PositionCacheService();
    }

    /// <summary>
    /// Create connection with pre-authenticated client (from ClientPortalLoginService)
    /// </summary>
    public Connection(AutoRevClient authenticatedClient)
    {
        _client = authenticatedClient;
        _credentials = new IBKRCredentials(); // Not needed when using pre-authenticated client
        _isConnected = true; // Assume already authenticated
        _positionCache = new PositionCacheService();
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // First check if already authenticated
            var authStatus = await _client.GetAuthStatusAsync();
            if (authStatus?.Authenticated == true && authStatus.Connected)
            {
                Console.WriteLine("[Connection] Already authenticated");
                _isConnected = true;

                // Get account ID
                var accounts = await _client.GetAccountsAsync();
                if (accounts?.Count > 0)
                {
                    _accountId = accounts[0].AccountId;
                }

                return true;
            }

            // Not authenticated - use browser automation to login
            if (string.IsNullOrEmpty(_credentials.Username) || string.IsNullOrEmpty(_credentials.Password))
            {
                Console.WriteLine("[Connection] Username or password not configured in secrets.json");
                return false;
            }

            Console.WriteLine("[Connection] Starting automated browser login...");

            // Create persistent browser session
            _browserSession = new ClientPortalBrowserLogin();
            var loginSuccess = await _browserSession.LoginAsync(
                _credentials.Username,
                _credentials.Password,
                headless: true,  // Run in headless mode (no visible browser)
                twoFactorTimeoutMinutes: 2,
                keepSessionAlive: true  // Keep browser running to maintain session
            );

            if (!loginSuccess)
            {
                Console.WriteLine("[Connection] Browser login failed");
                return false;
            }

            // Verify authentication via API
            Console.WriteLine("[Connection] Verifying API authentication...");
            await Task.Delay(2000); // Give session a moment to propagate

            var authCheck = await _client.GetAuthStatusAsync();
            if (authCheck?.Authenticated == true && authCheck.Connected)
            {
                _isConnected = true;

                // Get account ID
                var accounts = await _client.GetAccountsAsync();
                if (accounts?.Count > 0)
                {
                    _accountId = accounts[0].AccountId;
                }

                Console.WriteLine("[Connection] ✅ Successfully connected to IBKR");
                return true;
            }

            Console.WriteLine("[Connection] API authentication verification failed");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Connection] Connect failed: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    public bool IsConnected() => _isConnected;

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        if (!_isConnected || string.IsNullOrEmpty(_accountId)) return null;

        try
        {
            var summary = await _client.GetAccountSummaryAsync();
            if (summary != null)
            {
                _accountInfo = new AccountInfo
                {
                    AccountId = summary.AccountId ?? _accountId,
                    AllValues = new Dictionary<string, string>()
                };
            }

            return _accountInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Connection] GetAccountInfo failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<PositionInfo>> GetPositionsAsync()
    {
        if (!_isConnected || string.IsNullOrEmpty(_accountId)) return new List<PositionInfo>();

        try
        {
            var positions = await _client.GetPositionsAsync(_accountId);
            if (positions != null)
            {
                _positions = positions.Select(p => new PositionInfo
                {
                    Account = p.AccountId ?? _accountId,
                    Symbol = p.Ticker ?? "",
                    SecType = DetermineSecType(p),
                    Right = p.PutOrCall ?? "",
                    Strike = (double)(p.Strike ?? 0),
                    Expiry = p.Expiry ?? "",
                    Position = p.PositionSize,
                    AvgCost = (double)(p.AvgCost ?? 0),
                    MarketPrice = (double)(p.MarketPrice ?? 0),
                    MarketValue = (double)(p.MarketValue ?? 0),
                    UnrealizedPnL = (double)(p.UnrealizedPnl ?? 0),
                    RealizedPnL = (double)(p.RealizedPnl ?? 0)
                }).ToList();
            }

            // Update cache (only writes if changed)
            if (!string.IsNullOrEmpty(_accountId))
            {
                _positionCache.UpdatePositions(_positions, _accountId);
            }

            return _positions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Connection] GetPositions failed: {ex.Message}");
            return new List<PositionInfo>();
        }
    }

    private static string DetermineSecType(Position position)
    {
        if (!string.IsNullOrEmpty(position.PutOrCall))
            return "OPT";
        if (!string.IsNullOrEmpty(position.Expiry))
            return "FUT";
        return "STK";
    }

    /// <summary>
    /// Get cached positions (fast - no API call)
    /// Returns positions from local SQLite database
    /// </summary>
    public List<PositionInfo> GetCachedPositions()
    {
        return _positionCache.GetCachedPositions(_accountId);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStats GetCacheStats()
    {
        return _positionCache.GetStats();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _client?.Dispose();
        _positionCache?.Dispose();

        // Close browser session if it exists
        if (_browserSession != null)
        {
            _browserSession.Dispose();
            _browserSession = null;
        }

        _disposed = true;
    }
}
