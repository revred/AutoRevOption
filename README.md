# AutoRevOption

**Purpose:** MCP interface and console toolkit to scan, validate, verify, and act on options opportunities per `In2025At100K.md` rules.

## Layout
```
AutoRevOption/
├─ AutoRevOption.sln             # Solution file (.NET 9)
├─ AutoRevOption.Minimal/        # .NET console with rules engine & order builder
├─ AutoRevOption.Monitor/        # Read-only IBKR connection testbed
├─ AutoRevOption.Tests/          # Unit tests (xUnit)
├─ WorkPackages/                 # WP01–WP12 execution plan
├─ DOCS/                         # Specs (OptionsRadar.md copy, diagrams, notes)
├─ OptionsRadar.yaml             # Config knobs (risk, universe, strategies)
├─ secrets.json                  # IBKR/API credentials (gitignored)
└─ .gitignore
```

## Quick start

### 1. Test IBKR Connection (Monitor)
```bash
cd AutoRevOption.Monitor
dotnet run
```
Read-only connection to verify TWS/Gateway setup. See [Monitor/README.md](AutoRevOption.Monitor/README.md) for setup.

### 2. Run Rules Engine Demo (Minimal)
```bash
cd AutoRevOption.Minimal
dotnet run
```
Interactive console with mock data. Select option "3. Now" to see WP01 demo (RulesEngine + OrderBuilder).

### 3. Run Tests
```bash
dotnet test
```

**✅ TEST SAFETY:** All tests are 100% safe - no IBKR connection, no order placement, no account modifications. See [Test Safety Guide](DOCS/Test_Safety_Guide.md).

### 4. MCP Server Mode (Claude Desktop Integration)

**Minimal MCP Server** (Trading workflow with mock data):
```bash
cd AutoRevOption.Minimal
dotnet run -- --mcp
```
Exposes 6 tools: scan_candidates, validate_candidate, verify_candidate, build_order_plan, get_account_status, act_on_order

**Monitor MCP Server** (Live IBKR account monitoring):
```bash
cd AutoRevOption.Monitor
dotnet run -- --mcp
```
Exposes 6 tools: get_connection_status, get_account_summary, get_positions, get_option_positions, get_account_greeks, check_gateway

See [DOCS/MCP_Setup.md](DOCS/MCP_Setup.md) and [DOCS/Monitor_MCP_Setup.md](DOCS/Monitor_MCP_Setup.md) for Claude Desktop integration.

## Configuration

Edit `secrets.json` (gitignored):
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 1
  }
}
```
- Port 7497 = Paper Trading
- Port 7496 = Live Trading