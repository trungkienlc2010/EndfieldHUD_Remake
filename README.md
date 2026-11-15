# EndfieldHUD cho Windows 11

HUD kiểu *Arknights: Endfield* gọi bằng phím tắt toàn cục, viết lại cho Windows 11 (WPF, .NET 10),
tham khảo ý tưởng từ bản macOS [DDDuoDuo/EndfieldHUD](https://github.com/DDDuoDuo/EndfieldHUD) (MIT).
Dự án fan không chính thức; không dùng artwork/âm thanh của game.

> **Trạng thái:** bản khởi đầu (MVP). Mã được viết mà chưa biên dịch/chạy thử trên máy thật —
> nếu có lỗi build, hãy gửi nguyên văn thông báo lỗi để sửa.

## Yêu cầu

* Windows 11 (build 19041+ về mặt kỹ thuật, thiết kế cho Win 11)
* [.NET 10 SDK](https://dotnet.microsoft.com/download) (LTS)

## Chạy / đóng gói

```powershell
dotnet run -c Release
# đóng gói
dotnet publish -c Release -r win-x64 --self-contained false
```

## Cách dùng

| Thao tác | Kết quả |
|---|---|
| **Ctrl + Alt + `** | Mở/ẩn HUD (đổi được trong `%AppData%\EndfieldHUD\settings.json`) |
| **Esc** | Thoát chế độ soạn thảo, nhấn lần nữa để đóng HUD |
| Nhấp ra ngoài HUD | Tự đóng (tắt bằng `"CloseOnFocusLoss": false`) |
| Icon khay hệ thống | Mở HUD, bật "Chạy cùng Windows", Thoát |
| Nút nguồn đỏ | Thoát hẳn ứng dụng |

Tổ hợp phím trong `settings.json`: `HotkeyModifiers` = Alt(1) + Ctrl(2) + Shift(4) + Win(8);
`HotkeyKey` là mã phím ảo (backtick = 192 = 0xC0). Mặc định không dùng Ctrl+` vì trùng terminal của VS Code.

## Cấu trúc

```
App.xaml(.cs)          khởi động, khay hệ thống, single-instance
HudWindow.xaml(.cs)    cửa sổ overlay: animation, góc vát, đặt theo màn hình có chuột
Themes/Endfield.xaml   màu, style (nền than + vàng nhấn)
Core/
  HotkeyService        RegisterHotKey trên cửa sổ message-only
  ClipboardService     AddClipboardFormatListener, bỏ qua dữ liệu từ password manager
  NativeMethods        P/Invoke
  AppSettings          settings.json trong %AppData%
  NoteStore            notes.json, ghi nguyên tử
  ModuleCatalog        danh sách module hiển thị trên thanh bên
Modules/               mỗi module là một UserControl
  SystemPage           đồng hồ, CPU, RAM, ổ đĩa, pin
  NotesPage            ghi chú nhiều mục, tự lưu
  ClipboardPage        lịch sử clipboard (chỉ trong RAM)
  NowPlayingPage       nhạc đang phát qua SMTC (Spotify, YouTube Music, trình duyệt...)
```

Thêm module mới: tạo `UserControl` trong `Modules/`, thêm một dòng vào `ModuleCatalog.All`.

## So với bản macOS

| macOS (bản gốc) | Windows (bản này) | Trạng thái |
|---|---|---|
| Menu bar | Icon khay hệ thống (`NotifyIcon`) | Có |
| Ctrl+` toàn cục | `RegisterHotKey`, cấu hình được | Có |
| Now Playing (thư viện MediaRemoteAdapter, API riêng tư) | SMTC — API công khai, ổn định hơn | Có (chưa lời bài hát, chưa thanh tiến trình) |
| Clipboard Cache | `AddClipboardFormatListener` (không polling) + lọc password manager | Có (text/đường dẫn file; chưa ảnh) |
| Notes | JSON cục bộ, ghi nguyên tử | Có (chưa vẽ/checklist/ảnh) |
| Battery / Storage / Activity Monitor | `PowerStatus`, `DriveInfo`, `GetSystemTimes`, `GlobalMemoryStatusEx` | Có (chưa danh sách tiến trình) |
| Per-app volume (cần quyền Screen/System Audio) | WASAPI Audio Session (NAudio) — không cần quyền đặc biệt | Roadmap |
| Work Mode / Focus | Đồng hồ Pomodoro; tích hợp Do Not Disturb cần khảo sát thêm | Roadmap |
| Calendar + nhắc việc | Toast notification (Windows App SDK) | Roadmap |
| Temporary File Shelf | Kéo-thả file (`DragDrop`), chỉ lưu đường dẫn | Roadmap |
| E-Reader / Archive | WebView2 hoặc `Windows.Data.Pdf` | Roadmap |
| Liên kết tài khoản game + đếm ngược sanity | Windows Credential Manager (`PasswordVault`) | Roadmap — xem lưu ý dưới |
| Map, Minigame, Event Log, Personal ID | — | Roadmap, ưu tiên thấp |

## Lưu ý khi dùng cùng game

* Overlay hoạt động với game chạy **Windowed / Borderless Windowed**. Chế độ Fullscreen độc quyền (exclusive)
  sẽ che overlay — đó là giới hạn của Windows, không phải lỗi.
* Ứng dụng là **cửa sổ độc lập**, không tiêm DLL, không hook vào tiến trình game, không đọc bộ nhớ game.
  Đừng thay đổi điều đó: các hành vi kiểu này có thể bị anti-cheat coi là vi phạm.
* Tính năng liên kết tài khoản (nếu làm) sẽ dựa vào API không chính thức của nhà phát hành;
  có rủi ro vi phạm điều khoản hoặc bị đổi API bất ngờ. Chỉ nên dùng cho tài khoản của chính bạn
  và lưu token bằng Credential Manager, không lưu file thường.

## Việc kỹ thuật nên làm tiếp

1. Thanh cuộn tối màu (hiện đang dùng thanh cuộn mặc định, hơi chói trên nền tối).
2. Hiệu ứng nghiêng 3D như bản gốc: cần `Viewport3D`, hoặc chuyển sang WinUI 3 / Composition API.
3. Cửa sổ cài đặt (đổi phím tắt bằng giao diện thay vì sửa JSON).
4. Phát hiện game đang chạy toàn màn hình để tự tắt overlay hoặc cảnh báo.
5. Thanh tiến trình bài hát (tính từ `GetTimelineProperties()` + `LastUpdatedTime`).
