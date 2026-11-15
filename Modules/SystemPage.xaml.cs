using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Controls;
using System.Windows.Threading;
using EndfieldHud.Core;
using Forms = System.Windows.Forms;

namespace EndfieldHud.Modules;

public partial class SystemPage : UserControl
{
    private const double Gib = 1024.0 * 1024 * 1024;
    private static readonly CultureInfo Vi = new("vi-VN");

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private ulong _prevIdle, _prevTotal;

    public SystemPage()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Refresh();

        // Chỉ chạy timer khi HUD đang hiển thị -> gần như 0% CPU khi ẩn
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true) { Refresh(); _timer.Start(); }
            else _timer.Stop();
        };
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        BigClock.Text = now.ToString("HH:mm:ss");
        DateText.Text = now.ToString("dddd, dd/MM/yyyy", Vi);

        // CPU
        var cpu = ReadCpu();
        CpuText.Text = $"{cpu:0}%";
        CpuBar.Value = cpu;

        // RAM
        var mem = new NativeMethods.MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>()
        };
        if (NativeMethods.GlobalMemoryStatusEx(ref mem))
        {
            var total = mem.ullTotalPhys / Gib;
            var used = total - mem.ullAvailPhys / Gib;
            RamText.Text = $"{used:0.0} / {total:0.0} GB";
            RamBar.Value = mem.dwMemoryLoad;
        }

        // Ổ đĩa hệ thống
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory)!;
            var drive = new DriveInfo(root);
            var used = (double)(drive.TotalSize - drive.AvailableFreeSpace);
            DiskText.Text = $"{used / Gib:0} / {drive.TotalSize / Gib:0} GB";
            DiskBar.Value = used / drive.TotalSize * 100;
        }
        catch { DiskText.Text = "—"; }

        // Pin
        var ps = Forms.SystemInformation.PowerStatus;
        var hasBattery = (ps.BatteryChargeStatus & Forms.BatteryChargeStatus.NoSystemBattery) == 0;
        if (hasBattery)
        {
            var pct = Math.Clamp(ps.BatteryLifePercent * 100f, 0f, 100f);
            BatteryText.Text = $"{pct:0}%";
            BatteryBar.Value = pct;
            BatteryLabel.Text = ps.PowerLineStatus == Forms.PowerLineStatus.Online ? "PIN · ĐANG CẮM SẠC" : "PIN";
        }
        else
        {
            BatteryText.Text = "Nguồn điện";
            BatteryBar.Value = 100;
            BatteryLabel.Text = "KHÔNG CÓ PIN";
        }
    }

    private double ReadCpu()
    {
        if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user)) return 0;

        var idleNow = ToUInt64(idle);
        var totalNow = ToUInt64(kernel) + ToUInt64(user); // kernel đã bao gồm thời gian idle

        var dIdle = idleNow - _prevIdle;
        var dTotal = totalNow - _prevTotal;
        _prevIdle = idleNow;
        _prevTotal = totalNow;

        return dTotal == 0 ? 0 : (1.0 - (double)dIdle / dTotal) * 100;
    }

    private static ulong ToUInt64(FILETIME f) =>
        ((ulong)(uint)f.dwHighDateTime << 32) | (uint)f.dwLowDateTime;
}
