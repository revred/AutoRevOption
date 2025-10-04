#!/bin/bash
# Build AutoRevOption solution

echo "Building AutoRevOption..."
cd "$(dirname "$0")/.."
dotnet build AutoRevOption.sln -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Build succeeded!"
    echo ""
    echo "To run Monitor: cd AutoRevOption.Monitor && dotnet run"
    echo "To run Minimal: cd AutoRevOption.Minimal && dotnet run"
    echo "To run tests:   dotnet test"
else
    echo ""
    echo "❌ Build failed with errors"
    exit 1
fi
