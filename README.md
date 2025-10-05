# AutoRevOption

Purpose: MCP interface and console toolkit to **scan**, **select**, **stage**, and **act** on options opportunities per `In2025At100K.md` rules.

## Safety-first split
We **separate selection from execution**:

- `SelectTVC` → computes *Trade Vet Cards* (TVC) and enforces **POP ≥ 75%** and **Reward/Day ≥ 2.5%** (after fees, 60% realization). **No orders here.**
- `WriteTVC` → consumes TVCs and performs **funds/leverage** checks and, if allowed, **stages** or **executes** orders with OCO (TP 50%, SL 2× credit).

This split ensures we can screen continuously even when funds are tight or leverage is capped.

## Layout

    AutoRevOption/
    ├─ AutoRevOption.sln             # Solution file (.NET 9)
    ├─ AutoRevOption.Minimal/        # Console + MCP (selection + staging)
    ├─ AutoRevOption.Monitor/        # Read-only IBKR connection testbed
    ├─ AutoRevOption.Tests/          # Unit tests (xUnit)
    ├─ WorkPackages/                 # WP01–WP12 execution plan
    ├─ DOCS/                         # Specs (OptionsRadar.md copy, diagrams, notes)
    ├─ OptionsRadar.yaml             # Config knobs (risk, universe, strategies)
    ├─ secrets.json                  # IBKR/API credentials (gitignored)
    └─ .gitignore

### Key Documents
- `docs/SelectTVC.md` – selection-only pipeline spec
- `docs/WriteTVC.md` – write/act pipeline spec
- `docs/PlotGateway.md` – IB Gateway connection troubleshooting & resolution
- `WorkPackages/WP05_MondayReadiness.md` – **operational end-to-end plan** for Monday trading

## Run

### 1) Selection (no broker calls)
```
cd AutoRevOption.Minimal
dotnet run -- select --symbol SOFI --dte 7
```
Output: TVC JSON + human summary in `logs/tvc/`.

### 2) Write/Act (with admissibility checks)
```
dotnet run -- write --tvc logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json --mode STAGE
```
Modes: `DRY_RUN|STAGE|EXECUTE`. Execution requires Monitor/TWS availability.

## Tests
```
dotnet test
```
Key gates validated by tests:
- POP ≥ 0.75
- Reward/Day ≥ 2.5%
- Event avoidance (ER/FOMC/CPI/PCE)
- Liquidity floors and credit drift

## Scripts

See [scripts/README.md](scripts/README.md) for all available helper scripts.

```bash
./scripts/build.sh       # Build solution
./scripts/test.sh        # Run all tests
./scripts/run-minimal.sh # Start demo console
./scripts/run-monitor.sh # Start IBKR monitor
```

## MCP Server Mode (Claude Desktop Integration)

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

See [docs/MCP_Setup.md](docs/MCP_Setup.md) and [docs/Monitor_MCP_Setup.md](docs/Monitor_MCP_Setup.md) for Claude Desktop integration.

## Configuration

Edit `secrets.json` (gitignored):
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 4001,
    "ClientId": 10,
    "IsPaperTrading": false
  }
}
```
- Port 4001 = IB Gateway (default)
- Port 7497 = TWS Paper Trading
- Port 7496 = TWS Live Trading

**Note:** IB Gateway connection requires TWS API 10.37.02+ with full ProtoBuf method support. See [docs/PlotGateway.md](docs/PlotGateway.md) for troubleshooting.

## Monday Playbook
See [docs/MondayReady.md](docs/MondayReady.md). Quick start:

**Bash**
```bash
./scripts/monday-smoke.sh
```

This smoke test runs:
1. Morning snapshot (paper TWS)
2. Selection across universe (SOFI, APP, RKLB, META, AMD, GOOGL)
3. Stage all PASS TVCs
4. Summary of artifacts generated