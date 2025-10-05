#!/usr/bin/env bash
set -euo pipefail

echo "=== Client Portal API Connection Test ==="
echo ""

# Check if Gateway is running
echo "1. Checking if Client Portal Gateway is running..."
if ! curl -k -s -f https://localhost:5000/v1/api/iserver/auth/status > /dev/null 2>&1; then
    echo "❌ Client Portal Gateway is not running on port 5000"
    echo ""
    echo "Install and start Gateway first:"
    echo "  1. Download: https://www.interactivebrokers.com/en/trading/ibgateway-latest.php"
    echo "  2. Extract to C:\\IBKR\\clientportal\\"
    echo "  3. Run: C:\\IBKR\\clientportal\\bin\\run.bat"
    echo "  4. Authenticate: https://localhost:5000"
    exit 1
fi

echo "✅ Gateway is running"
echo ""

# Test auth status
echo "2. Testing authentication status..."
AUTH_RESPONSE=$(curl -k -s -X POST https://localhost:5000/v1/api/iserver/auth/status)
echo "Response: $AUTH_RESPONSE"

if echo "$AUTH_RESPONSE" | grep -q '"authenticated":true'; then
    echo "✅ Authenticated"
else
    echo "⚠️  Not authenticated - open https://localhost:5000 and log in"
    exit 2
fi
echo ""

# Test accounts
echo "3. Testing portfolio accounts..."
ACCOUNTS_RESPONSE=$(curl -k -s https://localhost:5000/v1/api/portfolio/accounts)
echo "Accounts: $ACCOUNTS_RESPONSE"
echo ""

# Extract account ID for position test
ACCOUNT_ID=$(echo "$ACCOUNTS_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$ACCOUNT_ID" ]; then
    echo "❌ No account ID found"
    exit 3
fi

echo "Using account: $ACCOUNT_ID"
echo ""

# Test positions
echo "4. Testing positions retrieval..."
POSITIONS_RESPONSE=$(curl -k -s "https://localhost:5000/v1/api/portfolio/$ACCOUNT_ID/positions/0")
echo "Positions response: $POSITIONS_RESPONSE"
echo ""

# Count positions
POSITION_COUNT=$(echo "$POSITIONS_RESPONSE" | grep -o '"conid"' | wc -l)
echo "✅ Found $POSITION_COUNT positions"
echo ""

echo "=== All Tests Passed ==="
echo "CPAPI client is working correctly!"
