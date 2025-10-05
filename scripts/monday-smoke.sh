#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATE="$(date +%F)"

echo "== Monday Smoke (bash) =="

echo "[1/4] Morning snapshot (paper)"
dotnet run --project "$ROOT/AutoRevOption.Monitor" -- --snapshot --mode PAPER

echo "[2/4] Selection (universe)"
dotnet run --project "$ROOT/AutoRevOption.Minimal" -- select --universe SOFI,APP,RKLB,META,AMD,GOOGL --dte 5-9

echo "[3/4] Stage all PASS TVCs"
dotnet run --project "$ROOT/AutoRevOption.Minimal" -- write --mode STAGE --input "$ROOT/logs/tvc/$DATE"/*.json

echo "[4/4] Summary"
echo "Snapshot: logs/snapshots/$DATE"
echo "TVCs:     logs/tvc/$DATE"
echo "Tickets:  logs/exec/cards"

echo "Done."
