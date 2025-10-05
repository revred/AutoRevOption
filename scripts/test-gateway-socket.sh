#!/usr/bin/env bash
# scripts/test-gateway-socket.sh - Quick raw socket test for Gateway API

set -e
set -u

cd "$(dirname "$0")/.."

echo "Running raw socket diagnostic test..."
echo ""

dotnet test AutoRevOption.Tests/AutoRevOption.Tests.csproj \
    --filter "FullyQualifiedName~IbkrRawSocketTests.TestRawSocketConnection" \
    --logger "console;verbosity=detailed" \
    --no-build \
    -- RunConfiguration.TestSessionTimeout=10000
