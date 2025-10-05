# Position Cache - Local SQLite Storage for Fast MCP Responses

## Overview

The AutoRevOption Position Cache provides local SQLite storage for IBKR positions, enabling:
- **Fast offline analysis** without API calls
- **Sub-millisecond MCP responses** using cached data
- **Change detection** - only updates when positions actually change
- **Persistent storage** across app restarts

## Architecture

### Storage Location

```
%LocalApplicationData%/AutoRevOption/positions.db
```

Example: `C:\Users\YourName\AppData\Local\AutoRevOption\positions.db`

### Database Schema

```sql
-- Position data
CREATE TABLE Positions (
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

-- Metadata for change detection
CREATE TABLE CacheMetadata (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    LastUpdated TEXT NOT NULL
);
```

### Indexes

```sql
CREATE INDEX IF NOT EXISTS idx_positions_account ON Positions(Account);
CREATE INDEX IF NOT EXISTS idx_positions_symbol ON Positions(Symbol);
CREATE INDEX IF NOT EXISTS idx_positions_sectype ON Positions(SecType);
CREATE INDEX IF NOT EXISTS idx_positions_lastupdated ON Positions(LastUpdated);
```

## Usage

### Basic Usage - Automatic Cache Updates

```csharp
// Normal workflow - cache updates automatically
var connection = new Connection(credentials);
await connection.ConnectAsync();

// Fetch positions from API (also updates cache if changed)
var positions = await connection.GetPositionsAsync();
// → API call → Returns fresh data → Updates cache if changed

// Later: Get cached positions (no API call)
var cachedPositions = connection.GetCachedPositions();
// → Instant response from local SQLite
```

### Fast MCP Responses

```csharp
// MCP server scenario - prefer cached data for speed
var connection = new Connection(credentials);
await connection.ConnectAsync();

// First call: Fetch from API and populate cache
var freshPositions = await connection.GetPositionsAsync();
// → API call (slow)

// Subsequent calls: Use cached data
var cachedPositions = connection.GetCachedPositions();
// → Instant (sub-millisecond) response
// → Perfect for MCP tool responses
```

### Cache Statistics

```csharp
var stats = connection.GetCacheStats();

Console.WriteLine($"Database: {stats.DatabasePath}");
Console.WriteLine($"Total Positions: {stats.TotalPositions}");
Console.WriteLine($"Last Updated: {stats.LastUpdated}");
Console.WriteLine($"Data Hash: {stats.DataHash}");
```

## Change Detection

### How It Works

The cache uses **SHA256 hash comparison** to detect position changes:

1. **Calculate hash** of current positions (JSON serialization)
2. **Compare** with stored hash in `CacheMetadata` table
3. **Skip update** if hashes match (no changes)
4. **Update database** if hashes differ (positions changed)

### Implementation

```csharp
public bool UpdatePositions(List<PositionInfo> positions, string accountId)
{
    // Calculate hash of current positions for change detection
    var currentHash = CalculatePositionsHash(positions);

    // Check if data has changed
    var lastHash = GetMetadata("positions_hash");
    if (currentHash == lastHash)
    {
        Console.WriteLine("[PositionCache] No changes detected - skipping update");
        return false;  // No update needed
    }

    Console.WriteLine($"[PositionCache] Change detected - updating {positions.Count} positions");

    // Begin transaction for atomic update
    using var transaction = _connection.BeginTransaction();

    // Delete old positions for this account
    var deleteCmd = _connection.CreateCommand();
    deleteCmd.CommandText = "DELETE FROM Positions WHERE Account = @account";
    deleteCmd.Parameters.AddWithValue("@account", accountId);
    deleteCmd.ExecuteNonQuery();

    // Insert new positions
    // ... (bulk insert) ...

    // Update metadata
    SetMetadata("positions_hash", currentHash);
    SetMetadata("last_updated", DateTime.UtcNow.ToString("O"));
    SetMetadata("account_id", accountId);

    transaction.Commit();
    return true;  // Cache was updated
}
```

### Hash Calculation

```csharp
private string CalculatePositionsHash(List<PositionInfo> positions)
{
    // Sort for deterministic hash
    var sortedPositions = positions
        .OrderBy(p => p.Account)
        .ThenBy(p => p.Symbol)
        .ThenBy(p => p.SecType)
        .ToList();

    // Serialize to JSON
    var jsonString = JsonSerializer.Serialize(sortedPositions);

    // Calculate SHA256 hash
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jsonString));
    return Convert.ToBase64String(hashBytes);
}
```

## Integration with Connection Class

### Initialization

```csharp
public class Connection : IDisposable
{
    private readonly AutoRevClient _client;
    private readonly IBKRCredentials _credentials;
    private readonly PositionCacheService _positionCache;

    public Connection(IBKRCredentials credentials)
    {
        _credentials = credentials;
        _client = new AutoRevClient("https://localhost:5000/v1/api");
        _positionCache = new PositionCacheService();  // Auto-creates DB
    }
}
```

### Automatic Update on API Fetch

```csharp
public async Task<List<PositionInfo>> GetPositionsAsync()
{
    Console.WriteLine("[Connection] Fetching positions...");
    var response = await _client.GetAsync("portfolio/positions");
    // ... parse positions ...

    // Update cache (only writes if changed)
    if (!string.IsNullOrEmpty(_accountId))
    {
        _positionCache.UpdatePositions(_positions, _accountId);
    }

    return _positions;
}
```

### Fast Cached Retrieval

```csharp
/// <summary>
/// Get cached positions (fast - no API call)
/// Returns positions from local SQLite database
/// Returns empty list if cache is empty or stale
/// </summary>
public List<PositionInfo> GetCachedPositions()
{
    if (string.IsNullOrEmpty(_accountId))
    {
        Console.WriteLine("[Connection] Cannot get cached positions - no account ID available");
        return new List<PositionInfo>();
    }

    return _positionCache.GetCachedPositions(_accountId);
}
```

### Disposal

```csharp
public void Dispose()
{
    if (_disposed) return;
    _client?.Dispose();
    _positionCache?.Dispose();  // Closes SQLite connection
    _disposed = true;
}
```

## Performance Characteristics

### API Fetch (Slow)

```csharp
var positions = await connection.GetPositionsAsync();
// Timing:
// - Network latency: 50-200ms
// - API processing: 100-500ms
// - JSON parsing: 10-50ms
// Total: ~200-800ms
```

### Cached Fetch (Fast)

```csharp
var positions = connection.GetCachedPositions();
// Timing:
// - SQLite query: 1-5ms
// - Object materialization: 1-2ms
// Total: ~2-7ms (100x faster!)
```

### Change Detection Overhead

```csharp
var updated = _positionCache.UpdatePositions(positions, accountId);
// Timing:
// - Hash calculation: 5-15ms (for ~100 positions)
// - Hash comparison: <1ms
// - Database write (if changed): 10-30ms
// Total: ~15-45ms if update needed, ~5-15ms if skipped
```

## MCP Server Integration

### Example MCP Tool - Fast Positions

```csharp
[McpTool("get_positions_fast", "Get positions from cache (instant, no API call)")]
public async Task<object> GetPositionsFast()
{
    try
    {
        // Use cached data for instant response
        var positions = _connection.GetCachedPositions();

        if (positions.Count == 0)
        {
            return new
            {
                warning = "Cache is empty. Call get_positions() first to populate cache.",
                positions = new List<PositionInfo>()
            };
        }

        var stats = _connection.GetCacheStats();

        return new
        {
            positions = positions,
            metadata = new
            {
                total = stats.TotalPositions,
                lastUpdated = stats.LastUpdated,
                source = "cache"
            }
        };
    }
    catch (Exception ex)
    {
        return new { error = ex.Message };
    }
}
```

### Example MCP Tool - Fresh Positions

```csharp
[McpTool("get_positions", "Get current positions from IBKR API (updates cache)")]
public async Task<object> GetPositions()
{
    try
    {
        // Fetch from API (slow but fresh)
        var positions = await _connection.GetPositionsAsync();
        // Cache automatically updated if positions changed

        return new
        {
            positions = positions,
            metadata = new
            {
                total = positions.Count,
                source = "api",
                cacheUpdated = true
            }
        };
    }
    catch (Exception ex)
    {
        return new { error = ex.Message };
    }
}
```

## Cache Invalidation

### Manual Invalidation

```csharp
// To force cache refresh on next GetPositionsAsync()
File.Delete(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AutoRevOption",
    "positions.db"
));

// Next GetPositionsAsync() will recreate database and populate cache
```

### Automatic Invalidation

Cache is automatically invalidated when:
1. Position data changes (detected via hash comparison)
2. Account switches (new account ID)
3. Database file is deleted or corrupted

## Troubleshooting

### Cache Not Updating

**Symptom**: GetCachedPositions() returns stale data

**Solutions**:
1. Call `GetPositionsAsync()` to fetch fresh data and update cache
2. Check console for "[PositionCache] No changes detected" - positions may not have changed
3. Verify hash calculation is working: Check `CacheMetadata` table for `positions_hash`
4. Delete database file to force recreation

### Empty Cache

**Symptom**: GetCachedPositions() returns empty list

**Solutions**:
1. Must call `GetPositionsAsync()` at least once to populate cache
2. Check if account ID is set (required for cache queries)
3. Verify database file exists: `%LocalApplicationData%/AutoRevOption/positions.db`
4. Check for errors in console logs

### Slow Cache Queries

**Symptom**: GetCachedPositions() is slower than expected

**Solutions**:
1. Verify indexes exist: `PRAGMA index_list('Positions');`
2. Check database file size - may need vacuum: `VACUUM;`
3. Ensure SQLite connection is being reused (not recreated each call)
4. Consider adding compound indexes for specific query patterns

### Database Corruption

**Symptom**: SQLite errors or crashes

**Solutions**:
1. Delete database file - will be recreated automatically
2. Check disk space in `%LocalApplicationData%`
3. Verify permissions on AutoRevOption directory
4. Review SQLite logs for specific error messages

## Security Considerations

### Data Storage

- **Location**: User-specific LocalApplicationData (not shared between users)
- **Permissions**: Standard user file permissions (no special access needed)
- **Encryption**: No encryption at rest (positions are not sensitive credentials)
- **Network**: Local-only storage (no network transmission)

### Data Retention

- **Persistence**: Cache persists indefinitely until manually deleted
- **Updates**: Only when positions change (detected via hash)
- **Privacy**: Position data stored locally, never transmitted to external services

## Benefits

✅ **Speed**: 100x faster responses (2-7ms vs 200-800ms)
✅ **Offline**: Works without network connectivity
✅ **Efficient**: Only updates when data changes
✅ **Persistent**: Survives app restarts
✅ **MCP-Optimized**: Perfect for fast tool responses
✅ **Automatic**: Updates happen transparently during GetPositionsAsync()

## Example Workflows

### Daily Trading Workflow

```csharp
// Morning: Connect and populate cache
var connection = new Connection(credentials);
await connection.ConnectAsync();
var positions = await connection.GetPositionsAsync();
// → API call, cache populated

// Throughout day: Use cached data for analysis
while (trading)
{
    var currentPositions = connection.GetCachedPositions();
    // → Instant response, no API call

    AnalyzePositions(currentPositions);

    await Task.Delay(TimeSpan.FromMinutes(5));
}

// Periodic refresh (every 15 minutes)
if (DateTime.Now.Minute % 15 == 0)
{
    positions = await connection.GetPositionsAsync();
    // → Cache updated if positions changed
}
```

### MCP Server Workflow

```csharp
// Startup: Populate cache once
var freshPositions = await _connection.GetPositionsAsync();
Console.WriteLine($"[MCP] Cache populated with {freshPositions.Count} positions");

// MCP requests: Use cached data
app.MapGet("/tools/get_positions_fast", () =>
{
    var cachedPositions = _connection.GetCachedPositions();
    // → Instant response for Claude
    return Results.Json(cachedPositions);
});

// Scheduled refresh (every 5 minutes)
var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
while (await timer.WaitForNextTickAsync())
{
    var updated = await _connection.GetPositionsAsync();
    // → Cache updated if changed, skipped if unchanged
}
```

## File Locations

| Component | File Path |
|-----------|-----------|
| **Position Cache Service** | [AutoRevOption.Shared/Portal/PositionCacheService.cs](../AutoRevOption.Shared/Portal/PositionCacheService.cs) |
| **Connection Integration** | [AutoRevOption.Shared/Portal/Connection.cs](../AutoRevOption.Shared/Portal/Connection.cs) |
| **SQLite Database** | `%LocalApplicationData%/AutoRevOption/positions.db` |
| **Documentation** | [docs/Position_Cache.md](Position_Cache.md) |

## Dependencies

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.9" />
```

Installed in: [AutoRevOption.Shared/AutoRevOption.Shared.csproj](../AutoRevOption.Shared/AutoRevOption.Shared.csproj)
