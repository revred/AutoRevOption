# IB Gateway API Setup Guide

## Current Issue

The application connects to the Gateway socket (port 4001) but receives no TWS API callbacks (`connect Ack`, `nextValidId`, or error messages). This indicates the **Gateway API is not enabled or configured correctly**.

## Required Setup Steps

### 1. Open IB Gateway Configuration

1. Launch IB Gateway (should already be running)
2. Click on the **gear icon** (⚙️) or go to **File → Global Configuration**

### 2. Enable API Settings

1. In the configuration window, navigate to: **API → Settings**
2. Check the following boxes:
   - ✅ **Enable ActiveX and Socket Clients** (REQUIRED)
   - ✅ **Allow connections from localhost only** (recommended for security)
   - ❌ **Read-Only API** (should be UNCHECKED for account queries)
   - ✅ **Download open orders on connection** (optional but recommended)

3. **Socket Port:** Verify it shows `4001` for live trading or `4002` for paper trading

4. **Trusted IPs:** Ensure `127.0.0.1` is in the list

### 3. Master API Configuration

1. Still in **API → Settings**, check:
   - ✅ **Create API message log file** (helpful for debugging)
   - Set **Logging Level** to `Detail` or `Error` (for troubleshooting)

### 4. Apply and Restart

1. Click **OK** to save changes
2. **Restart IB Gateway** (important - API settings require restart)
3. Log back in
4. Wait for Gateway to fully initialize (you should see "System ready" message)

### 5. Verify API is Enabled

After restarting, check:
- Gateway should show a small **API icon** in the status bar (usually bottom right)
- The API icon should be **green** or **active**, not grayed out

---

## Testing the Connection

Once Gateway is configured, test with:

```bash
cd AutoRevOption.Monitor
dotnet run
```

**Expected output:**
```
[IBKR] Connecting to 127.0.0.1:4001 (ClientId: 10)...
[IBKR] Error/Info [2104]: Market data farm connection is OK:usfarm.nj
[IBKR] Error/Info [2106]: HMDS data farm connection is OK:ushmds
[IBKR] Next valid order ID: 1
[IBKR] ✅ Connected successfully
[IBKR] Managed accounts: U123456
```

---

## Common Issues

### Issue 1: No callbacks received (current problem)
**Symptom:** Connection hangs, no error messages
**Cause:** API not enabled in Gateway settings
**Fix:** Follow steps above to enable API

### Issue 2: "Connection refused" error
**Symptom:** `Error [502]: Couldn't connect to TWS`
**Cause:** Gateway not running or wrong port
**Fix:**
- Verify Gateway is running
- Check port: 4001 (live) or 4002 (paper)
- Match port in `secrets.json`

### Issue 3: "Authentication failed"
**Symptom:** `Error [504]: Not connected`
**Cause:** API authentication issue
**Fix:**
- Ensure "Read-Only API" is unchecked
- Try different ClientId (change from 10 to 11 in secrets.json)

### Issue 4: "Connection blocked"
**Symptom:** Connection established but immediately disconnected
**Cause:** IP not in trusted list
**Fix:**
- Add `127.0.0.1` to Trusted IPs in API Settings
- Disable firewall temporarily to test

---

## Port Reference

| Mode | Port | secrets.json Setting |
|------|------|---------------------|
| **Live Trading** | 4001 | `"Port": 4001, "IsPaperTrading": false` |
| **Paper Trading** | 4002 | `"Port": 4002, "IsPaperTrading": true` |

---

## API Settings Screenshot Locations

In IB Gateway:
```
File → Global Configuration → API → Settings
```

Key settings:
- **Enable ActiveX and Socket Clients**: ✅ MUST BE CHECKED
- **Socket port**: 4001 (live) or 4002 (paper)
- **Trusted IPs**: 127.0.0.1
- **Read-Only API**: ❌ MUST BE UNCHECKED
- **Create API message log**: ✅ Helpful for debugging

---

## Next Steps

1. Follow the setup steps above
2. **Restart IB Gateway**
3. Run the Monitor application again
4. You should see connection callbacks and account information

If you continue to have issues, check the IB Gateway API log file (usually in `~/Jts/` folder) for error messages.
