using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EndfieldHud.Core;

namespace EndfieldHud.Modules;

public partial class ClipboardPage : UserControl
{
    public ClipboardPage()
    {
        InitializeComponent();
        ClipList.ItemsSource = App.ClipboardHistory.History;
    }

    private void ClipList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ClipList.SelectedItem is not ClipEntry entry) return;
        try { Clipboard.SetText(entry.Text); }
        catch (COMException) { /* clipboard đang bị app khác giữ */ }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) =>
        App.ClipboardHistory.History.Clear();

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        var service = App.ClipboardHistory;
        service.Paused = !service.Paused;
        PauseButton.Content = service.Paused ? "TIẾP TỤC" : "TẠM DỪNG";
    }
}
