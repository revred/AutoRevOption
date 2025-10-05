# AutoRevOption Scripts

Helper scripts for building, running, and testing AutoRevOption projects.

## Quick Reference

| Task | Script |
|------|--------|
| Build solution | `./scripts/build.sh` |
| Run tests | `./scripts/test.sh` |
| Run Minimal (demo) | `./scripts/run-minimal.sh` |
| Run Monitor (IBKR) | `./scripts/run-monitor.sh` |
| **Get positions** | `./scripts/get-positions.sh` |
| Start Gateway + Monitor | `./scripts/start-gateway.sh` |
| Check Gateway port | `./scripts/check-gateway-port.sh` |
| Test MCP server (Minimal) | `./scripts/test-mcp-server.sh` |
| Test MCP server (Monitor) | `./scripts/test-mcp-monitor.sh` |

## Quick Start

```bash
chmod +x scripts/*.sh
./scripts/build.sh       # Build solution
./scripts/test.sh        # Run all tests
./scripts/run-minimal.sh # Start demo console
./scripts/run-monitor.sh # Start IBKR monitor
```

**Windows users:** Use Git Bash or WSL to run .sh scripts.

## Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IB Gateway** (for Monitor only) - [IBKR Download](https://www.interactivebrokers.com/en/trading/ibgateway-stable.php)
- **secrets.json** configured (see Configuration section below)

## Configuration

### secrets.json (Required for Monitor)

Create `secrets.json` in the repository root:

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 1,
    "GatewayPath": "C:\\IBKR\\ibgateway\\1040\\ibgateway.exe",
    "Username": "your-ibkr-username",
    "IsPaperTrading": true
  },
  "IBKRMarketData": {
    "SubscriptionActive": true,
    "SubscriptionType": "US Securities Snapshot Bundle"
  },
  "TradingLimits": {
    "MaxDebit": 500,
    "MaxML": 500,
    "MaxOpenSpreads": 10,
    "MaintPctMax": 0.40,
    "DeltaMax": 200,
    "ThetaMin": -50
  }
}
```

**Gateway Paths by Platform:**
- Windows: `C:\IBKR\ibgateway\1040\ibgateway.exe`
- Mac: `/Applications/IB Gateway.app`
- Linux: `~/Jts/ibgateway/latest/ibgateway`

**Ports:**
- 7497 - Paper trading (default)
- 7496 - Live trading

**Security:** `secrets.json` is gitignored - never commit credentials!

### OptionsRadar.yaml (Optional)

Trading rules and risk parameters for RulesEngine in Minimal.

**Location:** Repository root

## Architecture

See [Architecture Overview](../docs/Architecture_Overview.md) for complete system architecture.

**Key Projects:**
- **AutoRevOption.Shared** - Common types (Prime.Models, Context, Configuration)
- **AutoRevOption.Minimal** - Demo MCP server (no IBKR dependency)
- **AutoRevOption.Monitor** - Read-only IBKR monitor with MCP interface
- **AutoRevOption.Tests** - 100% safe unit tests (no live connections)

**Prime.Models ⭐** - The execution models used by TVC and IBKR:
- Located in `AutoRevOption.Shared.Prime.Models`
- Flow: TVC Selection → WriteTVC → Prime.Models → IBKR
