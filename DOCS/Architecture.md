# AutoRevOption - Architecture Documentation

**Date:** 2025-10-05
**Status:** Active Development

---

## Project Overview

AutoRevOption is an automated options revenue generation system that integrates with Interactive Brokers (IBKR) to monitor positions and execute trading strategies.

## Solution Structure

```
AutoRevOption/
├── AutoRevOption.Shared/          # Core shared libraries
│   ├── Configuration/             # Configuration models (IBKRCredentials, etc.)
│   ├── Ibkr/                      # IBKR TWS API integration
│   │   └── IbkrConnection.cs      # Main connection wrapper (EWrapper implementation)
│   └── Models/                    # Shared data models
│
├── AutoRevOption.Monitor/         # MCP server for Claude Desktop integration
│   ├── Program.cs                 # MCP stdin/stdout server
│   └── McpHandlers.cs             # MCP tool handlers (get-positions, etc.)
│
├── AutoRevOption.Minimal/         # Interactive demo console app
│   └── Program.cs                 # Simple connection test
│
├── AutoRevOption.Tests/           # Test suite
│   ├── IbkrConnectionTests.cs     # Connection integration tests
│   └── uProbe/                    # Minimal IBKR handshake probe
│       ├── uProbe.csproj          # Standalone diagnostic tool
│       └── Program.cs             # Tests Gateway API handshake
│
├── scripts/                       # Automation scripts
│   ├── connect-gateway.sh         # Controlled connection with validation
│   ├── get-positions.sh           # Helper to retrieve positions
│   ├── test-gateway-socket.sh     # Raw socket diagnostics
│   ├── uprobe.sh                  # Run uProbe handshake test
│   └── README.md                  # Script documentation
│
├── docs/                          # Documentation
│   ├── Architecture.md            # This file
│   ├── Gateway_Connection_Procedure.md  # Connection troubleshooting
│   └── IB_Gateway_Connection_Issue.md   # Historical debugging notes
│
├── DOCS/                          # Historical documentation
│   └── PlotGateway.md             # Complete connection debugging timeline
│
├── CODING_GUIDELINES.md           # Project coding standards
├── secrets.json                   # IBKR credentials (gitignored)
└── AutoRevOption.sln              # Solution file
```

---

## Component Details

### 1. AutoRevOption.Shared

**Purpose:** Core library with IBKR integration
**Target:** .NET 9.0
**Key Dependencies:**
- `CSharpAPI.dll` - IBKR TWS API 10.37.02 (local reference)
- `Google.Protobuf` - ProtoBuf support for API messages
- `YamlDotNet` - Configuration parsing

**Main Classes:**

#### IbkrConnection.cs
- **Implements:** `EWrapper` interface (74+ methods including ProtoBuf)
- **Responsibility:** Manages TWS API connection lifecycle
- **Key Methods:**
  - `ConnectAsync()` - Establishes Gateway connection with `startApi()` call
  - `RequestPositions()` - Retrieves account positions
  - `RequestAccountSummary()` - Gets account balance/margin data
- **Connection Flow:**
  1. `eConnect()` - TCP connection
  2. Start `EReader` message processor
  3. **`startApi()`** ⚡ **CRITICAL** - Triggers handshake
  4. Wait for `nextValidId` callback (10s timeout)
  5. Connection established

**Critical Discovery:** Missing `client.startApi()` call was root cause of connection failures. Gateway accepts TCP but won't send handshake bytes until `startApi()` is called.

---

### 2. AutoRevOption.Monitor (MCP Server)

**Purpose:** Model Context Protocol server for Claude Desktop
**Protocol:** stdin/stdout JSON-RPC
**Target:** .NET 9.0

**MCP Tools Exposed:**
- `get-positions` - Returns all IBKR positions
- `get-account-info` - Returns account summary (balance, margin, etc.)

**Usage:**
```bash
dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj -- --mcp
```

**Integration:** Configure in Claude Desktop `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "autorev": {
      "command": "dotnet",
      "args": ["run", "--project", "AutoRevOption.Monitor/AutoRevOption.Monitor.csproj", "--", "--mcp"]
    }
  }
}
```

---

### 3. AutoRevOption.Tests

**Purpose:** Test suite and diagnostic tools
**Framework:** xUnit
**Target:** .NET 9.0

**Test Classes:**
- `IbkrConnectionTests` - Integration tests for Gateway connection
- `IbkrRawSocketTests` - Low-level TCP socket diagnostics

**uProbe Diagnostic Tool:**
- **Location:** `AutoRevOption.Tests/uProbe/`
- **Purpose:** Minimal handshake probe to test Gateway API
- **Features:**
  - Complete EWrapper implementation
  - Step-by-step connection logging
  - Calls `startApi()` to trigger handshake
  - 10 second timeout with diagnostics
  - Exit codes: 0=success, 1=eConnect failed, 2=timeout

**Run uProbe:**
```bash
bash scripts/uprobe.sh [host] [port] [clientId]
# Example: bash scripts/uprobe.sh 127.0.0.1 4001 10
```

---

### 4. Scripts Directory

All automation scripts follow CODING_GUIDELINES.md:
- ✅ Bash scripts (`.sh`) only - no PowerShell in root
- ✅ Placed in `scripts/` folder
- ✅ Proper error handling and cleanup
- ✅ Documented in `scripts/README.md`

**Key Scripts:**

#### connect-gateway.sh
Controlled connection with pre-flight validation:
1. Terminates all dotnet processes
2. Detects CLOSE_WAIT zombie sockets
3. **Fails fast** if zombies detected (requires Gateway restart)
4. Verifies Gateway listening state
5. Single connection attempt with timeout
6. Post-connection socket verification

#### uprobe.sh
Runs minimal handshake test to diagnose Gateway API issues.

---

## Configuration

### secrets.json
**Location:** Root directory (gitignored)
**Format:**
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
    "AutoReconnect": true,
    "ReconnectDelaySeconds": 5
  }
}
```

**Port Reference:**
- `4001` - IB Gateway (live trading)
- `4002` - IB Gateway (paper trading)
- `7497` - TWS Workstation (paper trading)
- `7496` - TWS Workstation (live trading)

---

## External Dependencies

### IBKR TWS API
- **Version:** 10.37.02
- **Location:** `C:\IBKR\TWS_API\source\CSharpClient\`
- **DLL Path:** `client\bin\Release\netstandard2.0\CSharpAPI.dll`
- **Reference:** Local file reference (not NuGet)

### IB Gateway
- **Version:** 10.40.1b
- **Install Path:** `C:\IBKR\ibgateway\1040\`
- **Configuration:** `camjbohogbpeiedgbeikipnekejkfebbjgkchfih/*.xml`
- **API Settings:**
  - Enable ActiveX and Socket Clients: ON (by default in 10.40)
  - Trusted IPs: 127.0.0.1
  - Read-Only API: OFF
  - Master API Client ID: Blank (or fixed to 10)

---

## Data Flow

### Position Retrieval Flow
```
1. User/Claude requests positions
   ↓
2. MCP Server receives get-positions tool call
   ↓
3. IbkrConnection.ConnectAsync()
   → eConnect() to Gateway
   → Start EReader message processor
   → startApi() to trigger handshake ⚡ CRITICAL
   → Wait for nextValidId callback
   ↓
4. IbkrConnection.RequestPositions()
   → client.reqPositions()
   ↓
5. Gateway calls position() callbacks
   → Stored in _positions dictionary
   ↓
6. Gateway calls positionEnd() callback
   → Signals completion
   ↓
7. Return positions to MCP client
   ↓
8. Claude displays to user
```

---

## Known Issues & Solutions

### Issue 1: Connection Hangs Indefinitely

**Symptom:** `eConnect()` blocks, no callbacks triggered
**Root Cause:** Missing `client.startApi()` call after eConnect()
**Solution:** Added `_client.startApi()` in IbkrConnection.cs:100

**Evidence:**
- Gateway accepts TCP connection (socket opens)
- Gateway never sends handshake bytes without `startApi()`
- Logs never show "eConnect() completed"

**Fix Applied:** 2025-10-05 in commit [pending]

---

### Issue 2: CLOSE_WAIT Zombie Sockets

**Symptom:** Gateway accumulates sockets in CLOSE_WAIT state
**Root Cause:** Failed connection attempts don't close cleanly
**Impact:** Each zombie consumes a connection slot; when limit reached, new connections blocked

**Detection:**
```bash
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
```

**Solution:** Restart IB Gateway to clear zombie sockets (they don't timeout)

**Prevention:** Use `scripts/connect-gateway.sh` which detects zombies pre-flight

**Documentation:** `docs/Gateway_Connection_Procedure.md`

---

### Issue 3: Gateway API Initialization Delay

**Symptom:** Connection fails if attempted immediately after Gateway login
**Root Cause:** Gateway API server takes 30-60s to initialize after UI shows "connected"
**Solution:** Wait 60s after Gateway restart before attempting connection

---

## Testing Strategy

### Unit Tests
- Not applicable (external Gateway dependency)

### Integration Tests
- `IbkrConnectionTests.cs` - Tests actual Gateway connection
- Requires Gateway running and configured

### Diagnostic Tools
- `uProbe` - Minimal handshake probe
- `scripts/connect-gateway.sh` - Automated validation
- `scripts/test-gateway-socket.sh` - Raw socket test

### Manual Testing
```bash
# 1. Pre-flight check
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
# Should return empty

# 2. Run uProbe
bash scripts/uprobe.sh 127.0.0.1 4001 10
# Should return: ✅ SUCCESS in ~100ms

# 3. Test Monitor
dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj
# Should connect and retrieve positions
```

---

## Development Guidelines

See [CODING_GUIDELINES.md](../CODING_GUIDELINES.md) for:
- Script placement rules (`.sh` in `scripts/`, no `.ps1` in root)
- Async/await patterns
- Process cleanup requirements
- Naming conventions

---

## Troubleshooting

### Connection Fails

**1. Check CLOSE_WAIT zombies:**
```bash
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
```
If present → restart Gateway

**2. Verify Gateway listening:**
```bash
netstat -ano | findstr :4001 | findstr LISTENING
```
Should show: `TCP 0.0.0.0:4001 ... LISTENING`

**3. Check Gateway API settings:**
- Configure → Settings → API → Precautions
- Trusted IPs: 127.0.0.1
- Read-Only API: OFF

**4. Run diagnostic probe:**
```bash
bash scripts/uprobe.sh
```

**5. Review Gateway logs:**
- Location: `C:\IBKR\ibgateway\1040\<profile>\logs\`
- Look for session token authentication and API connection messages

**Full troubleshooting guide:** [docs/Gateway_Connection_Procedure.md](../docs/Gateway_Connection_Procedure.md)

---

## Historical Context

**Complete debugging timeline:** [DOCS/PlotGateway.md](../DOCS/PlotGateway.md)

**Key milestones:**
1. Initial connection failures - thought to be missing ProtoBuf methods
2. Implemented 74 EWrapper methods - still failed
3. Discovered CLOSE_WAIT zombie socket accumulation
4. Found Gateway logs showing successful connection at 15:39:19
5. **Identified missing `startApi()` call as root cause** (2025-10-05)
6. Created uProbe diagnostic tool
7. Added automated pre-flight validation

**Lessons learned:**
- TWS API requires explicit `startApi()` call to trigger handshake
- Gateway initialization takes 30-60s after login
- CLOSE_WAIT sockets only clear on Gateway restart
- Pre-flight validation prevents zombie accumulation
- Connection attempts are NOT idempotent

---

## Future Enhancements

1. **Automated Gateway management** - Start/stop Gateway programmatically
2. **Connection retry logic** - With exponential backoff
3. **Health monitoring** - Detect connection loss and reconnect
4. **TWS support** - Add TWS Workstation as alternative to Gateway
5. **Multi-account support** - Handle multiple IBKR accounts
6. **Position analysis** - Calculate Greeks, risk metrics
7. **Strategy execution** - Implement trading strategies

---

## References

- **IBKR TWS API Documentation:** https://interactivebrokers.github.io/tws-api/
- **MCP Protocol Spec:** https://spec.modelcontextprotocol.io/
- **Project Repository:** (local development)

---

**Last Updated:** 2025-10-05
**Maintainer:** Claude Code + User
