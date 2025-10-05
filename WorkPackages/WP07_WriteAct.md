# WP07 — Write/Act (WriteTVC)
**Goal:** Consume TVCs and decide QUEUE/STAGE/EXECUTE; handle funds/leverage.

---

## Overview

Implement the **WriteTVC** service that consumes Trade Vet Cards from SelectTVC, performs admissibility checks (funds, leverage, position limits), and executes orders based on configured mode (DRY_RUN, STAGE, EXECUTE).

**Critical:** WriteTVC is the **only component** allowed to place broker orders.

---

## Tasks

### 1. Core Implementation

- [ ] Implement `IWriteTvcService` interface with `ActAsync()` method
- [ ] Load `OptionsRadar.yaml` execution_policy configuration
- [ ] Parse TVC JSON artifacts from `logs/tvc/`
- [ ] Extract order details (symbol, legs, credit, brackets)
- [ ] Build ExecutionCard artifact with mode, admissibility, action results

### 2. Admissibility Checks

Implement all checks per `DOCS/WriteTVC.md`:

- [ ] **Maintenance Percentage:**
  - Query current account `MaintReq` and `NetLiq`
  - Project maintenance with new trade margin
  - Enforce: `projected_maint_pct ≤ max_maint_pct` (e.g., 35%)

- [ ] **Portfolio Defined Risk:**
  - Sum current positions' max loss
  - Add TVC risk capital
  - Enforce: `projected_risk_pct ≤ portfolio_defined_risk_pct` (default 12%)

- [ ] **Per-Trade Max Loss:**
  - Enforce: `tvc.RiskCapital ≤ per_trade_max_loss_usd` (default $800)

- [ ] **Per-Symbol Exposure:**
  - Sum symbol-specific position risk
  - Enforce: `symbol_risk_pct ≤ per_symbol_defined_risk_pct` (default 4%)

- [ ] **Active Spread Count:**
  - Count open spread positions
  - Enforce: `active_spreads < max_active_spreads` (default 5)

- [ ] **TVC Freshness:**
  - Check TVC creation timestamp
  - Enforce: `age ≤ max_age_minutes` (default 15)

- [ ] **Credit Drift Check (Optional):**
  - Query current mid credit from broker
  - Calculate drift: `|current - tvc| / tvc`
  - Enforce: `drift ≤ max_credit_drift` (default 5%)

### 3. Order Building

- [ ] Build combo order from TVC legs (BAG contract)
- [ ] Set limit price to TVC credit
- [ ] Generate `clientOrderId = Hash(TVC)` for idempotency
- [ ] Create OCA brackets:
  - [ ] **Take Profit:** Limit order at `credit × (1 - tp_pct/100)` (default 50%)
  - [ ] **Stop Loss:** Stop order at `credit × sl_multiple_credit` (default 2.0×)
  - [ ] Assign OCA group ID: `"OCA-{GUID}"`
  - [ ] Set Time In Force per `execution_policy.tif` (default GTC)

### 4. Execution Modes

- [ ] **DRY_RUN:**
  - Log order intent to console
  - Write ExecutionCard to `logs/exec/dry_run/`
  - **No broker calls** (except optional account query)

- [ ] **STAGE:**
  - Perform admissibility checks
  - Optionally preview current market (drift check)
  - Write ExecutionCard to `logs/exec/staged/`
  - **No order placement**

- [ ] **EXECUTE:**
  - Perform all admissibility checks
  - Place combo order via IBKR API
  - Place OCA bracket orders
  - Write ExecutionCard to `logs/exec/tickets/` with order ID
  - Handle rejection → write to `logs/exec/rejects/`

### 5. Logging and Persistence

- [ ] Create directory structure:
  ```
  logs/exec/
  ├─ dry_run/
  ├─ staged/
  ├─ tickets/
  ├─ rejects/
  └─ cards/
  ```
- [ ] Write ExecutionCard JSON for every action
- [ ] Log admissibility failures with reasons
- [ ] (Optional) Store execution history in SQLite: `db/exec.sqlite`

### 6. CLI Integration

- [ ] Add `write` verb to `AutoRevOption.Minimal/Program.cs`
- [ ] Support arguments: `--tvc`, `--mode`
- [ ] Example: `dotnet run -- write --tvc logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json --mode STAGE`
- [ ] Display ExecutionCard summary and file path on completion

### 7. MCP Tools

- [ ] Implement `build_order_plan` tool:
  - Input: `{ tvc_path }`
  - Output: Order plan with combo + OCA brackets (no placement)

- [ ] Implement `get_account_status` tool:
  - Input: `{}`
  - Output: `{ net_liq, maint_pct, active_spreads, defined_risk_pct }`

- [ ] Implement `act_on_order` tool:
  - Input: `{ tvc_path, mode }`
  - Output: ExecutionCard JSON

### 8. Idempotency

- [ ] Generate deterministic `clientOrderId` from TVC hash
- [ ] Handle IBKR duplicate order rejection gracefully
- [ ] Log duplicate attempts to `logs/exec/duplicates/`
- [ ] Ensure same TVC → same order → no double execution

### 9. Error Handling

- [ ] Catch admissibility failures → reject
- [ ] Catch broker connection errors → retry with exponential backoff
- [ ] Catch order rejection errors → log to rejects/
- [ ] Provide clear error messages in ExecutionCard

### 10. Testing

- [ ] Write unit tests in `AutoRevOption.Tests/WriteTvcTests.cs`:
  - [ ] Test admissibility checks (pass/fail scenarios)
  - [ ] Test maintenance percentage calculation
  - [ ] Test portfolio defined risk enforcement
  - [ ] Test per-trade and per-symbol limits
  - [ ] Test TVC freshness check
  - [ ] Test credit drift calculation (optional)
  - [ ] Test OCA bracket generation
  - [ ] Test clientOrderId determinism
  - [ ] Test DRY_RUN mode (no broker calls)
  - [ ] Test STAGE mode (staged queue)
  - [ ] Test EXECUTE mode (mock IBKR API)
- [ ] Add integration test for end-to-end WriteTVC flow

---

## Deliverables

- [ ] **Code:**
  - `AutoRevOption.Minimal/Services/IWriteTvcService.cs`
  - `AutoRevOption.Minimal/Services/WriteTvcService.cs`
  - Updated `AutoRevOption.Minimal/Program.cs` with `write` verb
  - Updated `ExecuteContext.cs` with new MCP tools

- [ ] **Documentation:**
  - `DOCS/WriteTVC.md` (specification)

- [ ] **Tests:**
  - `AutoRevOption.Tests/WriteTvcTests.cs` with ≥8 test cases

- [ ] **Artifacts:**
  - Sample ExecutionCard JSON files in `logs/exec/` demonstrating all modes

---

## Acceptance Criteria

1. ✅ `WriteTvcService.ActAsync()` produces valid ExecutionCard records
2. ✅ All admissibility checks enforced per `OptionsRadar.yaml`
3. ✅ OCA brackets generated with correct TP/SL prices
4. ✅ Idempotent execution via deterministic `clientOrderId`
5. ✅ DRY_RUN mode operates without broker calls
6. ✅ STAGE mode queues for manual approval
7. ✅ EXECUTE mode places orders via IBKR API
8. ✅ CLI `write` command produces ExecutionCard artifacts
9. ✅ MCP tools `build_order_plan`, `get_account_status`, `act_on_order` functional
10. ✅ Unit tests demonstrate admissibility behavior (≥90% code coverage)

---

## Dependencies

- **WP06:** SelectTVC implementation (TVC JSON artifacts)
- **WP04:** Monitor IBKR connection (for account queries and order placement)
- **WP01:** RulesEngine implementation (for YAML parsing)

---

## Timeline

- **Phase 1:** Core implementation + admissibility checks (4 days)
- **Phase 2:** Order building + OCA brackets (2 days)
- **Phase 3:** Execution modes + CLI (2 days)
- **Phase 4:** Testing + documentation (2 days)
- **Total:** ~10 days

---

## Safety Notes

- **WriteTVC is the ONLY place orders can be placed** — strict access control required
- **Default mode should be STAGE** — require explicit `--mode EXECUTE` flag
- **Never auto-execute** — always require human approval for EXECUTE mode
- **Log everything** — audit trail is critical for post-trade review
- **Fail safe** — if unsure, reject and queue for human review

---

## Notes

- Prioritize **safety** — multiple checks before order placement
- Ensure **idempotency** — duplicate TVC should not create duplicate orders
- Design for **transparency** — every decision logged with reasons
- Support **batch execution** — ability to process staged queue in bulk

---

**Status:** 🟡 In Progress
**Owner:** AutoRevOption.Minimal
**Last Updated:** 2025-10-05
