@echo off
REM Check which IB Gateway port is open

echo Checking IB Gateway ports...
echo.

netstat -an | findstr "7496" > nul
if %ERRORLEVEL% EQU 0 (
    echo ✅ Port 7496 is OPEN - Live Trading Gateway is running
) else (
    echo ❌ Port 7496 is CLOSED - Live Trading Gateway is NOT running
)

netstat -an | findstr "7497" > nul
if %ERRORLEVEL% EQU 0 (
    echo ✅ Port 7497 is OPEN - Paper Trading Gateway is running
) else (
    echo ❌ Port 7497 is CLOSED - Paper Trading Gateway is NOT running
)

echo.
echo Your secrets.json is configured for Port: 7496 (Live Trading)
echo.
pause
