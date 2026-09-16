using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using FxSsh;
using FxSsh.Services;

namespace miVPN.Services.Implementations;

/// <summary>
/// Servidor SSH real que acepta conexiones de HTTP Injector y proxifica los túneles TCP.
///
/// Notas de estabilidad:
///   • NO llamamos channel.SendClose() desde el código del túnel.
///     FxSsh elimina el canal de su diccionario interno en cuanto recibe SendClose();
///     si el cliente aún no procesó ese close y manda SSH_MSG_CHANNEL_WINDOW_ADJUST,
///     FxSsh lanza excepción y mata TODA la sesión SSH. Solución: solo enviamos EOF
///     y dejamos que FxSsh gestione el cierre cuando llegue el close del cliente.
///   • El hot-path de datos (pumpToClient) NO tiene try/catch de excepciones de
///     "ventana" ni "canal" porque eso rompe el flujo de datos y baja el throughput
///     de 8 MB/s a ~8 KB/s.
/// </summary>
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

            // Registramos 'ssh-rsa' y 'ecdsa-sha2-nistp256'.
            // NO registramos 'rsa-sha2-256' para compatibilidad con HTTP Injector Android.
            string rsaPem   = GetOrCreateHostKey();
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
            // Keepalive cada 25 s para mantener viva la sesión en NAT de operadoras LTE/4G.
            session.ConfigureKeepalive(TimeSpan.FromSeconds(25));
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

                    uArgs.Result = authOk;

                    if (authOk)
                        Log($"✅ Auth OK → '{uArgs.Username}'");
                    else
                        Log($"❌ Auth DENEGADA → '{uArgs.Username}'");
                };
            }
            else if (e is ConnectionService connection)
            {
                connection.TcpForwardRequest += HandleTcpForwardRequest;

                connection.CommandOpened += (cSender, cArgs) =>
                {
                    try
                    {
                        cArgs.Channel.SendData(Encoding.UTF8.GetBytes("TatoVPN SSH Server OK\r\n"));
                        // No llamamos SendClose aquí tampoco; FxSsh lo cerrará solo.
                    }
                    catch { }
                };
            }
        };
    }

    private void HandleTcpForwardRequest(object? sender, TcpRequestArgs cArgs)
    {
        string destHost = cArgs.Host;
        int    destPort = (int)cArgs.Port;
        var    channel  = cArgs.Channel;

        Interlocked.Increment(ref _activeTunnels);
        OnActiveTunnelsChanged?.Invoke(_activeTunnels);

        Log($"🌐 [Túnel] → {destHost}:{destPort}");

        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // Cola sin pérdida para datos que llegan del cliente SSH mientras conectamos.
            var inbound = System.Threading.Channels.Channel.CreateUnbounded<byte[]>(
                new System.Threading.Channels.UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            // ── Handlers del canal SSH ──────────────────────────────────────────────
            EventHandler<ReadOnlyMemory<byte>> onData = (_, data) =>
            {
                if (!data.IsEmpty)
                    inbound.Writer.TryWrite(data.ToArray());
            };

            EventHandler onEof = (_, _) =>
            {
                inbound.Writer.TryComplete();
            };

            EventHandler onClose = (_, _) =>
            {
                inbound.Writer.TryComplete();
                try { cts.Cancel(); } catch { }
            };

            channel.DataReceived  += onData;
            channel.EofReceived   += onEof;
            channel.CloseReceived += onClose;
            // ───────────────────────────────────────────────────────────────────────

            var remote = new TcpClient { NoDelay = true, ReceiveBufferSize = 32768, SendBufferSize = 32768 };

            try
            {
                // Conectar al destino con timeout 12 s
                using var connCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                connCts.CancelAfter(TimeSpan.FromSeconds(12));
                await remote.ConnectAsync(destHost, destPort, connCts.Token);

                // TCP keepalive en el socket de destino para detectar cierres silenciosos
                remote.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                var stream = remote.GetStream();

                // ── Pump 1: Cliente SSH → Servidor destino ──────────────────────────
                var toRemote = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var chunk in inbound.Reader.ReadAllAsync(ct))
                        {
                            if (chunk.Length > 0 && remote.Connected)
                                await stream.WriteAsync(chunk, ct);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                    finally
                    {
                        // Señalar al servidor remoto que no vienen más datos del cliente
                        try { remote.Client.Shutdown(SocketShutdown.Send); } catch { }
                    }
                }, ct);

                // ── Pump 2: Servidor destino → Cliente SSH ──────────────────────────
                // HOT PATH: no envolver en try/catch de excepciones específicas de canal/ventana.
                // Cualquier excepción aquí (incl. canal cerrado por el cliente) termina el loop
                // normalmente. No filtramos "window" ni "closed" porque eso paraba el flujo.
                var toClient = Task.Run(async () =>
                {
                    var buf = new byte[32768];
                    try
                    {
                        while (!ct.IsCancellationRequested && remote.Connected)
                        {
                            int n = await stream.ReadAsync(buf, 0, buf.Length, ct);
                            if (n <= 0) break;

                            var slice = new byte[n];
                            Buffer.BlockCopy(buf, 0, slice, 0, n);
                            channel.SendData(slice);   // bloquea si la ventana SSH está llena → correcto
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { /* canal cerrado por el cliente u otra excepción de FxSsh; terminamos limpiamente */ }
                    finally
                    {
                        // EOF señala al cliente que terminamos de enviar datos.
                        // NO llamamos SendClose() porque FxSsh eliminaría el canal de su dict
                        // antes de que el cliente procese el close, provocando
                        // "SSH_MSG_CHANNEL_WINDOW_ADJUST message for non-existent channel".
                        try { channel.SendEof(); } catch { }
                    }
                }, ct);

                // Esperar a que alguno de los dos pumps termine primero
                await Task.WhenAny(toRemote, toClient);

                // Completar la cola y cancelar el otro pump
                inbound.Writer.TryComplete();
                try { cts.Cancel(); } catch { }

                // Dar hasta 3 s para que el segundo pump termine graciosamente
                try { await Task.WhenAll(toRemote, toClient).WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    Log($"⚠️ Túnel {destHost}:{destPort}: {ex.Message}");
            }
            finally
            {
                // Desuscribir SIEMPRE para no recibir más eventos en canal ya muerto
                channel.DataReceived  -= onData;
                channel.EofReceived   -= onEof;
                channel.CloseReceived -= onClose;
                inbound.Writer.TryComplete();

                // SendEof final de seguridad (idempotente; FxSsh lo ignora si ya se envió)
                try { channel.SendEof(); } catch { }

                try { remote.Close(); remote.Dispose(); } catch { }

                Interlocked.Decrement(ref _activeTunnels);
                OnActiveTunnelsChanged?.Invoke(_activeTunnels);
            }
        });
    }

    // ── Claves de Host ───────────────────────────────────────────────────────────

    private string GetOrCreateHostKey()
    {
        try
        {
            string dir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TatoVPN");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "server_rsa_hostkey.pem");

            if (File.Exists(path))
            {
                string txt = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(txt) && txt.Contains("PRIVATE KEY")) return txt;
            }

            using var rsa = RSA.Create(2048);
            string pem = rsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(path, pem);
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
            string dir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TatoVPN");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "server_ecdsa_hostkey.pem");

            if (File.Exists(path))
            {
                string txt = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(txt) && txt.Contains("PRIVATE KEY")) return txt;
            }

            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string pem = ecdsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(path, pem);
            return pem;
        }
        catch
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return ecdsa.ExportPkcs8PrivateKeyPem();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private void Log(string message) => OnLog?.Invoke(message);

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            try
            {
                _server?.Stop();
                _server    = null;
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
