// PositionCacheService.cs — SQLite cache for positions with change detection

using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace AutoRevOption.Shared.Portal;

/// <summary>
/// Local SQLite cache for positions - enables fast MCP responses and offline analysis
/// Updates only when positions change (change detection via hash comparison)
/// </summary>
public class PositionCacheService : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public PositionCacheService(string? dbPath = null)
    {
        // Default: store in user's app data
        _dbPath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoRevOption",
            "positions.db"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize database
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        InitializeDatabase();

        Console.WriteLine($"[PositionCache] Database initialized at {_dbPath}");
    }

    private void InitializeDatabase()
    {
        var createTableCmd = _connection.CreateCommand();
        createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Positions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Account TEXT NOT NULL,
                Symbol TEXT NOT NULL,
                SecType TEXT NOT NULL,
                Right TEXT,
                Strike REAL,
                Expiry TEXT,
                Position REAL NOT NULL,
                AvgCost REAL,
                MarketPrice REAL,
                MarketValue REAL,
                UnrealizedPnL REAL,
                RealizedPnL REAL,
                LastUpdated TEXT NOT NULL,
                DataHash TEXT NOT NULL,
                UNIQUE(Account, Symbol, SecType, Right, Strike, Expiry)
            );

            CREATE INDEX IF NOT EXISTS idx_positions_account ON Positions(Account);
            CREATE INDEX IF NOT EXISTS idx_positions_symbol ON Positions(Symbol);
            CREATE INDEX IF NOT EXISTS idx_positions_updated ON Positions(LastUpdated);

            CREATE TABLE IF NOT EXISTS CacheMetadata (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
        ";
        createTableCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Update positions cache - only writes if data has changed
    /// Returns true if cache was updated, false if no changes detected
    /// </summary>
    public bool UpdatePositions(List<PositionInfo> positions, string accountId)
    {
        if (!positions.Any())
        {
            Console.WriteLine("[PositionCache] No positions to cache");
            return false;
        }

        // Calculate hash of current positions for change detection
        var currentHash = CalculatePositionsHash(positions);

        // Check if data has changed
        var lastHash = GetMetadata("positions_hash");
        if (currentHash == lastHash)
        {
            Console.WriteLine("[PositionCache] No changes detected - skipping update");
            return false;
        }

        Console.WriteLine($"[PositionCache] Change detected - updating {positions.Count} positions");

        using var transaction = _connection.BeginTransaction();

        try
        {
            // Clear existing positions for this account
            var deleteCmd = _connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Positions WHERE Account = $account";
            deleteCmd.Parameters.AddWithValue("$account", accountId);
            deleteCmd.ExecuteNonQuery();

            // Insert new positions
            var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Positions
                (Account, Symbol, SecType, Right, Strike, Expiry, Position, AvgCost,
                 MarketPrice, MarketValue, UnrealizedPnL, RealizedPnL, LastUpdated, DataHash)
                VALUES
                ($account, $symbol, $secType, $right, $strike, $expiry, $position, $avgCost,
                 $marketPrice, $marketValue, $unrealizedPnL, $realizedPnL, $lastUpdated, $dataHash)
            ";

            foreach (var pos in positions)
            {
                insertCmd.Parameters.Clear();
                insertCmd.Parameters.AddWithValue("$account", pos.Account);
                insertCmd.Parameters.AddWithValue("$symbol", pos.Symbol);
                insertCmd.Parameters.AddWithValue("$secType", pos.SecType);
                insertCmd.Parameters.AddWithValue("$right", pos.Right ?? string.Empty);
                insertCmd.Parameters.AddWithValue("$strike", pos.Strike);
                insertCmd.Parameters.AddWithValue("$expiry", pos.Expiry ?? string.Empty);
                insertCmd.Parameters.AddWithValue("$position", (double)pos.Position);
                insertCmd.Parameters.AddWithValue("$avgCost", pos.AvgCost);
                insertCmd.Parameters.AddWithValue("$marketPrice", pos.MarketPrice);
                insertCmd.Parameters.AddWithValue("$marketValue", pos.MarketValue);
                insertCmd.Parameters.AddWithValue("$unrealizedPnL", pos.UnrealizedPnL);
                insertCmd.Parameters.AddWithValue("$realizedPnL", pos.RealizedPnL);
                insertCmd.Parameters.AddWithValue("$lastUpdated", DateTime.UtcNow.ToString("O"));
                insertCmd.Parameters.AddWithValue("$dataHash", currentHash);
                insertCmd.ExecuteNonQuery();
            }

            // Update metadata
            SetMetadata("positions_hash", currentHash);
            SetMetadata("last_update", DateTime.UtcNow.ToString("O"));
            SetMetadata("position_count", positions.Count.ToString());

            transaction.Commit();
            Console.WriteLine($"[PositionCache] ✅ Updated {positions.Count} positions");
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"[PositionCache] ❌ Update failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get cached positions (fast - no API call)
    /// </summary>
    public List<PositionInfo> GetCachedPositions(string? accountFilter = null)
    {
        var positions = new List<PositionInfo>();

        var query = _connection.CreateCommand();
        if (!string.IsNullOrEmpty(accountFilter))
        {
            query.CommandText = "SELECT * FROM Positions WHERE Account = $account ORDER BY Symbol";
            query.Parameters.AddWithValue("$account", accountFilter);
        }
        else
        {
            query.CommandText = "SELECT * FROM Positions ORDER BY Symbol";
        }

        using var reader = query.ExecuteReader();
        while (reader.Read())
        {
            positions.Add(new PositionInfo
            {
                Account = reader.GetString(reader.GetOrdinal("Account")),
                Symbol = reader.GetString(reader.GetOrdinal("Symbol")),
                SecType = reader.GetString(reader.GetOrdinal("SecType")),
                Right = reader.GetString(reader.GetOrdinal("Right")),
                Strike = reader.GetDouble(reader.GetOrdinal("Strike")),
                Expiry = reader.GetString(reader.GetOrdinal("Expiry")),
                Position = (decimal)reader.GetDouble(reader.GetOrdinal("Position")),
                AvgCost = reader.GetDouble(reader.GetOrdinal("AvgCost")),
                MarketPrice = reader.GetDouble(reader.GetOrdinal("MarketPrice")),
                MarketValue = reader.GetDouble(reader.GetOrdinal("MarketValue")),
                UnrealizedPnL = reader.GetDouble(reader.GetOrdinal("UnrealizedPnL")),
                RealizedPnL = reader.GetDouble(reader.GetOrdinal("RealizedPnL"))
            });
        }

        return positions;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStats GetStats()
    {
        var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Positions";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());

        var lastUpdate = GetMetadata("last_update");
        var hash = GetMetadata("positions_hash");

        return new CacheStats
        {
            PositionCount = count,
            LastUpdate = lastUpdate != null ? DateTime.Parse(lastUpdate) : null,
            CurrentHash = hash,
            DatabasePath = _dbPath
        };
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public void Clear()
    {
        var deleteCmd = _connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM Positions; DELETE FROM CacheMetadata;";
        deleteCmd.ExecuteNonQuery();
        Console.WriteLine("[PositionCache] Cache cleared");
    }

    private string CalculatePositionsHash(List<PositionInfo> positions)
    {
        // Sort positions for consistent hashing
        var sortedPositions = positions
            .OrderBy(p => p.Account)
            .ThenBy(p => p.Symbol)
            .ThenBy(p => p.SecType)
            .ToList();

        // Create deterministic string representation
        var jsonString = JsonSerializer.Serialize(sortedPositions, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Calculate hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jsonString));
        return Convert.ToBase64String(hashBytes);
    }

    private string? GetMetadata(string key)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM CacheMetadata WHERE Key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar()?.ToString();
    }

    private void SetMetadata(string key, string value)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO CacheMetadata (Key, Value, UpdatedAt)
            VALUES ($key, $value, $updatedAt)
        ";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _disposed = true;
    }
}

public class CacheStats
{
    public int PositionCount { get; set; }
    public DateTime? LastUpdate { get; set; }
    public string? CurrentHash { get; set; }
    public string DatabasePath { get; set; } = string.Empty;
}
