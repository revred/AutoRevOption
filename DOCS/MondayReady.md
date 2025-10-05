# Monday Ready — Operational Checklist & Runbook

This is the step-by-step for **Monday morning**. Selection and execution are **separate**. Live trading remains **off** until paper SLA is met.

## 0) Modes
- **DRY_RUN**: selection only, no orders, writes TVCs.
- **PAPER**: selection + write with paper TWS (7497). Orders are allowed if admissible.
- **LIVE_LOCKED**: selection + write, but execution hard-disabled (stage only).

## 1) Pre-flight (15 min)
- Verify TWS **paper** connection (Monitor app).
- Run `scripts/monday-smoke.sh` (or `.ps1`) to generate:
  - snapshot JSON (`logs/snapshots/...`)
  - at least 3 TVCs across tickers
  - staged Execution Cards (`mode=STAGE`)

## 2) Selection (SelectTVC)
```
dotnet run --project AutoRevOption.Minimal -- select --universe SOFI,APP,RKLB,META,AMD,GOOGL --dte 5-9
```
Outputs TVCs; gates: **POP ≥ 75%**, **Reward/Day ≥ 2.5%**, IVR/liquidity/event checks.

## 3) Write/Stage (WriteTVC)
```
dotnet run --project AutoRevOption.Minimal -- write --mode STAGE --input logs/tvc/$(date +%F)/*.json
```
Admissibility gates: maint%, defined-risk caps, per-symbol caps, freshness & drift.

## 4) Execute (Paper)
If `OptionsRadar.yaml: run.mode=PAPER`, executor may place paper orders with OCO (TP 50%, SL 2×).

## 5) Roll & Close
- Roll triggers: Δshort > 0.35 or strike touch; write roll plans.
- Auto-close at TP; document stop actions.

## 6) Reporting
Run daily report aggregation to produce `DailyReport.md` and zip logs.

## SLA to unlock LIVE
- 5 consecutive trading days of paper runs with:
  - ≥ 80% adherence to gates,
  - net ≥ 0 P&L (after fees) for the period,
  - no policy violations.

## Kill-switch
- Set `OptionsRadar.yaml: run.mode=LIVE_LOCKED` to disable execution immediately.

## Artefact Paths
- `logs/snapshots/YYYY-MM-DD/*.json`
- `logs/tvc/YYYY-MM-DD/*.json`
- `logs/exec/cards/*.json` (mode recorded)
- `logs/roll/YYYY-MM-DD/*.json`
- `logs/fills/*.json`

---
**Reminder:** Never blend selection with execution. Selection emits **TVCs** only.
