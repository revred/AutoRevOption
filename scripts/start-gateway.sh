#!/bin/bash
# Start IB Gateway and AutoRevOption.Monitor

echo "Starting IB Gateway..."

# Detect OS and start Gateway
if [[ "$OSTYPE" == "darwin"* ]]; then
    # Mac
    open "/Applications/IB Gateway.app"
    GATEWAY_PATH="/Applications/IB Gateway.app"
else
    # Linux
    ~/Jts/ibgateway/latest/ibgateway &
    GATEWAY_PATH="~/Jts/ibgateway/latest/ibgateway"
fi

echo "Waiting 60 seconds for Gateway to initialize..."
echo "Please log in to IB Gateway when it appears!"
sleep 60

echo ""
echo "Starting AutoRevOption.Monitor..."
cd "$(dirname "$0")/../AutoRevOption.Monitor"
dotnet run
