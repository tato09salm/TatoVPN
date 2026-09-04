using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text.Json;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

/// <summary>
/// Implementación de blindaje y restauración DNS a bajo nivel mediante WMI y Netsh.
/// Captura el estado DNS de los adaptadores físicos y los fuerza a 127.0.0.1 durante la conexión VPN
/// para evitar que Windows envíe consultas DNS en paralelo a servidores del ISP (Multi-Homed DNS Leak).
/// Al desconectar o ante cierres inesperados, restaura de forma infalible la configuración original (DHCP o estática).
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
        public string AdapterName { get; set; } = string.Empty;
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
            var guidMap = GetGuidToInterfaceNameMap();

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

                guidMap.TryGetValue(settingId, out string? adapterName);
                if (string.IsNullOrEmpty(adapterName))
                {
                    adapterName = settingId;
                }

                string[]? originalDns = obj["DNSServerSearchOrder"] as string[];
                bool dhcpEnabled = (bool)(obj["DHCPEnabled"] ?? false);

                // Si los DNS actuales son 127.0.0.1 o localhost, NO son los originales; provienen de una sesión previa no limpiada
                if (originalDns != null && originalDns.All(ip => ip == "127.0.0.1" || ip == "::1" || ip.StartsWith("127.")))
                {
                    originalDns = null;
                    dhcpEnabled = true;
                }

                var backup = new AdapterDnsBackup
                {
                    SettingId = settingId,
                    AdapterName = adapterName,
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
            _logger.Log("🔄 Servidores DNS restaurados exitosamente en adaptadores físicos.");
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al restaurar DNS: {ex.Message}");
        }
    }

    /// <summary>
    /// Escanea los adaptadores de red físicos y repara automáticamente cualquiera que haya quedado bloqueado con 127.0.0.1.
    /// </summary>
    public void AutoHealStuckDns()
    {
        try
        {
            EmergencyRestoreDns();
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error durante auto-recuperación de DNS: {ex.Message}");
        }
    }

    /// <summary>
    /// Restauración estática de emergencia invocable desde ProcessExit, UnhandledException o al iniciar la app.
    /// Utiliza WMI, Netsh y chequeo exhaustivo para garantizar que ningún adaptador quede huérfano con 127.0.0.1.
    /// </summary>
    public static void EmergencyRestoreDns()
    {
        try
        {
            var guidMap = GetGuidToInterfaceNameMap();

            // 1. Cargar respaldos desde memoria o desde el archivo persistido en disco
            var backups = new Dictionary<string, AdapterDnsBackup>(_activeBackups, StringComparer.OrdinalIgnoreCase);
            if (File.Exists(BackupFilePath))
            {
                try
                {
                    string json = File.ReadAllText(BackupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<AdapterDnsBackup>>(json);
                    if (loaded != null)
                    {
                        foreach (var b in loaded)
                        {
                            if (!backups.ContainsKey(b.SettingId))
                            {
                                backups[b.SettingId] = b;
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Restaurar adaptadores usando WMI
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection.Cast<ManagementObject>())
                {
                    try
                    {
                        string settingId = obj["SettingID"]?.ToString() ?? string.Empty;
                        string description = obj["Description"]?.ToString() ?? string.Empty;

                        if (description.Contains("Wintun", StringComparison.OrdinalIgnoreCase) ||
                            description.Contains("TatoVPN", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        backups.TryGetValue(settingId, out var backup);
                        guidMap.TryGetValue(settingId, out string? adapterName);
                        if (string.IsNullOrEmpty(adapterName) && backup != null)
                        {
                            adapterName = backup.AdapterName;
                        }

                        bool shouldResetToDhcp = backup == null || backup.DhcpEnabled ||
                            backup.OriginalDnsServers == null || backup.OriginalDnsServers.Length == 0 ||
                            backup.OriginalDnsServers.All(ip => ip == "127.0.0.1" || ip.StartsWith("127."));

                        if (shouldResetToDhcp)
                        {
                            // Restaurar asignación automática (DHCP) en WMI pasando null
                            try
                            {
                                obj.InvokeMethod("SetDNSServerSearchOrder", null);
                            }
                            catch { }

                            // Restaurar también mediante netsh para 100% de fiabilidad
                            if (!string.IsNullOrEmpty(adapterName))
                            {
                                RunNetshDirect($"interface ipv4 set dnsservers name=\"{adapterName}\" source=dhcp");
                                RunNetshDirect($"interface ipv6 set dnsservers name=\"{adapterName}\" source=dhcp");
                            }
                        }
                        else
                        {
                            // Restaurar DNS estáticos originales válidos
                            try
                            {
                                using var inParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                                inParams["DNSServerSearchOrder"] = backup!.OriginalDnsServers;
                                obj.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                            }
                            catch { }

                            if (!string.IsNullOrEmpty(adapterName) && backup?.OriginalDnsServers != null && backup.OriginalDnsServers.Length > 0)
                            {
                                RunNetshDirect($"interface ipv4 set dnsservers name=\"{adapterName}\" static {backup.OriginalDnsServers[0]} primary validate=no");
                                for (int i = 1; i < backup.OriginalDnsServers.Length; i++)
                                {
                                    RunNetshDirect($"interface ipv4 add dnsservers name=\"{adapterName}\" {backup.OriginalDnsServers[i]} index={i + 1} validate=no");
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 3. Blindaje Universal de Seguridad:
            // Escanear TODOS los adaptadores de red activos. Si alguno sigue teniendo DNS apuntando a 127.0.0.1 o loopback,
            // forzarlo inmediatamente a DHCP mediante Netsh. Ningún adaptador debe quedar con DNS roto tras cerrar TatoVPN.
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    if (ni.Name.Equals("TatoVPN", StringComparison.OrdinalIgnoreCase) ||
                        ni.Description.Contains("Wintun", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var ipProps = ni.GetIPProperties();
                        var dnsServers = ipProps.DnsAddresses
                            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .Select(a => a.ToString())
                            .ToList();

                        if (dnsServers.Any(ip => ip == "127.0.0.1" || ip.StartsWith("127.")))
                        {
                            RunNetshDirect($"interface ipv4 set dnsservers name=\"{ni.Name}\" source=dhcp");
                            RunNetshDirect($"interface ipv6 set dnsservers name=\"{ni.Name}\" source=dhcp");
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 4. Limpiar memoria y archivo de respaldo temporal
            _activeBackups.Clear();
            if (File.Exists(BackupFilePath))
            {
                try { File.Delete(BackupFilePath); } catch { }
            }

            // 5. Vaciar caché DNS de Windows
            FlushDnsCache();
        }
        catch { }
    }

    private static Dictionary<string, string> GetGuidToInterfaceNameMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                map[ni.Id] = ni.Name;
            }
        }
        catch { }
        return map;
    }

    private static void RunNetshDirect(string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(3000);
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
