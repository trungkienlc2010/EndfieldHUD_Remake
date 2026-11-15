using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EndfieldHud.Core;

namespace EndfieldHud.Modules;

public partial class NotesPage : UserControl
{
    private readonly ObservableCollection<NoteItem> _notes;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private bool _loading;

    public NotesPage()
    {
        InitializeComponent();

        _notes = new ObservableCollection<NoteItem>(NoteStore.Load());
        if (_notes.Count == 0) _notes.Add(new NoteItem());

        NoteList.ItemsSource = _notes;
        NoteList.SelectedIndex = 0;

        _saveTimer.Tick += (_, _) => Flush();

        // Lưu ngay khi HUD ẩn hoặc app thoát
        IsVisibleChanged += (_, e) => { if (e.NewValue is false) Flush(); };
        Dispatcher.ShutdownStarted += (_, _) => Flush();
    }

    private void Flush()
    {
        _saveTimer.Stop();
        NoteStore.Save(_notes);
    }

    private void NoteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loading = true;
        var note = NoteList.SelectedItem as NoteItem;
        Editor.Text = note?.Body ?? "";
        Editor.IsEnabled = note != null;
        _loading = false;
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || NoteList.SelectedItem is not NoteItem note) return;

        note.Body = Editor.Text;        // lưu tự động có trễ 700ms (debounce)
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var note = new NoteItem();
        _notes.Insert(0, note);
        NoteList.SelectedItem = note;
        Editor.Focus();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (NoteList.SelectedItem is not NoteItem note) return;

        var index = NoteList.SelectedIndex;
        _notes.Remove(note);
        if (_notes.Count == 0) _notes.Add(new NoteItem());
        NoteList.SelectedIndex = Math.Min(index, _notes.Count - 1);
        Flush();
    }
}
