# ABA-Ranked-Anti-lag

🇬🇧 [View in English](README.md)

> Công cụ Windows dành cho người chơi **ABA (Anime Battle Arena)** — tự động chặn server EU/JP/lag trong Ranked, giữ lobby theo khu vực.

Viết bằng C# WPF. Không AutoHotkey, không script ngoài. Chỉ một file `.exe`.

---

## Mục lục

- [Cách hoạt động](#cách-hoạt-động)
- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Thiết lập lần đầu](#thiết-lập-lần-đầu)
- [Cách dùng](#cách-dùng)
- [Preview window & detect points](#preview-window--detect-points)
- [Xuất IP từ firewall rule](#xuất-ip-từ-firewall-rule)
- [Tính năng](#tính-năng)
- [Vị trí file](#vị-trí-file)
- [Build từ source](#build-từ-source)
- [FAQ](#faq)

---

## Cách hoạt động

Khi Roblox load vào Ranked match, có một khoảnh khắc **màn hình đen ngắn** giữa loading screen và game thật sự. Tool này theo dõi màn hình đen đó bằng cách lấy màu pixel tại các điểm cố định trên cửa sổ Roblox theo thời gian thực.

Ngay khi phát hiện màn hình đen, tool **bật rule chặn outbound của Windows Firewall** chứa dải IP của server bạn muốn tránh. Điều này đẩy bạn về lobby trước khi match kết nối hoàn toàn — không cần Alt-F4 hay tắt game.

Khi đã về lobby, nhấn **Reset Trigger** để tắt rule firewall và chuẩn bị cho lần queue tiếp theo.

---

## Yêu cầu

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| Runtime | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Quyền | Administrator (UAC tự động hiện khi khởi động) |

---

## Cài đặt

1. Cài [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone hoặc tải repository này về.
3. Double-click file `build.bat` ở thư mục gốc — nó sẽ tự compile và xuất `RegionBlocker.exe` vào `bin\publish\`.
4. Chạy file `.exe` đó với quyền Administrator.

---

## Thiết lập lần đầu

![Giao diện chính của Region Blocker](img/MainUI.jpg)

### IP mặc định — chặn tất cả trừ Singapore & Hong Kong

Tool được đóng gói sẵn danh sách IP mặc định gồm các dải server EU, JP và các khu vực lag cao. **Danh sách này chặn tất cả các server không phải Singapore / Hong Kong**, giúp bạn chỉ kết nối vào server gần nhất ngay từ đầu.

Khi mở app lần đầu, danh sách IP đã có sẵn trong ô **IP / CIDR BLOCK LIST**. Bạn chỉ cần:

### 1. Apply danh sách IP vào firewall rule

Nhấn **>> APPLY TO RULE**.

Tool sẽ tự tạo một Windows Firewall rule tên `BlockIP` ở trạng thái **DISABLED**. Monitor sẽ tự bật rule này khi phát hiện màn hình đen.

> Bạn chỉ cần làm bước này **một lần duy nhất**, hoặc mỗi khi thay đổi danh sách IP.

### 2. Thêm / chỉnh sửa IP (nếu cần)

Nếu muốn tùy chỉnh:

- Gõ IP hoặc dải CIDR vào ô nhập liệu phía dưới (ví dụ: `128.116.1.0/24`) rồi nhấn **+ ADD**, hoặc
- Nhấn **^ IMPORT TXT** để nạp file `.txt` với mỗi dòng là một IP/CIDR.

Sau khi chỉnh sửa, nhấn **>> APPLY TO RULE** lại để cập nhật rule.

### 3. Cấu hình detect points (tuỳ chọn)

Nhấn **⊞ PREVIEW** để mở cửa sổ preview. Khi Roblox đang mở, bạn sẽ thấy thumbnail live của cửa sổ game với các marker màu cyan hiển thị vị trí tool đang lấy mẫu pixel.

Nếu các vị trí mặc định không nằm đúng vùng đen trong loading screen, kéo marker đến vị trí khác rồi nhấn **Apply**.

---

## Cách dùng

![Trạng thái khi trigger đã kích hoạt](img/TriggeredState.jpg)

### Workflow một session thông thường

```
Nhấn Start Monitor  →  Queue Ranked  →  Phát hiện màn đen  →  Firewall tự bật
        ↓                                                              ↓
    Đang chơi...                                          Bị đá về lobby
                                                                  ↓
                                                       Nhấn Reset Trigger
                                                      (tắt rule firewall)
                                                                  ↓
                                                           Queue lại
```

### Từng bước

1. Mở app, kiểm tra **RULE STATUS** hiển thị `DISABLED` (màu xanh lá). Nếu hiện `NO RULE`, nhấn **>> APPLY TO RULE** trước.
2. Nhấn **> START MONITOR**.
3. Mở Roblox và queue Ranked match.
4. Monitor phát hiện màn hình đen → tự bật rule chặn → bạn bị đá về lobby.
5. Nút **Reset Trigger** chuyển màu **cam** — báo hiệu trigger vừa kích hoạt.
6. Nhấn **⚠ RESET TRIGGER** để tắt rule firewall và reset. Nút về màu bình thường, bạn sẵn sàng queue lại.

### Điều khiển firewall thủ công

- **[ON] ENABLE BLOCK** — bật rule ngay lập tức.
- **[OFF] DISABLE BLOCK** — tắt rule ngay lập tức.
- **<< REFRESH** — đọc lại trạng thái rule hiện tại từ Windows.

---

## Preview window & detect points

![Preview window với detect points](img/PreviewWindow.jpg)

Mở cửa sổ preview bằng **⊞ PREVIEW** khi Roblox đang chạy.

| Thao tác | Chức năng |
|---|---|
| Kéo marker | Di chuyển detect point |
| Click vào ảnh | Đặt detect point mới tại vị trí đó |
| Sửa Rel X% / Rel Y% | Đặt vị trí chính xác theo phần trăm |
| + Add point | Thêm điểm mới ở giữa |
| - Remove last | Xoá điểm cuối cùng |
| Reset defaults | Khôi phục layout 5 điểm mặc định |
| Apply | Lưu vị trí hiện tại và áp dụng cho monitor |

**Color swatch** — hiển thị màu pixel đang lấy mẫu live cho từng điểm.  
**● / ○** — chấm đỏ = điểm đó đang detect đen; xanh = bình thường.  
**Black if ≥ N** — slider điều chỉnh số điểm phải detect đen trước khi trigger bắn (mặc định: 4/5).

> Vị trí detect point được lưu tự động và khôi phục khi mở lại app.

---

## Xuất IP từ firewall rule

Nếu bạn muốn **lấy danh sách IP đang có trong rule `BlockIP`** ra file `.txt` để chỉnh sửa rồi import lại, chạy lệnh PowerShell sau (với quyền Admin):

> **Lưu ý:** Các lệnh dưới dùng `"BlockIP"` làm tên rule — đây là tên mặc định tool tự tạo. Nếu bạn đã đổi tên rule, hãy thay `"BlockIP"` trong lệnh bằng tên thật của rule đó. Bạn có thể kiểm tra tên chính xác trong Windows Defender Firewall → Outbound Rules.

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

Để xuất thẳng ra file `.txt` trên Desktop:

```powershell
$rule = Get-NetFirewallRule -DisplayName "BlockIP" -ErrorAction SilentlyContinue
if ($rule) {
    $addresses = ($rule | Get-NetFirewallAddressFilter).RemoteAddress
    $addresses | Out-File -FilePath "$env:USERPROFILE\Desktop\block_ips.txt" -Encoding UTF8
    Write-Host "Đã xuất $($addresses.Count) IPs ra Desktop\block_ips.txt" -ForegroundColor Cyan
} else {
    Write-Host "Rule 'BlockIP' not found" -ForegroundColor Red
}
```

### Workflow chỉnh sửa IP

```
Xuất IP ra .txt  →  Chỉnh sửa file (thêm/xoá IP)  →  Import TXT vào app
                                                              ↓
                                              Xoá IP cũ trong danh sách nếu cần
                                                              ↓
                                                    >> APPLY TO RULE
```

1. Chạy lệnh trên để xuất IP ra file `block_ips.txt` trên Desktop.
2. Mở file, thêm hoặc xoá các IP/CIDR theo ý muốn.
3. Trong app, nhấn **X DEL** để xoá từng IP cũ, hoặc xoá hết rồi dùng **^ IMPORT TXT** để nạp file mới.
4. Nhấn **>> APPLY TO RULE** để cập nhật rule firewall.

---

## Tính năng

### Phát hiện màn hình đen
- Lấy mẫu pixel qua `PrintWindow` — hoạt động kể cả khi Roblox bị minimize
- Tối đa 5 điểm mẫu có thể kéo thả tùy chỉnh
- Ngưỡng đen điều chỉnh được (1–5 điểm)
- Polling thích ứng: 5 giây khi không tìm thấy Roblox, 1 giây khi đang chạy

### Quản lý Firewall
- Tự động bật rule `BlockIP` outbound khi phát hiện
- Bật / tắt thủ công bất cứ lúc nào
- Danh sách IP được xây dựng lại sạch mỗi lần bật để đảm bảo chính xác
- Hỗ trợ IP và CIDR mọi định dạng (`x.x.x.x`, `x.x.x.x/24`, `x.x.x.x/255.255.255.0`)

### Quản lý danh sách IP
- Thêm / xoá từng mục
- Import từ `.txt` (mỗi dòng một IP hoặc CIDR)
- Export danh sách hiện tại ra `.txt`
- Danh sách tự lưu mỗi khi thay đổi, tự nạp lại khi khởi động

### Trigger gate
- Chỉ bắn một lần mỗi session — không spam bật firewall
- Nút Reset Trigger chuyển **cam** khi trigger đang active
- Một click reset gate **và** tắt rule firewall đồng thời

### Hiển thị trạng thái
- Rule status: `ENABLED` / `DISABLED` / `NO RULE`
- Roblox state: `NOT OPEN` / `LOADING` / `RUNNING` / `MINIMIZED`
- Screen state: `NORMAL` / `BLACK`
- Bộ đếm trigger (tổng trong session)
- 5 chấm màu pixel live
- Log bar hiển thị hành động cuối với timestamp

### Hệ thống
- File `.exe` đơn, không cần dependency ngoài .NET 8 runtime
- Tự UAC elevation khi khởi động
- System tray icon với balloon tip cảnh báo khi Roblox bị minimize
- Toàn bộ config lưu tại `%AppData%\RegionBlocker\`

---

## Vị trí file

```
%AppData%\RegionBlocker\
    iplist.json     — danh sách IP / CIDR đã lưu
    points.json     — vị trí detect point đã lưu
    trigger.log     — lịch sử trigger với timestamp
```

---

## Build từ source

**Yêu cầu:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bat
:: Cách 1 — double-click
build.bat

:: Cách 2 — thủ công
dotnet publish -c Release -r win-x64 --no-self-contained -o bin\publish
```

Output: `bin\publish\RegionBlocker.exe`

---

## FAQ

**Q: Rule firewall không được tạo.**  
A: Đảm bảo app đang chạy với quyền Administrator. UAC sẽ tự hiện khi khởi động — nếu bạn đã bỏ qua, click chuột phải vào `.exe` và chọn *Run as administrator*.

**Q: Không phát hiện màn hình đen.**  
A: Mở Preview window trong lúc đang ở loading screen và kiểm tra xem có chấm nào chuyển đỏ không. Nếu không có, kéo các detect point đến vùng chắc chắn đen trong loading screen. Nhấn Apply khi xong.

**Q: Bị đá mỗi match dù không muốn chặn.**  
A: Đảm bảo rule firewall đang ở trạng thái **DISABLED** trước khi queue. Nhấn **[OFF] DISABLE BLOCK** hoặc **<< REFRESH** để kiểm tra. Chỉ bật monitor khi thực sự muốn chặn.

**Q: App crash khi khởi động.**  
A: Cài [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) rồi thử lại.

**Q: Thêm app vào Windows startup?**  
A: Tạo shortcut đến `RegionBlocker.exe` và đặt vào:
```
shell:startup
```
Dán đường dẫn đó vào hộp thoại Run của Windows (`Win + R`) để mở thư mục startup.

---

*Made for the ABA community by GoldSkin_Collector*
