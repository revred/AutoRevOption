# WP04 — IBKR Market Data Subscription

**Goal:** Pull real-time option chains, greeks, IV rank, and stock data using IBKR TWS API subscription; compute credit/width and Δ for spreads.

---

## Prerequisites

- WP01 completed: `AutoRevOption.Monitor` verifies IBKR connection
- `secrets.json` configured with IBKR credentials
- **IBKR Market Data Subscription** active:
  - US Securities Snapshot and Futures Value Bundle ($10/month)
  - OR US Equity and Options Add-On Streaming Bundle ($4.50/month)

---

## Overview

Replace external market data providers with **IBKR native data subscription**:
- Real-time option chains via `reqSecDefOptParams()` and `reqContractDetails()`
- Live greeks via `reqMktData()` with generic tick types (13=Model Option Greeks)
- Streaming quotes for options and underlying stocks
- Calculate IV Rank from historical IV data via `reqHistoricalData()`

---

## Tasks

### 1. Market Data Subscription Setup

- [ ] Verify IBKR account has active market data subscription
  - Login to IBKR Account Management
  - Navigate to Settings → Market Data Subscriptions
  - Subscribe to: **US Securities Snapshot and Futures Value Bundle** ($10/month)
  - Alternative: **US Equity and Options Add-On Streaming Bundle** ($4.50/month)
- [ ] Confirm subscription in TWS by requesting market data for test symbol
- [ ] Update `secrets.json` with market data permissions flag

### 2. Option Chain Retrieval

- [ ] Implement option chain scanner using IBKR API:
  - `reqSecDefOptParams()` - Get option parameters (strikes, expirations)
  - Filter expirations to 5-9 DTE range
  - Build option contracts for each strike/expiry combination
- [ ] Request contract details via `reqContractDetails()`
  - Get bid/ask, last price, volume, open interest
  - Filter by liquidity thresholds (OI ≥ 200)
- [ ] Implement option chain caching (5-minute TTL)

### 3. Greeks and IV Data

- [ ] Request market data with greeks:
  - `reqMktData()` with generic tick type 13 (Model Option Greeks)
  - Parse: delta, gamma, vega, theta, implied volatility
- [ ] Calculate IV Rank:
  - Request 52-week historical IV via `reqHistoricalData()`
  - Formula: `IVR = (Current IV - 52w Low) / (52w High - 52w Low) * 100`
  - Cache IV rank per symbol (daily refresh)
- [ ] Validate greeks consistency (delta range checks, IV sanity)

### 4. Streaming Market Data

- [ ] Implement streaming quotes for underlying stocks:
  - `reqMktData()` for spot price, bid/ask spread
  - Real-time updates via `tickPrice()` and `tickSize()` callbacks
- [ ] Implement streaming option quotes:
  - Subscribe to bid/ask for option contracts
  - Track NBBO spread for liquidity validation
  - Alert on stale quotes (no update >30 seconds)
- [ ] Handle market data disconnects and reconnects

### 5. Credit/Width Calculations

- [ ] Build spread calculator:
  - Input: Short strike, long strike, option type (PUT/CALL)
  - Calculate: Credit (short bid - long ask), Width (strike difference)
  - Compute: Risk capital = Width - Credit
- [ ] Delta calculations for spreads:
  - Net delta = Short delta - Long delta
  - Validate delta band: [0.20, 0.25] for income strategies
- [ ] Pricing validation:
  - Check bid-ask spread ≤ $0.05 or ≤ 10% of credit
  - Reject if spread too wide (poor liquidity)

### 6. Liquidity Checks

- [ ] Implement liquidity filters:
  - Open Interest (OI) ≥ 200 per leg
  - Volume ≥ 50 per leg (optional)
  - Bid-ask spread checks (absolute and % of credit)
- [ ] NBBO spread threshold validation
- [ ] Real-time liquidity monitoring during market hours

### 7. Scanner Integration

- [ ] Create `IBKRScanner.cs`:
  - Input: Universe tickers, DTE range, strategy type
  - Output: Ranked candidates with greeks, credit, IV rank
- [ ] Integrate with SelectTVC service:
  - Scanner produces raw candidate data
  - SelectTVC applies gates (POP, Reward/Day, IVR, events)
- [ ] Test scanner with paper TWS connection

### 8. Market Data Quality Checks

- [ ] Implement data validation:
  - Stale quote detection (timestamp checks)
  - Missing greeks handling (fallback to bid-ask-based estimates)
  - IV outlier detection (flag IV > 200% or < 5%)
- [ ] Add logging for data quality issues
- [ ] Create alerts for subscription issues (e.g., market data not subscribed)

---

## Deliverables

- [ ] **Code:**
  - `AutoRevOption.Shared/Ibkr/IBKRMarketData.cs` - Market data client
  - `AutoRevOption.Shared/Ibkr/IBKRScanner.cs` - Option chain scanner
  - `AutoRevOption.Shared/Ibkr/IVRankCalculator.cs` - IV Rank calculator
  - Update `IbkrConnection.cs` with market data methods

- [ ] **Configuration:**
  - Update `secrets.json` schema with market data subscription flag
  - Document subscription requirements in `DOCS/IBKR_Setup.md`

- [ ] **Tests:**
  - `AutoRevOption.Tests/IBKRMarketDataTests.cs`
  - Mock market data responses for unit tests
  - Integration test with paper TWS (subscription required)

---

## IBKR Market Data Subscriptions

### Recommended: US Securities Snapshot and Futures Value Bundle
- **Cost:** $10/month
- **Includes:**
  - Real-time US stock quotes
  - Real-time US options quotes
  - Greeks and implied volatility
  - Market depth (Level II)
- **Suitable for:** AutoRevOption full functionality

### Alternative: US Equity and Options Add-On Streaming Bundle
- **Cost:** $4.50/month
- **Includes:**
  - Streaming US stock quotes
  - Streaming US options quotes
  - Greeks (requires separate request)
- **Suitable for:** Budget option, sufficient for SelectTVC

### Free Delayed Data (15-minute delay)
- **Cost:** Free
- **Limitation:** 15-minute delayed quotes
- **Not recommended:** Selection gates require real-time data for freshness checks

---

## API Methods Reference

### Option Chain Discovery
```csharp
// Get option parameters (strikes, expirations)
_client.reqSecDefOptParams(reqId, underlyingSymbol, "", "STK", underlyingConId);

// Callback: securityDefinitionOptionalParameter()
// Returns: strikes[], expirations[]

// Request contract details for each option
_client.reqContractDetails(reqId, optionContract);

// Callback: contractDetails()
// Returns: Contract details with market data
```

### Market Data with Greeks
```csharp
// Subscribe to market data with greeks
_client.reqMktData(reqId, contract, "13", snapshot: false, regularMarketSnapshot: false, mktDataOptions: null);

// Generic tick type 13 = Model Option Greeks
// Callbacks:
//   - tickPrice(): bid, ask, last, close
//   - tickSize(): bid size, ask size, volume
//   - tickOptionComputation(): IV, delta, gamma, vega, theta
```

### IV Rank Calculation
```csharp
// Request 52-week historical IV
_client.reqHistoricalData(reqId, contract, endDateTime, durationStr: "1 Y",
    barSizeSetting: "1 day", whatToShow: "OPTION_IMPLIED_VOLATILITY",
    useRTH: 1, formatDate: 1, keepUpToDate: false, chartOptions: null);

// Callback: historicalData()
// Calculate: IVR = (CurrentIV - Min52wIV) / (Max52wIV - Min52wIV) * 100
```

---

## Testing Strategy

### Unit Tests (No Subscription Required)
- Mock IBKR responses
- Test spread calculations (credit, width, delta)
- Test IV rank formula
- Test liquidity filters

### Integration Tests (Requires Paper Account + Subscription)
- Connect to paper TWS (port 7497)
- Request option chain for SOFI
- Validate greeks received
- Test scanner with 5-9 DTE filter
- Verify liquidity checks

### Production Readiness
- [ ] Verify subscription active in live account
- [ ] Test with live TWS connection (port 7496)
- [ ] Monitor for market data throttling (max 100 simultaneous quotes)
- [ ] Implement request pacing (max 50 requests per second)

---

## Migration from ThetaData

**Removed:**
- `ThetaDataClient.cs`
- `secrets.json` field: `ThetaDataCredentials.ApiKey`
- External API dependency

**Added:**
- IBKR native market data subscription
- Real-time greeks via TWS API
- IV rank calculation from IBKR historical data
- No external API keys needed (credentials already in IBKR account)

**Benefits:**
- Single vendor (IBKR) for execution and data
- Lower latency (direct TWS connection)
- No additional API complexity
- Cost: $10/month vs ThetaData $50+/month

---

## Acceptance Criteria

1. ✅ IBKR market data subscription verified and active
2. ✅ Option chains retrieved for 6+ tickers (SOFI, APP, RKLB, META, AMD, GOOGL)
3. ✅ Greeks (delta, IV) received for all option contracts
4. ✅ IV Rank calculated from 52-week historical IV
5. ✅ Scanner produces ranked candidates with liquidity filters
6. ✅ Credit/width/delta calculations accurate
7. ✅ Real-time streaming quotes working
8. ✅ Data quality checks flag stale/missing data
9. ✅ Integration tests pass with paper TWS
10. ✅ No ThetaData dependencies remaining

---

## Timeline

- **Phase 1:** Market data subscription setup + option chain retrieval (2 days)
- **Phase 2:** Greeks and IV rank implementation (2 days)
- **Phase 3:** Scanner integration + liquidity checks (2 days)
- **Phase 4:** Testing + documentation (1 day)
- **Total:** ~7 days

---

## Dependencies

- **WP01:** IBKR connection established (Monitor app working)
- **WP02:** RulesEngine for strategy parameters
- **WP06:** SelectTVC for consuming scanner output

---

## Notes

- IBKR market data has usage limits: 100 simultaneous streaming quotes
- Implement request pacing to avoid throttling (50 req/sec max)
- Cache option chains and IV rank to minimize API calls
- Market data subscription must be active in both paper and live accounts
- TWS must be running for market data (Gateway does not support all tick types)

---

**Status:** 🔴 Not Started
**Owner:** AutoRevOption.Shared + AutoRevOption.Monitor
**Last Updated:** 2025-10-05
