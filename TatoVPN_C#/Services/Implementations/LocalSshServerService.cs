using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using FxSsh;
using FxSsh.Services;

namespace miVPN.Services.Implementations;

public class LocalSshServerService : IDisposable
{
    private SshServer? _server;
    private bool _isRunning;
    private string _username = "tatouser";
    private string _password = "tatopass123";
    private int _port = 2222;
    private int _activeTunnels;
    private readonly object _lock = new();

    public bool IsRunning => _isRunning;
    public int ActiveTunnels => _activeTunnels;

    public event Action<string>? OnLog;
    public event Action<int>? OnActiveTunnelsChanged;

    public void Start(int port, string username, string password)
    {
        lock (_lock)
        {
            if (_isRunning) Stop();

            _port = port;
            _username = username;
            _password = password;

            var info = new StartingInfo(IPAddress.Any, port, "SSH-2.0-TatoVPN");
            _server = new SshServer(info);

            // Cargar o generar claves de host
            // Registramos 'ssh-rsa' y 'ecdsa-sha2-nistp256'. NO registramos 'rsa-sha2-256'
            // para compatibilidad nativa con HTTP Injector en Android (evita error 'Unknown key type rsa-sha2-256').
            string rsaPem = GetOrCreateHostKey();
            string ecdsaPem = GetOrCreateEcdsaHostKey();
            try
            {
                _server.AddHostKey("ssh-rsa", rsaPem);
                _server.AddHostKey("ecdsa-sha2-nistp256", ecdsaPem);
            }
            catch (Exception ex)
            {
                Log($"⚠️ Error registrando llaves de host: {ex.Message}");
            }

            _server.ConnectionAccepted += HandleConnectionAccepted;
            _server.Start();
            _isRunning = true;

            Log($"🚀 Servidor SSH activo en puerto {port} (Usuario: {_username})");
        }
    }

    private void HandleConnectionAccepted(object? sender, Session session)
    {
        Log("📲 Dispositivo cliente conectado al servidor SSH.");

        try
        {
            // Keepalive activo cada 15 segundos para evitar desconexiones por inactividad de NAT, operadoras o Pinggy
            session.ConfigureKeepalive(TimeSpan.FromSeconds(15));
        }
        catch { }

        session.Disconnected += (s, e) =>
        {
            Log("📲 Dispositivo cliente desconectado del servidor SSH.");
        };

        session.ServiceRegistered += (s, e) =>
        {
            if (e is UserAuthService userauth)
            {
                userauth.UserAuth += (uSender, uArgs) =>
                {
                    bool authOk = string.Equals(uArgs.Username, _username, StringComparison.Ordinal) &&
                                  string.Equals(uArgs.Password, _password, StringComparison.Ordinal);

                    if (authOk)
                    {
                        uArgs.Result = true;
                        Log($"✅ Autenticación EXITOSA para usuario '{uArgs.Username}' desde cliente.");
                    }
                    else
                    {
                        uArgs.Result = false;
                        Log($"❌ Autenticación DENEGADA para usuario '{uArgs.Username}'. Credenciales incorrectas.");
                    }
                };
            }
            else if (e is ConnectionService connection)
            {
                connection.TcpForwardRequest += HandleTcpForwardRequest;

                connection.CommandOpened += (cSender, cArgs) =>
                {
                    try
                    {
                        var msg = Encoding.UTF8.GetBytes("TatoVPN SSH Server OK\r\n");
                        cArgs.Channel.SendData(msg);
                        cArgs.Channel.SendClose();
                    }
                    catch { }
                };
            }
        };
    }

    private void HandleTcpForwardRequest(object? sender, TcpRequestArgs cArgs)
    {
        string destHost = cArgs.Host;
        int destPort = (int)cArgs.Port;

        Interlocked.Increment(ref _activeTunnels);
        OnActiveTunnelsChanged?.Invoke(_activeTunnels);

        Log($"🌐 [Túnel SSH] Solicitud de conexión hacia {destHost}:{destPort}");

        _ = Task.Run(async () =>
        {
            var channel = cArgs.Channel;
            using var tunnelCts = new CancellationTokenSource();
            var ct = tunnelCts.Token;

            // Cola para canalizar los datos entrantes del cliente SSH sin pérdida de paquetes ni bloqueos
            var inboundQueue = System.Threading.Channels.Channel.CreateUnbounded<byte[]>(
                new System.Threading.Channels.UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            // Suscripción inmediata para no perder datos que el cliente envíe mientras conectamos con el servidor destino
            EventHandler<ReadOnlyMemory<byte>> onDataReceived = (s, data) =>
            {
                if (!data.IsEmpty)
                {
                    inboundQueue.Writer.TryWrite(data.ToArray());
                }
            };

            EventHandler onEofReceived = (s, e) =>
            {
                inboundQueue.Writer.TryComplete();
            };

            EventHandler onCloseReceived = (s, e) =>
            {
                inboundQueue.Writer.TryComplete();
                try { tunnelCts.Cancel(); } catch { }
            };

            channel.DataReceived += onDataReceived;
            channel.EofReceived += onEofReceived;
            channel.CloseReceived += onCloseReceived;

            var targetClient = new TcpClient
            {
                NoDelay = true,
                ReceiveBufferSize = 65536,
                SendBufferSize = 65536
            };

            try
            {
                // Conectar al destino con timeout de 10s
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                connectCts.CancelAfter(TimeSpan.FromSeconds(10));
                await targetClient.ConnectAsync(destHost, destPort, connectCts.Token);

                var remoteStream = targetClient.GetStream();

                // Tarea 1: Cliente SSH -> Destino remoto (escritura secuencial y protegida)
                var pumpToTarget = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var chunk in inboundQueue.Reader.ReadAllAsync(ct))
                        {
                            if (chunk.Length > 0 && targetClient.Connected)
                            {
                                await remoteStream.WriteAsync(chunk, ct);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                    finally
                    {
                        try { targetClient.Client.Shutdown(SocketShutdown.Send); } catch { }
                    }
                }, ct);

                // Tarea 2: Destino remoto -> Cliente SSH (asíncrono con SendDataAsync en bloques de 32KB)
                var pumpToClient = Task.Run(async () =>
                {
                    byte[] buffer = new byte[32768];
                    try
                    {
                        while (!ct.IsCancellationRequested && targetClient.Connected)
                        {
                            int bytesRead = await remoteStream.ReadAsync(buffer, 0, buffer.Length, ct);
                            if (bytesRead <= 0) break;

                            byte[] toSend = new byte[bytesRead];
                            Buffer.BlockCopy(buffer, 0, toSend, 0, bytesRead);
                            channel.SendData(toSend);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ObjectDisposedException) { }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Excepciones normales cuando la sesión se desconecta o cierra
                    }
                    finally
                    {
                        // Notificar fin de datos suavemente con EOF (no cerrar abruptamente el canal en la cara del cliente)
                        try { channel.SendEof(); } catch { }
                    }
                }, ct);

                // Esperar a que una de las dos direcciones concluya
                var firstCompleted = await Task.WhenAny(pumpToTarget, pumpToClient);

                if (firstCompleted == pumpToClient)
                {
                    // El servidor remoto terminó de enviar la respuesta y cerró su socket.
                    // Completar la cola ANTES de cancelar el CTS para desbloquear ReadAllAsync de pumpToTarget
                    // de forma limpia sin dejar el thread zombie esperando indefinidamente.
                    inboundQueue.Writer.TryComplete();
                    try { tunnelCts.Cancel(); } catch { }
                }
                else
                {
                    // El cliente SSH terminó de enviar datos. Damos hasta 3 segundos al servidor remoto para terminar de responder.
                    try
                    {
                        await pumpToClient.WaitAsync(TimeSpan.FromSeconds(3));
                    }
                    catch { }
                    inboundQueue.Writer.TryComplete();
                    try { tunnelCts.Cancel(); } catch { }
                }

                try { await Task.WhenAll(pumpToTarget, pumpToClient); } catch { }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                // No alarmar al usuario si fue desconexión general del socket o cierre normal
                if (!ct.IsCancellationRequested && 
                    !ex.Message.Contains("Object reference") && 
                    !ex.Message.Contains("disposed") &&
                    !ex.Message.Contains("Connection reset"))
                {
                    Log($"⚠️ Error en túnel hacia {destHost}:{destPort}: {ex.Message}");
                }
            }
            finally
            {
                // Limpieza garantizada de eventos para que FxSsh no dispare ajustes de ventana en canales cerrados
                channel.DataReceived -= onDataReceived;
                channel.EofReceived -= onEofReceived;
                channel.CloseReceived -= onCloseReceived;
                inboundQueue.Writer.TryComplete();

                try { targetClient.Close(); targetClient.Dispose(); } catch { }
                try { channel.SendClose(); } catch { }

                Interlocked.Decrement(ref _activeTunnels);
                OnActiveTunnelsChanged?.Invoke(_activeTunnels);
            }
        });
    }

    private string GetOrCreateHostKey()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string keyDir = Path.Combine(appData, "TatoVPN");
            Directory.CreateDirectory(keyDir);
            string keyPath = Path.Combine(keyDir, "server_rsa_hostkey.pem");

            if (File.Exists(keyPath))
            {
                string existing = File.ReadAllText(keyPath);
                if (!string.IsNullOrWhiteSpace(existing) && existing.Contains("PRIVATE KEY"))
                {
                    return existing;
                }
            }

            using var rsa = RSA.Create(2048);
            string pem = rsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(keyPath, pem);
            return pem;
        }
        catch
        {
            using var rsa = RSA.Create(2048);
            return rsa.ExportPkcs8PrivateKeyPem();
        }
    }

    private string GetOrCreateEcdsaHostKey()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string keyDir = Path.Combine(appData, "TatoVPN");
            Directory.CreateDirectory(keyDir);
            string keyPath = Path.Combine(keyDir, "server_ecdsa_hostkey.pem");

            if (File.Exists(keyPath))
            {
                string existing = File.ReadAllText(keyPath);
                if (!string.IsNullOrWhiteSpace(existing) && existing.Contains("PRIVATE KEY"))
                {
                    return existing;
                }
            }

            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string pem = ecdsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(keyPath, pem);
            return pem;
        }
        catch
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return ecdsa.ExportPkcs8PrivateKeyPem();
        }
    }

    private void Log(string message)
    {
        OnLog?.Invoke(message);
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            try
            {
                _server?.Stop();
                _server = null;
                _isRunning = false;
                _activeTunnels = 0;
                OnActiveTunnelsChanged?.Invoke(0);
                Log("🛑 Servidor SSH detenido.");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Error al detener servidor SSH: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
