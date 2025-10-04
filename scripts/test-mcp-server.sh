#!/bin/bash
# Test MCP server functionality

export PATH="$PATH:/c/Program Files/dotnet"
cd /c/Code/AutoRevOption/AutoRevOption.Minimal

echo "========================================="
echo "AutoRevOption MCP Server Test Suite"
echo "========================================="
echo ""

echo "✅ Test 1: Initialize"
echo '{"method":"initialize","params":{}}' | dotnet run -- --mcp 2>/dev/null
echo ""

echo "✅ Test 2: List Tools"
echo '{"method":"tools/list"}' | dotnet run -- --mcp 2>/dev/null | head -1
echo ""

echo "✅ Test 3: Scan Candidates"
echo '{"method":"tools/call","params":{"name":"scan_candidates","arguments":{}}}' | dotnet run -- --mcp 2>/dev/null
echo ""

echo "✅ Test 4: Get Account Status"
echo '{"method":"tools/call","params":{"name":"get_account_status","arguments":{}}}' | dotnet run -- --mcp 2>/dev/null
echo ""

echo "✅ Test 5: Validate Candidate"
echo '{"method":"tools/call","params":{"name":"validate_candidate","arguments":{"candidateId":"PCS:SHOP:2025-10-11:22-21:8dc9"}}}' | dotnet run -- --mcp 2>/dev/null
echo ""

echo "========================================="
echo "MCP Server Tests Complete"
echo "========================================="
