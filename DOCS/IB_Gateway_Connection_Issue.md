# IB Gateway API Connection Issue - Troubleshooting Guide

**Date**: 2025-10-05
**Status**: Unresolved
**Severity**: Blocking for MCP Monitor service

## Problem Summary

The IB TWS API `eConnect()` call hangs indefinitely when attempting to connect to IB Gateway, preventing the MCP Monitor service from starting and retrieving positions.

## Symptoms

- `eConnect()` blocks forever, never returns
- No error messages or exceptions thrown
- TCP connection to port 4001 succeeds
- Gateway never sends API handshake response
- No callbacks triggered (`connectAck()`, `nextValidId()`, error codes)

## What We Tried

### 1. ClientId Changes
- ❌ Changed from ClientId 10 → 1 → 11 → 10
- **Result**: No difference, all hung the same way

### 2. Gateway Restarts
- ❌ Restarted IB Gateway multiple times
- ❌ Completely closed and reopened Gateway
- **Result**: Same behavior after every restart

### 3. API Settings Verification
- ✅ Port 4001 confirmed listening (`netstat -an | findstr :4001`)
- ✅ "Allow connections from localhost only" enabled
- ✅ "Read-Only API" unchecked
- ✅ Socket port set to 4001

### 4. Master API Client ID
- ❌ Changed from 10 to 0 (allow all clients)
- ❌ Changed to blank/empty
- **Result**: No improvement

### 5. Code Changes
- ❌ Wrapped `eConnect()` in `Task.Run()` to avoid blocking
- ❌ Added verbose logging (never reached, hung before logs)
- ❌ Increased timeouts from 10s to 30s to 60s
- **Result**: All timeout waiting for connection

### 6. Process Cleanup
- ❌ Killed all dotnet.exe processes
- ❌ Verified no existing ClientId connections
- ❌ Cleaned up orphaned TCP connections (CLOSE_WAIT states)
- **Result**: Fresh connection attempts still hung

## Diagnostic Tests Run

### Connection Debug Test
```bash
dotnet test --filter "IbkrConnectionDebugTests"
```
**Result**: Timeout after 30 seconds waiting for connection

### Raw Socket Test
```bash
dotnet test --filter "IbkrRawSocketTests"
```
**Result**: Timeout after 30 seconds (likely - didn't complete)

## Technical Details

### Connection Flow
1. `eConnect(host, port, clientId)` called
2. TCP socket connects successfully (verified with netstat)
3. **Hangs here** - waiting for Gateway to send handshake
4. Should receive `connectAck()` or `nextValidId()` callback
5. Should receive error code 2104, 2106, or 2158 (connection success)
6. None of these callbacks ever fire

### Gateway Logs
- Location: `C:\IBKR\ibgateway\1040\camjbohogbpeiedgbeikipnekejkfebbjgkchfih\`
- Latest: `ibgateway.20251005.153916.ibgzenc` (compressed/encoded)
- No API connection attempts logged
- API log file exists: `api.10.20251005.143928.ibgzenc`

### Network State
```
TCP    0.0.0.0:4001           0.0.0.0:0              LISTENING
TCP    127.0.0.1:4001         127.0.0.1:56334        ESTABLISHED
```
- Port listening ✅
- TCP connection established ✅
- But API handshake never completes ❌

## Possible Root Causes

### 1. Gateway Version Incompatibility
**Likelihood**: High
The IB Gateway version may have changed its API protocol or requirements.

**Check**:
- Gateway version: 1040 (from path `C:\IBKR\ibgateway\1040\`)
- TWS API version: Using IBApi NuGet package 10.19.2
- May need to update IBApi client library

### 2. Missing Gateway Configuration
**Likelihood**: Medium
There may be an undocumented Gateway setting preventing API connections.

**Missing checks**:
- "Enable ActiveX and Socket Clients" option (user reported this option doesn't exist)
- Trusted IPs whitelist
- API version restrictions
- Connection limits

### 3. Platform/OS Issue
**Likelihood**: Low
Windows-specific issue with localhost connections or Gateway installation.

**Platform**: Windows (Git Bash environment)

### 4. Gateway Not Fully Initialized
**Likelihood**: Low
Gateway UI shows "connected" but API server may not be ready.

**Observations**:
- "Show API messages" checkbox enabled
- No messages appear in Gateway window
- Suggests API server not accepting connections

## Workaround

Use the interactive Monitor instead of MCP service:

```bash
./scripts/get-positions.sh
```

Then manually select option "2" (Get Positions) from the menu.

## Next Steps for Resolution

### 1. Check Gateway API Version
In IB Gateway, check **Help → About IB Gateway** for version info.

### 2. Review Gateway Configuration Files
Check for XML config files that might have API settings:
```bash
ls C:\IBKR\ibgateway\1040\camjbohogbpeiedgbeikipnekejkfebbjgkchfih\*.xml
```

### 3. Enable Gateway Debug Logging
Look for a way to enable verbose API logging in Gateway settings.

### 4. Test with TWS Instead of Gateway
Try connecting to TWS (not Gateway) to see if issue is Gateway-specific:
- TWS Paper: port 7497
- TWS Live: port 7496

### 5. Contact IBKR Support
Open ticket with Interactive Brokers API support:
- Describe: `eConnect()` hangs, no callbacks received
- Provide: Gateway version, API settings screenshots
- Ask: Required Gateway configuration for API connections

### 6. Try Older IBApi Version
Downgrade IBApi NuGet package to older version:
```bash
dotnet remove package IBApi
dotnet add package IBApi --version 10.19.1  # or earlier
```

### 7. Check for Firewall/Antivirus
Temporarily disable Windows Firewall and antivirus to rule out blocking.

### 8. Use Alternative API Library
Consider using alternative IB client libraries:
- TWSLib (C#/.NET wrapper)
- ib_insync (Python - via REST bridge)

## Files Created

- `CODING_GUIDELINES.md` - Project coding standards
- `scripts/get-positions.sh` - Helper script for getting positions
- `.gitignore` updated - Prevent .ps1/.csx files in root

## Lessons Learned

1. **Always clean up background processes** - Left 13+ zombie bash processes running
2. **Use proper folder structure** - Don't create scripts in root folder
3. **Follow platform conventions** - Use .sh (not .ps1) for cross-platform
4. **Check tests first** - Diagnostic tests exist, should have run them earlier
5. **Gateway configuration is complex** - Not well documented, many hidden settings

## References

- [IB Gateway Setup Guide](./IB_Gateway_24x7_Setup.md)
- [AutoRevOption.Monitor README](../AutoRevOption.Monitor/README.md)
- [IB TWS API Documentation](https://interactivebrokers.github.io/tws-api/)
- [IBKR API Support](https://www.interactivebrokers.com/en/support/api.php)

---

**Last Updated**: 2025-10-05
**Next Action**: Try connecting to TWS instead of Gateway, or contact IBKR support
