#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/AutoRevOption.Tests/uProbe"
dotnet build -c Release
dotnet run -c Release -- "${1:-127.0.0.1}" "${2:-4001}" "${3:-10}"
