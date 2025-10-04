# WP01 — IBKR Connection & Base Rules Engine

**Goal:** Prove connectivity to IBKR (demo), draft the initial Rules Engine for entries/exits that can be tuned by `OptionsRadar.yaml`.

## Tasks
- IBKR Client Portal: auth flow (secure storage), heartbeat, account snapshot.
- Rules Engine v0: parse YAML (risk, income, convex), expose policy in code.
- Define Profit/Stop policies: TP 50–60% credit, SL 2x credit or short strike touch.
- Dry-run order builder for combos + OCA (no transmit).

## Deliverables
- `IbkrClient.cs` (demo heartbeat + account read).
- `RulesEngine.cs` with unit tests.
- Config: `OptionsRadar.yaml` overrides.
- Demo: print 3 mock candidates → build OrderPlan JSON with exits.