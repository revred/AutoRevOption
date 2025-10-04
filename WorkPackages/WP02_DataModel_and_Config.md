# WP02 — Data Model & Config Loader

**Goal:** Finalize models (Candidate, OrderPlan, Risk) and load `OptionsRadar.yaml`.

## Tasks
- Models: ensure serializable, stable IDs, audit fields.
- YAML loader (YamlDotNet); env var overrides.
- Validation schema for config (ranges, enums).

## Deliverables
- `Models.cs`, `Config.cs`, tests for parsing & validation.