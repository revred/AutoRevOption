
# WriteTVC.md
## Trade Vet Card — **Write/Act Layer** Spec
**Project:** AutoRevOption.Minimal  
**Scope:** Consume **selection artifacts** (TVC JSON) and decide whether to **stage**, **queue**, or **execute** orders. This module performs funding/leverage checks and interacts with brokerage endpoints **optionally** via your MCP. It never computes selection metrics—that’s SelectTVC’s job.

---

### 1) Purpose
WriteTVC bridges *selected* ideas to actionable orders while enforcing account‑level constraints (funds, leverage, maintenance, exposure caps). It supports three modes:
- **Dry‑Run**: persist intent only (no broker calls).
- **Stage**: create an execution ticket and await human/automation approval.
- **Execute**: place orders with OCO brackets, subject to admissibility checks.

---

### 2) Responsibilities
- Load TVC JSON from SelectTVC (filesystem/db/queue).
- Re‑check **admissibility** at account level (NetLiq, maintenance %, exposure caps).
- Optionally call `/orders/preview` for fee/margin validation at the intended limit.
- Decide **Action**: REJECT | QUEUE | STAGE | EXECUTE.
- If executing, submit `/orders/place` with OCO (TP/SL) and write an immutable **Execution Card**.
- Handle `/fills` webhook to update state and roll policies (handled outside this spec if needed).

**Non‑Goals:** No recalculation of POP, Reward/Day, IVR, Δ, etc. No re‑screening of candidates beyond admissibility and freshness (age/price drift).

---

### 3) Inputs
- **Selection Artifact (TVC JSON)** from SelectTVC.
- `GET /portfolio/health` for *current* funds/leverage.
- Optional `POST /orders/preview` for final validation at the chosen limit price.

---

### 4) Admissibility Rules (Account-Level)
- `maint_pct < 35`
- `total_defined_risk + tvc.risk_capital ≤ 12% of NetLiq`
- `active_spreads < max_active_spreads` (policy)
- `per_trade_max_loss_usd ≤ 800`
- `symbol_exposure_limits` (per‑ticker cap of defined risk, e.g., 4% NetLiq)
- **Freshness**: TVC age ≤ `max_age_minutes` and **drift**: |mid − tvc.credit_gross| ≤ `max_credit_drift`

These are **hard gates**. If any fails → **REJECT** or **QUEUE** (if allowed).

---

### 5) Actions & Queues
- **REJECT**: Write reason; archive under `logs/exec/rejects/`.
- **QUEUE**: Insufficient funds or over‑exposed; store under `queues/ready/` with TTL; retry when health improves.
- **STAGE**: Generate an execution ticket requiring human approval; write to `tickets/pending/`.
- **EXECUTE**: Call `/orders/place` with OCO brackets from policy.

All actions write an **Execution Card** (see §6).

---

### 6) Execution Card (EC) Schema
```json
{
  "ec_version": "1.0",
  "timestamp_utc": "2025-10-05T12:05:00Z",
  "mode": "EXECUTE", // DRY_RUN | STAGE | EXECUTE | QUEUE | REJECT
  "tvc_ref": "logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json",
  "symbol": "SOFI",
  "strategy": "PUT_CREDIT_SPREAD",
  "legs": [
    {"side":"SELL","right":"PUT","strike":6.5,"expiry":"2025-10-10"},
    {"side":"BUY","right":"PUT","strike":5.5,"expiry":"2025-10-10"}
  ],
  "intended_credit_limit": 0.38,
  "brackets": {"tp_pct": 50, "sl_multiple_credit": 2.0, "time_in_force": "GTC"},
  "admissibility": {
    "maint_pct_ok": true,
    "defined_risk_ok": true,
    "symbol_exposure_ok": true,
    "fresh_enough": true,
    "credit_drift_ok": true,
    "reasons": []
  },
  "broker_preview": {
    "max_loss": 62,
    "fees_total": 0.04,
    "margin_effect": 62,
    "est_fill_prob": 0.62
  },
  "action_result": {
    "status": "PLACED",
    "order_id": "A1B2C3",
    "notes": "OCO attached"
  }
}
```

---

### 7) MCP Endpoints (Write Layer)
- `POST /orders/preview` → `{ max_loss, fees_total, margin, est_fill_prob }`
- `POST /orders/place` (idempotent; include clientOrderId) → `{ orderId, status }`
- `POST /orders/oco` (if not inline) → `{ status }`
- `POST /orders/cancel`
- `POST /fills` (webhook from broker → stored under `logs/fills/`)

WriteTVC **must not** mutate selection artifacts.

---

### 8) Policy (Write Layer)
```json
{
  "execution": {
    "mode": "STAGE",           // DRY_RUN | STAGE | EXECUTE
    "tp_pct": 50,
    "sl_multiple_credit": 2.0,
    "tif": "GTC",
    "max_active_spreads": 5,
    "max_age_minutes": 15,
    "max_credit_drift": 0.05
  },
  "exposure_caps": {
    "portfolio_defined_risk_pct": 12,
    "per_trade_max_loss_usd": 800,
    "per_symbol_defined_risk_pct": 4
  }
}
```

---

### 9) C# Interfaces
```csharp
public interface IWriteTvcService
{
    Task<ExecutionCard> ActAsync(TVCSelection tvc, ExecutionRequest req, CancellationToken ct);
}
```
- `ActAsync` performs admissibility checks, optional broker preview, and either queues, stages, or executes.
- It **never** recomputes selection metrics.

---

### 10) File & Audit Layout
- `logs/exec/cards/` — all Execution Cards (JSON)
- `logs/exec/rejects/` — rejected with reasons
- `tickets/pending/` — staged tickets awaiting approval
- `queues/ready/` — queued actions waiting for funds
- `logs/fills/` — broker fills (immutable append‑only)

---

### 11) Safety & Idempotency
- Use **clientOrderId** derived from TVC hash + timestamp to prevent duplicates.
- Two‑phase place: preview → place; if preview drifts beyond thresholds, abort.
- On retries, ensure last action result is read and compared before re‑issuing.

---

### 12) Extensibility
- Plug‑in bracket templates (ICS, BWB roll kits).
- Per‑symbol exposure caps and blackout windows.
- Human‑in‑the‑loop approval via MCP `POST /tickets/approve`.
