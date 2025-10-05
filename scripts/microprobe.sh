#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/AutoRevOption.MicroProbe"
dotnet build -c Release
dotnet run -c Release -- --host=127.0.0.1 --port="${1:-4001}" --id="${2:-10}" --try-ssl
