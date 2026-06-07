# ABA-Ranked-Anti-lag

🇻🇳 [Xem bản tiếng Việt](README.vi.md)

> A Windows desktop tool for **ABA (Anime Battle Arena)** players to automatically block EU/JP/unwanted region servers during Ranked matches — reducing lag and keeping lobbies regional.

---

## Table of contents

- [How it works](#how-it-works)
- [Requirements](#requirements)
- [Installation](#installation)
- [First-time setup](#first-time-setup)
- [How to use](#how-to-use)
- [Preview window & detect points](#preview-window--detect-points)
- [Export IPs from firewall rule](#export-ips-from-firewall-rule)
- [Features](#features)
- [File locations](#file-locations)
- [Build from source](#build-from-source)
- [FAQ](#faq)

---

## How it works

When Roblox loads into a Ranked match, there is a brief **black screen** between the loading screen and the actual game. This tool watches for that black screen by sampling a set of pixel points on the Roblox window in real time.

The moment it detects the black screen it **enables a Windows Firewall outbound block rule** containing the IP ranges of servers you want to avoid. This kicks you back to the lobby before the match fully connects — without needing to alt-F4 or close the game.

Once the firewall rule is active, the monitor will not re-trigger until you **disable the block rule** (either manually with **[OFF] DISABLE BLOCK**, or by stopping the monitor). As soon as the rule is disabled, the tool is ready to trigger again automatically on the next black screen.

---

## Requirements

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| Runtime | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Privileges | Administrator (UAC prompt is automatic on launch) |

---

## Installation

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone or download this repository.
3. Double-click `build.bat` in the root folder — it compiles and outputs `RegionBlocker.exe` to `bin\publish\`.
4. Run the `.exe` from that folder as Administrator.

---

## First-time setup

![Region Blocker main UI](img/MainUI.jpg)

### Default IP list — blocks everything except Singapore & Hong Kong

The tool ships with a built-in IP list covering EU, JP, and other high-latency server ranges. **This list blocks all regions except Singapore and Hong Kong**, so you only connect to the nearest servers right away.

When you open the app for the first time, the IP list is already populated in the **IP / CIDR BLOCK LIST** panel. All you need to do is:

### 1. Apply the IP list to the firewall rule

Click **>> APPLY TO RULE**.

The tool creates a Windows Firewall rule named `BlockIP` in a **disabled** state. The monitor will enable it automatically when a black screen is detected.

> You only need to do this **once**, or whenever you change the IP list.

### 2. Add / edit IPs (optional)

- Type an IP or CIDR range in the input box (e.g. `128.116.1.0/24`) and click **+ ADD**, or
- Click **^ IMPORT TXT** to load a `.txt` file with one IP/CIDR per line.

After editing, click **>> APPLY TO RULE** again to update the rule.

### 3. Configure detect points (optional)

Click **⊞ PREVIEW** to open the preview window. With Roblox open, you will see a live thumbnail of the game window. The cyan markers show where the tool samples pixels. If the defaults are not landing on black areas during loading, drag the markers and click **Apply**.

---

## How to use

![Region Blocker triggered state](img/TriggeredState.jpg)

### Normal session workflow

```
Start Monitor  →  Queue for Ranked  →  Black screen detected  →  Firewall ENABLED automatically
                                                                            ↓
                                                               Kicked back to lobby
                                                                            ↓
                                                           Press [OFF] DISABLE BLOCK
                                                                            ↓
                                                                     Queue again
                                                              (monitor re-arms itself)
```

### Step by step

1. Open the app and make sure **RULE STATUS** shows `DISABLED` (green). If it shows `NO RULE`, click **>> APPLY TO RULE** first.
2. Click **> START MONITOR**.
3. Open Roblox and queue for a Ranked match.
4. The monitor detects the black screen and enables the block rule automatically. You will be sent back to the lobby.

   > A **system tray notification** will appear confirming the trigger, which block number it was, and how many IPs are now blocked.

   ![Trigger notification balloon](img/TriggerNotification.jpg)

5. Once back in the lobby, click **[OFF] DISABLE BLOCK** to turn off the firewall rule. The monitor re-arms automatically — no extra steps needed.
6. Queue again.

### Manual firewall control

- **[ON] ENABLE BLOCK** — enables the rule immediately.
- **[OFF] DISABLE BLOCK** — disables the rule immediately and re-arms the monitor.
- **<< REFRESH** — re-reads the current rule status from Windows.

---

## Preview window & detect points

![Preview window with detect points](img/PreviewWindow.jpg)

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

> Detect point positions are saved to disk and restored automatically on next launch.

---

## Export IPs from firewall rule

If you want to pull the current IP list out of the `BlockIP` rule into a `.txt` file, run the following PowerShell command as Administrator:

> **Note:** These commands use `"BlockIP"` as the rule name — the default name the tool creates. If you renamed your rule, replace `"BlockIP"` with your actual rule name. Check the name under Windows Defender Firewall → Outbound Rules.

```powershell
$rule = Get-NetFirewallRule -DisplayName "BlockIP" -ErrorAction SilentlyContinue
if ($rule) {
    $addresses = ($rule | Get-NetFirewallAddressFilter).RemoteAddress
    $addresses | ForEach-Object { Write-Host $_ }
    Write-Host "`nTotal: $($addresses.Count) IPs" -ForegroundColor Cyan
} else {
    Write-Host "Rule 'BlockIP' not found" -ForegroundColor Red
}
```

To export directly to a `.txt` file on your Desktop:

```powershell
$rule = Get-NetFirewallRule -DisplayName "BlockIP" -ErrorAction SilentlyContinue
if ($rule) {
    $addresses = ($rule | Get-NetFirewallAddressFilter).RemoteAddress
    $addresses | Out-File -FilePath "$env:USERPROFILE\Desktop\block_ips.txt" -Encoding UTF8
    Write-Host "Exported $($addresses.Count) IPs to Desktop\block_ips.txt" -ForegroundColor Cyan
} else {
    Write-Host "Rule 'BlockIP' not found" -ForegroundColor Red
}
```

### Edit workflow

```
Export IPs to .txt  →  Edit file (add / remove IPs)  →  Import TXT into app
                                                                ↓
                                                  Remove old IPs from the list if needed
                                                                ↓
                                                       >> APPLY TO RULE
```

---

## Features

### Black screen detection
- Pixel sampling via `PrintWindow`
- Up to 5 configurable sample points with drag-and-drop placement
- Adjustable black threshold (1–5 points)
- Adaptive polling: 3 s when Roblox is not found, 300 ms when running

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

### Auto re-arm trigger
- Trigger fires whenever a black screen is detected **and** the firewall rule is currently disabled
- No manual reset needed — disabling the block rule is all it takes to re-arm
- System tray balloon notification on every trigger with trigger count and IP count

### Minimized window warning
- If Roblox is minimized, detection is unavailable
- A tray notification will appear prompting you to restore the Roblox window

  ![Minimized warning notification](img/MinimizeWarning.jpg)

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
A: Make sure the app is running as Administrator. The UAC prompt appears automatically on launch — if you dismissed it, right-click the `.exe` and choose *Run as administrator*.

**Q: The black screen is not being detected.**  
A: Open the Preview window during a loading screen and check if any pixel dots turn red. If not, drag the detect points to areas that are definitely black during loading, then click Apply.

**Q: I get kicked every match even when I don't want to block.**  
A: Make sure the firewall rule is **DISABLED** before queuing. Press **[OFF] DISABLE BLOCK** or **<< REFRESH** to check the current state. Only start the monitor when you actually want to block.

**Q: The monitor triggered but I'm already in the match.**  
A: The detect points may be landing on areas that turn black mid-game (e.g. dark scenery). Open the Preview window and move the points to the center of the screen or areas that stay bright during normal gameplay.

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
