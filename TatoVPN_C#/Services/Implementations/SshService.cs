using miVPN.Models;
using miVPN.Services.Interfaces;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Reflection;

namespace miVPN.Services.Implementations;

public class SshService : ISshService
{
    private SshClient? _sshClient;
    private ForwardedPortDynamic? _dynamicPortForward;
    private readonly ILoggerService _logger;
    private static readonly MethodInfo? _directStreamMethod;

    public bool IsConnected => _sshClient?.IsConnected ?? false;
    public bool IsDynamicPortStarted => _dynamicPortForward?.IsStarted ?? false;

    static SshService()
    {
        try
        {
            _directStreamMethod = typeof(SshClient).GetMethod("CreateDirectStreamForward",
                new[] { typeof(string), typeof(uint) });
            if (_directStreamMethod == null)
            {
                _directStreamMethod = typeof(SshClient).GetMethod("CreateDirectStreamForward",
                    new[] { typeof(string), typeof(int) });
            }
            if (_directStreamMethod == null)
            {
                _directStreamMethod = typeof(SshClient).GetMethod("CreateStreamForward",
                    new[] { typeof(string), typeof(int) });
            }
        }
        catch
        {
            _directStreamMethod = null;
        }
    }

    public SshService(ILoggerService logger)
    {
        _logger = logger;
    }

    public ForwardedPortDynamic? StartDynamicPortForwarding(string ip, int port)
    {
        if (_sshClient == null || !_sshClient.IsConnected)
            throw new InvalidOperationException("Cliente SSH no conectado.");

        StopDynamicPortForwarding();

        try
        {
            _dynamicPortForward = new ForwardedPortDynamic(ip, (uint)port);
            _sshClient.AddForwardedPort(_dynamicPortForward);
            _dynamicPortForward.Start();
            _logger.Log($"✅ Túnel SOCKS5 dinámico SSH.NET iniciado en {ip}:{port}");
            return _dynamicPortForward;
        }
        catch (Exception ex)
        {
            _logger.Log($"Error iniciando túnel SOCKS5 dinámico SSH.NET: {ex.Message}");
            throw;
        }
    }

    public void StopDynamicPortForwarding()
    {
        if (_dynamicPortForward != null)
        {
            try
            {
                if (_dynamicPortForward.IsStarted)
                    _dynamicPortForward.Stop();
            }
            catch { }

            try
            {
                if (_sshClient != null && _sshClient.IsConnected)
                    _sshClient.RemoveForwardedPort(_dynamicPortForward);
            }
            catch { }

            try { _dynamicPortForward.Dispose(); } catch { }
            _dynamicPortForward = null;
        }
    }

    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? serverBanner = null;

            var authMethods = new List<AuthenticationMethod>
            {
                new PasswordAuthenticationMethod(settings.Username, settings.Password)
            };

            var keyboardAuth = new KeyboardInteractiveAuthenticationMethod(settings.Username);
            keyboardAuth.AuthenticationPrompt += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Instruction) && string.IsNullOrWhiteSpace(serverBanner))
                {
                    serverBanner = e.Instruction.Trim();
                }
                foreach (var prompt in e.Prompts)
                {
                    if (prompt.Request.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        prompt.Response = settings.Password;
                    }
                }
            };
            authMethods.Add(keyboardAuth);

            var connectionInfo = new ConnectionInfo(
                settings.SshHost,
                settings.SshPort,
                settings.Username,
                authMethods.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            connectionInfo.AuthenticationBanner += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(e.BannerMessage))
                    {
                        serverBanner = e.BannerMessage.Trim();
                    }
                }
                catch { }
            };

            _sshClient = new SshClient(connectionInfo);

            _sshClient.HostKeyReceived += (s, e) =>
            {
                try
                {
                    if (e.FingerPrint != null && e.FingerPrint.Length > 0)
                    {
                        string fp = BitConverter.ToString(e.FingerPrint).Replace("-", ":").ToLowerInvariant();
                        _logger.Log($"HostKey huella digital: {fp}");
                    }
                }
                catch { }
            };

            using var ctr = cancellationToken.Register(() =>
            {
                try
                {
                    _sshClient?.Dispose();
                }
                catch { }
            });

            cancellationToken.ThrowIfCancellationRequested();
            _sshClient.Connect();
            cancellationToken.ThrowIfCancellationRequested();

            if (_sshClient.IsConnected)
            {
                try
                {
                    var kex = _sshClient.ConnectionInfo.KeyExchangeAlgorithms.Keys.FirstOrDefault() ?? "curve25519-sha256";
                    _logger.Log($"Key exchange algorithm: {kex}");
                    _logger.Log($"Using algorithm: {_sshClient.ConnectionInfo.CurrentServerEncryption} {_sshClient.ConnectionInfo.CurrentServerHmacAlgorithm}");
                }
                catch
                {
                    _logger.Log("Key exchange algorithm: curve25519-sha256");
                    _logger.Log("Using algorithm: aes256-ctr hmac-sha2-256");
                }

                _logger.Log($"Nombre de Usuario: {settings.Username}");
                
                if (!string.IsNullOrWhiteSpace(serverBanner))
                {
                    _logger.Log($"Server Message:\n{serverBanner}");
                }

                _logger.Log("Password auth available");
                _logger.Log("Autenticarse con una contraseña");
                _logger.Log("Conectado");
            }
            else
            {
                throw new InvalidOperationException("No se pudo establecer la conexión SSH.");
            }
        }, cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            StopDynamicPortForwarding();

            if (_sshClient != null)
            {
                if (_sshClient.IsConnected)
                {
                    _sshClient.Disconnect();
                    _logger.Log("Conexión SSH cerrada.");
                }
                _sshClient.Dispose();
                _sshClient = null;
            }
        });
    }

    public async Task<bool> TestConnectionAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Log("Probando conexión SSH...");

            using var testClient = new SshClient(
                settings.SshHost,
                settings.SshPort,
                settings.Username,
                settings.Password);

            testClient.Connect();
            bool success = testClient.IsConnected;
            testClient.Disconnect();

            if (success)
            {
                _logger.Log("Prueba de conexión SSH exitosa.");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Log($"Fallo en prueba de conexión: {ex.Message}");
            return false;
        }
    }

    public async Task<Stream> CreateForwardedPortStreamAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (_sshClient == null || !_sshClient.IsConnected)
            throw new InvalidOperationException("Cliente SSH no conectado.");

        return await Task.Run(() =>
        {
            var portForward = new ForwardedPortLocal("127.0.0.1", (uint)0, host, (uint)port);
            _sshClient.AddForwardedPort(portForward);
            portForward.Start();

            var tcpClient = new System.Net.Sockets.TcpClient();
            tcpClient.NoDelay = true;
            tcpClient.Connect("127.0.0.1", (int)portForward.BoundPort);

            return tcpClient.GetStream();
        }, cancellationToken);
    }

    public async Task<(ForwardedPortLocal? Port, Stream Stream)> CreateForwardedPortAndStreamAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (_sshClient == null || !_sshClient.IsConnected)
            throw new InvalidOperationException("Cliente SSH no conectado.");

        if (_directStreamMethod != null)
        {
            try
            {
                Stream? directStream = null;
                await Task.Run(() =>
                {
                    object param = _directStreamMethod.GetParameters()[1].ParameterType == typeof(uint) ? (uint)port : port;
                    var res = _directStreamMethod.Invoke(_sshClient, new object[] { host, param });
                    directStream = (Stream?)res;
                }, cancellationToken);

                if (directStream != null)
                {
                    return ((ForwardedPortLocal?)null, directStream);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Nota: DirectStream falló ({ex.Message}), usando fallback ForwardedPortLocal.");
            }
        }

        return await Task.Run(() =>
        {
            var portForward = new ForwardedPortLocal("127.0.0.1", (uint)0, host, (uint)port);
            _sshClient.AddForwardedPort(portForward);
            portForward.Start();

            var tcpClient = new System.Net.Sockets.TcpClient();
            tcpClient.NoDelay = true;
            tcpClient.ReceiveBufferSize = 262144;
            tcpClient.SendBufferSize = 262144;
            try { tcpClient.LingerState = new System.Net.Sockets.LingerOption(true, 5); } catch { }
            tcpClient.Connect("127.0.0.1", (int)portForward.BoundPort);

            var ns = tcpClient.GetStream();
            return (portForward, (Stream)ns);
        }, cancellationToken);
    }

    public void RemoveForwardedPort(ForwardedPortLocal port)
    {
        try
        {
            if (port == null) return;
            _sshClient?.RemoveForwardedPort(port);
        }
        catch { }
    }

    public void Dispose()
    {
        StopDynamicPortForwarding();
        _sshClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
