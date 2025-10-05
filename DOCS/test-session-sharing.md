# Session Sharing Demo

## Setup
Both Monitor and Minimal use `ClientPortalLoginService` which internally uses `GatewayProcessManager.Instance` (singleton).

## Test Scenario

### Step 1: Start Monitor App
```bash
dotnet run --project AutoRevOption.Monitor
```

**What happens:**
1. `ClientPortalLoginService.LoginAsync()` called
2. `GatewayProcessManager.Instance.EnsureGatewayRunningAsync()` checks port 5000
3. Port 5000 is free → Starts Java Gateway process
4. Browser automation logs in (requires 2FA approval on mobile)
5. Monitor gets authenticated `AutoRevClient`
6. Monitor app runs with full IBKR access

**Gateway Status:** ✅ Running on port 5000

---

### Step 2: Start Minimal App (while Monitor still running)
```bash
# In a different terminal
dotnet run --project AutoRevOption.Minimal --mcp
```

**What happens:**
1. `ClientPortalLoginService.LoginAsync()` called
2. `GatewayProcessManager.Instance.EnsureGatewayRunningAsync()` checks port 5000
3. Port 5000 is ALREADY IN USE → Skips starting new gateway
4. Checks authentication status via existing gateway
5. Authentication already valid → Returns authenticated `AutoRevClient`
6. Minimal app runs with full IBKR access
7. **NO 2FA REQUIRED** - session is shared!

**Gateway Status:** ✅ Same gateway process serving both apps

---

### Step 3: Verify Sharing

**Check running processes:**
```powershell
tasklist | findstr java
```
**Result:** Only ONE java.exe process running

**Check port usage:**
```bash
netstat -an | findstr :5000
```
**Result:** `TCP [::]:5000 LISTENING` (one listener)

---

## Key Points

✅ **Singleton Pattern:** `GatewayProcessManager.Instance` ensures one gateway across all processes
✅ **Port Detection:** Automatically detects if gateway is already running
✅ **Session Reuse:** Browser session cookies shared via gateway
✅ **No Re-authentication:** Second app doesn't need 2FA
✅ **Automatic Cleanup:** Gateway stays alive as long as any app needs it

## Architecture Benefits

1. **Efficient:** One gateway serves all apps
2. **Seamless:** No manual coordination needed
3. **Secure:** All apps share same authenticated session
4. **Convenient:** 2FA only once per login session
