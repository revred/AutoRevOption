# WP11 — Persistence, KPIs & Audit

**Goal:** Store scans, plans, actions, fills; compute weekly KPIs and fee ratios.

## Tasks
- SQLite/LiteDB schema; append‑only logs.
- KPI report (WTD/MTD): hit‑rate, avg win/loss, fees %.
- Export CSV/Markdown for reviews.

## Deliverables
- `Store.cs`, `Reports.cs` + scheduled summary.