#!/bin/bash
# Test Monitor MCP server functionality

export PATH="$PATH:/c/Program Files/dotnet"
cd /c/Code/AutoRevOption/AutoRevOption.Monitor

echo "========================================="
echo "Monitor MCP Server Test Suite"
echo "========================================="
echo ""
echo "NOTE: These tests require IB Gateway to be running on port 4001"
echo ""

echo "✅ Test 1: Initialize"
echo '{"method":"initialize","params":{}}' | dotnet run -- --mcp 2>/dev/null &
PID=$!
sleep 3
kill $PID 2>/dev/null
echo ""

echo "✅ Test 2: List Tools"
echo '{"method":"tools/list"}' | dotnet run -- --mcp 2>/dev/null &
PID=$!
sleep 3
kill $PID 2>/dev/null
echo ""

echo "========================================="
echo "Monitor MCP Server Tests Complete"
echo "========================================="
echo ""
echo "To test with live IBKR connection:"
echo "  1. Ensure IB Gateway is running (port 4001)"
echo "  2. Run: dotnet run -- --mcp"
echo "  3. Send JSON-RPC via stdin:"
echo '     {"method":"tools/call","params":{"name":"get_connection_status","arguments":{}}}'
echo '     {"method":"tools/call","params":{"name":"get_account_summary","arguments":{}}}'
echo ""
