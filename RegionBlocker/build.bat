@echo off
echo [RegionBlocker] Building...

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: dotnet CLI not found.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

dotnet publish RegionBlocker.csproj -c Release -r win-x64 --no-self-contained -o .\bin\publish

if %errorlevel%==0 (
    echo.
    echo Build OK.  Output: .\bin\publish\RegionBlocker.exe
    start "" ".\bin\publish"
) else (
    echo Build FAILED.
)
pause
