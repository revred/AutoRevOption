# WP05 — Monday Readiness (E2E)
**Goal:** Be operational on Mondays with a repeatable flow: **Monitor ➜ Select ➜ Write/Stage ➜ Execute (paper) ➜ Roll ➜ Close**, with full audit & safety.

---

## Scope

- Covers *weekday open* routine, with emphasis on Mondays
- Applies to **paper** first, then **live** after passing gates
- Execution is optional; staging allowed if funds/leverage not admissible

---

## Inputs

- **AutoRevOption.Monitor** MCP (read-only): `get_account_summary`, `get_positions`, `get_option_positions`, `get_account_greeks`
- **AutoRevOption.Minimal** MCP (selection): `scan_candidates`, `validate_candidate`, `verify_candidate`
- **Event feeds**: earnings, FOMC, CPI/PCE (per SelectTVC spec)

---

## Deliverables (Definition of Done)

- [ ] **Morning Snapshot**: JSON under `logs/snapshots/YYYY-MM-DD/` (health, greeks, exposure)
- [ ] **Selection Artifacts**: TVC JSONs under `logs/tvc/YYYY-MM-DD/` (PASS/FAIL reasons)
- [ ] **Staged Tickets** *(paper mode)*: Execution Cards under `logs/exec/cards/` with `mode=STAGE`
- [ ] **Paper Trades Executed**: if admissible & allowed in config, place paper orders; artifacts written
- [ ] **Roll Plan**: file per position under `logs/roll/YYYY-MM-DD/<SYMBOL>_<EXP>.json`
- [ ] **Close Plan**: candidates for close/TP/stop with rationale
- [ ] **Audit Bundle**: zipped `logs/**` for the day with a human `DailyReport.md`

---

## Tasks

### 1. Monitoring

- [ ] Implement `/snapshots/morning` command (console & MCP) → writes snapshot JSON
- [ ] Verify IBKR TWS connectivity for **paper** (`Port: 7497`)
- [ ] Query account summary: NetLiq, BuyingPower, MaintReq, TotalCash
- [ ] Query positions and compute portfolio greeks (Delta, Theta)
- [ ] Query option positions with detailed greeks per position

### 2. Selection

- [ ] Run `select` verb for SOFI/APP/RKLB/META/AMD/GOOGL with DTE 5–9
- [ ] Enforce gates:
  - POP ≥ 75%
  - Reward/Day ≥ 2.5%
  - Liquidity (OI ≥ 200, spread checks)
  - IVR ≥ 30
  - Event avoidance (earnings/FOMC/CPI/PCE)
- [ ] Output TVC JSON artifacts with PASS/FAIL verdicts

### 3. Write/Stage

- [ ] For PASS TVCs, create **Execution Cards** with `mode=STAGE`
- [ ] Check admissibility:
  - Maintenance % within limits
  - Defined-risk caps (portfolio & per-symbol)
  - TVC freshness (≤ 15 min)
  - Credit drift (≤ 5%)
- [ ] Write staged execution cards to `logs/exec/staged/`

### 4. Execute (Paper)

- [ ] If admissible and `mode=EXECUTE_PAPER`, place orders via paper TWS
- [ ] Attach OCO brackets:
  - Take Profit: 50% of credit
  - Stop Loss: 2× credit
  - Time In Force: GTC
- [ ] Store broker previews, orderIds, and fills under `logs/fills/`
- [ ] Log all execution results to `logs/exec/tickets/`

### 5. Roll Management

- [ ] Generate roll cues when:
  - Δshort > 0.35 (defense threshold)
  - Touch event (price near short strike)
- [ ] Write roll JSON plan under `logs/roll/YYYY-MM-DD/<SYMBOL>_<EXP>.json`
- [ ] Include roll strategy: up-and-out, same-strike roll, or close

### 6. Close

- [ ] Auto-close at TP hit (50% profit target)
- [ ] List near-expiry actions (DTE ≤ 2)
- [ ] Handle losers per stop rule (2× credit loss)
- [ ] Write close plan to `logs/close/YYYY-MM-DD/`

### 7. Reporting

- [ ] Compile `DailyReport.md` with:
  - Morning snapshot summary
  - Selection results (PASS/FAIL counts)
  - Staged vs executed counts
  - Roll/close actions taken
  - KPIs: POP realized vs expected, P&L, fee drag
- [ ] Create audit bundle: `logs/audit/YYYY-MM-DD.zip`

---

## Test Matrix

| Mode | IBKR | Data | MCP | Expectation |
|------|------|------|-----|-------------|
| **Unit** | none | mock | n/a | Gates enforced (POP/Reward/IVR/liquidity/events) |
| **Dry-run** | none | live chain | Select | TVCs produced, no order endpoints |
| **Paper** | TWS:7497 | live chain | Select+Write | Staged ➜ Executed (paper), OCO attached |
| **Live (locked)** | TWS:7496 | live chain | Select+Write | **STAGE only** until unblocked by checklist |

---

## Readiness Checklist (must all be ✅)

- [ ] **Connectivity**: TWS paper reachable; heartbeat stable
- [ ] **Safety Gates**: Unit tests green; Reward/Day & POP gates correct
- [ ] **Fees Model**: Brokerage calc validated on 2+ tickers
- [ ] **Events Feed**: earnings/FOMC/CPI/PCE wired in TVCs
- [ ] **Risk Caps**: per-trade ≤ $800, portfolio defined-risk ≤ 12% NetLiq
- [ ] **Artifacts**: snapshots, TVCs, Exec Cards, rolls, fills saved
- [ ] **Rollback**: ability to cancel open orders & disable executor quickly

---

## Acceptance Criteria

Monday run produces:
1. ✅ Morning snapshot JSON
2. ✅ ≥1 PASS TVC artifact
3. ✅ ≥1 staged execution card
4. ✅ (If allowed) ≥1 executed **paper** position with OCO brackets
5. ✅ Roll/close plans emitted
6. ✅ Audit bundle archived

---

## Operational Flow (Monday Morning)

```
09:25 ET - Pre-market
  ├─ Monitor: Snapshot (health, greeks, exposure)
  ├─ Check: TWS connectivity (paper: 7497)
  └─ Verify: No pending orders from Friday

09:30 ET - Market open
  ├─ Select: Scan universe for PASS TVCs (5-9 DTE)
  ├─ Validate: POP ≥ 75%, Reward/Day ≥ 2.5%, events, liquidity
  └─ Output: TVC artifacts

09:45 ET - Write/Stage
  ├─ Write: Create Execution Cards for PASS TVCs
  ├─ Admissibility: Check maint%, risk caps, freshness, drift
  └─ Stage: Queue cards for review (mode=STAGE)

10:00 ET - Execute (if approved)
  ├─ Execute: Place paper orders via TWS
  ├─ OCO: Attach TP (50%) + SL (2×) brackets
  └─ Log: Fills to logs/fills/

Throughout day
  ├─ Monitor: Positions, greeks, P&L
  ├─ Roll: Trigger on Δ > 0.35 or touch
  └─ Close: TP hits, expiry, stops

15:45 ET - Pre-close
  ├─ Review: Day's P&L and positions
  ├─ Plan: Roll/close actions for tomorrow
  └─ Report: DailyReport.md + audit bundle
```

---

## Notes

- **Live trades remain disabled by default** until Paper SLA is met for a week
- **Paper SLA**: 5 consecutive trading days with:
  - All gates enforced correctly
  - No duplicate orders (idempotency working)
  - OCO brackets attached to every entry
  - TP/SL triggers functioning
  - Roll logic correct (no early assignments)
- **Graduation to Live**: Requires explicit approval + config change to `mode=EXECUTE_LIVE`

---

## Dependencies

- **WP01**: RulesEngine (OptionsRadar.yaml parsing)
- **WP02**: Market data provider (option chains, greeks)
- **WP03**: Event calendar (earnings, FOMC, CPI, PCE)
- **WP04**: Monitor IBKR connection (account queries)
- **WP06**: SelectTVC (selection pipeline)
- **WP07**: WriteTVC (execution pipeline)

---

## File Structure

```
logs/
├─ snapshots/
│  └─ 2025-10-07/
│     └─ morning_09_25.json
├─ tvc/
│  └─ 2025-10-07/
│     ├─ SOFI_2025-10-14_PCS.json (PASS)
│     ├─ APP_2025-10-14_PCS.json (FAIL: low IVR)
│     └─ META_2025-10-14_PCS.json (PASS)
├─ exec/
│  ├─ staged/
│  │  ├─ SOFI_2025-10-14_PCS_exec.json
│  │  └─ META_2025-10-14_PCS_exec.json
│  └─ tickets/
│     └─ SOFI_2025-10-14_PCS_ORDER_12345.json
├─ fills/
│  └─ 2025-10-07/
│     └─ SOFI_ORDER_12345_fill.json
├─ roll/
│  └─ 2025-10-07/
│     └─ EXISTING_POSITION_roll.json
├─ close/
│  └─ 2025-10-07/
│     └─ TP_candidates.json
└─ audit/
   └─ 2025-10-07.zip (contains all above + DailyReport.md)
```

---

**Status:** 🟡 In Progress
**Owner:** AutoRevOption.Minimal + AutoRevOption.Monitor
**Target:** Monday, 2025-10-07 09:30 ET
**Last Updated:** 2025-10-05
