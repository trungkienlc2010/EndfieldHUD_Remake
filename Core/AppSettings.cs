using System.Text.Json;

namespace EndfieldHud.Core;

public sealed class AppSettings
{
    /// <summary>MOD_CONTROL(2) | MOD_ALT(1) = 3. Mặc định không dùng Ctrl+` vì trùng VS Code.</summary>
    public uint HotkeyModifiers { get; set; } = 0x0003;

    /// <summary>VK_OEM_3 = 0xC0 (phím dấu huyền/backtick).</summary>
    public uint HotkeyKey { get; set; } = 0xC0;

    public bool CloseOnFocusLoss { get; set; } = true;

    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EndfieldHUD");

    private static string FilePath => Path.Combine(DataDir, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded != null) return loaded;
            }
        }
        catch { /* file hỏng -> dùng mặc định */ }

        var s = new AppSettings();
        s.Save();
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
        }
        catch { }
    }
}
