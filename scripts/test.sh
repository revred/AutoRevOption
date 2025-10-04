#!/bin/bash
# Run tests for AutoRevOption

echo "Running AutoRevOption tests..."
cd "$(dirname "$0")/.."
dotnet test --verbosity normal

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ All tests passed!"
else
    echo ""
    echo "❌ Some tests failed"
    exit 1
fi
