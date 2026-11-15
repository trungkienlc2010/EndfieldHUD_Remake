using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace EndfieldHud.Modules;

/// <summary>
/// Đọc phiên phát nhạc/video hiện tại qua System Media Transport Controls (SMTC) —
/// API công khai của Windows, hoạt động với Spotify, YouTube Music, trình duyệt, v.v.
/// </summary>
public partial class NowPlayingPage : UserControl
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;

    public NowPlayingPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        if (_manager != null) return;
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        }
        catch
        {
            TitleText.Text = "Không truy cập được SMTC";
            return;
        }

        _manager.CurrentSessionChanged += (_, _) => Dispatcher.InvokeAsync(() => { AttachSession(); });
        AttachSession();
    }

    private void AttachSession()
    {
        if (_session != null)
        {
            _session.MediaPropertiesChanged -= OnSessionChanged;
            _session.PlaybackInfoChanged -= OnSessionChanged;
        }

        _session = _manager?.GetCurrentSession();

        if (_session != null)
        {
            _session.MediaPropertiesChanged += OnSessionChanged;
            _session.PlaybackInfoChanged += OnSessionChanged;
        }

        _ = UpdateAsync();
    }

    // Sự kiện SMTC đến từ luồng nền -> đẩy về UI thread
    private void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object? args) =>
        Dispatcher.InvokeAsync(() => { _ = UpdateAsync(); });

    private async Task UpdateAsync()
    {
        var session = _session;
        if (session == null)
        {
            TitleText.Text = "Chưa có nội dung đang phát";
            ArtistText.Text = "";
            SourceText.Text = "";
            Art.Source = null;
            PlayButton.Content = "\uE768";
            return;
        }

        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            TitleText.Text = string.IsNullOrWhiteSpace(props.Title) ? "(không rõ tiêu đề)" : props.Title;
            ArtistText.Text = props.Artist;
            SourceText.Text = session.SourceAppUserModelId;
            Art.Source = await LoadThumbnailAsync(props.Thumbnail);

            var playing = session.GetPlaybackInfo().PlaybackStatus ==
                          GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            PlayButton.Content = playing ? "\uE769" : "\uE768";
        }
        catch { /* phiên vừa đóng */ }
    }

    private static async Task<BitmapImage?> LoadThumbnailAsync(IRandomAccessStreamReference? reference)
    {
        if (reference == null) return null;
        try
        {
            using var random = await reference.OpenReadAsync();
            using var stream = random.AsStreamForRead();
            var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            memory.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memory;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null) await _session.TryTogglePlayPauseAsync();
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null) await _session.TrySkipNextAsync();
    }

    private async void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null) await _session.TrySkipPreviousAsync();
    }
}
