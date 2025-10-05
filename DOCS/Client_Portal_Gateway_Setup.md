# Client Portal Gateway Setup Guide

**Date:** 2025-10-05
**Purpose:** Switch from TWS API to Client Portal Web API (CPAPI) for reliable REST-based IBKR integration

---

## Why Client Portal Gateway?

### Problems with TWS API / IB Gateway
- ❌ `eConnect()` blocks indefinitely (unresolved after 3+ hours)
- ❌ Requires 74+ EWrapper callback implementations
- ❌ Binary protocol handshake failures
- ❌ CLOSE_WAIT zombie socket accumulation
- ❌ Complex initialization timing requirements
- ❌ Gateway API listener stuck state

### Benefits of Client Portal API
- ✅ **Simple REST/WebSocket** endpoints (no binary protocol)
- ✅ **No EWrapper surface** required (pure HTTP)
- ✅ **JSON responses** - easy to debug and parse
- ✅ **Stateless calls** with session management
- ✅ **Container-friendly** - easy to dockerize
- ✅ **HTTP-first** integration for MCP proxy

---

## Client Portal Gateway Overview

**What it is:** Lightweight local server that proxies REST calls to IBKR servers

**Default URL:** `https://localhost:5000/v1/api/`

**Process:**
1. Download Client Portal Gateway (clientportal.gw.zip)
2. Start gateway locally (runs on port 5000 by default)
3. Authenticate via browser (one-time per session)
4. Make REST API calls to localhost:5000
5. Gateway forwards to IBKR, returns JSON

---

## Installation

### Step 1: Download Client Portal Gateway

**Download Link:**
https://www.interactivebrokers.com/en/trading/ibgateway-stable.php

Look for **"Client Portal Gateway"** section (separate from IB Gateway!)

**File:** `clientportal.gw.zip` (~50MB)

### Step 2: Extract

```bash
# Extract to C:\IBKR\clientportal\
unzip clientportal.gw.zip -d C:\IBKR\clientportal\
```

**Structure:**
```
C:\IBKR\clientportal\
├── bin/
│   └── run.sh / run.bat
├── root/
│   └── conf.yaml
└── logs/
```

### Step 3: Configure (Optional)

**File:** `C:\IBKR\clientportal\root\conf.yaml`

```yaml
# Default configuration
ips:
  allow:
    - 127.0.0.1

proxyRemoteSsl: true
proxyRemoteHost: null

# Port configuration
listenPort: 5000
listenSsl: true

# Session configuration
reauthenticate: true
```

**Key Settings:**
- `listenPort: 5000` - REST API port (default)
- `listenSsl: true` - Uses self-signed cert (our client accepts it)
- `ips.allow` - Restrict to localhost only

---

## Starting Client Portal Gateway

### Windows

```bash
cd C:\IBKR\clientportal\bin
run.bat
```

**Expected Output:**
```
Client Portal Gateway starting...
HTTPS server listening on port 5000
Browse to https://localhost:5000 to authenticate
```

### Keep Running in Background

```bash
# PowerShell (Windows)
Start-Process -FilePath "C:\IBKR\clientportal\bin\run.bat" -WindowStyle Hidden

# Or use Task Scheduler for auto-start on login
```

---

## Authentication Flow

### First-Time Setup

1. **Start Gateway:**
   ```bash
   cd C:\IBKR\clientportal\bin
   run.bat
   ```

2. **Open Browser:**
   Navigate to: `https://localhost:5000`

   Accept self-signed certificate warning (this is normal!)

3. **Login Page:**
   Enter IBKR username and password

4. **Two-Factor Auth:**
   Complete 2FA if enabled on account

5. **Session Active:**
   Once logged in, browser shows: "Client Portal Gateway session is live"

### Session Management

**Session Duration:** ~24 hours (configurable)

**Keep-Alive:** Send `POST /tickle` every 60 seconds (our client does this automatically)

**Re-authentication:** When session expires, gateway returns `401 Unauthorized`
→ Repeat browser authentication flow

---

## API Endpoints We Use

### Base URL
```
https://localhost:5000/v1/api
```

### Authentication & Session

**Check Auth Status:**
```http
POST /iserver/auth/status
```

**Response:**
```json
{
  "authenticated": true,
  "connected": true,
  "competing": false,
  "message": ""
}
```

**Keep Session Alive (every 60s):**
```http
POST /tickle
```

### Account Data

**Get Accounts:**
```http
GET /portfolio/accounts
```

**Response:**
```json
[
  {
    "id": "U21146542",
    "accountId": "U21146542",
    "accountVan": "U21146542",
    "accountTitle": "RevOption Trading",
    "currency": "USD",
    "type": "INDIVIDUAL",
    "tradingType": "CASH"
  }
]
```

**Get Account Summary:**
```http
GET /iserver/account
```

### Positions

**Get Positions for Account:**
```http
GET /portfolio/{accountId}/positions/0
```

**Example:** `GET /portfolio/U21146542/positions/0`

**Response:**
```json
[
  {
    "acctId": "U21146542",
    "conid": 265598,
    "contractDesc": "AAPL",
    "position": 100,
    "mktPrice": 175.43,
    "mktValue": 17543.00,
    "currency": "USD",
    "avgCost": 170.25,
    "avgPrice": 170.25,
    "realizedPnl": 0.00,
    "unrealizedPnl": 518.00,
    "ticker": "AAPL"
  }
]
```

---

## Integration with AutoRevOption

### Project Structure

```
AutoRevOption.CpApi/
├── AutoRevOption.CpApi.csproj
└── CpApiClient.cs          # REST client implementation
```

### Usage Example

```csharp
using AutoRevOption.CpApi;

// Create client
using var client = new CpApiClient("https://localhost:5000/v1/api");

// Check authentication
var auth = await client.GetAuthStatusAsync();
if (auth?.Authenticated != true)
{
    Console.WriteLine("Not authenticated - login via browser at https://localhost:5000");
    return;
}

// Get accounts
var accounts = await client.GetAccountsAsync();
Console.WriteLine($"Found {accounts?.Count ?? 0} accounts");

// Get positions for first account
if (accounts?.FirstOrDefault() is { } account)
{
    var positions = await client.GetPositionsAsync(account.AccountId);
    Console.WriteLine($"Retrieved {positions?.Count ?? 0} positions");

    foreach (var pos in positions ?? Enumerable.Empty<Position>())
    {
        Console.WriteLine($"  {pos.Ticker}: {pos.PositionSize} @ ${pos.AvgCost} (P/L: ${pos.UnrealizedPnl})");
    }
}
```

### Automatic Session Keep-Alive

CpApiClient automatically sends `/tickle` every 60 seconds to maintain session.

---

## Testing the Setup

### Step 1: Start Client Portal Gateway

```bash
cd C:\IBKR\clientportal\bin
run.bat
```

### Step 2: Authenticate via Browser

Open: `https://localhost:5000`
Login with IBKR credentials

### Step 3: Test with curl

```bash
# Check auth status
curl -k -X POST https://localhost:5000/v1/api/iserver/auth/status

# Expected: {"authenticated":true,"connected":true,"competing":false}

# Get accounts
curl -k https://localhost:5000/v1/api/portfolio/accounts

# Get positions (replace U21146542 with your account ID)
curl -k https://localhost:5000/v1/api/portfolio/U21146542/positions/0
```

### Step 4: Test with AutoRevOption.CpApi

Create simple console app:

```csharp
using AutoRevOption.CpApi;

var client = new CpApiClient();
var auth = await client.GetAuthStatusAsync();
Console.WriteLine($"Authenticated: {auth?.Authenticated}");

var accounts = await client.GetAccountsAsync();
foreach (var account in accounts ?? [])
{
    Console.WriteLine($"Account: {account.AccountId} ({account.AccountTitle})");

    var positions = await client.GetPositionsAsync(account.AccountId);
    Console.WriteLine($"  Positions: {positions?.Count ?? 0}");
}
```

---

## Troubleshooting

### Issue: "Connection refused" on https://localhost:5000

**Cause:** Client Portal Gateway not running

**Fix:**
```bash
cd C:\IBKR\clientportal\bin
run.bat
```

### Issue: Browser shows "Not authenticated"

**Cause:** Session expired or never logged in

**Fix:** Navigate to `https://localhost:5000` and complete login flow

### Issue: API returns 401 Unauthorized

**Cause:** Session expired

**Fix:**
1. Navigate to `https://localhost:5000`
2. Complete re-authentication
3. Retry API call

### Issue: "Certificate error" in browser

**Cause:** Gateway uses self-signed certificate (this is normal!)

**Fix:** Click "Advanced" → "Proceed to localhost (unsafe)" - it's safe because it's your local machine

### Issue: API call times out

**Cause:** Gateway may be starting up or under heavy load

**Fix:**
```bash
# Check Gateway logs
cat C:\IBKR\clientportal\logs\clientportal.log

# Look for errors or "server ready" message
```

---

## Production Deployment

### Auto-Start on Windows Login

1. Open Task Scheduler
2. Create New Task:
   - **Trigger:** At log on
   - **Action:** Start program
   - **Program:** `C:\IBKR\clientportal\bin\run.bat`
   - **Start in:** `C:\IBKR\clientportal\bin`

3. **Settings:**
   - ✅ Run whether user is logged in or not
   - ✅ Run with highest privileges
   - ✅ If task fails, restart every 1 minute

### Health Check Script

```bash
#!/bin/bash
# scripts/check-cpapi-health.sh

CPAPI_URL="https://localhost:5000/v1/api"

# Check if gateway is running
STATUS=$(curl -k -s -X POST "$CPAPI_URL/iserver/auth/status" | jq -r '.authenticated')

if [ "$STATUS" == "true" ]; then
    echo "✅ Client Portal Gateway: Authenticated"
    exit 0
else
    echo "❌ Client Portal Gateway: Not authenticated"
    echo "   Login at https://localhost:5000"
    exit 1
fi
```

---

## Comparison: TWS API vs Client Portal API

| Feature | TWS API / IB Gateway | Client Portal API |
|---------|---------------------|-------------------|
| **Protocol** | Binary (proprietary) | REST/WebSocket (JSON) |
| **Callbacks** | 74+ EWrapper methods | None (simple HTTP) |
| **Authentication** | ClientId handshake | Browser login + session |
| **Connection** | eConnect() + startApi() | HTTP GET/POST |
| **Session Management** | Manual reconnection | Automatic tickle |
| **Debugging** | Binary inspection | curl/browser/logs |
| **Containerization** | Difficult (X11/display) | Easy (headless server) |
| **Market Data** | Full depth, greeks | Basic quotes, limited greeks |
| **Order Placement** | Full featured | Full featured |
| **Complexity** | High | Low |
| **Reliability** | Variable (API listener can stuck) | Good (HTTP retry logic) |

---

## Migration Plan

### Phase 1: Parallel Testing (This Week)
- ✅ Install Client Portal Gateway
- ✅ Create CpApiClient
- ⏳ Test positions retrieval
- ⏳ Verify account data accuracy

### Phase 2: Update Monitor (Monday)
- Replace IbkrConnection with CpApiClient
- Update MCP handlers to use CPAPI
- Test with Claude Desktop integration

### Phase 3: Deprecate TWS API (After Monday)
- Archive AutoRevOption.Shared/Ibkr/IbkrConnection.cs
- Remove TWS API DLL references
- Update documentation

---

## Security Considerations

### Local-Only Access

Client Portal Gateway should **ONLY** accept localhost connections:

**conf.yaml:**
```yaml
ips:
  allow:
    - 127.0.0.1
```

**Never expose port 5000 externally!**

### Self-Signed Certificate

Gateway uses self-signed SSL cert. Our HttpClient accepts it:

```csharp
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};
```

**Safe** because connection is localhost-only.

### Session Tokens

Gateway maintains session state server-side. No tokens stored in our code.

Browser authentication required - ensures user presence.

---

## References

### IBKR Documentation
- **Client Portal API Guide:** https://interactivebrokers.github.io/cpwebapi/
- **Download:** https://www.interactivebrokers.com/en/trading/ibgateway-stable.php
- **Endpoints Reference:** https://www.interactivebrokers.com/api/doc.html

### AutoRevOption Documentation
- **Architecture:** [DOCS/Architecture.md](Architecture.md)
- **Monday Procedure:** [DOCS/Monday_GoLive_Procedure.md](Monday_GoLive_Procedure.md)
- **Final TWS Test Results:** [DOCS/Final_Connection_Test_Results.md](Final_Connection_Test_Results.md)

---

**Last Updated:** 2025-10-05 18:00 GMT
**Status:** Ready for testing
**Next Step:** Install Client Portal Gateway and test smoke connection
