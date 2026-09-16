using System.Diagnostics;
using System.Text.RegularExpressions;

namespace miVPN.Services.Implementations;

/// <summary>
/// Servicio para crear un túnel inverso TCP automático utilizando OpenSSH de Windows (ssh.exe).
/// Expone el puerto SSH local hacia un endpoint público accesible desde cualquier red o celular.
/// Incluye reconexión automática con backoff exponencial para máxima estabilidad.
/// </summary>
public class RemoteTunnelService : IDisposable
{
    private Process? _tunnelProcess;
    private bool _isRunning;
    private bool _disposed;
    private int _localPort;
    private readonly object _lock = new();

    // Control de reconexión automática
    private CancellationTokenSource? _reconnectCts;
    private Task? _reconnectTask;

    // Backoff exponencial: 5s → 15s → 30s → 60s → 60s (máximo)
    private static readonly int[] ReconnectDelaysSeconds = { 5, 15, 30, 60 };
    private int _reconnectAttempt;

    // Temporizador para reconexión preventiva antes del límite de 60 min de Pinggy free
    private CancellationTokenSource? _preventiveReconnectCts;

    private bool _isRemoteFilesMode;

    public bool IsRunning => _isRunning;
    public bool IsRemoteFilesMode => _isRemoteFilesMode;
    public string? PublicHost { get; private set; }
    public int? PublicPort { get; private set; }

    public event Action<string, int>? OnTunnelConnected;
    public event Action<string>? OnLog;
    public event Action? OnTunnelDisconnected;

    // Regex para detectar endpoints TCP asignados por Pinggy u otros proveedores
    // Ejemplo: tcp://iosde-181-176-45-182.run.pinggy-free.link:37629
    private static readonly Regex TcpEndpointRegex = new(
        @"tcp://(?<host>[^:\s]+):(?<port>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public void Start(int localPort, bool isRemoteFilesMode = false)
    {
        lock (_lock)
        {
            if (_disposed) return;
            if (_isRunning) StopProcess();

            _isRemoteFilesMode = isRemoteFilesMode;
            // En modo Conexión Remota / archivos, el túnel inverso reenvía obligatoriamente hacia el puerto 22 de OpenSSH de Windows
            _localPort = isRemoteFilesMode ? 22 : localPort;
            _reconnectAttempt = 0;
            _isRunning = true;

            StartProcess();
        }
    }

    /// <summary>
    /// Inicia el proceso ssh.exe real. Llamado internamente por Start() y por la lógica de reconexión.
    /// NO adquiere el lock (el caller ya lo debe tener, o llamar desde el reconnect task).
    /// </summary>
    private void StartProcess()
    {
        PublicHost = null;
        PublicPort = null;

        string sshExe = FindSshExecutable();
        if (string.IsNullOrEmpty(sshExe))
        {
            OnLog?.Invoke("❌ No se encontró 'ssh.exe' en Windows. Asegúrate de tener el cliente OpenSSH activado.");
            _isRunning = false;
            return;
        }

        if (_isRemoteFilesMode)
        {
            OnLog?.Invoke("🌍 Iniciando túnel inverso público para Conexión Remota (reenviando a Servidor OpenSSH en puerto 22)...");
        }
        else
        {
            OnLog?.Invoke($"🌍 Iniciando túnel inverso público TCP (reenviando a puerto local {_localPort})...");
        }

        // Usamos Pinggy en puerto 443 (SSL) con usuario tcp@a.pinggy.io
        // -T: Desactiva asignación de pseudo-terminal (evita bloqueos de consola)
        // -n: Redirige stdin desde NUL (imprescindible para procesos en segundo plano de Windows)
        // Usamos 127.0.0.1 explícito (no 'localhost') porque en Windows 'localhost' resuelve primero a IPv6 [::1] y falla el reenvío a servidores IPv4
        // ServerAliveInterval=30 + ServerAliveCountMax=10 → hasta 5 minutos de latencia tolerada en redes móviles (LATAM)
        // ConnectTimeout=20 → no quedarse colgado en el handshake
        string arguments = $"-p 443 -T -n" +
            $" -o StrictHostKeyChecking=no" +
            $" -o UserKnownHostsFile=NUL" +
            $" -o ConnectTimeout=20" +
            $" -o ServerAliveInterval=30" +
            $" -o ServerAliveCountMax=10" +
            $" -R 0:127.0.0.1:{_localPort} tcp@a.pinggy.io";

        var startInfo = new ProcessStartInfo
        {
            FileName = sshExe,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            _tunnelProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _tunnelProcess.OutputDataReceived += HandleOutputData;
            _tunnelProcess.ErrorDataReceived += HandleOutputData;
            _tunnelProcess.Exited += HandleProcessExited;

            _tunnelProcess.Start();
            _tunnelProcess.BeginOutputReadLine();
            _tunnelProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌ Error iniciando proceso de túnel: {ex.Message}");
            ScheduleReconnect();
        }
    }

    /// <summary>
    /// Se dispara cuando el proceso ssh.exe termina (por caída, timeout de NAT, límite Pinggy, etc.).
    /// Si el servicio sigue activo (_isRunning=true), programa reconexión automática con backoff.
    /// </summary>
    private void HandleProcessExited(object? sender, EventArgs e)
    {
        // Limpiar proceso actual
        try
        {
            _tunnelProcess?.Dispose();
            _tunnelProcess = null;
        }
        catch { }

        // Cancelar temporizador de reconexión preventiva porque el proceso ya cayó
        CancelPreventiveReconnect();

        if (!_isRunning || _disposed)
        {
            // Fue un Stop() intencional; no reconectar
            OnTunnelDisconnected?.Invoke();
            return;
        }

        // Caída inesperada → notificar y reconectar
        PublicHost = null;
        PublicPort = null;
        OnTunnelDisconnected?.Invoke();
        ScheduleReconnect();
    }

    /// <summary>
    /// Programa la reconexión automática con backoff exponencial.
    /// </summary>
    private void ScheduleReconnect()
    {
        if (_disposed || !_isRunning) return;

        // Cancelar cualquier reconexión pendiente anterior
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = new CancellationTokenSource();
        var cts = _reconnectCts;

        int delayIndex = Math.Min(_reconnectAttempt, ReconnectDelaysSeconds.Length - 1);
        int delaySecs = ReconnectDelaysSeconds[delayIndex];
        _reconnectAttempt++;

        OnLog?.Invoke($"🔄 Túnel caído. Reconectando en {delaySecs}s (intento #{_reconnectAttempt})...");

        _reconnectTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySecs), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // Stop() fue llamado mientras esperábamos
            }

            if (_disposed || !_isRunning || cts.IsCancellationRequested) return;

            OnLog?.Invoke($"🌍 Reconectando túnel inverso (intento #{_reconnectAttempt})...");
            lock (_lock)
            {
                if (!_disposed && _isRunning)
                    StartProcess();
            }
        });
    }

    /// <summary>
    /// Programa reconexión preventiva ~55 minutos después de que el túnel se conecta,
    /// para evitar la caída dura por el límite de 60 minutos del free tier de Pinggy.
    /// </summary>
    private void SchedulePreventiveReconnect()
    {
        CancelPreventiveReconnect();

        if (_disposed || !_isRunning) return;

        _preventiveReconnectCts = new CancellationTokenSource();
        var cts = _preventiveReconnectCts;

        _ = Task.Run(async () =>
        {
            try
            {
                // Reconectar a los 55 minutos (5 min antes del límite de Pinggy)
                await Task.Delay(TimeSpan.FromMinutes(55), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_disposed || !_isRunning || cts.IsCancellationRequested) return;

            OnLog?.Invoke("⏰ Renovando túnel inverso preventivamente (límite de 60 min de Pinggy próximo)...");
            lock (_lock)
            {
                if (!_disposed && _isRunning)
                {
                    // Matar el proceso actual para que HandleProcessExited dispare la reconexión
                    StopProcess(keepRunningFlag: true);
                    _reconnectAttempt = 0; // Resetear backoff para reconexión preventiva
                    StartProcess();
                }
            }
        });
    }

    private void CancelPreventiveReconnect()
    {
        try { _preventiveReconnectCts?.Cancel(); _preventiveReconnectCts?.Dispose(); } catch { }
        _preventiveReconnectCts = null;
    }

    private void HandleOutputData(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data)) return;

        string line = e.Data.Trim();

        // Detectar si la línea contiene la URL del túnel TCP
        var match = TcpEndpointRegex.Match(line);
        if (match.Success)
        {
            string host = match.Groups["host"].Value;
            if (int.TryParse(match.Groups["port"].Value, out int port))
            {
                PublicHost = host;
                PublicPort = port;
                _reconnectAttempt = 0; // Conexión exitosa: resetear backoff
                if (_isRemoteFilesMode)
                {
                    OnLog?.Invoke($"🎉 ¡Túnel Inverso para Conexión Remota (SFTP) CONECTADO con éxito!");
                    OnLog?.Invoke($"   🌐 Host Público : {host}");
                    OnLog?.Invoke($"   🔌 Puerto Remoto: {port}");
                    OnLog?.Invoke($"   📁 Reenviando al Servidor OpenSSH nativo de Windows (puerto 22)");
                }
                else
                {
                    OnLog?.Invoke($"🎉 ¡Túnel Inverso Remoto CONECTADO con éxito!");
                    OnLog?.Invoke($"   🌐 Host Público : {host}");
                    OnLog?.Invoke($"   🔌 Puerto Remoto: {port}");
                }
                OnTunnelConnected?.Invoke(host, port);

                // Programar reconexión preventiva antes de los 60 min de Pinggy
                SchedulePreventiveReconnect();
                return;
            }
        }

        // Ignorar avisos normales y no esenciales de OpenSSH
        if (line.Contains("Warning: Permanently added") ||
            line.Contains("Allocated port") ||
            line.Contains("Pseudo-terminal will not be allocated"))
        {
            return;
        }

        if (line.Contains("Your tunnel will expire") || line.Contains("Upgrade to Pinggy"))
        {
            OnLog?.Invoke($"ℹ️ Túnel temporal gratuito activo (60 min). Se renovará automáticamente.");
            return;
        }

        // Mostrar cualquier otro mensaje de diagnóstico del túnel en el log
        OnLog?.Invoke($"📡 [Túnel]: {line}");
    }

    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
            StopProcess(keepRunningFlag: false);
        }
    }

    /// <summary>
    /// Detiene el proceso ssh.exe sin cambiar _isRunning (usado para reconexión preventiva)
    /// o con keepRunningFlag=false para detención total.
    /// </summary>
    private void StopProcess(bool keepRunningFlag = false)
    {
        if (!keepRunningFlag)
        {
            _isRunning = false;
        }

        // Cancelar reconexiones pendientes
        try { _reconnectCts?.Cancel(); _reconnectCts?.Dispose(); } catch { }
        _reconnectCts = null;

        CancelPreventiveReconnect();

        PublicHost = null;
        PublicPort = null;

        if (_tunnelProcess != null)
        {
            // Desuscribir para evitar que HandleProcessExited dispare una reconexión no deseada
            try { _tunnelProcess.Exited -= HandleProcessExited; } catch { }

            try
            {
                if (!_tunnelProcess.HasExited)
                {
                    _tunnelProcess.Kill(true);
                }
            }
            catch { }
            finally
            {
                try { _tunnelProcess.Dispose(); } catch { }
                _tunnelProcess = null;
            }
        }

        if (!keepRunningFlag)
        {
            OnLog?.Invoke("🛑 Túnel Inverso Remoto desconectado.");
        }
    }

    private static string FindSshExecutable()
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        // 1. Sysnative para procesos WOW64 de 32 bits
        string sysnative = Path.Combine(winDir, "Sysnative", "OpenSSH", "ssh.exe");
        if (File.Exists(sysnative)) return sysnative;

        // 2. System32 directo
        string directSystem32 = Path.Combine(winDir, "System32", "OpenSSH", "ssh.exe");
        if (File.Exists(directSystem32)) return directSystem32;

        // 3. SpecialFolder.System
        string system32Ssh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OpenSSH", "ssh.exe");
        if (File.Exists(system32Ssh)) return system32Ssh;

        // 4. Búsqueda en PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var p in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                string candidate = Path.Combine(p.Trim(), "ssh.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        return "ssh.exe";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
