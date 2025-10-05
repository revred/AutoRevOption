#!/usr/bin/env bash
# Controlled Gateway connection with pre-flight validation
set -e
set -u

cd "$(dirname "$0")/.."

echo "🔍 Pre-Flight Validation"
echo "========================"

# Step 1: Kill any existing dotnet processes
echo "1️⃣  Terminating any existing dotnet processes..."
taskkill //F //IM dotnet.exe 2>/dev/null || echo "   ✅ No dotnet processes running"

# Step 2: Check for CLOSE_WAIT zombie sockets
echo ""
echo "2️⃣  Checking for CLOSE_WAIT zombie sockets on port 4001..."
ZOMBIE_COUNT=$(netstat -ano | grep ":4001" | grep -c "CLOSE_WAIT" || echo "0")

if [ "$ZOMBIE_COUNT" -gt 0 ]; then
    echo "   ❌ CRITICAL: Found $ZOMBIE_COUNT CLOSE_WAIT zombie socket(s)"
    echo ""
    echo "   Zombie sockets detected:"
    netstat -ano | grep ":4001" | grep "CLOSE_WAIT"
    echo ""
    echo "   🔧 REQUIRED ACTION: Restart IB Gateway to clear zombie sockets"
    echo "   These sockets will NOT clear automatically and block new connections"
    echo ""
    exit 1
else
    echo "   ✅ No zombie sockets detected"
fi

# Step 3: Verify Gateway is listening
echo ""
echo "3️⃣  Verifying Gateway is listening on port 4001..."
LISTENING=$(netstat -ano | grep ":4001" | grep -c "LISTENING" || echo "0")

if [ "$LISTENING" -eq 0 ]; then
    echo "   ❌ Gateway not listening on port 4001"
    echo "   Start IB Gateway and wait 60 seconds before retrying"
    exit 1
else
    echo "   ✅ Gateway is listening on port 4001"
fi

# Step 4: Display current socket state
echo ""
echo "4️⃣  Current port 4001 socket state:"
netstat -ano | grep ":4001" | grep "127.0.0.1" || echo "   (No localhost connections)"

# Step 5: Verify no background bash processes
echo ""
echo "5️⃣  Checking for background AutoRevOption processes..."
if tasklist | grep -q dotnet; then
    echo "   ⚠️  Dotnet processes still running after cleanup"
    tasklist | grep dotnet
    exit 1
else
    echo "   ✅ No background processes detected"
fi

echo ""
echo "✅ Pre-flight validation PASSED"
echo ""
echo "🚀 Attempting Gateway connection..."
echo "   ClientId: 10"
echo "   Port: 4001"
echo "   Timeout: 30 seconds"
echo ""

# Run with timeout and capture exit code
set +e
timeout 30 dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj
EXIT_CODE=$?
set -e

echo ""
echo "📊 Connection attempt completed with exit code: $EXIT_CODE"

# Clean up immediately
echo ""
echo "🧹 Post-connection cleanup..."
taskkill //F //IM dotnet.exe 2>/dev/null || echo "   ✅ No processes to clean"

# Verify socket state after attempt
echo ""
echo "🔍 Post-connection socket state check:"
ZOMBIE_COUNT_AFTER=$(netstat -ano | grep ":4001" | grep -c "CLOSE_WAIT" || echo "0")

if [ "$ZOMBIE_COUNT_AFTER" -gt 0 ]; then
    echo "   ❌ WARNING: Connection attempt created $ZOMBIE_COUNT_AFTER zombie socket(s)"
    echo ""
    netstat -ano | grep ":4001" | grep "CLOSE_WAIT"
    echo ""
    echo "   ⚠️  Gateway must be restarted before next connection attempt"
    exit 1
else
    echo "   ✅ No zombie sockets created"
fi

if [ $EXIT_CODE -eq 124 ]; then
    echo ""
    echo "⏱️  Connection timed out after 30 seconds"
    echo "   This indicates eConnect() blocking - check Gateway initialization state"
    exit 1
elif [ $EXIT_CODE -ne 0 ]; then
    echo ""
    echo "❌ Connection failed with exit code $EXIT_CODE"
    exit $EXIT_CODE
else
    echo ""
    echo "✅ Connection completed successfully"
    exit 0
fi
