using System.Windows.Interop;

namespace EndfieldHud.Core;

/// <summary>
/// Phím tắt toàn cục qua RegisterHotKey trên một cửa sổ "message-only" (không hiển thị).
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0xE001;
    private readonly HwndSource _source;
    private bool _registered;

    public event Action? Pressed;

    public HotkeyService()
    {
        var p = new HwndSourceParameters("EndfieldHud.Hotkey")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
    }

    /// <param name="modifiers">Tổ hợp MOD_* (Ctrl=2, Alt=1, Shift=4, Win=8).</param>
    /// <param name="virtualKey">Mã phím ảo, ví dụ 0xC0 = dấu huyền/backtick (VK_OEM_3).</param>
    public bool Register(uint modifiers, uint virtualKey)
    {
        Unregister();
        _registered = NativeMethods.RegisterHotKey(
            _source.Handle, HotkeyId, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered) return;
        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
