# MCP Servers Comparison Guide

AutoRevOption provides **two MCP servers** for different use cases. This guide helps you choose which one to use.

## Overview

| Feature | **Minimal MCP** | **Monitor MCP** |
|---------|-----------------|-----------------|
| **Purpose** | Trading workflow & rules validation | Live IBKR account monitoring |
| **Data Source** | Mock data (demo) | Real IBKR connection |
| **Dependencies** | None (works offline) | IB Gateway must be running |
| **Tools Count** | 6 | 6 |
| **Account Data** | Mock ($31k NLV) | Real balances from IBKR |
| **Positions** | None | Live positions |
| **Order Planning** | ✅ Yes (demo) | ❌ No (read-only) |
| **Order Execution** | ❌ No (demo only) | ❌ No (read-only) |
| **Greeks** | Not available | Portfolio-wide (placeholder) |
| **Gateway Check** | Not available | ✅ Yes (with auto-launch) |

## Quick Start

### Minimal MCP Server
```bash
cd AutoRevOption.Minimal
dotnet run -- --mcp
```

**Claude Desktop Config:**
```json
{
  "mcpServers": {
    "autorev-option": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Code\\AutoRevOption\\AutoRevOption.Minimal\\AutoRevOption.Minimal.csproj", "--", "--mcp"]
    }
  }
}
```

### Monitor MCP Server
```bash
cd AutoRevOption.Monitor
dotnet run -- --mcp
```

**Claude Desktop Config:**
```json
{
  "mcpServers": {
    "autorev-monitor": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Code\\AutoRevOption\\AutoRevOption.Monitor\\AutoRevOption.Monitor.csproj", "--", "--mcp"]
    }
  }
}
```

## Minimal MCP Server

### Purpose
- **Plan trades** using OptionsRadar rules
- **Validate candidates** against DTE/delta/credit rules
- **Build order plans** with TP/SL exits
- **Demo workflow** without IBKR connection

### Tools (6)

1. **scan_candidates** - Scan universe for trade opportunities
2. **validate_candidate** - Validate against OptionsRadar rules
3. **verify_candidate** - Verify against risk gates
4. **build_order_plan** - Build order with TP/SL exits
5. **get_account_status** - Get mock account snapshot
6. **act_on_order** - Submit order (DEMO ONLY, no real orders)

### Use Cases

**Scenario 1: Scan and Validate**
```
User: "Scan for PCS opportunities and validate the best one"

Claude:
1. Calls scan_candidates → Returns 5 candidates
2. Picks top candidate (SHOP, score 94)
3. Calls validate_candidate → Returns valid=true
4. Presents candidate details to user
```

**Scenario 2: Build Order Plan**
```
User: "Build an order plan for PCS:SHOP:2025-10-11:22-21:8dc9 with 2 contracts"

Claude:
1. Calls build_order_plan with candidateId and quantity=2
2. Returns OrderPlan with:
   - Entry combo order (sell 22P, buy 21P)
   - TP exit at 50-60% credit
   - SL exit at 2x credit
3. User reviews and submits manually via IB Gateway
```

**Scenario 3: Check Risk Gates**
```
User: "Verify if I can take this trade given my account size"

Claude:
1. Calls verify_candidate with candidateId
2. Checks against risk gates (max daily risk, position size, margin)
3. Returns verified=true/false with reason
```

### Advantages
- ✅ Works offline (no IBKR needed)
- ✅ Fast startup (instant)
- ✅ Safe (demo data only)
- ✅ Perfect for testing rules

### Limitations
- ❌ No real account data
- ❌ Cannot place orders
- ❌ Cannot check live positions
- ❌ Mock data only

## Monitor MCP Server

### Purpose
- **Monitor account** balances and margin
- **View positions** (stocks + options)
- **Check Gateway** connection status
- **Calculate greeks** (portfolio-wide)

### Tools (6)

1. **get_connection_status** - Check Gateway & API connection
2. **get_account_summary** - Get account balances & margin
3. **get_positions** - Get all positions (stocks + options)
4. **get_option_positions** - Get only options positions (filtered)
5. **get_account_greeks** - Calculate portfolio greeks
6. **check_gateway** - Check/launch IB Gateway

### Use Cases

**Scenario 1: Daily Account Check**
```
User: "What's my account status?"

Claude:
1. Calls get_connection_status → Gateway running, connected
2. Calls get_account_summary → Returns real balances
3. Presents:
   - Net Liq: $45,230.50
   - Buying Power: $90,461.00
   - Maintenance: 7.56%
```

**Scenario 2: Position Review**
```
User: "Show me all my SHOP option positions"

Claude:
1. Calls get_option_positions with ticker="SHOP"
2. Returns 2 positions:
   - SHOP P21 Oct 11: -2 @ $0.38, P&L: +$11
   - SHOP P22 Oct 11: -2 @ $0.45, P&L: +$8
```

**Scenario 3: Portfolio Risk**
```
User: "What's my portfolio delta exposure?"

Claude:
1. Calls get_account_greeks
2. Returns total greeks (currently placeholder)
3. Lists all option positions
```

**Scenario 4: Connection Troubleshooting**
```
User: "Is my IB Gateway running?"

Claude:
1. Calls check_gateway
2. Returns:
   - Gateway running: Yes (PID: 12345)
   - Port: 4001 (OPEN)
   - API connected: Yes
```

### Advantages
- ✅ Real account data
- ✅ Live positions
- ✅ Gateway management
- ✅ Portfolio analysis

### Limitations
- ❌ Requires IB Gateway running
- ❌ Read-only (cannot place orders)
- ❌ Greeks not yet implemented (placeholder)
- ❌ Slower startup (waits for connection)

## Using Both Together

**Recommended Setup:**

Add **both** servers to Claude Desktop config:

```json
{
  "mcpServers": {
    "autorev-option": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Code\\AutoRevOption\\AutoRevOption.Minimal\\AutoRevOption.Minimal.csproj", "--", "--mcp"]
    },
    "autorev-monitor": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Code\\AutoRevOption\\AutoRevOption.Monitor\\AutoRevOption.Monitor.csproj", "--", "--mcp"]
    }
  }
}
```

### Workflow

1. **Morning Check** (Monitor)
   ```
   User: "Check my account and positions"
   Claude uses: autorev-monitor
   - get_connection_status
   - get_account_summary
   - get_positions
   ```

2. **Scan Opportunities** (Minimal)
   ```
   User: "Scan for new trade opportunities"
   Claude uses: autorev-option
   - scan_candidates
   - validate_candidate (top 3)
   ```

3. **Build Order Plan** (Minimal)
   ```
   User: "Build order plan for SHOP candidate"
   Claude uses: autorev-option
   - build_order_plan
   - Returns order details for manual submission
   ```

4. **Verify Risk** (Minimal + Monitor)
   ```
   User: "Can I take this trade given my current positions?"
   Claude uses:
   - autorev-monitor.get_account_summary (real balance)
   - autorev-monitor.get_positions (current exposure)
   - autorev-option.verify_candidate (risk gates)
   ```

5. **Manual Execution** (User)
   ```
   User reviews order plan
   User submits order via IB Gateway manually
   ```

6. **Confirm Fill** (Monitor)
   ```
   User: "Did my order fill?"
   Claude uses: autorev-monitor
   - get_positions (check new position)
   ```

## Decision Tree

**Choose Minimal MCP if:**
- You want to plan trades
- You want to test OptionsRadar rules
- You don't have IBKR running
- You want fast startup
- You're learning the system

**Choose Monitor MCP if:**
- You want to check account status
- You want to see live positions
- You want to monitor Gateway
- You need real account data
- You're managing existing trades

**Use Both if:**
- You want complete workflow
- You trade regularly
- You want live monitoring + planning
- You have IBKR connection

## Performance

| Metric | **Minimal MCP** | **Monitor MCP** |
|--------|-----------------|-----------------|
| Startup Time | ~1s | ~3-5s (waits for IBKR) |
| Tool Call Speed | <100ms | 100-500ms (IBKR roundtrip) |
| Memory Usage | ~50MB | ~80MB (IBKR connection) |
| CPU Usage | Minimal | Low (background monitoring) |

## Security

| Aspect | **Minimal MCP** | **Monitor MCP** |
|--------|-----------------|-----------------|
| Network Connections | None | localhost:4001 (IBKR) |
| Account Access | Mock data | Full account read access |
| Order Execution | None (demo only) | None (read-only) |
| Secrets Required | No | Yes (secrets.json) |

## Troubleshooting

### Minimal MCP Issues

**"Tool call failed"**
- Check build: `dotnet build AutoRevOption.Minimal.csproj`
- Check OptionsRadar.yaml exists
- Review Claude Desktop logs

### Monitor MCP Issues

**"Gateway not running"**
```bash
bash scripts/check-gateway-port.sh
```
Expected: `✅ Port 4001 is OPEN`

**"Failed to connect to IBKR API"**
1. Check Gateway is logged in (not just running)
2. Check API enabled: Configure → Settings → API
3. Check trusted IP: 127.0.0.1
4. Check port in secrets.json matches (4001)

**"Connection timeout"**
- Gateway may be starting up
- Check firewall settings
- Verify secrets.json configuration

## Documentation Links

- **Minimal MCP Setup:** [MCP_Setup.md](MCP_Setup.md)
- **Monitor MCP Setup:** [Monitor_MCP_Setup.md](Monitor_MCP_Setup.md)
- **Minimal MCP Implementation:** [MCP_Implementation_Summary.md](MCP_Implementation_Summary.md)
- **Monitor MCP Implementation:** [Monitor_MCP_Implementation_Summary.md](Monitor_MCP_Implementation_Summary.md)
- **Main README:** [README.md](../README.md)

## Next Steps

1. **Try Minimal MCP First**
   - No setup required
   - Test all 6 tools
   - Learn the workflow

2. **Setup Monitor MCP**
   - Configure IB Gateway
   - Test connection
   - Try live account tools

3. **Use Both Together**
   - Add both to Claude Desktop
   - Practice combined workflow
   - Monitor + plan trades

4. **Future Enhancements**
   - Real-time greeks (Monitor)
   - Position analysis tools
   - Order status tracking
   - Integration between servers

---

*Generated: 2025-10-04*
*Last Updated: Both MCP servers operational*
