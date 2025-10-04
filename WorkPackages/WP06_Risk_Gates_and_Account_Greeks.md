# WP06 — Risk Gates & Account Greeks

**Goal:** Enforce risk caps (≤ $800 ML/debit, ≤5 spreads, margin < 35%, Δ/Θ limits).

## Tasks
- Account snapshot aggregation; projected margin impact.
- Risk evaluation before `act`.
- Block and explain failures with actionable text.

## Deliverables
- `RiskService.cs` + tests.