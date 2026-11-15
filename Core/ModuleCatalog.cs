using System.Windows.Controls;
using EndfieldHud.Modules;

namespace EndfieldHud.Core;

/// <summary>Một "module" trên HUD: icon (Segoe Fluent Icons) + hàm tạo giao diện.</summary>
public sealed record HudModule(string Id, string Title, string Glyph, Func<UserControl> Create);

public static class ModuleCatalog
{
    // Thêm module mới = thêm một dòng ở đây + một UserControl trong thư mục Modules
    public static IReadOnlyList<HudModule> All { get; } = new List<HudModule>
    {
        new("system",     "Hệ thống",  "\uE9D9", () => new SystemPage()),
        new("notes",      "Ghi chú",   "\uE70F", () => new NotesPage()),
        new("clipboard",  "Clipboard", "\uE77F", () => new ClipboardPage()),
        new("nowplaying", "Đang phát", "\uE8D6", () => new NowPlayingPage()),
    };
}
