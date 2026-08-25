using miVPN.Models;

namespace miVPN.Services.Interfaces;

public class SavedConfiguration
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string FileName { get; set; } = string.Empty;
    public ConnectionSettings Settings { get; set; } = new ConnectionSettings();
    public string CreatedAtDisplay => CreatedAt.ToString("dd/MM/yyyy");
}

public interface IConfigService
{
    event Action? OnConfigsChanged;
    string ConfigurationsFolder { get; }

    IReadOnlyList<SavedConfiguration> GetAll();
    SavedConfiguration? GetByName(string name);

    bool Save(string name, ConnectionSettings settings);
    bool Delete(string name);
    ConnectionSettings? Apply(string name, out SavedConfiguration? config);

    void SaveLastUsed(ConnectionSettings settings);
    ConnectionSettings? LoadLastUsed();
}
