# Session Management - Browser Authentication Persistence

## Overview

The AutoRevOption client maintains a persistent browser session for IBKR Client Portal authentication to minimize 2FA requirements.

## Session Persistence Strategy

### ✅ **INDEFINITE** Persistence (Default Behavior)

Browser sessions are kept alive **indefinitely** with the following characteristics:

1. **No Automatic Cleanup**: Browser session remains open in the background
2. **Persists Across App Restarts**: Gateway process continues running even when apps close
3. **Single 2FA**: Only authenticate once, all subsequent apps reuse the session
4. **Manual Reset Only**: Session ends only when explicitly instructed

### Session Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│ 1. First App Starts (e.g., Monitor)                         │
│    ├─ LoginAsync() → Browser automation starts              │
│    ├─ Enter username/password                               │
│    ├─ Approve 2FA on mobile app                             │
│    ├─ Session established                                   │
│    └─ Browser kept alive in background (keepSessionAlive=true)│
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Second App Starts (e.g., Minimal)                        │
│    ├─ LoginAsync() → Checks existing authentication         │
│    ├─ Session still valid → NO login needed                 │
│    ├─ NO 2FA needed                                         │
│    └─ Returns authenticated client immediately              │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Apps Close                                                │
│    ├─ Gateway process continues running                     │
│    ├─ Browser session remains active                        │
│    └─ Authentication persists                               │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Next Day - App Starts Again                              │
│    ├─ Gateway still running from yesterday                  │
│    ├─ Session still valid → NO login needed                 │
│    └─ Seamless reauth without 2FA                           │
└─────────────────────────────────────────────────────────────┘
```

## Configuration

### Default Settings (Recommended)

```csharp
var client = await loginService.LoginAsync(
    headless: true,                  // Run browser invisibly
    twoFactorTimeoutMinutes: 2       // Wait 2min for 2FA approval
);
// keepSessionAlive is TRUE by default (indefinite persistence)
```

### Session Behavior

| Setting | Behavior |
|---------|----------|
| `keepSessionAlive = true` (default) | Browser stays open indefinitely, session persists across app restarts |
| `keepSessionAlive = false` | Browser closes after login, session ends when app closes |

## Explicit Session Reset

### When to Reset Session

- Switching between paper/live trading accounts
- Security compliance (periodic re-authentication)
- Session appears corrupted
- Manual force logout required

### How to Reset Session

#### Option 1: Via ClientPortalBrowserLogin

```csharp
var browserLogin = new ClientPortalBrowserLogin();
await browserLogin.LoginAsync(username, password, keepSessionAlive: true);

// ... later, when you want to force logout ...
browserLogin.ResetSession();
// Next LoginAsync() call will require fresh 2FA
```

#### Option 2: Via Connection Class

```csharp
var connection = new Connection(credentials);
await connection.ConnectAsync();

// ... later ...
connection.ResetSession();  // Forces logout and clears browser session
// Next ConnectAsync() will require fresh login + 2FA
```

#### Option 3: Via ClientPortalLoginService

```csharp
var loginService = new ClientPortalLoginService();
var client = await loginService.LoginAsync();

// ... later ...
loginService.ResetSession();  // Clears all session state
// Next LoginAsync() will start fresh authentication
```

### Reset Behavior

```
ResetSession() does:
1. Close browser (ends Selenium WebDriver session)
2. Clear authentication cookies
3. Reset internal flags (_keepSessionAlive = false, _disposed = false)
4. Set _driver = null (allows fresh browser instance)
5. Log: "🔄 Resetting session - Next login will require fresh 2FA"
```

## Technical Implementation

### Browser Session Persistence

File: `AutoRevOption.Shared/Portal/ClientPortalBrowserLogin.cs`

```csharp
private IWebDriver? _driver;
private bool _keepSessionAlive;

public async Task<bool> LoginAsync(..., bool keepSessionAlive = true)
{
    _driver = new ChromeDriver(options);
    // ... perform login ...

    _keepSessionAlive = keepSessionAlive;

    if (_keepSessionAlive)
    {
        Console.WriteLine("[Browser] 🔒 Keeping browser session alive INDEFINITELY");
        Console.WriteLine("[Browser]    Session persists indefinitely - no auto-cleanup");
        Console.WriteLine("[Browser]    Call ResetSession() to force logout and re-authentication");
        // Browser stays open - NO Dispose() called
    }

    return true;
}

public void ResetSession()
{
    Console.WriteLine("[Browser] 🔄 Resetting session - closing browser and clearing authentication");
    Console.WriteLine("[Browser]    Next login will require fresh 2FA approval");
    Dispose();  // Close browser

    // Reset flags to allow fresh login
    _keepSessionAlive = false;
    _disposed = false;
    _driver = null;
}
```

### Gateway Process Persistence

File: `AutoRevOption.Client/GatewayProcessManager.cs`

```csharp
// Singleton - only one gateway across all app instances
public static GatewayProcessManager Instance { get; }

public async Task<bool> EnsureGatewayRunningAsync()
{
    if (IsGatewayRunning())  // Check port 5000
    {
        Console.WriteLine("[Gateway] Client Portal Gateway is already running");
        return true;  // Reuse existing gateway
    }

    // Start new gateway process
    _gatewayProcess = Process.Start(startInfo);
    // Gateway runs as background daemon
}
```

## Session Sharing

Multiple apps automatically share the same session:

1. **Monitor App** starts → Logs in → Browser session created → Gateway running
2. **Minimal App** starts → Detects gateway on port 5000 → Reuses session → No 2FA needed
3. Both apps close → Gateway keeps running → Session persists
4. Tomorrow: **Any app** starts → Detects gateway → Reuses session → No 2FA needed

## Security Notes

### Automatic Cleanup (None by Default)

⚠️ **IMPORTANT**: Browser sessions do NOT auto-cleanup. This is intentional for convenience.

If you need periodic re-authentication:
- Implement a scheduled task to call `ResetSession()` daily/weekly
- Monitor session age and force reset after N days
- Use IBKR's own session timeout mechanisms (they may force logout after extended periods)

### Manual Cleanup Scenarios

| Scenario | Action |
|----------|--------|
| End of trading day | Optional: `ResetSession()` for fresh start tomorrow |
| Switching accounts | Required: `ResetSession()` then login with new credentials |
| Security audit | Required: `ResetSession()` to force re-authentication |
| Session corruption | Required: `ResetSession()` to clear stale state |

## Benefits

✅ **Convenience**: 2FA only once, then seamless access
✅ **Multi-App Support**: Monitor, Minimal, Tests all share same session
✅ **Persistence**: Session survives app restarts, reboots (if gateway stays running)
✅ **Explicit Control**: Session ends only when you explicitly reset it
✅ **No Surprises**: Predictable behavior - you control session lifecycle

## Example Usage

### Typical Daily Workflow

```csharp
// Morning: First app starts
var loginService = new ClientPortalLoginService();
var client = await loginService.LoginAsync();
// → Browser opens, 2FA required, session established

// Throughout the day: Start/stop multiple apps
// → All apps reuse same session, no 2FA

// End of day: All apps closed
// → Gateway still running, session still valid

// Next morning: Start app again
// → Gateway detected, session reused, no 2FA
```

### Force Fresh Login

```csharp
// If you need to force logout and re-login:
loginService.ResetSession();

// Next login requires fresh 2FA
var client = await loginService.LoginAsync();
// → Browser opens, 2FA required, new session
```

## Troubleshooting

### Session Not Persisting

**Symptom**: Every app start requires 2FA

**Solutions**:
1. Check `keepSessionAlive` parameter (should be `true`)
2. Verify gateway process is running: `tasklist | findstr java`
3. Check port 5000: `netstat -an | findstr :5000`
4. Browser may have been disposed - check logs for "Resetting session"

### Can't Force Logout

**Symptom**: `ResetSession()` doesn't work

**Solutions**:
1. Ensure you have reference to the correct instance
2. Check if multiple `ClientPortalBrowserLogin` instances exist
3. Try `Dispose()` directly
4. Manually kill Chrome process if needed

### Gateway Won't Stay Running

**Symptom**: Gateway process dies after app closes

**Solutions**:
1. Check `GatewayProcessManager` - should NOT dispose on app exit
2. Look for exceptions in gateway logs
3. Verify Java path is correct
4. Check gateway process: `Get-Process | Where-Object {$_.ProcessName -like "*java*"}`
