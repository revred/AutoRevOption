#!/bin/bash
# Run AutoRevOption.Monitor

echo "Starting AutoRevOption.Monitor..."
cd "$(dirname "$0")/../AutoRevOption.Monitor"
dotnet run
