@echo off
setlocal
cd /d "%~dp0"

echo Building Terkoiz.Freecam for SP-Tushonka 5.0 BE...
dotnet build Terkoiz.Freecam.csproj -c Release
if errorlevel 1 (
    echo.
    echo BUILD FAILED.
    pause
    exit /b 1
)

echo.
echo BUILD SUCCEEDED.
echo Plugin copied to the configured Tarkov BepInEx plugins directory.
pause
