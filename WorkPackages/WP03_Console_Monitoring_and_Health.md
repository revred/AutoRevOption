# WP03 — Console Monitoring & Health Checks

**Goal:** Extend console to show connection status, latency, IV map, earnings calendar; add health pings.

## Prerequisites
- WP01 completed: `AutoRevOption.Monitor` verifies live IBKR connection

## Tasks
- Health: IBKR ping, quote latency, retry policy
- Status: account greeks, maint %, open orders summary
- Events: earnings map for universe (stub → later data feed)
- Integrate `AutoRevOption.Monitor` connection logic into main console

## Deliverables
- Enhanced console screens 0–3
- `HealthService.cs` with metrics
- Migrate `IbkrConnection.cs` from Monitor to shared library
- Connection state monitoring (connected, latency, last heartbeat)