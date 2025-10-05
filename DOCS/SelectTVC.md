# SelectTVC.md
## Trade Vet Card — **Selection-Only** Spec
**Project:** AutoRevOption.Minimal
**Scope:** Compute, vet, and score candidate option trades. **No order placement**.

---

## 1. Purpose

**SelectTVC** is the *selection-only* pipeline that:
1. Queries option chains for candidate spreads
2. Computes Greeks, POP (probability of profit), and Reward/Day metrics
3. Enforces hard gates (POP ≥ 75%, Reward/Day ≥ 2.5%, liquidity, event avoidance)
4. Produces **Trade Vet Cards (TVCs)** — JSON artifacts with PASS/FAIL verdicts

**Crucially:** SelectTVC **never** calls broker order placement APIs. It is safe to run continuously.

---

## 2. Architecture

```
SelectTVC Pipeline:
  ┌─────────────┐
  │ OptionsRadar│──────> selection_gates:
  │   .yaml     │          min_pop: 0.75
  └─────────────┘          min_reward_per_day_pct: 2.5
         │                 realization_haircut: 0.60
         v                 dte_min/max, min_ivr, min_oi
  ┌─────────────┐          max_spread_bidask_abs/pct
  │ SelectTVC   │          avoid_events: [earnings, fomc, cpi, pce]
  │  Service    │
  └─────────────┘
         │
         v
  ┌─────────────┐
  │ TVC JSON    │  PASS/FAIL + reasons + score
  │ Artifact    │
  └─────────────┘
```

---

## 3. Data Model

### 3.1 TVCSelection Record

```csharp
public record TVCSelection(
    string Symbol,
    string Strategy,              // "PUT_CREDIT_SPREAD", "CALL_CREDIT_SPREAD", etc.
    IReadOnlyList<Leg> Legs,
    decimal Spot,
    int DteCalendar,
    decimal DeltaShort,
    decimal Iv,
    int Ivr,
    decimal CreditGross,
    decimal FeesOpen,
    decimal CreditNetOpen,
    decimal Width,
    decimal RiskCapital,
    decimal Pop,                  // Probability of profit (breakeven-based)
    decimal RewardPerDayPct,      // 100 × (expected net) / (risk capital × DTE)
    Liquidity Liquidity,
    Events Events,
    SelectionResult Selection,
    string HumanSummary
);

public record Leg(string Side, string Right, decimal Strike, string Expiry);
public record Liquidity(int Oi, decimal BidAsk, decimal BidAskPctOfCredit);
public record Events(string? Earnings, string[]? Fomc, string? Cpi, string? Pce);
public record SelectionResult(bool Pass, IReadOnlyList<string> Reasons, decimal Score);
```

---

## 4. Computation Formulas

### 4.1 Probability of Profit (POP)

For a **PUT Credit Spread** (short strike `S_short`, long strike `S_long`, spot `S_0`):

```
breakeven = S_short - credit_net
POP = Prob(S_expiry > breakeven)
    ≈ (S_0 - breakeven) / (S_0 - S_long)  [linear approximation]
```

Or use standard normal CDF with implied volatility for exact POP.

**Gate:** `POP ≥ selection_gates.min_pop` (default 0.75 = 75%)

### 4.2 Reward Per Day

```
risk_capital = width - credit_gross
credit_net_open = credit_gross - fees_open
expected_net = realization_haircut × credit_net_open
reward_per_day_pct = 100 × expected_net / (risk_capital × DTE)
```

**Gate:** `reward_per_day_pct ≥ selection_gates.min_reward_per_day_pct` (default 2.5%)

**Rationale:** With 60% realization haircut, we assume we close at 40% of max profit on average.

### 4.3 Liquidity

- **Open Interest (OI):** `oi ≥ min_oi` (default 200)
- **Bid-Ask Spread:**
  - Absolute: `bid_ask ≤ max_spread_bidask_abs` (default $0.05)
  - Relative: `bid_ask / credit_gross ≤ max_spread_bidask_pct_of_credit` (default 10%)

### 4.4 Event Avoidance

Check if DTE window overlaps with:
- **Earnings:** ±1 day from ER date
- **FOMC:** ±1 day from FOMC decision
- **CPI/PCE:** ±1 day from release

**Action:** If overlap detected, add warning to `Selection.Reasons` and optionally fail the gate.

---

## 5. Selection Gates (OptionsRadar.yaml)

```yaml
selection_gates:
  min_pop: 0.75
  min_reward_per_day_pct: 2.5
  realization_haircut: 0.60
  dte_min: 5
  dte_max: 9
  min_ivr: 30
  min_oi: 200
  max_spread_bidask_abs: 0.05
  max_spread_bidask_pct_of_credit: 0.10
  avoid_events: [earnings, fomc, cpi, pce]
```

---

## 6. CLI Usage

```bash
cd AutoRevOption.Minimal
dotnet run -- select --symbol SOFI --dte 7
```

**Output:**
- Console: Human summary (POP, Reward/Day, OI, events)
- File: `logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json`

---

## 7. MCP Tools

### 7.1 `scan_candidates`
**Input:** `{ symbol, dte_min, dte_max, strategy }`
**Output:** Array of TVCSelection JSON

### 7.2 `validate_candidate`
**Input:** `{ symbol, strikes[], expiry, strategy }`
**Output:** Single TVCSelection with gate verdicts

### 7.3 `verify_candidate`
**Input:** TVC JSON
**Output:** Re-run gates + freshness check

---

## 8. Scoring (Optional)

```csharp
decimal score =
    0.40m × (pop - 0.75m) / 0.25m +              // POP: 75-100% → 0-0.40
    0.30m × (reward_per_day_pct - 2.5m) / 7.5m + // Reward/Day: 2.5-10% → 0-0.30
    0.20m × (ivr / 100m) +                        // IVR: 0-100 → 0-0.20
    0.10m × Math.Min(liquidity.Oi / 5000m, 1.0m); // OI: 0-5000+ → 0-0.10
```

**Range:** [0.0, 1.0]
**Use:** Prioritize trades when multiple candidates pass gates

---

## 9. File Layout

```
logs/
└─ tvc/
   └─ 2025-10-05/
      ├─ SOFI_2025-10-10_PCS.json
      ├─ APP_2025-10-12_PCS.json
      └─ META_2025-10-11_CCS.json
```

Each TVC JSON contains:
- Full candidate details
- Gate evaluation results
- Human summary
- Timestamp

---

## 10. Safety Guarantees

1. **No broker calls** — SelectTVC only queries market data (chains, greeks, events)
2. **Deterministic** — Same inputs → same TVC output
3. **Idempotent** — Can re-run without side effects
4. **Continuous screening** — Safe to run 24/7 even when funds are capped

---

## 11. Testing

See `AutoRevOption.Tests/SelectTvcTests.cs`:

```csharp
[Fact]
public async Task SelectionProducesPassWithGatesSatisfied()
{
    var svc = new SelectTvcService();
    var res = await svc.EvaluateAsync(new SelectionRequest("SOFI", 5, 9), ct);
    var tvc = res.Single();

    Assert.True(tvc.Selection.Pass);
    Assert.True(tvc.Pop >= 0.75m);
    Assert.True(tvc.RewardPerDayPct >= 2.5m);
}
```

---

## 12. Next Steps

After SelectTVC produces passing TVCs:
1. Human review (optional)
2. Feed to **WriteTVC** for admissibility checks and execution
3. Store in `db/tvc.sqlite` for audit trail

---

**Document Version:** 1.0
**Last Updated:** 2025-10-05
**Owner:** AutoRevOption.Minimal
