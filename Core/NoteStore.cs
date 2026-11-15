using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EndfieldHud.Core;

public sealed class NoteItem : INotifyPropertyChanged
{
    private string _body = "";
    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Body
    {
        get => _body;
        set
        {
            if (_body == value) return;
            _body = value;
            Updated = DateTime.Now;
            Raise(nameof(Body));
            Raise(nameof(Title));
            Raise(nameof(UpdatedText));
        }
    }

    // Khai báo SAU Body để khi đọc JSON, giá trị đã lưu ghi đè lên Now
    public DateTime Updated { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string Title
    {
        get
        {
            var first = _body.Split('\n', 2)[0].Trim();
            return first.Length == 0 ? "(ghi chú trống)" : first;
        }
    }

    [JsonIgnore]
    public string UpdatedText => Updated.ToString("dd/MM HH:mm");

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public static class NoteStore
{
    private static string FilePath => Path.Combine(AppSettings.DataDir, "notes.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<NoteItem> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<NoteItem>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    /// <summary>Ghi nguyên tử (file tạm rồi đổi tên) để không hỏng dữ liệu nếu tắt máy giữa chừng.</summary>
    public static void Save(IEnumerable<NoteItem> notes)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.DataDir);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(notes, Options));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch { }
    }
}
