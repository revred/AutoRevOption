// AutoRevClient.cs — REST client for IBKR Client Portal Web API
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoRevOption.Shared.Portal;

/// <summary>
/// AutoRev Client for IBKR Client Portal Gateway REST API
/// No EWrapper callbacks, pure HTTP/WebSocket
/// </summary>
public class AutoRevClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly Timer _tickleTimer;
    private bool _disposed;

    public AutoRevClient(string baseUrl = "https://localhost:5000/v1/api")
    {
        _baseUrl = baseUrl.TrimEnd('/');

        // Client Portal uses self-signed cert - accept it
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Session keep-alive every 60s
        _tickleTimer = new Timer(async _ => await TickleAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Authenticate with username and password (initiates 2FA)
    /// </summary>
    public async Task<SsoValidateResponse?> InitiateLoginAsync(string username, string password)
    {
        try
        {
            var payload = new { username, password };
            var response = await _http.PostAsJsonAsync("/iserver/auth/ssodh/init", payload);

            var result = await response.Content.ReadFromJsonAsync<SsoValidateResponse>();

            if (result != null)
            {
                Console.WriteLine($"[AutoRev] Login initiated. Check your IBKR mobile app for 2FA notification.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Login initiation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Wait for 2FA completion and check authentication status
    /// Polls for up to 'timeoutMinutes' minutes
    /// </summary>
    public async Task<bool> WaitForAuthenticationAsync(int timeoutMinutes = 2)
    {
        Console.WriteLine($"[AutoRev] Waiting up to {timeoutMinutes} minutes for 2FA approval...");

        var deadline = DateTime.Now.AddMinutes(timeoutMinutes);
        var pollInterval = TimeSpan.FromSeconds(3);

        while (DateTime.Now < deadline)
        {
            var authStatus = await GetAuthStatusAsync();

            if (authStatus?.Authenticated == true && authStatus.Connected)
            {
                Console.WriteLine($"[AutoRev] ✅ Authentication successful!");
                return true;
            }

            var remaining = (deadline - DateTime.Now).TotalSeconds;
            Console.WriteLine($"[AutoRev] Waiting for 2FA... ({remaining:F0}s remaining)");

            await Task.Delay(pollInterval);
        }

        Console.WriteLine($"[AutoRev] ❌ Authentication timeout after {timeoutMinutes} minutes");
        return false;
    }

    /// <summary>
    /// Check authentication status
    /// </summary>
    public async Task<AuthStatus?> GetAuthStatusAsync()
    {
        try
        {
            var response = await _http.PostAsync("/iserver/auth/status", null);

            // 401 or 403 means not authenticated
            if (!response.IsSuccessStatusCode)
            {
                return new AuthStatus(false, false, false, "Not authenticated");
            }

            return await response.Content.ReadFromJsonAsync<AuthStatus>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Auth status check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Keep session alive
    /// </summary>
    public async Task TickleAsync()
    {
        try
        {
            var response = await _http.PostAsync("/tickle", null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AutoRev] Session tickle OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Tickle failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all portfolio accounts
    /// </summary>
    public async Task<List<PortfolioAccount>?> GetAccountsAsync()
    {
        try
        {
            var response = await _http.GetAsync("/portfolio/accounts");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PortfolioAccount>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Get accounts failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get positions for account
    /// </summary>
    public async Task<List<Position>?> GetPositionsAsync(string accountId)
    {
        try
        {
            var response = await _http.GetAsync($"/portfolio/{accountId}/positions/0");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Position>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Get positions failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get account summary
    /// </summary>
    public async Task<AccountSummary?> GetAccountSummaryAsync()
    {
        try
        {
            var response = await _http.GetAsync("/iserver/account");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AccountSummary>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRev] Get account summary failed: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tickleTimer?.Dispose();
        _http?.Dispose();
        _disposed = true;
    }
}

// Response models
public record SsoValidateResponse(
    [property: JsonPropertyName("challenge")] string? Challenge,
    [property: JsonPropertyName("authenticated")] bool Authenticated,
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("competing")] bool Competing,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("MAC")] string? Mac,
    [property: JsonPropertyName("fail")] string? Fail
);

public record AuthStatus(
    [property: JsonPropertyName("authenticated")] bool Authenticated,
    [property: JsonPropertyName("competing")] bool Competing,
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("message")] string? Message
);

public record PortfolioAccount(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("accountId")] string AccountId,
    [property: JsonPropertyName("accountVan")] string? AccountVan,
    [property: JsonPropertyName("accountTitle")] string? AccountTitle,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("accountAlias")] string? AccountAlias,
    [property: JsonPropertyName("accountStatus")] string? AccountStatus,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("tradingType")] string? TradingType
);

public record Position(
    [property: JsonPropertyName("acctId")] string? AccountId,
    [property: JsonPropertyName("conid")] int ConId,
    [property: JsonPropertyName("contractDesc")] string? ContractDesc,
    [property: JsonPropertyName("position")] decimal PositionSize,
    [property: JsonPropertyName("mktPrice")] decimal? MarketPrice,
    [property: JsonPropertyName("mktValue")] decimal? MarketValue,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("avgCost")] decimal? AvgCost,
    [property: JsonPropertyName("avgPrice")] decimal? AvgPrice,
    [property: JsonPropertyName("realizedPnl")] decimal? RealizedPnl,
    [property: JsonPropertyName("unrealizedPnl")] decimal? UnrealizedPnl,
    [property: JsonPropertyName("exchs")] string? Exchange,
    [property: JsonPropertyName("expiry")] string? Expiry,
    [property: JsonPropertyName("putOrCall")] string? PutOrCall,
    [property: JsonPropertyName("multiplier")] decimal? Multiplier,
    [property: JsonPropertyName("strike")] decimal? Strike,
    [property: JsonPropertyName("exerciseStyle")] string? ExerciseStyle,
    [property: JsonPropertyName("ticker")] string? Ticker,
    [property: JsonPropertyName("undConid")] int? UnderlyingConId,
    [property: JsonPropertyName("model")] string? Model
);

public record AccountSummary(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("accountId")] string? AccountId,
    [property: JsonPropertyName("accountVan")] string? AccountVan,
    [property: JsonPropertyName("accountTitle")] string? AccountTitle,
    [property: JsonPropertyName("accountAlias")] string? AccountAlias,
    [property: JsonPropertyName("accountStatus")] string? AccountStatus,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("tradingType")] string? TradingType,
    [property: JsonPropertyName("faclient")] bool? FaClient,
    [property: JsonPropertyName("clearingStatus")] string? ClearingStatus,
    [property: JsonPropertyName("covestor")] bool? Covestor,
    [property: JsonPropertyName("parent")] JsonElement? Parent,
    [property: JsonPropertyName("desc")] string? Description
);
