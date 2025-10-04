#!/bin/bash
# Check which IB Gateway port is open

echo "Checking IB Gateway ports..."
echo ""

# Check port 7496 (Live Trading)
if netstat -an | grep -q "7496.*LISTENING\|7496.*LISTEN"; then
    echo "✅ Port 7496 is OPEN - Live Trading Gateway is running"
else
    echo "❌ Port 7496 is CLOSED - Live Trading Gateway is NOT running"
fi

# Check port 7497 (Paper Trading)
if netstat -an | grep -q "7497.*LISTENING\|7497.*LISTEN"; then
    echo "✅ Port 7497 is OPEN - Paper Trading Gateway is running"
else
    echo "❌ Port 7497 is CLOSED - Paper Trading Gateway is NOT running"
fi

echo ""
echo "Your secrets.json is configured for Port: 7496 (Live Trading)"
echo ""
