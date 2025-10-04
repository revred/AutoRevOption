@echo off
REM Build AutoRevOption solution

echo Building AutoRevOption...
cd /d "%~dp0.."
dotnet build AutoRevOption.sln -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build succeeded!
    echo.
    echo To run Monitor: cd AutoRevOption.Monitor ^&^& dotnet run
    echo To run Minimal: cd AutoRevOption.Minimal ^&^& dotnet run
    echo To run tests:   dotnet test
) else (
    echo.
    echo ❌ Build failed with errors
    exit /b %ERRORLEVEL%
)
