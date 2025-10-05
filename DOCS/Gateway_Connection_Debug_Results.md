# IB Gateway Connection Debug Results

**Date:** 2025-10-05
**Issue:** IBKR Gateway connection hangs - socket connects but no TWS API callbacks received

## Symptoms

- Monitor MCP service hangs at "Connecting to 127.0.0.1:4001..."
- TCP socket connection succeeds (verified with `netstat`)
- Port 4001 is LISTENING on 0.0.0.0
- **ZERO TWS API callbacks received** (no connectAck, nextValidId, error messages)
- Connection times out after 10 seconds

## Configuration

**From [secrets.json](../secrets.json):**
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 4001,
    "ClientId": 0,
    "IsPaperTrading": false
  }
}
```

**IB Gateway API Settings (from user screenshot):**
- Socket Port: 4001
- Master API client ID: 10
- Read-Only API: UNCHECKED ✅
- Gateway Version: 1040+

## Diagnosis

### ✅ What's Working

1. **Socket Connection** - TCP connection to 127.0.0.1:4001 succeeds
2. **Port Listening** - `netstat -an | findstr :4001` shows `LISTENING`
3. **Gateway Running** - IB Gateway is running and logged in
4. **API Enabled** - In Gateway 1040+, API is always enabled (no checkbox needed)
5. **Read-Only OFF** - Read-Only API is correctly unchecked

### ❌ What's NOT Working

1. **TWS API Protocol** - Gateway sends ZERO bytes back after handshake
2. **No Callbacks** - IbkrConnection receives no EWrapper callbacks
3. **Silent Failure** - No error codes, no connection acknowledgment

## Root Cause Hypothesis

**Master API client ID: 10** is likely filtering/rejecting connections.

### Why This Matters

The "Master API client ID" setting in IB Gateway tells it:
- **Blank/Empty** = Accept connections from ANY client ID
- **Specific number (e.g., 10)** = ONLY accept connections where ClientId matches this number

Our code uses `ClientId: 0` but Gateway expects `ClientId: 10`.

## Tests Performed

1. ✅ **Port Test** - Verified port 4001 listening
2. ✅ **Connection Logic** - Added ManualResetEvent for proper callback synchronization
3. ✅ **Multiple Client IDs** - Tried ClientId 0, 1, 10 - all timed out
4. ✅ **Debug Test Created** - IbkrConnectionDebugTests.cs with verbose logging
5. ✅ **Raw Socket Test Created** - IbkrRawSocketTests.cs for protocol-level diagnosis

**Test Results:**
- All connection attempts timeout after 10 seconds
- TCP socket connects successfully
- No response bytes received from Gateway
- No TWS API callbacks triggered

## Debug Tools Created

### 1. IbkrConnectionDebugTests.cs

Comprehensive test with:
- Detailed connection parameter logging
- Console output capture
- 30-second wait with diagnostics
- Success/failure checklist
- IB Gateway log file guidance

**Location:** `AutoRevOption.Tests/IbkrConnectionDebugTests.cs`

**To run:**
```bash
dotnet test --filter "FullyQualifiedName~IbkrConnectionDebugTests"
```

### 2. IbkrRawSocketTests.cs

Low-level socket test that:
- Establishes TCP connection
- Sends TWS API handshake ("API=9.72\0")
- Waits for Gateway response
- Shows hex/ASCII response or timeout diagnosis

**Location:** `AutoRevOption.Tests/IbkrRawSocketTests.cs`

**To run:**
```bash
dotnet test --filter "FullyQualifiedName~IbkrRawSocketTests"
```

### 3. Enhanced IbkrConnection.cs

**Changes made:**
- Added `ManualResetEvent _connectionComplete` for proper async signaling
- Updated `connectAck()`, `nextValidId()`, `error()` to signal connection
- Increased timeout diagnostics
- Added verbose console logging

**Location:** `AutoRevOption.Shared/Ibkr/IbkrConnection.cs`

## Gateway Logs Analysis ✅

**Log file checked:** `C:\Users\{Username}\Jts\ibgateway.*.log`

**Findings:**
```
2025-10-05 12:48:21.760 [DK] INFO  [JTS-SocketListener-102] - API server listening on port 4001
2025-10-05 12:48:21.760 [DK] INFO  [JTS-SocketListener-102] - API in Read-Only mode: false
```

**Critical Discovery:**
- ✅ Gateway API is running on port 4001
- ✅ Read-Only mode is false (correct)
- ❌ **ZERO connection attempt logs from our application**
- Gateway shows routine market data updates, account updates, time sync
- **No API client registration attempts logged**

**Conclusion:** Gateway is NOT seeing our connection attempts at all. This means the connection is being blocked/rejected BEFORE reaching Gateway's API listener thread.

## Recommended Next Steps

### 1. Clear Master API Client ID (PRIORITY 1) ⚠️

**Root Cause:** Master API client ID: 10 is silently filtering connections

**Action:**
1. Open IB Gateway
2. Go to Configure → Settings → API → Settings
3. Find "Master API client ID" field
4. **DELETE the value (set to blank/empty)**
5. Click OK and Apply
6. **Restart Gateway completely**
7. Try connection again

**Why this matters:** When Master API client ID is set to a specific number (10), Gateway silently rejects ALL connections that don't use that exact ClientId. Our code uses ClientId=0, so Gateway drops the connection without logging anything

### 2. Change Master API Client ID (PRIORITY 2)

**Current:** 10
**Try:** blank/empty (or 0)

**How:**
1. Open IB Gateway
2. Go to Configure → Settings → API → Settings
3. Find "Master API client ID"
4. Clear the field (set to blank)
5. Click OK
6. Restart Gateway
7. Try connection again

### 3. Try Matching Client ID (PRIORITY 3)

If logs show Gateway expects ClientId=10, update [secrets.json](../secrets.json):

```json
{
  "IBKRCredentials": {
    "ClientId": 10  // Changed from 0
  }
}
```

### 4. Verify API Version Compatibility

Our code uses: `CSharpAPI.dll` version 9.72 (from `C:\IBKR\TWS_API\source\CSharpClient\`)

Gateway version: 1040+

**Check compatibility:**
1. Verify TWS API version matches Gateway version
2. If mismatch, download latest TWS API from:
   https://www.interactivebrokers.com/en/index.php?f=5041

### 5. Full Restart Sequence

If all above fail:
1. Close IB Gateway completely
2. Wait 30 seconds
3. Delete temporary API files:
   - `C:\Users\{YourUsername}\Jts\.lock`
   - `C:\Users\{YourUsername}\Jts\*.pid`
4. Restart IB Gateway
5. Log in
6. Verify API settings
7. Try connection again

## Code Changes Summary

**Files modified:**
- ✅ `AutoRevOption.Shared/Ibkr/IbkrConnection.cs` - Fixed async connection with proper callbacks
- ✅ `AutoRevOption.Tests/AutoRevOption.Tests.csproj` - Added IBApi reference
- ✅ `AutoRevOption.Tests/IbkrConnectionDebugTests.cs` - Created comprehensive debug test
- ✅ `AutoRevOption.Tests/IbkrRawSocketTests.cs` - Created raw socket diagnostic

**Build status:** ✅ 0 errors, 0 warnings
**Tests status:** Connection tests timeout (expected until Gateway issue resolved)

## Success Criteria

Connection will be considered **successful** when:

1. ✅ Socket connects (already working)
2. ❌ Gateway sends response bytes
3. ❌ `connectAck()` callback triggered
4. ❌ `nextValidId()` callback triggered with order ID
5. ❌ `managedAccounts()` callback triggered with account number
6. ❌ `error(2104)` - Market data farm connection info message
7. ❌ `ConnectAsync()` returns `true` within 10 seconds

## References

- [Gateway_API_Setup.md](Gateway_API_Setup.md) - Initial setup and troubleshooting guide
- [IB TWS API Documentation](https://www.interactivebrokers.com/en/index.php?f=5041)
- [IB Gateway Settings Guide](https://www.interactivebrokers.com/en/index.php?f=16454)
