// IbkrClient.cs — IBKR Client Portal connection (WP01)
// Handles auth, heartbeat, account snapshot

using System.Text.Json;

namespace AutoRevOption;

public record IbkrConfig(string Host, int Port, int ClientId);
public record IbkrAccountInfo(decimal NetLiq, decimal Cash, decimal BuyingPower, decimal MaintMargin, decimal MaintPct);
public record IbkrPosition(string Symbol, string Right, decimal Strike, DateOnly Expiry, int Quantity, decimal AvgCost);

public interface IIbkrClient
{
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task<bool> HeartbeatAsync(CancellationToken ct = default);
    Task<IbkrAccountInfo> GetAccountSnapshotAsync(string accountId, CancellationToken ct = default);
    Task<List<IbkrPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default);
    Task DisconnectAsync();
}

/// <summary>
/// Mock IBKR client for WP01 demo. Replace with real Client Portal API integration.
/// </summary>
public class MockIbkrClient : IIbkrClient
{
    private readonly IbkrConfig _config;
    private bool _connected;
    private DateTime _lastHeartbeat;

    public MockIbkrClient(IbkrConfig config)
    {
        _config = config;
        _connected = false;
    }

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"[IBKR] Connecting to {_config.Host}:{_config.Port} (ClientId: {_config.ClientId})...");

        // Simulate connection delay
        Thread.Sleep(500);

        _connected = true;
        _lastHeartbeat = DateTime.UtcNow;

        Console.WriteLine("[IBKR] ✅ Connected successfully");
        return Task.FromResult(true);
    }

    public Task<bool> HeartbeatAsync(CancellationToken ct = default)
    {
        if (!_connected)
        {
            Console.WriteLine("[IBKR] ❌ Not connected - heartbeat failed");
            return Task.FromResult(false);
        }

        _lastHeartbeat = DateTime.UtcNow;
        var elapsed = (DateTime.UtcNow - _lastHeartbeat).TotalSeconds;

        Console.WriteLine($"[IBKR] 💓 Heartbeat OK (last: {elapsed:F1}s ago)");
        return Task.FromResult(true);
    }

    public Task<IbkrAccountInfo> GetAccountSnapshotAsync(string accountId, CancellationToken ct = default)
    {
        if (!_connected)
            throw new InvalidOperationException("Not connected to IBKR");

        Console.WriteLine($"[IBKR] Fetching account snapshot for {accountId}...");

        // Mock account data
        var snapshot = new IbkrAccountInfo(
            NetLiq: 31250.00m,
            Cash: 18500.00m,
            BuyingPower: 62500.00m,
            MaintMargin: 7200.00m,
            MaintPct: 0.23m
        );

        Console.WriteLine($"[IBKR] Account: NetLiq=${snapshot.NetLiq:N2}, Cash=${snapshot.Cash:N2}, Maint%={snapshot.MaintPct:P1}");

        return Task.FromResult(snapshot);
    }

    public Task<List<IbkrPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default)
    {
        if (!_connected)
            throw new InvalidOperationException("Not connected to IBKR");

        Console.WriteLine($"[IBKR] Fetching positions for {accountId}...");

        // Mock positions
        var positions = new List<IbkrPosition>
        {
            new("SOFI", "PUT", 22m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), -2, 0.34m),
            new("SOFI", "PUT", 21m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 2, 0.12m)
        };

        Console.WriteLine($"[IBKR] Found {positions.Count} positions");

        return Task.FromResult(positions);
    }

    public Task DisconnectAsync()
    {
        if (_connected)
        {
            Console.WriteLine("[IBKR] Disconnecting...");
            _connected = false;
            Console.WriteLine("[IBKR] Disconnected");
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Real IBKR Client Portal implementation (stub for future WP).
/// </summary>
public class IbkrClientPortal : IIbkrClient
{
    private readonly IbkrConfig _config;
    private readonly HttpClient _http;

    public IbkrClientPortal(IbkrConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            BaseAddress = new Uri($"https://{config.Host}:{config.Port}")
        };
    }

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        // TODO: Implement Client Portal SSO/auth flow
        // - POST /v1/api/iserver/auth/ssodh/init
        // - Handle 2FA if needed
        // - Store session cookies
        throw new NotImplementedException("Real IBKR connection not yet implemented - use MockIbkrClient for WP01");
    }

    public Task<bool> HeartbeatAsync(CancellationToken ct = default)
    {
        // TODO: POST /v1/api/tickle
        throw new NotImplementedException();
    }

    public Task<IbkrAccountInfo> GetAccountSnapshotAsync(string accountId, CancellationToken ct = default)
    {
        // TODO: GET /v1/api/portfolio/{accountId}/summary
        throw new NotImplementedException();
    }

    public Task<List<IbkrPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default)
    {
        // TODO: GET /v1/api/portfolio/{accountId}/positions
        throw new NotImplementedException();
    }

    public Task DisconnectAsync()
    {
        // TODO: POST /v1/api/logout
        _http.Dispose();
        return Task.CompletedTask;
    }
}
