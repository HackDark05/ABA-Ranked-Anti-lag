# ABA-Ranked-Anti-lag

> A Windows desktop tool for **ABA (Anime Battle Arena)** players to automatically block EU/JP/unwanted region servers during Ranked matches — reducing lag and keeping lobbies regional.

Built in C# WPF. No AutoHotkey, no external scripts. Just a single `.exe`.

---

## Table of contents

- [How it works](#how-it-works)
- [Requirements](#requirements)
- [Installation](#installation)
- [First-time setup](#first-time-setup)
- [How to use](#how-to-use)
- [Preview window & detect points](#preview-window--detect-points)
- [Features](#features)
- [File locations](#file-locations)
- [Build from source](#build-from-source)
- [FAQ](#faq)

---

## How it works

When Roblox loads into a Ranked match, there is a brief **black screen** between the loading screen and the actual game. This tool watches for that black screen by sampling a set of pixel points on the Roblox window in real time.

The moment it detects the black screen it **enables a Windows Firewall outbound block rule** containing the IP ranges of servers you want to avoid. This kicks you back to the lobby before the match fully connects — without needing to alt-F4 or close the game.

Once you are back in the lobby, press **Reset Trigger** to disable the firewall rule and prepare for the next queue.

---

## Requirements

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| Runtime | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Privileges | Administrator (UAC prompt is automatic on launch) |

---

## Installation

1. Download the latest release `.zip` from the [Releases](../../releases) page.
2. Extract the folder anywhere (e.g. `C:\Tools\ABA-Anti-lag\`).
3. Run `RegionBlocker.exe` — Windows will ask for administrator permission. Click **Yes**.

> **Note:** The `.NET 8 Desktop Runtime` must be installed. If you get an error on launch saying "you must install .NET", download it from the link above and re-run.

---

## First-time setup

### 1. Add IPs to the block list

The tool ships with a default list of EU/JP server IP ranges. If your list is empty:

- Type an IP or CIDR range in the input box at the bottom (e.g. `128.116.1.0/24`) and click **+ ADD**, or
- Click **^ IMPORT TXT** to load a `.txt` file with one IP/CIDR per line.

### 2. Apply the IP list to the firewall rule

Click **>> APPLY TO RULE**. This creates a Windows Firewall rule called `BlockIP` in a **disabled** state. The monitor will enable it automatically when a black screen is detected.

> You only need to do this once, or whenever you change the IP list.

### 3. Configure detect points (optional)

Click **⊞ PREVIEW** to open the preview window. With Roblox open, you will see a live thumbnail of the game window. The cyan markers show where the tool is sampling pixels.

If the default positions are not hitting black areas during the loading screen, drag the markers to better positions and click **Apply**.

---

## How to use

### Normal session workflow

```
Start Monitor  →  Queue for Ranked  →  Black screen detected  →  Firewall enables automatically
     ↓                                                                         ↓
 Playing...                                                        Kicked back to lobby
                                                                              ↓
                                                                    Press Reset Trigger
                                                                    (disables firewall rule)
                                                                              ↓
                                                                      Queue again
```

### Step by step

1. Open the app and make sure **RULE STATUS** shows `DISABLED` (green). If it shows `NO RULE`, click **>> APPLY TO RULE** first.
2. Click **> START MONITOR**.
3. Open Roblox and queue for a Ranked match.
4. The monitor will detect the black screen and enable the block rule automatically. You will be sent back to the lobby.
5. The **Reset Trigger** button will turn **orange** — this means a trigger has fired.
6. Click **⚠ RESET TRIGGER** to disable the firewall rule and reset the gate. The button returns to its normal color and you are ready to queue again.

### Manual firewall control

You can also control the firewall rule manually at any time:

- **[ON] ENABLE BLOCK** — enables the rule immediately.
- **[OFF] DISABLE BLOCK** — disables the rule immediately.
- **<< REFRESH** — re-reads the current rule status from Windows.

---

## Preview window & detect points

Open the preview window with **⊞ PREVIEW** while Roblox is running.

| Control | Action |
|---|---|
| Drag a marker | Move a detect point |
| Click on image | Place a new detect point at that position |
| Edit Rel X% / Rel Y% | Set an exact position by percentage |
| + Add point | Add a new point at the center |
| - Remove last | Remove the last point in the list |
| Reset defaults | Restore the built-in 5-point layout and save |
| Apply | Save current positions to disk and apply to the monitor |

**Color swatch** — shows the live sampled pixel color for each point.  
**● / ○** — red dot means that point is currently detecting black; green means it is not.  
**Black if ≥ N** — slider controls how many points must be black before a trigger fires (default: 4 out of 5).

> Detect point positions are saved to disk and restored automatically next launch.

---

## Features

### Black screen detection
- Pixel sampling via `PrintWindow` — works even when Roblox is minimized
- Up to 5 configurable sample points with drag-and-drop placement
- Adjustable black threshold (1–5 points)
- Adaptive polling: 5 s when Roblox is not found, 1 s when running

### Firewall management
- Automatically enables the `BlockIP` outbound rule on detection
- Manual enable / disable at any time
- IP list rebuilt cleanly on every enable to ensure accuracy
- Supports IP addresses and CIDR ranges in all formats (`x.x.x.x`, `x.x.x.x/24`, `x.x.x.x/255.255.255.0`)

### IP list manager
- Add / remove individual entries
- Import from `.txt` (one IP or CIDR per line)
- Export current list to `.txt`
- List auto-saved on every change, reloaded on launch

### Trigger gate
- Fires once per session — will not spam-enable the firewall
- Reset Trigger button turns **orange** when a trigger is active as a visual alert
- One click resets the gate **and** disables the firewall rule simultaneously

### Status display
- Rule status: `ENABLED` / `DISABLED` / `NO RULE`
- Roblox state: `NOT OPEN` / `LOADING` / `RUNNING` / `MINIMIZED`
- Screen state: `NORMAL` / `BLACK`
- Trigger counter (total for the session)
- 5 live pixel color dots
- Log bar showing the last action with timestamp

### System
- Single `.exe`, no dependencies beyond .NET 8 runtime
- Auto UAC elevation on launch
- System tray icon with balloon tip for minimized Roblox warning
- All config stored in `%AppData%\RegionBlocker\`

---

## File locations

```
%AppData%\RegionBlocker\
    iplist.json     — saved IP / CIDR block list
    points.json     — saved detect point positions
    trigger.log     — timestamped trigger history
```

---

## Build from source

**Requirements:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bat
:: Option 1 — double-click
build.bat

:: Option 2 — manual
dotnet publish -c Release -r win-x64 --no-self-contained -o bin\publish
```

Output: `bin\publish\RegionBlocker.exe`

---

## FAQ

**Q: The firewall rule is not being created.**  
A: Make sure the app is running as Administrator. The UAC prompt should appear automatically on launch — if you dismissed it, right-click the `.exe` and choose *Run as administrator*.

**Q: The black screen is not being detected.**  
A: Open the Preview window while in a loading screen and check if any pixel dots turn red. If not, drag the detect points to areas that are definitely black during loading. Click Apply when done.

**Q: I get kicked every match even when I don't want to block.**  
A: Make sure the firewall rule is **DISABLED** before queuing. Press **[OFF] DISABLE BLOCK** or **<< REFRESH** to check the current state. Only start the monitor when you actually want to block.

**Q: The app crashes on launch.**  
A: Install the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) and try again.

**Q: Can I add this to Windows startup?**  
A: Create a shortcut to `RegionBlocker.exe` and place it in:
```
shell:startup
```
Paste that path into the Windows Run dialog (`Win + R`) to open the startup folder.

---

*Made for the ABA community by GoldSkin_Collector*
