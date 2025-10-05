# IB Gateway Connection Procedure - Reliable Replication

## Root Cause Analysis

**Problem:** Gateway CLOSE_WAIT zombie sockets block new connections
**Evidence:** `netstat -ano | findstr :4001` shows multiple CLOSE_WAIT connections

```
TCP    127.0.0.1:4001         127.0.0.1:63650        CLOSE_WAIT      18684
TCP    127.0.0.1:4001         127.0.0.1:63651        CLOSE_WAIT      18684
TCP    127.0.0.1:4001         127.0.0.1:63652        CLOSE_WAIT      18684
TCP    127.0.0.1:4001         127.0.0.1:63653        CLOSE_WAIT      18684
```

**Why:** When client closes connection without proper shutdown sequence, Gateway holds socket in CLOSE_WAIT indefinitely. Multiple failed attempts accumulate until Gateway hits connection limit.

## Zombie Process Control Protocol

### Before ANY connection attempt:

```bash
# 1. Kill all dotnet processes
taskkill //F //IM dotnet.exe 2>/dev/null || true

# 2. Check for zombie sockets on 4001
netstat -ano | findstr :4001 | findstr CLOSE_WAIT

# 3. If CLOSE_WAIT sockets exist, Gateway MUST restart
#    - They will NOT clear automatically
#    - Each one blocks a connection slot
```

### Clean Connection Procedure (Controlled):

```bash
# Step 1: Verify no processes running
taskkill //F //IM dotnet.exe 2>/dev/null || true
if (netstat -ano | findstr :4001 | findstr CLOSE_WAIT) {
    echo "❌ CLOSE_WAIT sockets detected - restart Gateway first"
    exit 1
}

# Step 2: Verify Gateway is clean and listening
netstat -ano | findstr :4001
# Expected:
# TCP    0.0.0.0:4001           0.0.0.0:0              LISTENING       <PID>
# NO CLOSE_WAIT entries

# Step 3: Wait 60s after Gateway UI shows "connected"
sleep 60

# Step 4: Single connection attempt with timeout
timeout 30 dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj

# Step 5: If hangs/fails, immediately kill and check socket state
taskkill //F //IM dotnet.exe
netstat -ano | findstr :4001 | findstr CLOSE_WAIT
# If CLOSE_WAIT appears: This attempt created a zombie - Gateway must restart
```

## Connection Success Pattern (From Gateway Logs 15:39:19)

**What worked:**
1. Fresh Gateway restart (no CLOSE_WAIT sockets)
2. Wait 60+ seconds after UI shows connected
3. Single connection attempt with ClientId 10
4. No concurrent/zombie processes

**Result:**
- connectAck callback triggered
- nextValidId received
- 24 positions loaded for account U21146542
- 7 active orders retrieved

## Connection Failure Pattern

**What failed:**
1. Multiple rapid connection attempts
2. Background processes not properly terminated
3. CLOSE_WAIT sockets accumulating
4. Connecting before Gateway API fully initialized

**Result:**
- eConnect() blocks indefinitely
- No callbacks triggered (connectAck, nextValidId, error)
- TCP socket opens but no handshake bytes
- Gateway silently drops connection

## Diagnostic Commands

```bash
# Check Gateway process
tasklist | findstr ibgateway

# Check all port 4001 connections
netstat -ano | findstr :4001

# Check for CLOSE_WAIT zombies specifically
netstat -ano | findstr :4001 | findstr CLOSE_WAIT

# Check for running dotnet processes
tasklist | findstr dotnet

# Count zombie sockets (must be 0)
netstat -ano | findstr :4001 | findstr CLOSE_WAIT | wc -l
```

## Pre-Flight Checklist (MUST verify before each attempt)

- [ ] All dotnet.exe processes killed
- [ ] Zero CLOSE_WAIT sockets on port 4001
- [ ] Gateway UI shows "connected" for 60+ seconds
- [ ] ClientId 10 in secrets.json
- [ ] No background bash processes running AutoRevOption

**If any checklist item fails: DO NOT ATTEMPT CONNECTION - diagnose first**

## Lessons Learned

1. **We control zombie processes** - every background bash/dotnet process must be tracked and terminated
2. **CLOSE_WAIT is poison** - one zombie socket can block all future connections until Gateway restarts
3. **Gateway restart clears state** - CLOSE_WAIT sockets only clear on Gateway restart, not timeout
4. **Connection attempts are NOT idempotent** - each failed attempt leaves pollution
5. **Pre-flight checks are mandatory** - never attempt connection without verifying clean state

## Next Steps

Create automated script that:
1. Validates clean state (kills processes, checks sockets)
2. Fails fast if CLOSE_WAIT detected (instructs user to restart Gateway)
3. Implements single controlled connection attempt
4. Properly terminates on timeout with socket state verification
