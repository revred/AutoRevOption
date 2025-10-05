#!/usr/bin/env bash
# scripts/get-positions.sh - Get current positions from IBKR Gateway

set -e  # Exit on error
set -u  # Exit on undefined variable

# Change to project root
cd "$(dirname "$0")/.."

echo "╔════════════════════════════════════════════════════════════════════════════╗"
echo "║              AutoRevOption - Get IBKR Positions                            ║"
echo "╚════════════════════════════════════════════════════════════════════════════╝"
echo ""

# Clean up any hanging dotnet processes
echo "🧹 Cleaning up any hanging processes..."
taskkill //F //IM dotnet.exe 2>/dev/null || true
echo ""

echo "📋 Starting Monitor (Interactive Mode)..."
echo "   When the menu appears, select option 2 (Get Positions)"
echo ""

# Run the interactive monitor
dotnet run --project AutoRevOption.Monitor/AutoRevOption.Monitor.csproj
