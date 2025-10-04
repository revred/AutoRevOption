# AutoRevOption.Monitor MCP Server Setup

AutoRevOption.Monitor can run as an MCP (Model Context Protocol) server, providing Claude Desktop with **live IBKR account data** through read-only operations.

## Features

The Monitor MCP server exposes 6 tools for **real-time IBKR integration**:

1. **get_connection_status** - Check IBKR Gateway and API connection status
2. **get_account_summary** - Get account snapshot (net liq, cash, margin)
3. **get_positions** - Get all open positions (stocks + options)
4. **get_option_positions** - Get only options positions with details
5. **get_account_greeks** - Calculate portfolio-wide greeks (delta, gamma, theta, vega)
6. **check_gateway** - Check/launch IB Gateway if configured

## Difference from Minimal MCP Server

| Feature | **Minimal MCP** | **Monitor MCP** |
|---------|----------------|-----------------|
| **Backend** | MockAutoRevOption (demo data) | Live IBKR connection |
| **Account Data** | Mock ($31k NLV) | Real account balances |
| **Positions** | None | Live positions from IBKR |
| **Greeks** | Not available | Portfolio-wide greeks |
| **Gateway Check** | Not available | Gateway status + auto-launch |
| **Use Case** | Rules validation, order planning | Live monitoring, position analysis |

## Prerequisites

### 1. IB Gateway Running

The Monitor MCP server requires IB Gateway to be running and logged in.

**Check Gateway Status:**
```bash
cd /c/Code/AutoRevOption
bash scripts/check-gateway-port.sh
```

**Expected Output:**
```
✅ Port 4001 is OPEN - Custom port Gateway is running
```

### 2. Secrets Configured

Ensure [secrets.json](../secrets.json) is configured with correct port:

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 4001,
    "ClientId": 1,
    "GatewayPath": "C:\\IBKR\\ibgateway\\1040\\ibgateway.exe",
    "Username": "RevOption",
    "IsPaperTrading": false,
    "AutoLaunch": false,
    "AutoReconnect": true
  }
}
```

**IMPORTANT:** Port 4001 is used in this setup (not standard 7496/7497).

## Setup for Claude Desktop

### 1. Build the Project

```bash
cd C:\Code\AutoRevOption
dotnet build AutoRevOption.sln
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Configure Claude Desktop

Add to your Claude Desktop config file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "autorev-monitor": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Code\\AutoRevOption\\AutoRevOption.Monitor\\AutoRevOption.Monitor.csproj",
        "--",
        "--mcp"
      ]
    }
  }
}
```

**Note:** Update the path to match your installation directory.

### 3. Restart Claude Desktop

Close and reopen Claude Desktop. You should see "autorev-monitor" in the MCP servers list.

## Usage Examples

### Check Connection Status

```
Use get_connection_status to check if IBKR Gateway is running and connected.
```

**Claude Response:**
```json
{
  "gatewayRunning": true,
  "connected": true,
  "host": "127.0.0.1",
  "port": 4001,
  "clientId": 1,
  "isPaperTrading": false,
  "status": "✅ Gateway running (PID: 12345, Port: 4001 OPEN)",
  "timestamp": "2025-10-04T10:30:00Z"
}
```

### Get Account Summary

```
Get my account summary using get_account_summary.
```

**Claude Response:**
```json
{
  "accountId": "U1234567",
  "netLiquidation": 45230.50,
  "cash": 12500.00,
  "buyingPower": 90461.00,
  "maintenanceMargin": 3420.00,
  "maintenancePct": 0.0756,
  "timestamp": "2025-10-04T10:30:00Z"
}
```

### Get All Positions

```
Show me all my open positions using get_positions.
```

**Claude Response:**
```json
{
  "count": 8,
  "positions": [
    {
      "symbol": "SHOP",
      "secType": "OPT",
      "right": "P",
      "strike": 21.0,
      "expiry": "20251011",
      "position": -2,
      "avgCost": 0.38,
      "marketValue": -65.00,
      "unrealizedPnL": 11.00
    },
    ...
  ],
  "timestamp": "2025-10-04T10:30:00Z"
}
```

### Get Options Positions Only

```
Show me only my SHOP options positions using get_option_positions with ticker "SHOP".
```

**Claude Response:**
```json
{
  "count": 2,
  "ticker": "SHOP",
  "positions": [
    {
      "symbol": "SHOP",
      "right": "P",
      "strike": 21.0,
      "expiry": "20251011",
      "position": -2,
      "avgCost": 0.38,
      "marketValue": -65.00,
      "unrealizedPnL": 11.00
    },
    ...
  ],
  "timestamp": "2025-10-04T10:30:00Z"
}
```

### Get Portfolio Greeks

```
Calculate my portfolio greeks using get_account_greeks.
```

**Claude Response:**
```json
{
  "totalDelta": 0.0,
  "totalGamma": 0.0,
  "totalTheta": 0.0,
  "totalVega": 0.0,
  "optionCount": 8,
  "message": "Greeks require real-time market data subscription (not implemented in snapshot mode)",
  "positions": [...],
  "timestamp": "2025-10-04T10:30:00Z"
}
```

**Note:** Greeks calculation requires real-time market data subscription, which is not yet implemented. This will be added in a future version.

## Testing Locally

### Quick Test with Script

```bash
cd /c/Code/AutoRevOption
bash scripts/test-mcp-monitor.sh
```

### Manual Testing

**Start MCP Server:**
```bash
cd AutoRevOption.Monitor
dotnet run -- --mcp
```

**Send JSON-RPC Requests via stdin:**
```json
{"method":"initialize","params":{}}
{"method":"tools/list"}
{"method":"tools/call","params":{"name":"get_connection_status","arguments":{}}}
{"method":"tools/call","params":{"name":"get_account_summary","arguments":{}}}
{"method":"tools/call","params":{"name":"get_positions","arguments":{}}}
```

## Available Tools

### get_connection_status

Check IBKR Gateway and API connection status.

**Input:** None

**Output:**
```json
{
  "gatewayRunning": true,
  "connected": true,
  "host": "127.0.0.1",
  "port": 4001,
  "isPaperTrading": false,
  "status": "✅ Gateway running",
  "timestamp": "2025-10-04T10:30:00Z"
}
```

### get_account_summary

Get account snapshot with balances and margin.

**Input:**
```json
{
  "accountId": "All"  // Optional, defaults to "All"
}
```

**Output:**
```json
{
  "accountId": "U1234567",
  "netLiquidation": 45230.50,
  "cash": 12500.00,
  "buyingPower": 90461.00,
  "maintenanceMargin": 3420.00,
  "maintenancePct": 0.0756
}
```

### get_positions

Get all open positions (stocks + options).

**Input:** None

**Output:**
```json
{
  "count": 8,
  "positions": [
    {
      "symbol": "SHOP",
      "secType": "OPT",
      "right": "P",
      "strike": 21.0,
      "expiry": "20251011",
      "position": -2,
      "avgCost": 0.38,
      "marketValue": -65.00,
      "unrealizedPnL": 11.00
    }
  ]
}
```

### get_option_positions

Get only options positions with filtering.

**Input:**
```json
{
  "ticker": "SHOP"  // Optional, filters by ticker
}
```

**Output:**
```json
{
  "count": 2,
  "ticker": "SHOP",
  "positions": [...]
}
```

### get_account_greeks

Calculate portfolio-wide greeks from option positions.

**Input:** None

**Output:**
```json
{
  "totalDelta": 0.0,
  "totalGamma": 0.0,
  "totalTheta": 0.0,
  "totalVega": 0.0,
  "optionCount": 8,
  "message": "Greeks require real-time market data subscription",
  "positions": [...]
}
```

**Note:** Greeks calculation not yet implemented. Requires real-time market data subscription.

### check_gateway

Check if IB Gateway is running, optionally attempt auto-launch.

**Input:**
```json
{
  "autoLaunch": false  // Optional, defaults to false
}
```

**Output:**
```json
{
  "wasRunning": true,
  "isRunning": true,
  "launchAttempted": false,
  "status": "✅ Gateway running (PID: 12345, Port: 4001 OPEN)"
}
```

## Troubleshooting

### "MCP server not found"

- Check the path in claude_desktop_config.json
- Ensure .NET 9 SDK is installed
- Verify project builds: `dotnet build AutoRevOption.sln`

### "Connection timeout" / "Gateway not running"

- **Check Gateway Status:**
  ```bash
  bash scripts/check-gateway-port.sh
  ```
- **Expected Output:**
  ```
  ✅ Port 4001 is OPEN
  ```
- **If port closed:**
  1. Start IB Gateway manually
  2. Log in with your credentials
  3. Ensure API is enabled: Configure → Settings → API → Settings
  4. Add trusted IP: 127.0.0.1

### "Failed to connect to IBKR API"

- **Check API Settings in IB Gateway:**
  1. Configure → Settings → API → Settings
  2. Enable "Enable ActiveX and Socket Clients"
  3. Add trusted IP: 127.0.0.1
  4. Verify port matches secrets.json (4001 in this setup)
  5. Disable "Read-Only API" (if enabled)

### "Tool call failed" / Errors in Claude Desktop

- MCP server logs to stderr (visible in Claude Desktop logs)
- Check Claude Desktop logs: Help → View Logs
- Look for connection errors or IBKR API errors
- Verify secrets.json configuration
- Ensure Gateway is logged in (not just running)

### "Greeks not available"

This is expected. Greeks calculation requires real-time market data subscription, which is not yet implemented. This will be added in a future version.

## Security Notes

- MCP server runs **locally only** (no network exposure)
- Communication via **stdio** (no TCP/UDP ports)
- **Read-only operations** (cannot place orders)
- Connects to **live IBKR account** (not paper trading by default)
- Review secrets.json carefully (gitignored)

## Deployment Checklist

- [ ] IB Gateway installed and configured
- [ ] secrets.json created with correct credentials
- [ ] Gateway running on correct port (4001)
- [ ] API enabled in Gateway settings
- [ ] Trusted IP (127.0.0.1) added
- [ ] Monitor builds successfully: `dotnet build`
- [ ] Connection test passes: `dotnet run` (option 1)
- [ ] Claude Desktop config updated
- [ ] Claude Desktop restarted

## Next Steps

### Immediate:
- ✅ MCP server builds and runs
- ✅ 6 tools implemented
- ✅ Documentation complete
- ⏭️ Test with Claude Desktop integration
- ⏭️ Test with live IBKR connection

### Future Enhancements:
1. **Real-time Greeks** - Implement market data subscription for accurate greeks
2. **Position Analysis Tools** - Add P&L analysis, risk metrics
3. **Order Status Monitoring** - Track open/pending orders
4. **Alert System** - Notify on margin changes, position updates
5. **Historical Data** - Fetch historical account/position snapshots

---

*Generated: 2025-10-04*
*Version: 1.0.0 (Monitor MCP Server)*
