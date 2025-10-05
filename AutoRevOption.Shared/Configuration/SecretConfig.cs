// SecretConfig.cs — Load secrets.json for IBKR connection

using System.Text.Json;

namespace AutoRevOption.Shared.Configuration;

public class SecretConfig
{
    public IBKRCredentials IBKRCredentials { get; set; } = new();
    public ThetaDataCredentials ThetaDataCredentials { get; set; } = new();
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
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7497;
    public int ClientId { get; set; } = 1;
    public string GatewayPath { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsPaperTrading { get; set; } = true;
    public bool AutoLaunch { get; set; } = false;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 5;
}

public class ThetaDataCredentials
{
    public string ApiKey { get; set; } = string.Empty;
}

public class TradingLimits
{
    public decimal MaxDailyRisk { get; set; }
    public int MaxPositionSize { get; set; }
}
