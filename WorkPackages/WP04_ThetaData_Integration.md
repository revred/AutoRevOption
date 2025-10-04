# WP04 — ThetaData / Market Data Integration

**Goal:** Pull chains, greeks, IV rank; compute credit/width and Δ for spreads.

## Tasks
- Snapshot top strikes per expiry (5–9 DTE) for income.
- Greeks calc or API mapping; IV rank per ticker.
- Liquidity tests: OI, volume, NBBO spread threshold.

## Deliverables
- `MarketDataClient.cs`, `Scanner.cs` producing ranked Candidates.