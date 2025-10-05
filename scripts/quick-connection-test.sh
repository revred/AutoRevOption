#!/usr/bin/env bash
# Quick connection test after Gateway restart

echo "Testing IBKR Gateway connection..."
echo ""

# Wait for port to be ready
echo "Checking if port 4001 is listening..."
LISTENING=$(netstat -an | grep ":4001.*LISTENING" | head -1)

if [[ -z "$LISTENING" ]]; then
    echo "❌ Port 4001 is NOT listening - Gateway may not be fully started"
    exit 1
fi

echo "✅ Port 4001 is listening"
echo ""

# Run the full connection test
cd /c/Code/AutoRevOption
bash scripts/test-gateway-connection.sh
