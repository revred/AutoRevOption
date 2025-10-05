# AutoRevOption Architecture Overview

## System Architecture

AutoRevOption implements a **layered architecture** for systematic options trading with IBKR integration and Claude Desktop MCP integration.

```
┌─────────────────────────────────────────────────────────────┐
│                     Claude Desktop (MCP)                     │
│               Context Protocol Integration                   │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────────────┐
│                   AutoRevOption.Shared                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Context    │  │ Prime.Models │  │  TVC Models  │      │
│  │   (MCP)      │  │  (Execution) │  │  (Selection) │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │Configuration │  │     IBKR     │  │     Tvc      │      │
│  │  (Secrets)   │  │  (Gateway)   │  │  (Select +   │      │
│  └──────────────┘  └──────────────┘  │   Write)     │      │
│                                       └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
           │                    │                   │
┌──────────┴────────┐  ┌────────┴────────┐  ┌──────┴──────────┐
│ AutoRevOption     │  │ AutoRevOption   │  │ AutoRevOption   │
│   .Minimal        │  │   .Monitor      │  │   .Tests        │
│                   │  │                 │  │                 │
│ • MCP Server      │  │ • MCP Server    │  │ • Unit Tests    │
│ • Demo Console    │  │ • IBKR Gateway  │  │ • Integration   │
│ • Rules Engine    │  │ • Read-only     │  │ • Safety Checks │
│ • Mock Backend    │  │   Monitor       │  │                 │
└───────────────────┘  └─────────────────┘  └─────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │  IBKR Gateway     │
                    │  (TWS API)        │
                    │  • Paper Trading  │
                    │  • Live Trading   │
                    └───────────────────┘
```

## Project Structure

### AutoRevOption.Shared (Class Library)
**Purpose:** Common types and logic shared across all projects

#### Namespaces:

**`AutoRevOption.Shared.Context`**
- MCP protocol types (McpRequest, McpResponse, McpError)
- IMcpServer interface
- Model Context Protocol for Claude Desktop integration

**`AutoRevOption.Shared.Prime.Models`** ⭐ **Prime Execution Models**
- `Candidate` - Trade candidate selected for execution
- `OrderPlan` - Complete order with entry/exit brackets
- `OptionLeg` - Individual option contract
- `StrategyType` - PCS, CCS, BPS, BCS, DIAGONAL, PMCC, RV
- `RiskModels` - Validation, verification, risk guards
- **Used by:** TVC execution, RulesEngine, OrderBuilder, actual IBKR execution

**`AutoRevOption.Shared.Configuration`**
- `SecretConfig` - Credentials and secrets (loaded from secrets.json)
- `IBKRCredentials` - IBKR Gateway connection details
- `IBKRMarketData` - IBKR market data subscription settings
- `TradingLimits` - Account-level risk limits

**`AutoRevOption.Shared.Ibkr`**
- `GatewayManager` - IB Gateway lifecycle management (launch, monitor, health)

**`AutoRevOption.Shared.Tvc`** (Future - Phase 3)
- `Tvc.Common` - Shared TVC types (Leg, TVCEnums)
- `Tvc.SelectTVC` - Selection artifacts (TVCSelection, Liquidity, Events)
- `Tvc.WriteTVC` - Execution artifacts (ExecutionCard, Brackets, Admissibility)

### AutoRevOption.Minimal (Console App)
**Purpose:** Minimal MCP server with demo functionality

- **MCP Server:** Exposes tools for Claude Desktop
- **Mock Backend:** Demo implementation without IBKR connection
- **Rules Engine:** OptionsRadar.yaml policy enforcement
- **Order Builder:** OCA bracket order construction
- **Tools:**
  - `scan_candidates` - Find trade opportunities
  - `validate_candidate` - Rules validation
  - `verify_candidate` - Risk gate checks
  - `build_order_plan` - Create execution plan
  - `get_account_status` - Account snapshot
  - `act_on_order` - Order submission (DEMO)

### AutoRevOption.Monitor (Console App)
**Purpose:** Read-only IBKR connection monitor with MCP interface

- **MCP Server:** Exposes IBKR account/position monitoring
- **Gateway Manager:** Auto-launch and monitor IB Gateway
- **IBKR Integration:** Real TWS API connection (read-only)
- **Tools:**
  - `get_connection_status` - Gateway health check
  - `get_account_summary` - Account balances and margin
  - `get_positions` - All open positions
  - `get_option_positions` - Options positions only
  - `get_account_greeks` - Portfolio-level Greeks
  - `check_gateway` - Gateway status with auto-launch

### AutoRevOption.Tests (Test Project)
**Purpose:** Comprehensive unit and integration tests

- **Test Coverage:** 62 tests (54 passing)
- **Safety:** 100% safe - no live account connection
- **Test Files:**
  - `MinimalMcpServerTests.cs` - MCP protocol and tool tests
  - `MonitorMcpServerTests_Simple.cs` - Monitor protocol tests
  - `RulesEngineTests.cs` - Rules validation tests

## Data Flow

### TVC (Trade Vet Card) Flow ⭐ **Key Architecture**

```
1. Selection Phase (SelectTVC)
   └─→ Scan market data (ThetaData/IBKR)
   └─→ Apply selection criteria (IV rank, DTE, delta, liquidity)
   └─→ Generate TVCSelection artifact
        ├─ Symbol, Strategy, Legs
        ├─ Metrics: POP, Reward/Day, IV, IVR
        ├─ Liquidity: Bid-Ask spread, volume
        └─ Selection decision: YES/NO + reasoning

2. Feasibility Phase (WriteTVC)
   └─→ Load TVCSelection
   └─→ Apply admissibility checks
        ├─ Account balance (min capital)
        ├─ Margin requirements
        ├─ Position limits (max spreads, concentration)
        ├─ Risk limits (delta, theta)
   └─→ Generate ExecutionCard artifact
        ├─ Mode: DRY_RUN | EXECUTE
        ├─ Admissibility: PASS/FAIL + reasons
        ├─ Broker preview (margin impact)
        └─ Action result: submitted/rejected

3. Execution Phase (Prime.Models)  ⭐
   └─→ Load ExecutionCard (if approved)
   └─→ Convert to Prime.Models.Candidate
   └─→ Build Prime.Models.OrderPlan
   └─→ Submit to IBKR via TWS API
        ├─ Entry order (spread combo)
        └─ Exit brackets (TP + SL in OCA group)
```

### MCP Integration Flow

```
Claude Desktop
  ↓ (stdio)
  MCP Server (Minimal or Monitor)
  ↓ (McpRequest)
  Tool Handler (scan_candidates, get_positions, etc.)
  ↓
  Business Logic (RulesEngine, GatewayManager, etc.)
  ↓ (McpResponse)
  Claude Desktop
```

## Key Design Principles

### 1. **Prime Models for Execution**
- `AutoRevOption.Shared.Prime.Models` contains the **execution models**
- TVC generates selection artifacts → WriteTVC evaluates → Prime models execute
- Shared across all layers: evaluation, monitoring, rules, execution

### 2. **Separation of Concerns**
- **Minimal:** Demo/development without IBKR dependency
- **Monitor:** Read-only monitoring without order placement
- **Shared:** Common types, no business logic duplication

### 3. **MCP Protocol Abstraction**
- `AutoRevOption.Shared.Context` provides protocol types
- Both Minimal and Monitor implement `IMcpServer`
- Consistent tool interface for Claude Desktop

### 4. **Safety First**
- **Paper Trading Default:** All operations default to paper account
- **Test Safety:** No live connections in unit tests
- **Explicit Confirmation:** Order submission requires confirmation codes

### 5. **Configuration-Driven**
- `secrets.json` - Credentials and connection details
- `OptionsRadar.yaml` - Trading rules and risk parameters
- Environment-specific settings (paper vs. live)

## File Organization

```
AutoRevOption/
├── AutoRevOption.Shared/
│   ├── Context/              # MCP protocol types
│   ├── Prime/Models/         # ⭐ Prime execution models
│   ├── Configuration/        # Secrets and config
│   ├── Ibkr/                 # Gateway management
│   └── Tvc/                  # TVC models (Phase 3)
│       ├── Common/
│       ├── SelectTVC/
│       └── WriteTVC/
├── AutoRevOption.Minimal/    # MCP server + demo
├── AutoRevOption.Monitor/    # MCP server + IBKR monitor
├── AutoRevOption.Tests/      # Test suite
├── docs/                     # Documentation
│   ├── tvc_spec/            # SelectTVC.md, WriteTVC.md
│   ├── Architecture_Overview.md  # This file
│   └── ...
├── scripts/                  # Helper scripts
│   ├── build.sh/bat
│   ├── test.sh/bat
│   ├── run-minimal.sh/bat
│   └── run-monitor.sh/bat
└── WorkPackages/            # Development tracking
```

## Next Steps (Phase 3)

1. **Implement SelectTVC Service**
   - Read market data
   - Apply selection rules
   - Generate TVCSelection artifacts

2. **Implement WriteTVC Service**
   - Load TVCSelection
   - Admissibility checks
   - Generate ExecutionCard artifacts

3. **Wire TVC → Prime Models**
   - Convert ExecutionCard → Candidate
   - Build OrderPlan from approved cards
   - Submit to IBKR

4. **Add MCP Tools for TVC**
   - `select_trade` - Run SelectTVC
   - `vet_trade` - Run WriteTVC
   - `execute_trade` - Execute approved card

## References

- [SelectTVC Specification](tvc_spec/SelectTVC.md)
- [WriteTVC Specification](tvc_spec/WriteTVC.md)
- [MCP Setup Guide](MCP_Setup.md)
- [Test Safety Guide](Test_Safety_Guide.md)
