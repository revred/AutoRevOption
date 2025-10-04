# WP05 — Order Execution with OCA (TP/SL)

**Goal:** Submit combo orders and queue GTC exits (TP vs SL) in an OCA group.

## Prerequisites
- WP01 completed: `AutoRevOption.Monitor` validates IBKR connection
- `OrderBuilder.cs` from WP01 generates OCA brackets
- TWS/Gateway configured for order submission (API permissions)

## Tasks
- TWS API order submission for multi-leg combos
- Create linked GTC orders for exits (TP vs SL)
- Idempotency + audit log
- Test order flow using `AutoRevOption.Monitor` connection

## Deliverables
- `ExecutionClient.cs` with paper/live toggle (extends `IbkrConnection`)
- Integration tests against IBKR paper account
- Order status monitoring and fill confirmations
- Dry-run mode for validation before live submission