using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using EndfieldHud.Core;
using Forms = System.Windows.Forms;

namespace EndfieldHud;

public partial class HudWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Dictionary<string, UserControl> _views = new();

    private System.Drawing.Rectangle _bounds;   // pixel vật lý trên màn hình đích
    private bool _closing;
    private bool _suppressDeactivate;

    public HudWindow()
    {
        InitializeComponent();

        foreach (var module in ModuleCatalog.All)
        {
            var button = new RadioButton
            {
                Style = (Style)FindResource("NavButton"),
                Content = module.Glyph,
                ToolTip = module.Title,
                Tag = module,
                GroupName = "hud-nav"
            };
            button.Checked += OnNavChecked;
            NavPanel.Children.Add(button);
        }
        ((RadioButton)NavPanel.Children[0]).IsChecked = true;

        _clock.Tick += (_, _) => UpdateClock();

        // Góc vát kiểu Endfield (trên-phải và dưới-trái)
        PanelRoot.SizeChanged += (_, e) => PanelRoot.Clip = BuildCutCorners(e.NewSize, 22);
    }

    // ---------- Hiện / ẩn ----------

    public void Toggle()
    {
        if (IsVisible)
        {
            if (!_closing) Dismiss();
        }
        else
        {
            Present();
        }
    }

    private void Present()
    {
        _closing = false;
        CalculateBounds();

        Show();
        ApplyBounds();
        Activate();
        NativeMethods.SetForegroundWindow(new WindowInteropHelper(this).Handle);

        UpdateClock();
        _clock.Start();
        PlayOpen();
    }

    private void Dismiss()
    {
        if (_closing || !IsVisible) return;
        _closing = true;

        var duration = TimeSpan.FromMilliseconds(150);
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fade = new DoubleAnimation(0, duration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            if (!_closing) return;
            _clock.Stop();
            Hide();
        };

        PanelRoot.BeginAnimation(OpacityProperty, fade);
        PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.97, duration) { EasingFunction = ease });
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.97, duration) { EasingFunction = ease });
        PanelShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(10, duration) { EasingFunction = ease });
    }

    private void PlayOpen()
    {
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        PanelRoot.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
        PanelShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, 0, duration) { EasingFunction = ease });
    }

    // ---------- Vị trí & kích thước (tính theo pixel, độc lập DPI) ----------

    private void CalculateBounds()
    {
        // Hiện HUD trên màn hình đang có con trỏ chuột
        var screen = Forms.Screen.FromPoint(Forms.Cursor.Position);
        var wa = screen.WorkingArea;

        var h = (int)(wa.Height * 0.78);
        var w = (int)Math.Min(wa.Width * 0.72, h * 1.75); // giới hạn tỉ lệ trên màn siêu rộng
        _bounds = new System.Drawing.Rectangle(
            wa.X + (wa.Width - w) / 2,
            wa.Y + (wa.Height - h) / 2,
            w, h);
    }

    private void ApplyBounds()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST,
            _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, NativeMethods.SWP_SHOWWINDOW);
    }

    // Chuyển giữa các màn hình khác DPI sẽ kích hoạt WM_DPICHANGED; áp lại kích thước mong muốn
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (IsVisible) Dispatcher.InvokeAsync(ApplyBounds);
    }

    private static Geometry BuildCutCorners(Size s, double c)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(0, 0), true, true);
            ctx.LineTo(new Point(s.Width - c, 0), true, false);
            ctx.LineTo(new Point(s.Width, c), true, false);
            ctx.LineTo(new Point(s.Width, s.Height), true, false);
            ctx.LineTo(new Point(c, s.Height), true, false);
            ctx.LineTo(new Point(0, s.Height - c), true, false);
        }
        g.Freeze();
        return g;
    }

    // ---------- Điều hướng ----------

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: HudModule module }) return;

        if (!_views.TryGetValue(module.Id, out var view))
            _views[module.Id] = view = module.Create();   // tạo lười, giữ lại để không mất trạng thái

        Host.Content = view;
        TitleText.Text = module.Title.ToUpperInvariant();
    }

    private void UpdateClock() => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");

    // ---------- Phím & focus ----------

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;

        // Esc lần 1: thoát chế độ soạn thảo; Esc lần 2: đóng HUD
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            Keyboard.ClearFocus();
            Focus();
            return;
        }
        Dismiss();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_suppressDeactivate || _closing || !IsVisible) return;
        if (App.Settings.CloseOnFocusLoss) Dismiss();
    }

    // Alt+F4 chỉ ẩn HUD; thoát thật qua nút nguồn / menu khay
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.IsExiting) return;
        e.Cancel = true;
        Dismiss();
    }

    private void Power_Click(object sender, RoutedEventArgs e)
    {
        _suppressDeactivate = true;
        var result = MessageBox.Show(this, "Thoát EndfieldHUD?", "EndfieldHUD",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        _suppressDeactivate = false;

        if (result == MessageBoxResult.Yes) App.Quit();
    }
}
