# WP06 — Selection (SelectTVC)
**Goal:** Produce deterministic TVCs with PASS/FAIL & reasons. No broker calls.

---

## Overview

Implement the **SelectTVC** service that evaluates option spread candidates against hard gates (POP, Reward/Day, liquidity, events) and produces Trade Vet Card (TVC) artifacts in JSON format.

**Safety:** SelectTVC **never** places orders or modifies account state. It is safe to run continuously.

---

## Tasks

### 1. Core Implementation

- [ ] Implement `ISelectTvcService` interface with `EvaluateAsync()` method
- [ ] Load `OptionsRadar.yaml` selection_gates configuration
- [ ] Query option chains (via MCP endpoints or market data provider)
- [ ] Compute Greeks (Delta, IV, IVR) for candidate spreads
- [ ] Calculate POP (probability of profit) using breakeven method
- [ ] Calculate Reward/Day metric with realization haircut
- [ ] Surface all computed values in TVC JSON artifact

### 2. Gate Enforcement

- [ ] **POP Gate:** Enforce `POP ≥ min_pop` (default 75%)
- [ ] **Reward/Day Gate:** Enforce `Reward/Day ≥ min_reward_per_day_pct` (default 2.5%)
- [ ] **DTE Gate:** Enforce `dte_min ≤ DTE ≤ dte_max`
- [ ] **IVR Gate:** Enforce `IVR ≥ min_ivr` (default 30)
- [ ] **Liquidity Gates:**
  - [ ] Open Interest: `OI ≥ min_oi` (default 200)
  - [ ] Bid-Ask Spread (absolute): `spread ≤ max_spread_bidask_abs` (default $0.05)
  - [ ] Bid-Ask Spread (% of credit): `spread / credit ≤ max_spread_bidask_pct_of_credit` (default 10%)
- [ ] **Event Avoidance:**
  - [ ] Check for earnings dates within DTE window (±1 day)
  - [ ] Check for FOMC dates within DTE window (±1 day)
  - [ ] Check for CPI/PCE dates within DTE window (±1 day)
  - [ ] Add warnings to `Selection.Reasons` when events detected

### 3. Scoring Function (Optional)

- [ ] Implement composite score combining:
  - POP (weight: 40%)
  - Reward/Day (weight: 30%)
  - IVR (weight: 20%)
  - Liquidity/OI (weight: 10%)
- [ ] Normalize score to [0.0, 1.0] range
- [ ] Include score in TVC JSON for prioritization

### 4. Logging and Persistence

- [ ] Create directory structure: `logs/tvc/YYYY-MM-DD/`
- [ ] Write TVC JSON artifacts: `{SYMBOL}_{EXPIRY}_{STRATEGY}.json`
- [ ] Write human-readable summaries to console/log
- [ ] (Optional) Store TVCs in SQLite: `db/tvc.sqlite`

### 5. CLI Integration

- [ ] Add `select` verb to `AutoRevOption.Minimal/Program.cs`
- [ ] Support arguments: `--symbol`, `--dte`, `--strategy`
- [ ] Example: `dotnet run -- select --symbol SOFI --dte 7`
- [ ] Display TVC summary and file path on completion

### 6. MCP Tools

- [ ] Implement `scan_candidates` tool:
  - Input: `{ symbol, dte_min, dte_max, strategy }`
  - Output: Array of TVCSelection JSON
- [ ] Implement `validate_candidate` tool:
  - Input: `{ symbol, strikes[], expiry, strategy }`
  - Output: Single TVCSelection with gate verdicts
- [ ] Implement `verify_candidate` tool:
  - Input: TVC JSON path
  - Output: Re-run gates + freshness check

### 7. Testing

- [ ] Write unit tests in `AutoRevOption.Tests/SelectTvcTests.cs`:
  - [ ] Test POP calculation (breakeven method)
  - [ ] Test Reward/Day formula with realization haircut
  - [ ] Test gate enforcement (pass/fail scenarios)
  - [ ] Test event avoidance logic (earnings, FOMC, CPI, PCE)
  - [ ] Test liquidity checks (OI, bid-ask absolute and %)
  - [ ] Test scoring function (optional)
- [ ] Ensure all tests pass without broker connection
- [ ] Add integration test for end-to-end SelectTVC flow

---

## Deliverables

- [ ] **Code:**
  - `AutoRevOption.Minimal/Services/ISelectTvcService.cs`
  - `AutoRevOption.Minimal/Services/SelectTvcService.cs`
  - Updated `AutoRevOption.Minimal/Program.cs` with `select` verb
  - Updated `ExecuteContext.cs` with new MCP tools

- [ ] **Documentation:**
  - `DOCS/SelectTVC.md` (specification)

- [ ] **Tests:**
  - `AutoRevOption.Tests/SelectTvcTests.cs` with ≥5 test cases

- [ ] **Artifacts:**
  - Sample TVC JSON files in `logs/tvc/` demonstrating PASS and FAIL cases

---

## Acceptance Criteria

1. ✅ `SelectTvcService.EvaluateAsync()` produces valid TVCSelection records
2. ✅ All selection gates enforced per `OptionsRadar.yaml`
3. ✅ POP and Reward/Day calculations match formulas in `DOCS/SelectTVC.md`
4. ✅ Event avoidance detects earnings/FOMC/CPI/PCE overlaps
5. ✅ CLI `select` command produces TVC JSON artifacts
6. ✅ MCP tools `scan_candidates`, `validate_candidate`, `verify_candidate` functional
7. ✅ Unit tests demonstrate gate behavior (≥90% code coverage)
8. ✅ **No broker calls** — safe to run continuously

---

## Dependencies

- **WP01:** RulesEngine implementation (for YAML parsing)
- **WP02:** Market data provider (for option chains, greeks)
- **WP03:** Event calendar integration (earnings, FOMC, CPI, PCE)

---

## Timeline

- **Phase 1:** Core implementation + gate enforcement (3 days)
- **Phase 2:** CLI + MCP tools (2 days)
- **Phase 3:** Testing + documentation (2 days)
- **Total:** ~7 days

---

## Notes

- Prioritize **safety** — no order placement in this WP
- Keep SelectTVC **stateless** — no persistent connections, no account state
- Design for **continuous operation** — can run every minute without issues
- Use **deterministic** calculations — same inputs → same TVC output

---

**Status:** 🟡 In Progress
**Owner:** AutoRevOption.Minimal
**Last Updated:** 2025-10-05
