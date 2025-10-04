# WP04 — ThetaData / Market Data Integration

**Goal:** Pull chains, greeks, IV rank; compute credit/width and Δ for spreads.

## Prerequisites
- WP01 completed: `AutoRevOption.Monitor` verifies IBKR connection
- `secrets.json` configured with ThetaData API key

## Tasks
- Snapshot top strikes per expiry (5–9 DTE) for income
- Greeks calc or API mapping; IV rank per ticker
- Liquidity tests: OI, volume, NBBO spread threshold
- Test data feeds using `AutoRevOption.Monitor` as validation sandbox

## Deliverables
- `MarketDataClient.cs`, `Scanner.cs` producing ranked Candidates
- ThetaData client with credentials from `secrets.json`
- Market data quality checks (stale quotes, missing greeks)