# WP01 — IBKR Connection & Base Rules Engine

**Goal:** Prove connectivity to IBKR (demo), draft the initial Rules Engine for entries/exits that can be tuned by `OptionsRadar.yaml`.

## Tasks
- ✅ IBKR TWS API connection: heartbeat, account snapshot, positions (read-only)
- ✅ Rules Engine v0: parse YAML (risk, income, convex), expose policy in code
- ✅ Define Profit/Stop policies: TP 50–60% credit, SL 2x credit or short strike touch
- ✅ Dry-run order builder for combos + OCA (no transmit)
- ✅ Secure credential storage via `secrets.json` (gitignored)

## Deliverables

### AutoRevOption.Monitor (Connection Testbed)
- ✅ `IbkrConnection.cs` - TWS API wrapper with EWrapper implementation
- ✅ `SecretConfig.cs` - Load credentials from `secrets.json`
- ✅ `Program.cs` - Interactive console (account summary, positions, monitor loop)
- ✅ Read-only operations: account snapshot, positions, market data
- ✅ Connection verification before advancing to WP02

**Usage:**
```bash
cd AutoRevOption.Monitor
dotnet run
```

### AutoRevOption.Minimal (Rules Engine)
- ✅ `IbkrClient.cs` - Mock IBKR client for demo
- ✅ `RulesEngine.cs` - YAML-driven policy validation
- ✅ `OrderBuilder.cs` - Combo orders with OCA brackets
- ✅ Config: `OptionsRadar.yaml` overrides
- ✅ Demo: "3. Now" menu → generates 3 mock candidates → builds OrderPlan JSON

### AutoRevOption.Tests
- ✅ 13 unit tests for RulesEngine (income, convex, risk gates)
- ✅ Validates DTE, delta, credit/width, R:R ratios
- ✅ All tests passing

## Status
**COMPLETED** ✅

Connection testbed (`Monitor`) is ready for live IBKR verification. Rules engine (`Minimal`) validated with unit tests.