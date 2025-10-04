@echo off
REM Start IB Gateway and AutoRevOption.Monitor

echo Starting IB Gateway...
start "" "C:\IBKR\ibgateway\1040\ibgateway.exe"

echo Waiting 60 seconds for Gateway to initialize...
echo Please log in to IB Gateway when it appears!
timeout /t 60 /nobreak

echo.
echo Starting AutoRevOption.Monitor...
cd /d "%~dp0..\AutoRevOption.Monitor"
dotnet run
