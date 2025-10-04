# AutoRevOption

**Purpose:** MCP interface and console toolkit to scan, validate, verify, and act on options opportunities per `In2025At100K.md` rules.

## Layout
```
AutoRevOption/
├─ AutoRevOption.Minimal/        # .NET console starter
├─ WorkPackages/                 # WP01–WP12 execution plan
├─ DOCS/                         # Specs (OptionsRadar.md copy, diagrams, notes)
├─ OptionsRadar.yaml             # Config knobs (risk, universe, strategies)
└─ .gitignore
```

## Quick start
```bash
cd AutoRevOption/AutoRevOption.Minimal
dotnet run
```
The demo uses a mock agent. Replace with real IBKR/ThetaData agents per WP03/WP05.