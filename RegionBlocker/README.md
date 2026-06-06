# RegionBlocker -- C# WPF Edition

Standalone .exe -- no AHK, no PowerShell scripts required.

## Requirements
- Windows 10 / 11 x64
- .NET 8 Runtime (Desktop): https://dotnet.microsoft.com/download/dotnet/8.0
- Run as Administrator (UAC prompt is automatic)

## Build
1. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Double-click `build.bat`
3. Executable at `bin\publish\RegionBlocker.exe`

Or manually:
    dotnet publish -c Release -r win-x64 --no-self-contained -o bin\publish

## Features
- Roblox black-screen detection (pixel sampling, no AHK)
- Firewall rule enable / disable
- IP / CIDR list manager (add, remove, import, export)
- Apply IP list to firewall rule
- Trigger history log at %APPDATA%\RegionBlocker\trigger.log
- Auto UAC elevation on launch

## Config location
    %APPDATA%\RegionBlocker\
        iplist.json   -- saved IP list
        trigger.log   -- trigger history

## Startup with Windows (optional)
Create a shortcut to RegionBlocker.exe in:
    shell:startup
