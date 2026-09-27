@echo off
cd /d "%~dp0"

echo ==============================
echo        ShieldGuard
echo ==============================
echo.
echo Building project...
echo.

dotnet build

if errorlevel 1 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo Build successful.
echo Starting ShieldGuard...
echo.

dotnet run