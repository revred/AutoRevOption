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

**`AutoRevOption.Shared.Portal`**
- `GatewayProcessManager` - Client Portal Gateway lifecycle (singleton, auto-launch)
- `ClientPortalBrowserLogin` - Selenium automation for IBKR login with 2FA
- `ClientPortalLoginService` - High-level login orchestration
- `AutoRevClient` - REST client for Client Portal API (HTTPS port 5000)
- `Connection` - Main connection wrapper with position cache integration
- `PositionCacheService` - SQLite-based position cache for fast MCP responses

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
- **Gateway Manager:** Auto-launch and monitor Client Portal Gateway
- **IBKR Integration:** Client Portal REST API (HTTPS port 5000)
- **Position Cache:** SQLite storage for fast offline access
- **Browser Automation:** Headless Chrome with 2FA support
- **Tools:**
  - `get_connection_status` - Gateway health check
  - `get_account_summary` - Account balances and margin
  - `get_positions` - All open positions (updates cache)
  - `get_positions_fast` - Cached positions (instant, no API call)
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

### Position Cache Flow ⭐ **Fast MCP Responses**

```
1. API Fetch (Slow - 200-800ms)
   └─→ GetPositionsAsync()
   └─→ HTTP GET https://localhost:5000/v1/api/portfolio/positions
   └─→ Parse JSON response
   └─→ Calculate SHA256 hash
   └─→ Compare with cached hash
   └─→ Update SQLite if changed (atomic transaction)
   └─→ Return positions

2. Cached Fetch (Fast - 2-7ms) ⭐
   └─→ GetCachedPositions()
   └─→ SQLite query (indexed)
   └─→ Return positions from local DB
   └─→ No network I/O, instant response

3. Change Detection
   └─→ SHA256 hash of sorted position JSON
   └─→ Compare with CacheMetadata.positions_hash
   └─→ Skip DB write if unchanged (efficient)
   └─→ Update only when positions actually change
```

**Database Location:** `%LocalApplicationData%/AutoRevOption/positions.db`

**Use Case:** MCP tools can use cached positions for instant responses, periodically refresh from API.

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

### 6. **Performance Optimization**
- **Position Cache:** SQLite storage for 100x faster responses
- **Change Detection:** Only update cache when data actually changes
- **Session Persistence:** Browser stays alive indefinitely (no repeated 2FA)
- **Singleton Gateway:** Single gateway process shared across all apps

## File Organization

```
AutoRevOption/
├── AutoRevOption.Shared/
│   ├── Context/              # MCP protocol types
│   ├── Prime/Models/         # ⭐ Prime execution models
│   ├── Configuration/        # Secrets and config
│   ├── Portal/               # Client Portal API integration
│   │   ├── GatewayProcessManager.cs      # Gateway lifecycle
│   │   ├── ClientPortalBrowserLogin.cs   # Selenium automation
│   │   ├── ClientPortalLoginService.cs   # Login orchestration
│   │   ├── AutoRevClient.cs              # REST API client
│   │   ├── Connection.cs                 # Main connection wrapper
│   │   └── PositionCacheService.cs       # ⭐ SQLite position cache
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
│   ├── Position_Cache.md    # ⭐ Position cache guide
│   ├── Session_Management.md # Browser session persistence
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
- [Position Cache Guide](Position_Cache.md) ⭐ **SQLite-based fast MCP responses**
- [Session Management Guide](Session_Management.md) - Browser session persistence
- [MCP Setup Guide](MCP_Setup.md)
- [Test Safety Guide](Test_Safety_Guide.md)
