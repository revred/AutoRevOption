# Monday Go-Live Procedure - Single Source of Truth

**Date:** 2025-10-05
**Goal:** Reliable IBKR connection by Monday with zero fragility

---

## The SSOT Principles

1. **One Binary:** Monitor (or standalone smoke test)
2. **One IBApi:** Official TWS API 10.37.02 DLL (verified unified)
3. **One Configuration:** secrets.json controls everything
4. **One Attempt:** No parallel processes, no rapid retries
5. **One Validation:** Smoke test before MCP starts

---

## Pre-Flight Checklist (MUST complete before ANY connection attempt)

### 1. Clean the Environment

```bash
# Kill all dotnet processes
taskkill //F //IM dotnet.exe

# Verify NO CLOSE_WAIT zombies
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
# MUST return empty - if not, restart Gateway and wait
```

###2. Gateway Initialization Discipline

**After Gateway restart or login:**
1. Wait until UI shows "connected"
2. **Wait additional 60-90 seconds** (set timer!)
3. Do NOT click around during init period
4. Verify listening state:
```bash
netstat -ano | findstr :4001 | findstr LISTENING
# Expected: TCP 0.0.0.0:4001 ... LISTENING
```

### 3. Fix Identity (No More Flipping!)

**In Gateway Settings (Configure → Settings → API → Precautions):**
- Master API client ID: **10** (or blank, but choose ONE and never change)
- Trusted IPs: **127.0.0.1**
- Read-Only API: **OFF**

**In secrets.json:**
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 4001,
    "ClientId": 10,    ← NEVER CHANGE THIS
    ...
  }
}
```

**RULE:** ClientId stays 10 forever. No more 10→1→11→10→0 experiments!

---

## The Smoke Test Protocol

### Run ONCE, Wait for Result, Stop

**Command:**
```bash
cd /c/Code/AutoRevOption
dotnet run --project AutoRevOption.Tests/uProbe
```

**Expected Output (Success):**
```
uProbe — IBKR Gateway Handshake Test
Attempting: 127.0.0.1:4001 clientId=10

[1/5] Calling eConnect()...
✅ eConnect() completed, IsConnected=true (50ms)
[2/5] Starting EReader...
[3/5] Starting message processing thread...
[4/5] Calling startApi()... ⚡ CRITICAL STEP
[5/5] Waiting for nextValidId (10s timeout)...
[12:34:56.789] ✅ connectAck()
[12:34:56.792] ✅ nextValidId(1)
[12:34:56.795] managedAccounts(U21146542)

✅ SUCCESS in 105ms
   connectAck: True
   nextValidId: True
```

**Exit Codes:**
- `0` = Success → proceed to Monitor
- `1` = eConnect() failed → check Gateway is running
- `2` = Timeout → see troubleshooting below

###If Timeout (Exit Code 2)

**DO NOT RETRY IMMEDIATELY!**

1. Check for new CLOSE_WAIT zombies:
```bash
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
```

2. If zombies present:
   - **Stop everything**
   - Restart Gateway
   - Wait 90 seconds
   - Run smoke test again (ONCE)

3. If no zombies but still timeout:
   - Check Gateway logs: `C:\IBKR\ibgateway\1040\<profile>\logs\`
   - Look for API connection attempts
   - If zero API log entries → Gateway API listener stuck → reinstall

---

## Fallback: TWS Paper Trading (Port 7497)

If Gateway continues to fail after clean reinstall, switch to TWS:

**In secrets.json:**
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,    ← Change from 4001 to 7497
    "ClientId": 10,
    "IsPaperTrading": true,  ← Set to true
    ...
  }
}
```

**Start TWS:**
1. Launch TWS Workstation
2. Login to paper trading account
3. Wait 60-90 seconds after login
4. Run smoke test
5. If succeeds → use TWS for Monday

**Port Reference:**
- `4001` - IB Gateway (live)
- `4002` - IB Gateway (paper)
- `7496` - TWS (live)
- `7497` - TWS (paper) ← **Recommended fallback**

---

## Production Start Sequence (Monday Morning)

### Step 1: Environment Prep (5 minutes)
```bash
# 1.1 - Kill all dotnet
taskkill //F //IM dotnet.exe

# 1.2 - Check zombies (must be zero)
netstat -ano | findstr :4001 | findstr CLOSE_WAIT

# 1.3 - If zombies exist, restart Gateway now
```

### Step 2: Gateway Initialization (90 seconds)
```
1. Start IB Gateway
2. Login
3. Set 90-second timer
4. DO NOT TOUCH during init period
5. When timer expires, verify listening:
   netstat -ano | findstr :4001 | findstr LISTENING
```

### Step 3: Smoke Test (15 seconds)
```bash
cd /c/Code/AutoRevOption
dotnet run --project AutoRevOption.Tests/uProbe

# Expected: ✅ SUCCESS in ~100ms
# If timeout: DO NOT PROCEED - diagnose first
```

### Step 4: Start Monitor (Production)
```bash
# Only run if smoke test succeeded!
dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj

# Expected output:
# [IBKR] Connecting to 127.0.0.1:4001 (ClientId: 10)...
# [IBKR] eConnect() completed. IsConnected: True
# [IBKR] startApi() called - requesting handshake
# [IBKR] ✅ Connected successfully
# Retrieved 24 positions for account U21146542
```

### Step 5: Verify (30 seconds)
```bash
# Check no new zombies created
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
# Should still be empty

# Verify positions retrieved
# Monitor should show: "24 positions"
```

---

## What NOT To Do (Anti-Patterns)

❌ **Do NOT** run multiple connection attempts in parallel
❌ **Do NOT** retry immediately after failure
❌ **Do NOT** change ClientId between attempts
❌ **Do NOT** flip between Gateway and TWS rapidly
❌ **Do NOT** click around Gateway during initialization
❌ **Do NOT** skip the smoke test
❌ **Do NOT** ignore CLOSE_WAIT zombies

---

## Troubleshooting Decision Tree

```
Smoke test fails?
├─ Exit code 1 (eConnect failed)
│  ├─ Gateway not running? → Start Gateway, wait 90s, retry
│  └─ Port wrong? → Check secrets.json Port=4001
│
├─ Exit code 2 (Timeout)
│  ├─ CLOSE_WAIT zombies present?
│  │  └─ YES → Restart Gateway, wait 90s, retry ONCE
│  │  └─ NO → Check Gateway logs for API listener errors
│  │     ├─ No API log entries? → Gateway API stuck → Reinstall
│  │     └─ "Authentication failed"? → Check credentials
│  │
│  └─ Waited 90s after Gateway start?
│     └─ NO → That's the problem! Wait and retry
│
└─ Success → Proceed to Monitor
```

---

## Recovery Procedures

### Procedure A: Restart Gateway (fixes CLOSE_WAIT zombies)
```
1. Close IB Gateway
2. Verify Gateway process killed:
   tasklist | findstr ibgateway
3. Kill if still running:
   taskkill //F //IM ibgateway.exe
4. Wait 10 seconds
5. Start Gateway
6. Wait 90 seconds after login
7. Run smoke test
```

### Procedure B: Clean Reinstall Gateway (fixes stuck API listener)
```
1. Close Gateway
2. Backup config:
   Copy C:\IBKR\ibgateway\1040\camjbohogbpeiedgbeikipnekejkfebbjgkchfih\*.xml
3. Uninstall Gateway via Windows Settings
4. Delete C:\IBKR\ibgateway\1040\
5. Download fresh Gateway 10.40 from IBKR
6. Install
7. Configure:
   - API Settings → Trusted IPs: 127.0.0.1
   - API Settings → Read-Only: OFF
   - API Settings → Master API Client ID: 10
8. Login
9. Wait 90 seconds
10. Run smoke test
```

### Procedure C: Switch to TWS (fastest fallback)
```
1. Download TWS from IBKR
2. Install
3. Update secrets.json:
   Port: 7497
   IsPaperTrading: true
4. Start TWS, login to paper account
5. Wait 90 seconds
6. Run smoke test with port 7497
```

---

## Success Metrics (Monday)

✅ **Smoke test passes** (exit code 0, <1s execution)
✅ **Monitor connects** (shows "✅ Connected successfully")
✅ **Positions retrieved** (24 positions for U21146542)
✅ **No CLOSE_WAIT zombies** after 1 hour operation
✅ **MCP tools work** (get-positions returns data)

---

## Emergency Contacts

**If stuck on Monday morning:**

1. **Gateway issues** → IBKR Support: 877-442-2757
2. **TWS alternative** → Use port 7497 fallback (no support call needed)
3. **Code issues** → Review PlotGateway.md, Connection_Showstopper.md

**Escalation path:**
1. Try smoke test (15s)
2. If fails → restart Gateway (2min)
3. If still fails → switch to TWS fallback (5min)
4. If TWS works → proceed with TWS, debug Gateway offline

**DO NOT spend >10 minutes debugging Gateway on Monday - use TWS fallback**

---

## Configuration Files Reference

### secrets.json (Production - Gateway)
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 4001,
    "ClientId": 10,
    "GatewayPath": "C:\\IBKR\\ibgateway\\1040\\ibgateway.exe",
    "Username": "RevOption",
    "IsPaperTrading": false,
    "AutoLaunch": false,
    "AutoReconnect": false,
    "ReconnectDelaySeconds": 5
  }
}
```

### secrets.json (Fallback - TWS Paper)
```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 10,
    "GatewayPath": "",
    "Username": "RevOption",
    "IsPaperTrading": true,
    "AutoLaunch": false,
    "AutoReconnect": false,
    "ReconnectDelaySeconds": 5
  }
}
```

---

## Post-Monday Improvements

After Monday go-live succeeds, consider:

1. **Automated smoke test** in Monitor startup
2. **Health check endpoint** for monitoring
3. **Automatic TWS fallback** if Gateway fails
4. **Connection pooling** for multiple requests
5. **Better error messages** with recovery suggestions

---

**Last Updated:** 2025-10-05
**Owner:** Claude Code + User
**Next Review:** Monday after go-live

---

## Quick Reference Card (Print This!)

```
═══════════════════════════════════════════════════════
  MONDAY MORNING - IBKR CONNECTION CHECKLIST
═══════════════════════════════════════════════════════

□ 1. Kill all dotnet: taskkill //F //IM dotnet.exe
□ 2. Check zombies: netstat -ano | findstr :4001 | findstr CLOSE_WAIT
      └─ If any found: Restart Gateway!
□ 3. Start Gateway → Wait 90 seconds (SET TIMER!)
□ 4. Run smoke: dotnet run --project AutoRevOption.Tests/uProbe
      └─ Must see: ✅ SUCCESS in ~100ms
□ 5. If smoke fails: Use TWS fallback (Port 7497)
□ 6. Start Monitor: dotnet run --project AutoRevOption.Monitor
      └─ Must see: "24 positions"

⚠️  NO RETRIES! One attempt, diagnose, fix, retry.
⚠️  ClientId = 10 ALWAYS (never change!)
⚠️  Wait 90s after Gateway start (NOT negotiable!)

Fallback: secrets.json Port: 4001 → 7497 (takes 30s)
═══════════════════════════════════════════════════════
```
