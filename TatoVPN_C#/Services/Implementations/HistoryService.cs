using miVPN.Models;
using miVPN.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace miVPN.Services.Implementations;

public class HistoryService : IHistoryService
{
    private readonly ObservableCollection<ConnectionHistoryEntry> _entries = new();
    private static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "miVPN",
        "history.json");

    public event Action? OnHistoryChanged;

    public HistoryService()
    {
        try
        {
            Load();
        }
        catch
        {
        }
    }

    public IReadOnlyList<ConnectionHistoryEntry> GetEntries()
    {
        lock (_entries)
        {
            return _entries.Reverse().ToList().AsReadOnly();
        }
    }

    public void AddEntry(ConnectionHistoryEntry entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
            if (_entries.Count > 500)
                _entries.RemoveAt(0);
        }

        try
        {
            Save();
        }
        catch
        {
        }

        OnHistoryChanged?.Invoke();
    }

    public void Clear()
    {
        lock (_entries)
        {
            _entries.Clear();
        }

        try
        {
            Save();
        }
        catch
        {
        }

        OnHistoryChanged?.Invoke();
    }

    private void Load()
    {
        if (!File.Exists(StoragePath))
            return;

        string json = File.ReadAllText(StoragePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var list = JsonSerializer.Deserialize<List<ConnectionHistoryEntry>>(json);
        if (list == null)
            return;

        lock (_entries)
        {
            foreach (var item in list)
            {
                _entries.Add(item);
            }
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);

        List<ConnectionHistoryEntry> snapshot;
        lock (_entries)
        {
            snapshot = new List<ConnectionHistoryEntry>(_entries);
        }

        string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(StoragePath, json);
    }
}
