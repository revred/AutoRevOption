// CpApiClient.cs — REST client for IBKR Client Portal Web API
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoRevOption.Shared.Configuration;

namespace AutoRevOption.CpApi;

/// <summary>
/// Client for IBKR Client Portal Gateway REST API
/// No EWrapper callbacks, pure HTTP/WebSocket
/// </summary>
public class CpApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly Timer _tickleTimer;
    private bool _disposed;

    public CpApiClient(string baseUrl = "https://localhost:5000/v1/api")
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
    /// Check authentication status
    /// </summary>
    public async Task<AuthStatus?> GetAuthStatusAsync()
    {
        try
        {
            var response = await _http.PostAsync("/iserver/auth/status", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthStatus>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CPAPI] Auth status check failed: {ex.Message}");
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
                Console.WriteLine($"[CPAPI] Session tickle OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CPAPI] Tickle failed: {ex.Message}");
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
            Console.WriteLine($"[CPAPI] Get accounts failed: {ex.Message}");
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
            Console.WriteLine($"[CPAPI] Get positions failed: {ex.Message}");
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
            Console.WriteLine($"[CPAPI] Get account summary failed: {ex.Message}");
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
