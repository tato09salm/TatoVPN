using System.Text;
using System.Text.Json;
using miVPN.Models;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class ConfigService : IConfigService
{
    private static readonly string BaseFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "miVPN");
    public string ConfigurationsFolder { get; } = Path.Combine(BaseFolder, "configuraciones");
    private static readonly string LastUsedPath = Path.Combine(BaseFolder, "last_used.json");

    public event Action? OnConfigsChanged;

    public ConfigService()
    {
        try
        {
            Directory.CreateDirectory(ConfigurationsFolder);
        }
        catch { }
    }

    public IReadOnlyList<SavedConfiguration> GetAll()
    {
        var list = new List<SavedConfiguration>();
        try
        {
            if (!Directory.Exists(ConfigurationsFolder))
                return list.AsReadOnly();

            foreach (var file in Directory.GetFiles(ConfigurationsFolder, "*.json").OrderByDescending(f => File.GetCreationTime(f)))
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    var config = JsonSerializer.Deserialize<SavedConfiguration>(json);
                    if (config != null)
                    {
                        config.FileName = Path.GetFileName(file);
                        list.Add(config);
                    }
                }
                catch
                {
                }
            }
        }
        catch { }
        return list.AsReadOnly();
    }

    public SavedConfiguration? GetByName(string name)
    {
        return GetAll().FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool Save(string name, ConnectionSettings settings)
    {
        try
        {
            Directory.CreateDirectory(ConfigurationsFolder);

            string safeName = SanitizeFileName(name);
            if (string.IsNullOrWhiteSpace(safeName))
                return false;

            var existing = GetByName(name);
            string fileName = existing?.FileName ?? $"{safeName}_{Guid.NewGuid():N}.json";
            string filePath = Path.Combine(ConfigurationsFolder, fileName);

            var saved = new SavedConfiguration
            {
                Name = name,
                CreatedAt = existing?.CreatedAt ?? DateTime.Now,
                FileName = fileName,
                Settings = new ConnectionSettings
                {
                    SshHost = settings.SshHost,
                    SshPort = settings.SshPort,
                    Username = settings.Username,
                    Password = settings.Password,
                    SocksLocalIp = settings.SocksLocalIp,
                    SocksLocalPort = settings.SocksLocalPort,
                    UseSslTls = settings.UseSslTls,
                    TlsPort = settings.TlsPort,
                    TlsServerName = settings.TlsServerName,
                    TlsVersion = settings.TlsVersion,
                    EnableTunMode = settings.EnableTunMode
                }
            };

            string json = JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, Encoding.UTF8);

            OnConfigsChanged?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Delete(string name)
    {
        try
        {
            var cfg = GetByName(name);
            if (cfg == null) return false;

            string filePath = Path.Combine(ConfigurationsFolder, cfg.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            OnConfigsChanged?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ConnectionSettings? Apply(string name, out SavedConfiguration? config)
    {
        config = GetByName(name);
        if (config == null) return null;
        return new ConnectionSettings
        {
            SshHost = config.Settings.SshHost,
            SshPort = config.Settings.SshPort,
            Username = config.Settings.Username,
            Password = config.Settings.Password,
            SocksLocalIp = config.Settings.SocksLocalIp,
            SocksLocalPort = config.Settings.SocksLocalPort,
            UseSslTls = config.Settings.UseSslTls,
            TlsPort = config.Settings.TlsPort,
            TlsServerName = config.Settings.TlsServerName,
            TlsVersion = config.Settings.TlsVersion,
            EnableTunMode = config.Settings.EnableTunMode
        };
    }

    public void SaveLastUsed(ConnectionSettings settings)
    {
        try
        {
            Directory.CreateDirectory(BaseFolder);
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LastUsedPath, json, Encoding.UTF8);
        }
        catch { }
    }

    public ConnectionSettings? LoadLastUsed()
    {
        try
        {
            if (!File.Exists(LastUsedPath))
                return null;

            string json = File.ReadAllText(LastUsedPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<ConnectionSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (char c in name.Trim())
        {
            if (!invalid.Contains(c) && c != '.')
                sb.Append(c);
        }
        string result = sb.ToString().Trim();
        return result.Length > 60 ? result.Substring(0, 60) : result;
    }
}
