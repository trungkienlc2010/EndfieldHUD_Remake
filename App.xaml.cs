using System.Windows;
using EndfieldHud.Core;
using Forms = System.Windows.Forms;

namespace EndfieldHud;

public partial class App : Application
{
    private Mutex? _mutex;
    private bool _ownsMutex;
    private Forms.NotifyIcon? _tray;
    private HotkeyService? _hotkey;
    private HudWindow? _hud;

    public static AppSettings Settings { get; private set; } = new();
    public static ClipboardService ClipboardHistory { get; private set; } = null!;
    public static bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Chỉ cho chạy một bản
        _mutex = new Mutex(true, @"Local\EndfieldHUD.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        Settings = AppSettings.Load();

        ClipboardHistory = new ClipboardService();
        ClipboardHistory.Start();

        var hud = new HudWindow();
        _hud = hud;

        _hotkey = new HotkeyService();
        _hotkey.Pressed += () => hud.Toggle();

        BuildTray(hud);

        if (!_hotkey.Register(Settings.HotkeyModifiers, Settings.HotkeyKey))
        {
            _tray?.ShowBalloonTip(5000, "EndfieldHUD",
                "Không đăng ký được phím tắt (có thể đã bị app khác chiếm). Hãy đổi trong settings.json.",
                Forms.ToolTipIcon.Warning);
        }
    }

    private void BuildTray(HudWindow hud)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mở HUD", null, (_, _) => hud.Toggle());

        var auto = new Forms.ToolStripMenuItem("Chạy cùng Windows")
        {
            Checked = Autostart.IsEnabled,
            CheckOnClick = true
        };
        auto.CheckedChanged += (_, _) => Autostart.Set(auto.Checked);
        menu.Items.Add(auto);

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Thoát", null, (_, _) => Quit());

        _tray = new Forms.NotifyIcon
        {
            Icon = IconFactory.Create(),
            Text = "EndfieldHUD",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) hud.Toggle();
        };
    }

    public static void Quit()
    {
        IsExiting = true;
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        ClipboardHistory?.Dispose();

        if (_tray != null)
        {
            _tray.Visible = false; // tránh icon "ma" còn lại trên khay
            _tray.Dispose();
        }

        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        base.OnExit(e);
    }
}
