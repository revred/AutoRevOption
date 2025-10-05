
# SelectTVC.md
## Trade Vet Card — **Selection-Only** Spec
**Project:** AutoRevOption.Minimal  
**Scope:** Compute, vet, and score candidate option trades. **No order placement** or brokerage interactions in this module.

---

### 1) Purpose
SelectTVC standardizes *pre‑trade selection*. It ingests market/macro data via MCP, computes the TVC metrics, enforces selection gates, and emits **deterministic TVC JSON** + a human summary. Output is consumed by other modules (e.g., WriteTVC) which may decide to stage/execute later (subject to funds/leverage).

---

### 2) Responsibilities
- Fetch inputs from MCP (chains, events, portfolio health snapshot) without modifying state.
- Build spread candidates per strategy policy (PCS, BCS, BPS, BWB, etc.).
- Compute metrics:
  - POP at breakeven (broker POP if provided, fallback BSM).
  - Reward/Day on risk capital (post‑fees, 60% realization).
  - Liquidity quality (OI, bid‑ask % of credit).
  - IV/IVR, Δ bands, DTE.
- Enforce **selection gates** (no account‑level funding checks here).
- Produce **TVC JSON** (machine) and concise **Human Summary** (console/UI).
- Annotate **reasons** for PASS/FAIL with numeric contributions to a score.
- Persist selection results to `logs/tvc/YYYY‑MM‑DD/` and optional SQLite for analytics.

**Non‑Goals:** No `/orders/preview`, `/orders/place`, OCOs, queueing/exec policy, or funds checks.

---

### 3) Input Interfaces (MCP)
- `GET /options/chain?ticker=...&dte=..&side=put|call` → `[ { expiry, strike, right, bid, ask, mid, delta, iv, oi } ]`
- `GET /events/underlying?ticker=...` → `{ earnings_date, ex_div_date?, guidance_date? }`
- `GET /events/macro` → `{ fomc_dates[], cpi_date?, pce_date? }`
- `GET /portfolio/health` *(read‑only, snapshot)* → `{ delta, theta, vega, netliq, maint_pct, active_spreads, total_defined_risk }`
- Optional: `POST /options/pop` to compute broker‑aligned POP for a structure.

> SelectTVC **must not** call `/orders/preview` or any order endpoints.

---

### 4) Policy (Selection Gates)
Defaults (tunable via `policy.json`):
- POP **≥ 0.75**
- Reward/Day **≥ 2.5%** using:  
  `((0.60 × (credit_gross − fees_open)) / (risk_capital × DTE_calendar)) × 100%`
- Risk per spread **≤ $800** (based on width − credit_gross) — used only to **filter** candidates; final funds checks happen later.
- IVR **≥ 30** for income strategies
- Δ(short) in **[0.20, 0.25]** (PCS default)
- DTE **∈ [5, 9]** calendar days (weekly cadence)
- Liquidity: **OI ≥ 200** and bid‑ask ≤ **$0.05** *or* ≤ **10%** of credit
- Event proximity: avoid weeklys overlapping **earnings/FOMC/CPI/PCE** unless strategy explicitly allows.

---

### 5) Core Calculations
Given a vertical spread (width W), quote mid credit `credit_gross` and open fees `fees_open`:
- `risk_capital = W − credit_gross`
- `credit_net_open = credit_gross − fees_open`
- `expected_profit_net = 0.60 × credit_net_open`
- `RewardPerDay% = 100 × expected_profit_net / (risk_capital × DTE)`

**POP (fallback model):**
- Breakeven for PCS: `K_short − credit_gross`
- `POP = P(S_T > Breakeven)` via BSM/N(d2) using chain IV, T=DTE/365.

---

### 6) Output Schemas
#### 6.1 TVC JSON (Selection Artifact)
```json
{
  "tvc_version": "1.0",
  "timestamp_utc": "2025-10-05T12:00:00Z",
  "symbol": "SOFI",
  "strategy": "PUT_CREDIT_SPREAD",
  "legs": [
    {"side":"SELL","right":"PUT","strike":6.5,"expiry":"2025-10-10"},
    {"side":"BUY","right":"PUT","strike":5.5,"expiry":"2025-10-10"}
  ],
  "spot": 6.98,
  "dte_calendar": 7,
  "delta_short": 0.22,
  "iv": 0.46,
  "ivr": 33,
  "credit_gross": 0.38,
  "fees_open": 0.04,
  "credit_net_open": 0.34,
  "width": 1.0,
  "risk_capital": 0.62,
  "pop": 0.78,
  "reward_per_day_pct": 4.7,
  "liquidity": {"oi":2100, "bid_ask":0.03, "bid_ask_pct_of_credit":7.9},
  "events": {
    "earnings":"2025-10-28",
    "fomc":["2025-10-28","2025-10-29"],
    "cpi": null,
    "pce": "2025-10-31"
  },
  "selection": {
    "pass": true,
    "reasons": [
      "POP 78% ≥ 75%",
      "Reward/Day 4.7% ≥ 2.5%",
      "Δshort 0.22 within [0.20,0.25]",
      "Liquidity OK",
      "IVR 33 ≥ 30"
    ],
    "score": 0.82
  },
  "human_summary": "SOFI PCS 6.5/5.5 (7DTE) — POP 78%, Reward/Day 4.7%, OI 2.1k, $0.03 spread; ER 10/28, FOMC 10/28–29, PCE 10/31 — PASS"
}
```
**Notes**
- `score` ∈ [0,1] is optional; combine normalized sub‑scores (POP, Reward/Day, Liquidity quality, IVR) with weights from `policy.json`.
- `pass=true` only means the idea is **selectable**; it is *not* an execution approval.

#### 6.2 Human Summary (single line)
```
<MNEMO> <STRUCTURE> <STRIKES> (<DTE>DTE) — POP <x%>, R/Day <y%>, OI <n>, Spread <$>
Events: <key ones> — PASS/FAIL (<top reason>)
```

---

### 7) Files, Logs, Persistence
- Write each artifact to: `logs/tvc/<YYYY-MM-DD>/<SYMBOL>_<EXP>_<STRUCT>.json`
- Write human summary to `logs/tvc/<YYYY-MM-DD>/summary.md`
- Optional SQLite (`db/tvc.sqlite`): table `tvc_selection` with columns mirroring the JSON.

---

### 8) Configuration
`policy.json` keys consumed by SelectTVC:
```json
{
  "selection_gates": {
    "min_pop": 0.75,
    "min_reward_per_day_pct": 2.5,
    "realization_haircut": 0.60,
    "delta_band": [0.20, 0.25],
    "dte_min": 5,
    "dte_max": 9,
    "min_ivr": 30,
    "min_oi": 200,
    "max_spread_bidask_abs": 0.05,
    "max_spread_bidask_pct_of_credit": 10,
    "avoid_events": ["earnings","fomc","cpi","pce"]
  },
  "risk": { "max_spread_loss_usd": 800 }
}
```

---

### 9) C# Interfaces
```csharp
public record TVCSelection(
    string Symbol, string Strategy, IReadOnlyList<Leg> Legs,
    decimal Spot, int DteCalendar, decimal DeltaShort, decimal Iv, int Ivr,
    decimal CreditGross, decimal FeesOpen, decimal CreditNetOpen,
    decimal Width, decimal RiskCapital, decimal Pop, decimal RewardPerDayPct,
    Liquidity Liquidity, Events Events, SelectionResult Selection, string HumanSummary);

public interface ISelectTvcService
{
    Task<IReadOnlyList<TVCSelection>> EvaluateAsync(SelectionRequest req, CancellationToken ct);
}
```
`EvaluateAsync` **must not** call any order endpoints.
