# AutoRevOption Scripts

Helper scripts for building, running, and testing AutoRevOption.

## Windows Scripts (.bat)

### Build
```bash
scripts\build.bat
```
Builds the entire solution in Release mode.

### Run Monitor
```bash
scripts\run-monitor.bat
```
Starts AutoRevOption.Monitor (read-only IBKR connection).

### Run Minimal
```bash
scripts\run-minimal.bat
```
Starts AutoRevOption.Minimal (demo console with rules engine).

### Run Tests
```bash
scripts\test.bat
```
Runs all unit tests.

### Start Gateway + Monitor
```bash
scripts\start-gateway.bat
```
Launches IB Gateway, waits 60s, then starts Monitor.

## Linux/Mac Scripts (.sh)

### Build
```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

### Run Monitor
```bash
chmod +x scripts/run-monitor.sh
./scripts/run-monitor.sh
```

### Run Minimal
```bash
chmod +x scripts/run-minimal.sh
./scripts/run-minimal.sh
```

### Run Tests
```bash
chmod +x scripts/test.sh
./scripts/test.sh
```

### Start Gateway + Monitor
```bash
chmod +x scripts/start-gateway.sh
./scripts/start-gateway.sh
```

## Quick Start

**Windows:**
1. Build: `scripts\build.bat`
2. Test: `scripts\test.bat`
3. Start: `scripts\start-gateway.bat`

**Linux/Mac:**
1. Build: `./scripts/build.sh`
2. Test: `./scripts/test.sh`
3. Start: `./scripts/start-gateway.sh`

## Prerequisites

- .NET 9 SDK installed
- IB Gateway installed and configured
- `secrets.json` configured with your credentials

## Configuration

Update `secrets.json` for your environment:
- Windows: `C:\IBKR\ibgateway\1040\ibgateway.exe`
- Mac: `/Applications/IB Gateway.app`
- Linux: `~/Jts/ibgateway/latest/ibgateway`
