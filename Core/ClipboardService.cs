using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EndfieldHud.Core;

public sealed record ClipEntry(string Text, DateTime Time)
{
    public string TimeText => Time.ToString("HH:mm:ss");

    public string Preview
    {
        get
        {
            var s = Text.Replace("\r", "").Replace("\n", " ⏎ ");
            return s.Length > 140 ? s[..140] + "…" : s;
        }
    }
}

/// <summary>
/// Lịch sử clipboard trong bộ nhớ (xoá khi thoát). Dùng AddClipboardFormatListener
/// thay vì polling, và bỏ qua nội dung mà trình quản lý mật khẩu đánh dấu "đừng theo dõi".
/// </summary>
public sealed class ClipboardService : IDisposable
{
    private const int MaxItems = 50;
    private HwndSource? _source;

    public ObservableCollection<ClipEntry> History { get; } = new();
    public bool Paused { get; set; }

    public void Start()
    {
        var p = new HwndSourceParameters("EndfieldHud.Clipboard")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
        NativeMethods.AddClipboardFormatListener(_source.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            handled = true;
            _ = CaptureAsync();
        }
        return IntPtr.Zero;
    }

    private async Task CaptureAsync()
    {
        if (Paused) return;

        // Clipboard có thể đang bị app khác khoá -> thử lại vài lần
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                // Chuẩn của Windows cho password manager (1Password, Bitwarden, KeePass...)
                if (Clipboard.ContainsData("ExcludeClipboardContentFromMonitorProcessing")) return;

                string? text = null;
                if (Clipboard.ContainsText())
                {
                    text = Clipboard.GetText();
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    text = string.Join(Environment.NewLine, Clipboard.GetFileDropList().Cast<string>());
                }

                if (!string.IsNullOrWhiteSpace(text)) Add(text);
                return;
            }
            catch (COMException)
            {
                await Task.Delay(80);
            }
        }
    }

    private void Add(string text)
    {
        var existing = History.FirstOrDefault(h => h.Text == text);
        if (existing != null) History.Remove(existing);

        History.Insert(0, new ClipEntry(text, DateTime.Now));
        while (History.Count > MaxItems) History.RemoveAt(History.Count - 1);
    }

    public void Dispose()
    {
        if (_source == null) return;
        NativeMethods.RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }
}
