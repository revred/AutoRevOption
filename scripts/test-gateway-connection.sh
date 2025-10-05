#!/usr/bin/env bash
# test-gateway-connection.sh - Diagnose IB Gateway API connection issues

set -e

echo "╔════════════════════════════════════════════════════════════════════════════╗"
echo "║           IB Gateway API Connection Diagnostic Test                       ║"
echo "╚════════════════════════════════════════════════════════════════════════════╝"
echo ""

# Configuration
HOST="127.0.0.1"
PORT="4001"
TIMEOUT="3"

echo "📋 Test Configuration:"
echo "   Host: $HOST"
echo "   Port: $PORT"
echo "   Timeout: ${TIMEOUT}s"
echo ""

# Test 1: Ping localhost
echo "═══════════════════════════════════════════════════════════════════════════"
echo "Test 1: Ping Localhost"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""

if ping -n 2 $HOST > /dev/null 2>&1; then
    echo "✅ Localhost is reachable"
else
    echo "❌ Localhost ping failed (this is unusual)"
fi
echo ""

# Test 2: Check if port is listening
echo "═══════════════════════════════════════════════════════════════════════════"
echo "Test 2: Port Status"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""

echo "Checking if port $PORT is listening..."
NETSTAT_OUTPUT=$(netstat -an | grep ":$PORT" || echo "")

if [[ -z "$NETSTAT_OUTPUT" ]]; then
    echo "❌ Port $PORT is NOT listening"
    echo ""
    echo "💡 Next steps:"
    echo "   - Is IB Gateway running?"
    echo "   - Check Gateway API settings (should be port $PORT)"
    exit 1
else
    echo "✅ Port status:"
    echo "$NETSTAT_OUTPUT" | while read line; do echo "   $line"; done
fi
echo ""

# Test 3: TCP connection test
echo "═══════════════════════════════════════════════════════════════════════════"
echo "Test 3: TCP Socket Connection"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""

echo "Attempting TCP connection to $HOST:$PORT..."

# Use PowerShell for cross-platform TCP test
if powershell.exe -Command "
    \$client = New-Object System.Net.Sockets.TcpClient
    try {
        \$task = \$client.ConnectAsync('$HOST', $PORT)
        \$timeout = [System.TimeSpan]::FromSeconds($TIMEOUT)
        if (\$task.Wait(\$timeout)) {
            if (\$client.Connected) {
                Write-Host '✅ TCP connection successful'
                \$client.Close()
                exit 0
            } else {
                Write-Host '❌ Connection failed'
                exit 1
            }
        } else {
            Write-Host '❌ Connection timeout after ${TIMEOUT}s'
            exit 1
        }
    } catch {
        Write-Host \"❌ Exception: \$_\"
        exit 1
    } finally {
        \$client.Close()
    }
" ; then
    echo ""
    echo "✅ TCP layer is working"
else
    echo ""
    echo "❌ TCP connection failed"
    exit 1
fi
echo ""

# Test 4: TWS API handshake test
echo "═══════════════════════════════════════════════════════════════════════════"
echo "Test 4: TWS API Protocol Handshake"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""

echo "Sending TWS API handshake and waiting for response..."
echo ""

# PowerShell script to test API handshake
powershell.exe -Command "
    \$targetHost = '$HOST'
    \$port = $PORT
    \$timeout = $TIMEOUT * 1000

    try {
        # Connect
        \$client = New-Object System.Net.Sockets.TcpClient
        \$connectTask = \$client.ConnectAsync(\$targetHost, \$port)

        if (-not \$connectTask.Wait(\$timeout)) {
            Write-Host '❌ Connection timeout'
            exit 1
        }

        if (-not \$client.Connected) {
            Write-Host '❌ Failed to connect'
            exit 1
        }

        Write-Host '✅ TCP connected'
        Write-Host ''

        # Get stream
        \$stream = \$client.GetStream()
        \$stream.ReadTimeout = \$timeout
        \$stream.WriteTimeout = \$timeout

        # Send API handshake: 'API=9.72\0'
        Write-Host '📤 Sending: API=9.72'
        \$handshake = [System.Text.Encoding]::UTF8.GetBytes('API=9.72' + [char]0)
        \$stream.Write(\$handshake, 0, \$handshake.Length)
        \$stream.Flush()
        Write-Host \"   Sent: \$(\$handshake.Length) bytes\"
        Write-Host ''

        # Wait for response
        Write-Host '⏳ Waiting for Gateway response (${TIMEOUT}s timeout)...'
        Write-Host ''

        \$buffer = New-Object byte[] 4096
        \$bytesRead = 0

        try {
            # Try to read with timeout
            \$readTask = \$stream.ReadAsync(\$buffer, 0, \$buffer.Length)
            if (\$readTask.Wait(\$timeout)) {
                \$bytesRead = \$readTask.Result
            } else {
                Write-Host '❌ TIMEOUT - Gateway sent NO response'
                Write-Host ''
                Write-Host '💡 DIAGNOSIS:'
                Write-Host '   - TCP connection: ✅ SUCCESS'
                Write-Host '   - Gateway API response: ❌ TIMEOUT'
                Write-Host '   - Conclusion: Gateway is NOT processing API connections'
                Write-Host ''
                Write-Host '🔍 Possible causes:'
                Write-Host '   1. Master API client ID filtering connections'
                Write-Host '   2. Gateway API is disabled/locked'
                Write-Host '   3. Gateway needs restart'
                Write-Host '   4. Wrong API version handshake'
                Write-Host ''
                Write-Host '📋 Action items:'
                Write-Host '   1. Check Gateway Configuration → API → Settings'
                Write-Host '      - Master API client ID: Set to BLANK (not 10)'
                Write-Host '      - Socket Port: Verify it shows $PORT'
                Write-Host '      - Read-Only API: Verify UNCHECKED'
                Write-Host ''
                Write-Host '   2. Check Gateway logs:'
                Write-Host '      C:\Users\{YourUsername}\Jts\ibgateway.*.log'
                Write-Host '      Look for: API client connections, errors, rejections'
                Write-Host ''
                Write-Host '   3. Restart IB Gateway completely'
                Write-Host '      - Close Gateway'
                Write-Host '      - Wait 10 seconds'
                Write-Host '      - Restart and log in'
                Write-Host '      - Re-verify API settings'
                \$client.Close()
                exit 1
            }

            if (\$bytesRead -gt 0) {
                Write-Host \"✅ SUCCESS - Received \$bytesRead bytes from Gateway\"
                Write-Host ''
                Write-Host '📥 Response (hex):'
                Write-Host \"   \$([BitConverter]::ToString(\$buffer, 0, [Math]::Min(\$bytesRead, 100)))\"
                Write-Host ''
                Write-Host '📥 Response (ASCII):'
                \$response = [System.Text.Encoding]::ASCII.GetString(\$buffer, 0, \$bytesRead)
                \$response = \$response -replace '[\x00]', '[NULL]'
                Write-Host \"   \$response\"
                Write-Host ''
                Write-Host '✅ Gateway API is responding correctly!'
                Write-Host ''
                Write-Host '🎯 Next steps:'
                Write-Host '   - Run Monitor MCP service: dotnet run --project AutoRevOption.Monitor -- --mcp'
                Write-Host '   - Connection should now succeed'
                \$client.Close()
                exit 0
            } else {
                Write-Host '❌ Gateway closed connection (0 bytes)'
                Write-Host ''
                Write-Host '💡 Gateway rejected the connection'
                Write-Host '   - Check Master API client ID setting'
                Write-Host '   - Check Gateway logs for errors'
                \$client.Close()
                exit 1
            }
        } catch {
            Write-Host \"❌ Read error: \$_\"
            \$client.Close()
            exit 1
        }
    } catch {
        Write-Host \"❌ Exception: \$_\"
        exit 1
    }
"

if [ $? -eq 0 ]; then
    echo ""
    echo "╔════════════════════════════════════════════════════════════════════════════╗"
    echo "║                          ✅ ALL TESTS PASSED                               ║"
    echo "╚════════════════════════════════════════════════════════════════════════════╝"
else
    echo ""
    echo "╔════════════════════════════════════════════════════════════════════════════╗"
    echo "║                          ❌ TESTS FAILED                                   ║"
    echo "╚════════════════════════════════════════════════════════════════════════════╝"
    exit 1
fi
