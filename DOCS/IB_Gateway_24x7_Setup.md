# IB Gateway 24x7 Setup Guide

Complete guide to run IB Gateway continuously with AutoRevOption.

## Installation

### 1. Download IB Gateway

**Paper Trading (Recommended for testing):**
- https://www.interactivebrokers.com/en/trading/ibgateway-stable.php
- Select "IB Gateway - Paper Trading"

**Live Trading:**
- Same download link, select "IB Gateway - Live"

**Installation Paths:**
- Windows: `C:\Jts\ibgateway\latest\ibgateway.exe`
- Mac: `/Applications/IB Gateway.app`
- Linux: `~/Jts/ibgateway/latest/ibgateway`

### 2. Configure API Access

Launch IB Gateway and log in, then:

1. **Click: Configure → Settings** (gear icon)
2. **Navigate to: API → Settings**
3. **Configure:**

   **Socket Port:**
   - Paper: `7497`
   - Live: `7496`

   **Enable:**
   - ✅ Enable ActiveX and Socket Clients
   - ✅ Allow connections from localhost only
   - ✅ Read-Only API (for Monitor app)

   **Trusted IPs:**
   - Add: `127.0.0.1`

   **Optional:**
   - ✅ Create API message log file (for debugging)
   - Master API client ID: (leave blank or set to 0)

4. **Click OK and restart IB Gateway**

## Configuration

### secrets.json Setup

Edit `secrets.json` in project root:

```json
{
  "IBKRCredentials": {
    "Host": "127.0.0.1",
    "Port": 7497,
    "ClientId": 1,
    "GatewayPath": "C:\\Jts\\ibgateway\\latest\\ibgateway.exe",
    "Username": "your-ibkr-username",
    "IsPaperTrading": true,
    "AutoLaunch": true,
    "AutoReconnect": true,
    "ReconnectDelaySeconds": 5
  }
}
```

**Key Settings:**

- **Port:** 7497 (paper) or 7496 (live)
- **GatewayPath:** Full path to ibgateway.exe
- **AutoLaunch:** Set `true` to auto-launch Gateway if not running
- **AutoReconnect:** Set `true` to auto-reconnect on connection loss
- **ReconnectDelaySeconds:** Wait time before reconnect attempts

### Platform-Specific Paths

**Windows:**
```json
"GatewayPath": "C:\\Jts\\ibgateway\\latest\\ibgateway.exe"
```

**Mac:**
```json
"GatewayPath": "/Applications/IB Gateway.app/Contents/MacOS/ibgateway"
```

**Linux:**
```json
"GatewayPath": "/home/username/Jts/ibgateway/latest/ibgateway"
```

## Running 24x7

### Option 1: Manual Keep-Alive

**Disable Auto-Logout:**
1. IB Gateway → Configure → Settings
2. **Lock and Exit → Auto logoff time**
3. Set to maximum (e.g., 24 hours)
4. Must re-login daily

**Pros:** Simple, secure
**Cons:** Requires daily login

### Option 2: AutoRevOption.Monitor with Auto-Reconnect

The Monitor app includes automatic reconnection:

```bash
cd AutoRevOption.Monitor
dotnet run
```

**Features:**
- ✅ Checks Gateway status on startup
- ✅ Auto-launches Gateway if configured
- ✅ Monitors connection every 30 seconds
- ✅ Auto-reconnects on connection loss
- ✅ Alerts on disconnection

**Limitations:**
- You must still log in to IB Gateway manually
- Cannot auto-login (IBKR security policy)

### Option 3: Windows Service (Advanced)

Run Monitor as a Windows Service for true 24x7 operation:

**1. Install .NET Windows Service SDK:**
```bash
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
```

**2. Create Service Wrapper:**
```csharp
// In AutoRevOption.Monitor/Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddHostedService<MonitorService>();
var host = builder.Build();
host.Run();
```

**3. Publish and Install:**
```bash
dotnet publish -c Release -r win-x64 --self-contained
sc create AutoRevMonitor binPath="C:\path\to\AutoRevOption.Monitor.exe"
sc start AutoRevMonitor
```

**Note:** Still requires IB Gateway to be running and logged in.

## Auto-Launch Features

AutoRevOption.Monitor can automatically launch IB Gateway if not running:

### How It Works

1. **Check:** Monitors port 7497/7496 for connectivity
2. **Detect:** If port closed, checks if ibgateway.exe is running
3. **Launch:** If `AutoLaunch: true`, starts Gateway process
4. **Wait:** Waits up to 60 seconds for Gateway to initialize
5. **Notify:** You must still log in manually

### Enable Auto-Launch

In `secrets.json`:
```json
{
  "IBKRCredentials": {
    "AutoLaunch": true,
    "GatewayPath": "C:\\Jts\\ibgateway\\latest\\ibgateway.exe"
  }
}
```

### Behavior

When you run Monitor:
```
[Gateway] Checking IB Gateway status...
[Gateway] ⚠️  IB Gateway is not running
[Gateway] 🚀 Launching IB Gateway from: C:\Jts\ibgateway\latest\ibgateway.exe
[Gateway] Process started (PID: 12345)
[Gateway] ⏳ Waiting for Gateway to initialize...
[Gateway] NOTE: You must manually log in to IB Gateway!
.....
[Gateway] ✅ Gateway is ready on port 7497
```

## Monitoring & Health Checks

The Monitor app includes built-in health monitoring:

### Connection Monitoring

Runs in background, checks every 30 seconds:

```
[Gateway] Starting 24x7 monitoring...
[10:15:30] [Gateway] ✅ Running (port 7497 open)
[10:16:00] [Gateway] ✅ Running (port 7497 open)
[10:16:30] [Gateway] ⚠️  Gateway connection lost!
[Gateway] Attempting to reconnect in 5s...
```

### Auto-Reconnect

On connection loss:
1. Waits `ReconnectDelaySeconds` (default: 5s)
2. Checks if Gateway is running
3. Attempts to re-establish connection
4. Logs all reconnection attempts

### Status Command

Check Gateway status anytime:
```bash
cd AutoRevOption.Monitor
dotnet run
# Shows: ✅ Running (port 7497 open)
```

## Troubleshooting

### Gateway Won't Auto-Launch

**Error:** `Gateway executable not found`

**Fix:**
1. Verify `GatewayPath` in secrets.json
2. Check file exists at that path
3. Ensure path uses escaped backslashes: `C:\\Jts\\...`

### Gateway Launches but Won't Connect

**Cause:** Not logged in

**Fix:**
- Auto-launch cannot auto-login (IBKR security)
- You must manually enter credentials in Gateway window
- Consider using saved credentials in IB Gateway (File → Save Settings)

### Connection Keeps Dropping

**Causes:**
1. Auto-logout timer expired
2. Internet connection loss
3. IB Gateway crashed
4. IBKR server maintenance

**Fix:**
1. Increase auto-logout timer
2. Enable `AutoReconnect: true` in secrets.json
3. Check IB Gateway logs: File → View → Logs
4. Check IBKR system status: https://www.interactivebrokers.com/en/index.php?f=2225

### Port Already in Use

**Error:** `Socket port is already in use`

**Fix:**
1. Another app is connected with same ClientId
2. Change `ClientId` in secrets.json (e.g., 1 → 2)
3. Or close other application using the port

## Best Practices

### For Development
- ✅ Use Paper Trading (port 7497)
- ✅ Set `AutoLaunch: true` for convenience
- ✅ Monitor connection status in console
- ✅ Test reconnection by closing Gateway manually

### For Production
- ✅ Use dedicated server/VM
- ✅ Configure longer auto-logout time
- ✅ Enable `AutoReconnect: true`
- ✅ Run as Windows Service (advanced)
- ✅ Monitor logs regularly
- ✅ Set up alerts for connection loss
- ✅ Use `ReadOnly API` for Monitor app

### Security
- ✅ Keep `secrets.json` in `.gitignore`
- ✅ Use `localhost only` in API settings
- ✅ Use separate ClientId for each app
- ✅ Enable Read-Only API for Monitor
- ✅ Never commit credentials to git

## Scripts

### Windows: Auto-Start on Boot

Create `start-gateway.bat`:
```batch
@echo off
start "" "C:\Jts\ibgateway\latest\ibgateway.exe"
timeout /t 60
cd C:\Code\AutoRevOption\AutoRevOption.Monitor
dotnet run
```

Add to Task Scheduler:
1. Task Scheduler → Create Task
2. Trigger: At system startup
3. Action: Run `start-gateway.bat`
4. Settings: Restart on failure

### Linux: systemd Service

Create `/etc/systemd/system/ibgateway.service`:
```ini
[Unit]
Description=IB Gateway Monitor
After=network.target

[Service]
Type=simple
User=your-username
WorkingDirectory=/home/your-username/AutoRevOption/AutoRevOption.Monitor
ExecStart=/usr/bin/dotnet run
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable:
```bash
sudo systemctl enable ibgateway
sudo systemctl start ibgateway
sudo systemctl status ibgateway
```

## Logging

Monitor app logs to console. For persistent logs:

**Windows PowerShell:**
```powershell
cd AutoRevOption.Monitor
dotnet run | Tee-Object -FilePath "monitor.log"
```

**Linux/Mac:**
```bash
cd AutoRevOption.Monitor
dotnet run 2>&1 | tee monitor.log
```

## Next Steps

Once Gateway is running 24x7:
1. ✅ Verify connection stability over 24 hours
2. ✅ Test auto-reconnect by restarting Gateway
3. ✅ Integrate with WP02 data model
4. ✅ Add position monitoring and alerts
5. ✅ Set up automated trading workflows
