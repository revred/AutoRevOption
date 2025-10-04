# AutoRevOption MCP Server Setup

AutoRevOption can run as an MCP (Model Context Protocol) server, allowing Claude Desktop to interact with it directly.

## Features

The MCP server exposes 6 tools:

1. **scan_candidates** - Scan for options trade opportunities
2. **validate_candidate** - Validate against OptionsRadar rules
3. **verify_candidate** - Verify against risk gates
4. **build_order_plan** - Build order with TP/SL exits
5. **get_account_status** - Get account snapshot
6. **act_on_order** - Submit order (demo only)

## Setup for Claude Desktop

### 1. Build the Project

```bash
cd C:\Code\AutoRevOption
dotnet build AutoRevOption.sln
```

### 2. Configure Claude Desktop

Add to your Claude Desktop config file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "autorev-option": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Code\\AutoRevOption\\AutoRevOption.Minimal\\AutoRevOption.Minimal.csproj",
        "--",
        "--mcp"
      ]
    }
  }
}
```

**Note:** Update the path to match your installation directory.

### 3. Restart Claude Desktop

Close and reopen Claude Desktop. You should see "autorev-option" in the MCP servers list.

## Usage Examples

### Scan for Candidates

```
Use the scan_candidates tool to find options trade opportunities in the default universe.
```

Claude will call the MCP server and return ranked candidates.

### Validate a Candidate

```
Validate candidate PCS:SOFI:2025-10-11:22-21:abcd using the validate_candidate tool.
```

### Build Order Plan

```
Build an order plan for candidate PCS:SOFI:2025-10-11:22-21:abcd with quantity 2.
```

Claude will generate an OrderPlan JSON with entry and exit brackets.

### Get Account Status

```
Get my account status using get_account_status.
```

Returns net liquidation, margin %, delta, theta.

## Testing Locally

### Quick Test with Script

Run the automated test suite:

```bash
cd /c/Code/AutoRevOption
bash scripts/test-mcp-server.sh
```

### Manual Testing

Test the MCP server directly:

```bash
cd AutoRevOption.Minimal
dotnet run -- --mcp
```

Then send JSON-RPC requests via stdio:

```json
{"method":"initialize","params":{}}
{"method":"tools/list"}
{"method":"tools/call","params":{"name":"scan_candidates","arguments":{}}}
{"method":"tools/call","params":{"name":"get_account_status","arguments":{}}}
```

**Expected Results:**
- ✅ Initialize returns protocol version and capabilities
- ✅ Tools/list returns 6 tools (scan_candidates, validate_candidate, verify_candidate, build_order_plan, get_account_status, act_on_order)
- ✅ Scan_candidates returns ranked candidates with scores
- ✅ Get_account_status returns mock account data (net liq: $31,000, delta: 38, theta: 2.1)

## Available Tools

### scan_candidates

Scan universe for trade candidates.

**Input:**
```json
{
  "universe": ["AAPL", "MSFT"]  // Optional, uses default if omitted
}
```

**Output:**
```json
{
  "count": 3,
  "candidates": [
    {
      "id": "PCS:SOFI:2025-10-11:22-21:abcd",
      "ticker": "SOFI",
      "type": "PCS",
      "score": 85
    }
  ]
}
```

### validate_candidate

Validate candidate against rules.

**Input:**
```json
{
  "candidateId": "PCS:SOFI:2025-10-11:22-21:abcd"
}
```

**Output:**
```json
{
  "valid": true,
  "issues": []
}
```

### verify_candidate

Verify against risk gates.

**Input:**
```json
{
  "candidateId": "PCS:SOFI:2025-10-11:22-21:abcd",
  "accountId": "ibkr:primary"  // Optional
}
```

**Output:**
```json
{
  "verified": true,
  "score": 85,
  "reason": "Meets demo guards"
}
```

### build_order_plan

Build order with exits.

**Input:**
```json
{
  "candidateId": "PCS:SOFI:2025-10-11:22-21:abcd",
  "quantity": 2
}
```

**Output:**
```json
{
  "candidateId": "PCS:SOFI:2025-10-11:22-21:abcd",
  "orderPlanId": "OP-A1B2C3D4",
  "combination": {
    "route": "SMART",
    "timeInForce": "DAY",
    "legs": [...]
  },
  "exits": {
    "tp": {...},
    "sl": {...}
  }
}
```

### get_account_status

Get account snapshot.

**Input:**
```json
{
  "accountId": "ibkr:primary"  // Optional
}
```

**Output:**
```json
{
  "netLiquidation": 31000,
  "maintenancePercent": 0.23,
  "accountDelta": 38,
  "accountTheta": 2.1
}
```

### act_on_order

Submit order (DEMO ONLY).

**Input:**
```json
{
  "orderPlanId": "OP-A1B2C3D4",
  "confirmationCode": "CONFIRM-OP-A1B2C3D4",
  "paper": true
}
```

**Output:**
```json
{
  "success": true,
  "message": "Submitted to IBKR Demo",
  "mode": "Paper Trading"
}
```

## Troubleshooting

### "MCP server not found"

- Check the path in claude_desktop_config.json
- Ensure .NET 9 SDK is installed
- Verify project builds: `dotnet build AutoRevOption.sln`

### "Connection timeout"

- The MCP server uses stdio communication
- Check logs in Claude Desktop: Help → View Logs
- Test locally first with `dotnet run -- --mcp`

### "Tool call failed"

- MCP server logs to stderr
- Check for build errors
- Verify secrets.json is configured

## Integration with Monitor

To connect MCP server to live IBKR data:

1. Ensure AutoRevOption.Monitor is running
2. Update McpServer.cs to use real IbkrConnection instead of MockAutoRevOption
3. Rebuild and restart Claude Desktop

## Security Notes

- MCP server runs locally on your machine
- No network connections (stdio only)
- DEMO mode by default (no real orders)
- Review all order plans before acting

## Next Steps

- Integrate with real IBKR connection (Monitor)
- Add more tools (position management, roll proposals)
- Implement persistent order tracking
- Add risk gate enforcement
