# WriteTVC.md
## Trade Vet Card — **Write/Act Layer** Spec
**Scope:** Consume TVC JSON and decide whether to **queue**, **stage**, or **execute** based on funds/leverage caps.

---

## 1. Purpose

**WriteTVC** is the *execution layer* that:
1. Loads a TVC JSON artifact from SelectTVC
2. Performs **admissibility checks** (funds, leverage, position limits)
3. Optionally previews current market price vs. TVC credit (drift check)
4. Based on `mode`, either:
   - **DRY_RUN:** Log intent, no broker call
   - **STAGE:** Queue for manual review/approval
   - **EXECUTE:** Place order via IBKR API

**Key distinction:** WriteTVC is the **only component** that can place orders.

---

## 2. Architecture

```
WriteTVC Pipeline:
  ┌─────────────┐
  │ TVC JSON    │ (from SelectTVC)
  │ Artifact    │
  └─────────────┘
         │
         v
  ┌─────────────┐
  │ WriteTVC    │──────> execution_policy:
  │  Service    │          mode: STAGE
  └─────────────┘          tp_pct: 50, sl_multiple_credit: 2.0
         │                 max_active_spreads: 5
         v                 portfolio_defined_risk_pct: 12
  ┌─────────────┐          per_trade_max_loss_usd: 800
  │Admissibility│          per_symbol_defined_risk_pct: 4
  │   Checks    │          max_age_minutes: 15
  └─────────────┘          max_credit_drift: 0.05
         │
         v
  ┌─────────────┐
  │ Execution   │  DRY_RUN → log
  │   Card      │  STAGE   → logs/exec/staged/
  └─────────────┘  EXECUTE → IBKR API + logs/exec/tickets/
```

---

## 3. Data Model

### 3.1 ExecutionCard Record

```csharp
public record ExecutionCard(
    string Mode,                      // "DRY_RUN" | "STAGE" | "EXECUTE"
    string TvcRef,                    // Path to source TVC JSON
    string Symbol,
    string Strategy,
    IReadOnlyList<Leg> Legs,
    decimal IntendedCreditLimit,
    object Brackets,                  // TP/SL brackets (OCA group)
    object Admissibility,             // Check results
    object? BrokerPreview,            // Optional: current market vs. TVC credit
    object ActionResult               // DRY_RUN | STAGED | ORDER_ID
);
```

---

## 4. Admissibility Checks

### 4.1 Maintenance Percentage

```csharp
decimal current_maint_pct = account.MaintReq / account.NetLiq;
decimal projected_maint_req = current_maint_req + trade.margin;
decimal projected_maint_pct = projected_maint_req / account.NetLiq;

bool maint_ok = projected_maint_pct <= execution_policy.max_maint_pct;  // e.g., 0.35
```

### 4.2 Portfolio Defined Risk

```csharp
decimal current_defined_risk = positions.Sum(p => p.MaxLoss);
decimal projected_defined_risk = current_defined_risk + tvc.RiskCapital;
decimal projected_risk_pct = 100m * projected_defined_risk / account.NetLiq;

bool risk_ok = projected_risk_pct <= execution_policy.portfolio_defined_risk_pct;  // 12%
```

### 4.3 Per-Trade Max Loss

```csharp
bool trade_size_ok = tvc.RiskCapital <= execution_policy.per_trade_max_loss_usd;  // $800
```

### 4.4 Per-Symbol Exposure

```csharp
decimal symbol_risk = positions
    .Where(p => p.Symbol == tvc.Symbol)
    .Sum(p => p.MaxLoss);
decimal symbol_risk_pct = 100m * symbol_risk / account.NetLiq;

bool symbol_ok = symbol_risk_pct <= execution_policy.per_symbol_defined_risk_pct;  // 4%
```

### 4.5 Active Spread Count

```csharp
int active_spreads = positions.Count(p => p.IsSpread && p.IsOpen);
bool spread_count_ok = active_spreads < execution_policy.max_active_spreads;  // 5
```

### 4.6 TVC Freshness

```csharp
TimeSpan age = DateTime.UtcNow - tvc.CreatedAt;
bool fresh_enough = age.TotalMinutes <= execution_policy.max_age_minutes;  // 15
```

### 4.7 Credit Drift Check (Optional)

If `BrokerPreview` is requested:
```csharp
decimal current_mid_credit = (bid + ask) / 2;
decimal drift = Math.Abs(current_mid_credit - tvc.CreditGross) / tvc.CreditGross;
bool drift_ok = drift <= execution_policy.max_credit_drift;  // 5%
```

---

## 5. Execution Modes

### 5.1 DRY_RUN

- **Action:** Log intended order to console + `logs/exec/dry_run/`
- **Broker call:** None
- **Use:** Validate logic without market impact

### 5.2 STAGE

- **Action:** Write execution card to `logs/exec/staged/`
- **Broker call:** Optional preview (market data only)
- **Use:** Queue for human approval; can be batch-executed later

### 5.3 EXECUTE

- **Action:** Place combo order + OCA brackets via IBKR API
- **Broker call:** `placeOrder()` with `clientOrderId = Hash(TVC)`
- **Idempotency:** Same TVC → same `clientOrderId` → duplicate reject
- **Result:** Order ID logged to `logs/exec/tickets/`

---

## 6. OCA Brackets

For every entry combo order, attach exit brackets:

```csharp
public record OcaBracket(
    string OcaGroup,       // e.g., "OCA-A3F5C2B1"
    LimitOrder TakeProfit, // TP at 50% of max profit
    StopOrder StopLoss     // SL at 2× credit (total loss = 2× risk)
);

decimal tp_price = entry_credit × (1 - execution_policy.tp_pct / 100m);  // 50% → exit at 50% of credit
decimal sl_price = entry_credit × execution_policy.sl_multiple_credit;   // 2.0× → exit at 2× credit
```

**Time In Force:** `execution_policy.tif` (default: GTC)

---

## 7. CLI Usage

```bash
cd AutoRevOption.Minimal
dotnet run -- write --tvc logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json --mode STAGE
```

**Output:**
- Console: Admissibility results
- File: `logs/exec/staged/SOFI_2025-10-10_PCS_exec.json`

**Execute Mode:**
```bash
dotnet run -- write --tvc logs/tvc/2025-10-05/SOFI_2025-10-10_PCS.json --mode EXECUTE
```
Requires Monitor/TWS connection.

---

## 8. MCP Tools

### 8.1 `build_order_plan`
**Input:** `{ tvc_path }`
**Output:** Order plan with combo + OCA brackets (no placement)

### 8.2 `get_account_status`
**Input:** `{}`
**Output:** `{ net_liq, maint_pct, active_spreads, defined_risk_pct }`

### 8.3 `act_on_order`
**Input:** `{ tvc_path, mode }`
**Output:** ExecutionCard JSON

---

## 9. File Layout

```
logs/
└─ exec/
   ├─ dry_run/
   │  └─ SOFI_2025-10-10_PCS_dry.json
   ├─ staged/
   │  └─ SOFI_2025-10-10_PCS_exec.json
   ├─ tickets/
   │  └─ SOFI_2025-10-10_PCS_ORDER_12345.json
   ├─ rejects/
   │  └─ APP_2025-10-12_PCS_REJECT_MAINT.json
   └─ cards/
      └─ 2025-10-05_SOFI_card.json
```

- **dry_run/**: DRY_RUN mode outputs
- **staged/**: STAGE mode outputs (awaiting approval)
- **tickets/**: EXECUTE mode outputs (order placed)
- **rejects/**: Failed admissibility checks
- **cards/**: All ExecutionCard artifacts

---

## 10. Safety Guarantees

1. **Gated execution** — All checks must pass before EXECUTE
2. **Idempotent** — Same TVC → same `clientOrderId` → no duplicate orders
3. **Fallback to STAGE** — If unsure, queue for human review
4. **Audit trail** — Every action logged with timestamp, mode, results

---

## 11. Testing

See `AutoRevOption.Tests/WriteTvcTests.cs`:

```csharp
[Fact]
public async Task AdmissibilityRejectsWhenMaintenanceTooHigh()
{
    var account = new Account { NetLiq = 10000m, MaintReq = 3400m };
    var tvc = LoadTvc("SOFI_2025-10-10_PCS.json");
    var svc = new WriteTvcService(account);

    var card = await svc.ActAsync(new ExecutionRequest("DRY_RUN", tvc.Path), ct);

    Assert.False(card.Admissibility.maint_pct_ok);
    Assert.Contains("REJECT_MAINT", card.ActionResult.notes);
}
```

---

## 12. Integration with SelectTVC

```
SelectTVC (selection)
    ↓ produces
  TVC JSON
    ↓ consumed by
WriteTVC (execution)
    ↓ produces
Execution Card (staged/executed)
```

**Key:** SelectTVC never knows about WriteTVC; WriteTVC depends on SelectTVC artifacts.

---

## 13. Configuration (OptionsRadar.yaml)

```yaml
execution_policy:
  mode: STAGE
  tp_pct: 50
  sl_multiple_credit: 2.0
  tif: GTC
  max_active_spreads: 5
  portfolio_defined_risk_pct: 12
  per_trade_max_loss_usd: 800
  per_symbol_defined_risk_pct: 4
  max_age_minutes: 15
  max_credit_drift: 0.05
```

---

**Document Version:** 1.0
**Last Updated:** 2025-10-05
**Owner:** AutoRevOption.Minimal
