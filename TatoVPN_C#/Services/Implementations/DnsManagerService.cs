using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text.Json;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

/// <summary>
/// Implementación de blindaje DNS a bajo nivel mediante WMI (Win32_NetworkAdapterConfiguration).
/// Captura el estado DNS de los adaptadores físicos y los fuerza a 127.0.0.1 para evitar
/// que Windows envíe consultas DNS en paralelo a servidores del ISP.
/// </summary>
public class DnsManagerService : IDnsManagerService
{
    private readonly ILoggerService _logger;
    private static readonly ConcurrentDictionary<string, AdapterDnsBackup> _activeBackups = new();
    private static readonly string BackupFilePath = Path.Combine(Path.GetTempPath(), "tatovpn_dns_backup.json");
    private bool _isEnforced = false;

    public class AdapterDnsBackup
    {
        public string SettingId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[]? OriginalDnsServers { get; set; }
        public bool DhcpEnabled { get; set; }
    }

    public DnsManagerService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Forzar 127.0.0.1 en todos los adaptadores físicos con IPEnabled = true.
    /// </summary>
    public bool ForceLoopbackDnsOnPhysicalAdapters()
    {
        try
        {
            _activeBackups.Clear();

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
            using var collection = searcher.Get();

            int modifiedCount = 0;

            foreach (ManagementObject obj in collection.Cast<ManagementObject>())
            {
                string settingId = obj["SettingID"]?.ToString() ?? string.Empty;
                string description = obj["Description"]?.ToString() ?? string.Empty;
                string caption = obj["Caption"]?.ToString() ?? string.Empty;

                // Ignorar adaptadores virtuales de TatoVPN / Wintun / TAP / Loopback
                if (description.Contains("Wintun", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("TatoVPN", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("TAP-Windows", StringComparison.OrdinalIgnoreCase) ||
                    caption.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[]? originalDns = obj["DNSServerSearchOrder"] as string[];
                bool dhcpEnabled = (bool)(obj["DHCPEnabled"] ?? false);

                var backup = new AdapterDnsBackup
                {
                    SettingId = settingId,
                    Description = description,
                    OriginalDnsServers = originalDns,
                    DhcpEnabled = dhcpEnabled
                };

                _activeBackups[settingId] = backup;

                // Establecer 127.0.0.1 como único servidor DNS en el adaptador físico
                using var inParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                inParams["DNSServerSearchOrder"] = new string[] { "127.0.0.1" };
                using var outParams = obj.InvokeMethod("SetDNSServerSearchOrder", inParams, null);

                uint returnValue = (uint)(outParams?["ReturnValue"] ?? 1);
                if (returnValue == 0 || returnValue == 1) // 0: Éxito, 1: Requiere reinicio (comportamiento normal en WMI)
                {
                    modifiedCount++;
                }
                else
                {
                    _logger.Log($"⚠️ WMI SetDNSServerSearchOrder en '{description}' retornó código: {returnValue}");
                }
            }

            // Persistir respaldo en disco en caso de cierre inesperado
            PersistBackupToDisk();

            _isEnforced = true;
            _logger.Log($"🔒 Blindaje WMI DNS aplicado: {modifiedCount} adaptador(es) físico(s) forzado(s) exclusivamente a 127.0.0.1.");

            // Limpiar caché DNS de Windows
            FlushDnsCache();

            return true;
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error en WMI DnsManagerService al forzar DNS: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Restaura los DNS originales en los adaptadores modificados.
    /// </summary>
    public void RestorePhysicalAdaptersDns()
    {
        try
        {
            EmergencyRestoreDns();
            _isEnforced = false;
            _logger.Log("🔄 Servidores DNS originales restaurados exitosamente en adaptadores físicos.");
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al restaurar DNS mediante WMI: {ex.Message}");
        }
    }

    /// <summary>
    /// Restauración estática de emergencia invocable desde ProcessExit o UnhandledException.
    /// </summary>
    public static void EmergencyRestoreDns()
    {
        try
        {
            // Cargar desde memoria o desde el archivo persistido en disco
            var backups = new Dictionary<string, AdapterDnsBackup>(_activeBackups);
            if (backups.Count == 0 && File.Exists(BackupFilePath))
            {
                try
                {
                    string json = File.ReadAllText(BackupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<AdapterDnsBackup>>(json);
                    if (loaded != null)
                    {
                        foreach (var b in loaded)
                        {
                            backups[b.SettingId] = b;
                        }
                    }
                }
                catch { }
            }

            if (backups.Count == 0) return;

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection.Cast<ManagementObject>())
            {
                string settingId = obj["SettingID"]?.ToString() ?? string.Empty;
                if (backups.TryGetValue(settingId, out var backup))
                {
                    using var inParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                    if (backup.OriginalDnsServers != null && backup.OriginalDnsServers.Length > 0)
                    {
                        inParams["DNSServerSearchOrder"] = backup.OriginalDnsServers;
                    }
                    else
                    {
                        // Restaurar asignación automática (DHCP) pasando null
                        inParams["DNSServerSearchOrder"] = null;
                    }

                    try
                    {
                        obj.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                    }
                    catch { }
                }
            }

            _activeBackups.Clear();
            if (File.Exists(BackupFilePath))
            {
                try { File.Delete(BackupFilePath); } catch { }
            }

            FlushDnsCache();
        }
        catch { }
    }

    private static void PersistBackupToDisk()
    {
        try
        {
            var list = _activeBackups.Values.ToList();
            string json = JsonSerializer.Serialize(list);
            File.WriteAllText(BackupFilePath, json);
        }
        catch { }
    }

    private static void FlushDnsCache()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(2000);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_isEnforced)
        {
            RestorePhysicalAdaptersDns();
        }
        GC.SuppressFinalize(this);
    }
}
