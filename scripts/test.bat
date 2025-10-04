@echo off
REM Run tests for AutoRevOption

echo Running AutoRevOption tests...
cd /d "%~dp0.."
dotnet test --verbosity normal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ All tests passed!
) else (
    echo.
    echo ❌ Some tests failed
    exit /b %ERRORLEVEL%
)
