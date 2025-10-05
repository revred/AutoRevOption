// SecretConfig.cs — Load secrets.json for IBKR connection

using System.Text.Json;

namespace AutoRevOption.Shared.Configuration;

public class SecretConfig
{
    public IBKRCredentials IBKRCredentials { get; set; } = new();
    public IBKRMarketData IBKRMarketData { get; set; } = new();
    public TradingLimits TradingLimits { get; set; } = new();

    public static SecretConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Secrets file not found: {path}");

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<SecretConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return config ?? throw new InvalidOperationException("Failed to deserialize secrets.json");
    }
}

public class IBKRCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class IBKRMarketData
{
    public bool SubscriptionActive { get; set; } = false;
    public string SubscriptionType { get; set; } = string.Empty;
}

public class TradingLimits
{
    public decimal MaxDailyRisk { get; set; }
    public int MaxPositionSize { get; set; }
}
