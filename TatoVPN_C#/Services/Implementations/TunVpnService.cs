using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using miVPN.Models;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class TunVpnService : ITunVpnService
{
    private readonly ILoggerService _logger;
    private readonly IDnsProxyService _dnsProxyService;
    private readonly IFirewallService _firewallService;
    private readonly IDnsManagerService _dnsManagerService;
    private Process? _tunProcess;
    private string? _addedSshHostIp;
    private string? _addedGatewayIp;
    private int _addedInterfaceIndex = -1;
    private bool _isRunning;
    private volatile bool _isExplicitStopping;

    public event Action<string>? OnTunProcessExited;

    public bool IsRunning => _isRunning;
    public bool IsProcessAlive => !_isExplicitStopping && _tunProcess != null && !_tunProcess.HasExited;

    public TunVpnService(
        ILoggerService logger, 
        IDnsProxyService? dnsProxyService = null,
        IFirewallService? firewallService = null,
        IDnsManagerService? dnsManagerService = null)
    {
        _logger = logger;
        _dnsProxyService = dnsProxyService ?? new DnsProxyService(logger);
        _firewallService = firewallService ?? new FirewallService(logger);
        _dnsManagerService = dnsManagerService ?? new DnsManagerService(logger);
    }

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public async Task StartAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        if (!IsAdministrator())
        {
            throw new InvalidOperationException("Se requieren permisos de Administrador para activar la VPN a nivel de sistema (TUN). Por favor ejecuta TatoVPN como Administrador.");
        }

        await StopAsync();
        _isExplicitStopping = false;

        // 0. Limpiar cualquier proceso tun2socks huérfano antes de iniciar
        KillAllTun2SocksProcesses();

        // 1. Iniciar servicio de Proxy DNS UDP-a-TCP en loopback (127.0.0.1:53)
        // Esto asegura que el puerto esté escuchando antes de cualquier cambio de rutas o DNS físico.
        await _dnsProxyService.StartAsync(settings, cancellationToken);

        // 2. Buscar binarios de tun2socks y wintun.dll
        string tun2socksPath = GetBinaryPath("tun2socks.exe");
        string wintunPath = GetBinaryPath("wintun.dll");

        if (!File.Exists(tun2socksPath))
        {
            throw new FileNotFoundException($"No se encontró el ejecutable tun2socks en {tun2socksPath}");
        }

        // Copiar wintun.dll al directorio de tun2socks si es necesario
        string tun2socksDir = Path.GetDirectoryName(tun2socksPath)!;
        string targetWintun = Path.Combine(tun2socksDir, "wintun.dll");
        if (File.Exists(wintunPath) && !File.Exists(targetWintun))
        {
            try { File.Copy(wintunPath, targetWintun, true); } catch { }
        }

        // 3. Iniciar proceso tun2socks con configuración óptima para evitar retrasos iniciales
        string proxyUrl = $"socks5://{settings.SocksLocalIp}:{settings.SocksLocalPort}";
        var psi = new ProcessStartInfo
        {
            FileName = tun2socksPath,
            Arguments = $"--device tun://TatoVPN --proxy {proxyUrl} --loglevel silent",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = tun2socksDir
        };

        _tunProcess = new Process { StartInfo = psi };
        _tunProcess.EnableRaisingEvents = true;
        _tunProcess.Exited += (s, e) =>
        {
            if (!_isExplicitStopping && _isRunning)
            {
                _logger.Log($"⚠️ Proceso tun2socks.exe finalizó inesperadamente (código: {_tunProcess?.ExitCode ?? -1}).");
                OnTunProcessExited?.Invoke("tun2socks se cerró de forma imprevista");
            }
        };

        try
        {
            _tunProcess.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al iniciar tun2socks: {ex.Message}", ex);
        }

        // 4. Esperar a que Windows inicialice la interfaz Wintun "TatoVPN"
        await Task.Delay(2000, cancellationToken);

        if (_tunProcess.HasExited)
        {
            throw new InvalidOperationException($"El proceso tun2socks finalizó inesperadamente con código {_tunProcess.ExitCode}.");
        }

        // Obtener la interfaz Wintun y su índice en Windows
        var (actualAdapterName, tatoVpnIfIndex) = GetWintunInterfaceInfo();

        // Habilitar la interfaz si estaba previamente desactivada
        try { RunCommandDirect("netsh", $"interface set interface name=\"{actualAdapterName}\" admin=enabled"); } catch { }

        // 5. Configurar IP estática, Gateway y DNS apuntando EXCLUSIVAMENTE a 127.0.0.1 (Loopback) en el adaptador TUN
        RunCommandDirect("netsh", $"interface ipv4 set address name=\"{actualAdapterName}\" static 10.255.0.2 255.255.255.0 gateway=10.255.0.1 gwmetric=1");
        RunCommandDirect("netsh", $"interface ipv4 set dnsservers name=\"{actualAdapterName}\" static 127.0.0.1 primary validate=no");
        RunCommandDirect("netsh", $"interface ipv4 set interface name=\"{actualAdapterName}\" metric=1");
        try { RunCommandDirect("netsh", $"interface ipv6 set interface name=\"{actualAdapterName}\" admin=disabled"); } catch { }

        // 6. Resolver IP del servidor SSH
        string sshHostIp = await ResolveIpAsync(settings.SshHost);

        // 7. Obtener Gateway e Interfaz de red física por defecto
        var (gatewayIp, ifIndex) = GetDefaultGatewayAndInterfaceIndex();

        // 8. Crear ruta estática directa para el servidor SSH para evitar bucles de enrutamiento (Routing Loop Bypass)
        if (!string.IsNullOrEmpty(gatewayIp) && !IsLocalIp(sshHostIp))
        {
            string routeCmd = ifIndex > 0
                ? $"add {sshHostIp} mask 255.255.255.255 {gatewayIp} metric 1 if {ifIndex}"
                : $"add {sshHostIp} mask 255.255.255.255 {gatewayIp} metric 1";

            RunCommand("route", routeCmd);
            _addedSshHostIp = sshHostIp;
            _addedGatewayIp = gatewayIp;
            _addedInterfaceIndex = ifIndex;
        }

        // 9. Redirigir todo el tráfico global IPv4 de Windows al adaptador TUN mediante subredes /1
        string routeCmd0 = tatoVpnIfIndex > 0
            ? $"add 0.0.0.0 mask 128.0.0.0 10.255.0.1 metric 1 if {tatoVpnIfIndex}"
            : $"add 0.0.0.0 mask 128.0.0.0 10.255.0.1 metric 1";

        string routeCmd128 = tatoVpnIfIndex > 0
            ? $"add 128.0.0.0 mask 128.0.0.0 10.255.0.1 metric 1 if {tatoVpnIfIndex}"
            : $"add 128.0.0.0 mask 128.0.0.0 10.255.0.1 metric 1";

        RunCommandDirect("route", routeCmd0);
        RunCommandDirect("route", routeCmd128);

        // 10. AHORA que el túnel y el enrutamiento están 100% operativos, aplicar el Blindaje DNS y Firewall
        _dnsManagerService.ForceLoopbackDnsOnPhysicalAdapters();
        _firewallService.ApplyUdpContainmentRules();

        // 11. Limpiar caché DNS para forzar al SO y navegadores a usar la nueva ruta y DNS de inmediato
        RunCommandDirect("ipconfig", "/flushdns");

        // 12. Validar que la resolución de dominios es exitosa antes de declarar la VPN operativa
        try
        {
            _logger.Log("Verificando resolución de DNS por el túnel...");
            using var timeoutCts = new CancellationTokenSource(3000);
            await Dns.GetHostAddressesAsync("google.com", timeoutCts.Token);
            _logger.Log("✅ Resolución DNS confirmada.");
        }
        catch
        {
            _logger.Log("⚠️ Advertencia: La resolución inicial DNS tardó más de lo esperado, pero el túnel está activo.");
        }

        _isRunning = true;
    }

    public async Task StopAsync()
    {
        _isExplicitStopping = true;
        _logger.Log("🧹 Limpiando rutas y restaurando red original...");

        // 1. Eliminar rutas globales /1 (de manera directa/silenciosa ya que puede que no existan previamente)
        RunCommandDirect("route", "delete 0.0.0.0 mask 128.0.0.0");
        RunCommandDirect("route", "delete 128.0.0.0 mask 128.0.0.0");

        // 2. Eliminar ruta estática del servidor SSH
        if (!string.IsNullOrEmpty(_addedSshHostIp))
        {
            RunCommandDirect("route", $"delete {_addedSshHostIp}");
            if (_addedInterfaceIndex > 0)
            {
                try { RunCommandDirect("route", $"delete {_addedSshHostIp} if {_addedInterfaceIndex}"); } catch { }
            }
            _addedSshHostIp = null;
            _addedGatewayIp = null;
            _addedInterfaceIndex = -1;
        }

        // 3. Eliminar Puerta de Enlace, DNS y restaurar métricas en la interfaz virtual TatoVPN
        var (actualAdapterName, _) = GetWintunInterfaceInfo();
        RunCommandDirect("netsh", $"interface ipv4 delete address name=\"{actualAdapterName}\" gateway=all");
        RunCommandDirect("netsh", $"interface ipv4 set dnsservers name=\"{actualAdapterName}\" dhcp");
        RunCommandDirect("netsh", $"interface ipv4 set address name=\"{actualAdapterName}\" dhcp");
        RunCommandDirect("netsh", $"interface ipv4 set interface name=\"{actualAdapterName}\" metric=automatic");

        // 4. Desactivar / Desconectar la interfaz virtual TatoVPN para liberar la prioridad de red en Windows
        RunCommandDirect("netsh", $"interface set interface name=\"{actualAdapterName}\" admin=disabled");
        try
        {
            RunCommandDirect("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"Disable-NetAdapter -Name '{actualAdapterName}' -Confirm:$false -ErrorAction SilentlyContinue\"");
        }
        catch { }

        // 5. Desmontar reglas de Firewall COM
        try
        {
            _firewallService.RemoveUdpContainmentRules();
        }
        catch { }

        // 6. Restaurar DNS originales en adaptadores físicos mediante WMI
        try
        {
            _dnsManagerService.RestorePhysicalAdaptersDns();
        }
        catch { }

        // 7. Detener servicio DNS Proxy
        try
        {
            await _dnsProxyService.StopAsync();
        }
        catch { }

        // 8. Detener proceso tun2socks
        if (_tunProcess != null)
        {
            try
            {
                if (!_tunProcess.HasExited)
                {
                    _tunProcess.Kill(true);
                    await _tunProcess.WaitForExitAsync();
                }
            }
            catch { }

            try { _tunProcess.Dispose(); } catch { }
            _tunProcess = null;
        }

        KillAllTun2SocksProcesses();

        // 9. Flush DNS
        RunCommandDirect("ipconfig", "/flushdns");

        _isRunning = false;
    }

    public static void EmergencyCleanup()
    {
        try
        {
            KillAllTun2SocksProcesses();

            RunCommandDirect("route", "delete 0.0.0.0 mask 128.0.0.0");
            RunCommandDirect("route", "delete 128.0.0.0 mask 128.0.0.0");

            RunCommandDirect("netsh", "interface ipv4 delete address name=\"TatoVPN\" gateway=all");
            RunCommandDirect("netsh", "interface ipv4 set dnsservers name=\"TatoVPN\" dhcp");
            RunCommandDirect("netsh", "interface ipv4 set address name=\"TatoVPN\" dhcp");
            RunCommandDirect("netsh", "interface ipv4 set interface name=\"TatoVPN\" metric=automatic");
            RunCommandDirect("netsh", "interface set interface name=\"TatoVPN\" admin=disabled");

            // Limpieza de emergencia de reglas de Firewall y restauración de DNS WMI
            FirewallService.EmergencyCleanupFirewallRules();
            DnsManagerService.EmergencyRestoreDns();

            RunCommandDirect("ipconfig", "/flushdns");
        }
        catch { }
    }

    private static void KillAllTun2SocksProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName("tun2socks");
            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill(true);
                        p.WaitForExit(1000);
                    }
                }
                catch { }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }
        }
        catch { }
    }

    private static string GetBinaryPath(string binaryName)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string path1 = Path.Combine(baseDir, "Assets", "bin", binaryName);
        if (File.Exists(path1)) return path1;

        string path2 = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "bin", binaryName);
        if (File.Exists(path2)) return path2;

        return path1;
    }

    private static async Task<string> ResolveIpAsync(string host)
    {
        if (IPAddress.TryParse(host, out var parsedIp))
        {
            return parsedIp.ToString();
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ipv4?.ToString() ?? addresses.First().ToString();
        }
        catch
        {
            return host;
        }
    }

    private static bool IsLocalIp(string ip)
    {
        return ip == "127.0.0.1" || ip == "localhost" || ip.StartsWith("192.168.") || ip.StartsWith("10.");
    }

    private (string? GatewayIp, int InterfaceIndex) GetDefaultGatewayAndInterfaceIndex()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                    ni.Name.Contains("TatoVPN", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("Wintun", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("TAP", StringComparison.OrdinalIgnoreCase))
                    continue;

                var ipProps = ni.GetIPProperties();
                var gateway = ipProps.GatewayAddresses
                    .Select(g => g.Address)
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !a.ToString().StartsWith("0."));

                if (gateway != null)
                {
                    int ifIndex = -1;
                    try
                    {
                        var ipv4Props = ipProps.GetIPv4Properties();
                        if (ipv4Props != null) ifIndex = ipv4Props.Index;
                    }
                    catch { }

                    return (gateway.ToString(), ifIndex);
                }
            }
        }
        catch { }

        return (null, -1);
    }

    private static (string AdapterName, int InterfaceIndex) GetWintunInterfaceInfo()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Name.Equals("TatoVPN", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Equals("TatoVPN", StringComparison.OrdinalIgnoreCase) ||
                    ni.Description.Contains("Wintun", StringComparison.OrdinalIgnoreCase))
                {
                    var ipProps = ni.GetIPProperties();
                    var ipv4Props = ipProps?.GetIPv4Properties();
                    int ifIndex = ipv4Props?.Index ?? -1;
                    return (ni.Name, ifIndex);
                }
            }
        }
        catch { }

        return ("TatoVPN", -1);
    }

    private void RunCommand(string filename, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.Default,
                StandardErrorEncoding = System.Text.Encoding.Default
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit(5000);

                bool isBenignNotFound = (!string.IsNullOrWhiteSpace(error) && (error.Contains("No se ha encontrado", StringComparison.OrdinalIgnoreCase) || error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                                     || (!string.IsNullOrWhiteSpace(output) && (output.Contains("No se ha encontrado", StringComparison.OrdinalIgnoreCase) || output.Contains("not found", StringComparison.OrdinalIgnoreCase)));

                if (!string.IsNullOrWhiteSpace(output) && !output.Trim().Equals("Correcto", StringComparison.OrdinalIgnoreCase) && !output.Trim().Equals("OK!", StringComparison.OrdinalIgnoreCase) && !isBenignNotFound)
                {
                    _logger.Log($"[{filename}] {output.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(error) && !isBenignNotFound)
                {
                    _logger.Log($"[{filename} Log] {error.Trim()}");
                }

                if (proc.ExitCode != 0 && !isBenignNotFound)
                {
                    _logger.Log($"⚠️ [{filename} {arguments}] devolvió código {proc.ExitCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al ejecutar '{filename} {arguments}': {ex.Message}");
        }
    }

    private static void RunCommandDirect(string filename, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            if (_tunProcess != null)
            {
                try { if (!_tunProcess.HasExited) _tunProcess.Kill(true); } catch { }
                try { _tunProcess.Dispose(); } catch { }
                _tunProcess = null;
            }
            KillAllTun2SocksProcesses();
        }
        catch { }

        try
        {
            _dnsProxyService.Dispose();
        }
        catch { }

        try
        {
            _firewallService.Dispose();
        }
        catch { }

        try
        {
            _dnsManagerService.Dispose();
        }
        catch { }

        GC.SuppressFinalize(this);
    }
}

