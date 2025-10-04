# AutoRevOption.Monitor

Read-only IBKR connection monitor for account and position tracking.

## Features

- ✅ Connects to IBKR TWS/Gateway via TWS API
- ✅ Read-only operations (no order placement)
- ✅ Account summary (net liq, cash, buying power, margin)
- ✅ Position tracking (stocks, options, greeks)
- ✅ Auto-detect and launch IB Gateway
- ✅ 24x7 connection monitoring with auto-reconnect
- ✅ Configuration via `secrets.json`

## Setup

### 1. Install TWS or IB Gateway

Download from: https://www.interactivebrokers.com/en/trading/tws.php

### 2. Enable API Access

In TWS/Gateway:
1. **File → Global Configuration → API → Settings**
2. Check **"Enable ActiveX and Socket Clients"**
3. Check **"Allow connections from localhost only"** (recommended)
4. Add **127.0.0.1** to Trusted IPs
5. Set Socket port:
   - **7497** = Paper Trading
   - **7496** = Live Trading

### 3. Configure secrets.json

Edit `../secrets.json`:

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 1,
    "GatewayPath": "C:\\Jts\\ibgateway\\latest\\ibgateway.exe",
    "AutoLaunch": true,
    "AutoReconnect": true,
    "ReconnectDelaySeconds": 5
  }
}
```

**Configuration:**
- `Port: 7497` for paper trading, `7496` for live
- `ClientId` should be unique if running multiple connections
- `GatewayPath`: Path to ibgateway.exe (for auto-launch)
- `AutoLaunch`: Set `true` to auto-launch Gateway if not running
- `AutoReconnect`: Set `true` to reconnect on connection loss
- `ReconnectDelaySeconds`: Wait time before reconnect attempts

## Usage

```bash
cd AutoRevOption.Monitor
dotnet run
```

### Menu Options

1. **Get Account Summary** - Display account values (net liq, cash, margin)
2. **Get Positions** - List all open positions
3. **Monitor Loop** - Auto-refresh every 30 seconds
4. **q** - Quit

## Gateway Auto-Launch & Monitoring

The Monitor includes automatic Gateway detection and launch:

### Auto-Launch
When enabled in `secrets.json`, Monitor will:
- ✅ Check if Gateway is running on startup
- ✅ Launch Gateway if not running
- ✅ Wait for Gateway to initialize (up to 60 seconds)
- ✅ Prompt you to log in

**Note:** Auto-launch cannot log in automatically (IBKR security policy). You must enter credentials in the Gateway window.

### 24x7 Connection Monitoring
Monitor runs a background health check:
- ✅ Checks connection every 30 seconds
- ✅ Auto-reconnects if connection drops
- ✅ Logs all connection events
- ✅ Configurable retry delay

**For detailed 24x7 setup**, see: [IB Gateway 24x7 Setup Guide](../DOCS/IB_Gateway_24x7_Setup.md)

## Troubleshooting

### "IB Gateway is not running"

Monitor detects this and shows options:
1. Start IB Gateway manually
2. Enable `AutoLaunch: true` in secrets.json
3. Verify `GatewayPath` is correct for your OS

### "Failed to connect to IBKR API"

Gateway is running but API connection failed:
- ✅ Log in to IB Gateway
- ✅ API enabled: Configure → Settings → API
- ✅ Port matches (7497 for paper, 7496 for live)
- ✅ ClientId is not already in use
- ✅ Trusted IP includes 127.0.0.1

### "Account summary request timed out"

- ✅ You're logged into Gateway with valid credentials
- ✅ Account is active
- ✅ Try increasing timeout in code (default: 5000ms)

### "Gateway connection lost"

Monitor will auto-reconnect if enabled:
- ✅ Verify Gateway is still running
- ✅ Check `AutoReconnect: true` in secrets.json
- ✅ Monitor retries every 5 seconds (configurable)

### Empty positions

- Normal if you have no open positions
- Verify positions exist in Gateway Portfolio view

## Security Notes

- `secrets.json` is in `.gitignore` - never commit credentials
- Monitor runs read-only - no trading operations permitted
- Uses localhost connection only (127.0.0.1)

## Next Steps

After confirming connection works:
- Integrate with WP02 data model
- Add Greek calculations (delta, theta, vega)
- Feed data to OptionsRadar scanner
