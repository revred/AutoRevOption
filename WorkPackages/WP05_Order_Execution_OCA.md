# WP05 — Order Execution with OCA (TP/SL)

**Goal:** Submit combo orders and queue GTC exits (TP vs SL) in an OCA group.

## Tasks
- CP/TWS API order submission for multi-leg combos.
- Create linked GTC orders for exits.
- Idempotency + audit log.

## Deliverables
- `ExecutionClient.cs` with paper/live toggle.
- Integration tests against IBKR demo.