using System.Net;
using System.Net.Sockets;
using System.Text;

namespace miVPN.Services.Implementations;

public class LocalHttpProxyService : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _port = 1080;
    private int _activeConnections;

    public bool IsRunning => _isRunning;
    public int ActiveConnections => _activeConnections;

    public event Action<string>? OnLog;
    public event Action<int>? OnActiveConnectionsChanged;

    public void Start(int port)
    {
        Stop();

        _port = port;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _listener = new TcpListener(IPAddress.Any, port);
        try
        {
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }
        catch { }

        _listener.Start(100);
        _isRunning = true;
        Log($"🌐 Proxy Local (HTTP CONNECT / SOCKS5) activo en puerto {port}");

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _ = HandleClientAsync(client, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Log($"⚠️ Error en listener de Proxy: {ex.Message}");
                    }
                    try { await Task.Delay(200, token); } catch { }
                }
            }
        }, token);
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        Interlocked.Increment(ref _activeConnections);
        OnActiveConnectionsChanged?.Invoke(_activeConnections);

        client.NoDelay = true;
        client.ReceiveBufferSize = 65536;
        client.SendBufferSize = 65536;

        try
        {
            using (client)
            {
                var stream = client.GetStream();

                // Leer los primeros bytes para determinar si es HTTP (CONNECT) o SOCKS5 (0x05)
                byte[] peekBuf = new byte[1];
                int read = await stream.ReadAsync(peekBuf.AsMemory(0, 1), ct);
                if (read <= 0) return;

                if (peekBuf[0] == 0x05)
                {
                    // Manejar SOCKS5
                    await HandleSocks5Async(stream, client, ct);
                }
                else
                {
                    // Manejar HTTP CONNECT / GET / etc.
                    await HandleHttpProxyAsync(stream, peekBuf[0], client, ct);
                }
            }
        }
        catch { }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
            OnActiveConnectionsChanged?.Invoke(_activeConnections);
        }
    }

    private async Task HandleHttpProxyAsync(NetworkStream stream, byte firstByte, TcpClient client, CancellationToken ct)
    {
        using var reader = new MemoryStream();
        reader.WriteByte(firstByte);

        byte[] buf = new byte[4096];
        string requestHeader = "";

        while (true)
        {
            int r = await stream.ReadAsync(buf.AsMemory(0, buf.Length), ct);
            if (r <= 0) return;
            reader.Write(buf, 0, r);

            string currentText = Encoding.ASCII.GetString(reader.ToArray());
            int headerEnd = currentText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd >= 0)
            {
                requestHeader = currentText.Substring(0, headerEnd);
                break;
            }
            if (reader.Length > 16384) return; // Cabecera demasiado grande
        }

        var lines = requestHeader.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        var requestLine = lines[0]; // Ej: CONNECT 127.0.0.1:2222 HTTP/1.1 o GET http://example.com/ HTTP/1.1
        var parts = requestLine.Split(' ');
        if (parts.Length < 2) return;

        string method = parts[0].ToUpperInvariant();
        string target = parts[1];

        string targetHost;
        int targetPort = 80;

        if (method == "CONNECT")
        {
            // CONNECT host:port HTTP/1.1
            var hostParts = target.Split(':');
            targetHost = hostParts[0];
            if (hostParts.Length > 1 && int.TryParse(hostParts[1], out int p))
            {
                targetPort = p;
            }
            else
            {
                targetPort = 443;
            }
        }
        else
        {
            // GET http://host:port/... HTTP/1.1
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                targetHost = uri.Host;
                targetPort = uri.Port;
            }
            else
            {
                var hostHeader = lines.FirstOrDefault(l => l.StartsWith("Host:", StringComparison.OrdinalIgnoreCase));
                if (hostHeader == null) return;
                var val = hostHeader.Substring(5).Trim();
                var hParts = val.Split(':');
                targetHost = hParts[0];
                if (hParts.Length > 1 && int.TryParse(hParts[1], out int p))
                    targetPort = p;
            }
        }

        Log($"🔗 [Proxy HTTP] Petición {method} hacia {targetHost}:{targetPort}");

        using var remoteClient = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = 65536,
            SendBufferSize = 65536
        };

        try
        {
            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await remoteClient.ConnectAsync(targetHost, targetPort, connectCts.Token);
        }
        catch (Exception ex)
        {
            Log($"⚠️ No se pudo conectar con {targetHost}:{targetPort} ({ex.Message})");
            byte[] errorResp = Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
            await stream.WriteAsync(errorResp.AsMemory(0, errorResp.Length), ct);
            return;
        }

        var remoteStream = remoteClient.GetStream();

        if (method == "CONNECT")
        {
            byte[] okResp = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            await stream.WriteAsync(okResp.AsMemory(0, okResp.Length), ct);
        }
        else
        {
            // Para GET/POST directo, reenviar la petición original
            byte[] rawReq = reader.ToArray();
            await remoteStream.WriteAsync(rawReq.AsMemory(0, rawReq.Length), ct);
        }

        // Relaying bidireccional
        await RelayStreamsAsync(stream, remoteStream, ct);
    }

    private async Task HandleSocks5Async(NetworkStream clientStream, TcpClient client, CancellationToken ct)
    {
        // Handshake SOCKS5: ya leímos 0x05
        byte[] lenBuf = new byte[1];
        await ReadExactAsync(clientStream, lenBuf, 0, 1, ct);
        int nMethods = lenBuf[0];
        byte[] methods = new byte[nMethods];
        await ReadExactAsync(clientStream, methods, 0, nMethods, ct);

        // Respuesta sin autenticación: 0x05, 0x00
        byte[] authResp = new byte[] { 0x05, 0x00 };
        await clientStream.WriteAsync(authResp.AsMemory(0, 2), ct);

        // Petición SOCKS5 (VER, CMD, RSV, ATYP, DST.ADDR, DST.PORT)
        byte[] reqHeader = new byte[4];
        await ReadExactAsync(clientStream, reqHeader, 0, 4, ct);

        if (reqHeader[0] != 0x05 || reqHeader[1] != 0x01) // Solo comando CONNECT (0x01)
        {
            await clientStream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }.AsMemory(0, 10), ct);
            return;
        }

        byte atyp = reqHeader[3];
        string targetHost;
        int targetPort;

        if (atyp == 0x01) // IPv4
        {
            byte[] ipv4 = new byte[6];
            await ReadExactAsync(clientStream, ipv4, 0, 6, ct);
            targetHost = $"{ipv4[0]}.{ipv4[1]}.{ipv4[2]}.{ipv4[3]}";
            targetPort = (ipv4[4] << 8) | ipv4[5];
        }
        else if (atyp == 0x03) // Domain
        {
            byte[] dLenBuf = new byte[1];
            await ReadExactAsync(clientStream, dLenBuf, 0, 1, ct);
            int dLen = dLenBuf[0];
            byte[] domainAndPort = new byte[dLen + 2];
            await ReadExactAsync(clientStream, domainAndPort, 0, dLen + 2, ct);
            targetHost = Encoding.ASCII.GetString(domainAndPort, 0, dLen);
            targetPort = (domainAndPort[dLen] << 8) | domainAndPort[dLen + 1];
        }
        else if (atyp == 0x04) // IPv6
        {
            byte[] ipv6 = new byte[18];
            await ReadExactAsync(clientStream, ipv6, 0, 18, ct);
            byte[] ipBytes = new byte[16];
            Array.Copy(ipv6, 0, ipBytes, 0, 16);
            targetHost = new IPAddress(ipBytes).ToString();
            targetPort = (ipv6[16] << 8) | ipv6[17];
        }
        else
        {
            await clientStream.WriteAsync(new byte[] { 0x05, 0x08, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }.AsMemory(0, 10), ct);
            return;
        }

        Log($"🔗 [Proxy SOCKS5] Conexión hacia {targetHost}:{targetPort}");

        using var remoteClient = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = 65536,
            SendBufferSize = 65536
        };

        try
        {
            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await remoteClient.ConnectAsync(targetHost, targetPort, connectCts.Token);
            var remoteStream = remoteClient.GetStream();

            // Respuesta de éxito
            byte[] okResp = new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
            await clientStream.WriteAsync(okResp.AsMemory(0, 10), ct);

            await RelayStreamsAsync(clientStream, remoteStream, ct);
        }
        catch (Exception ex)
        {
            Log($"⚠️ Fallo al conectar SOCKS5 con {targetHost}:{targetPort} ({ex.Message})");
            try
            {
                byte[] failResp = new byte[] { 0x05, 0x04, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
                await clientStream.WriteAsync(failResp.AsMemory(0, 10), ct);
            }
            catch { }
        }
    }

    private static async Task RelayStreamsAsync(Stream stream1, Stream stream2, CancellationToken ct)
    {
        using var relayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var t1 = CopyStreamAsync(stream1, stream2, relayCts.Token);
        var t2 = CopyStreamAsync(stream2, stream1, relayCts.Token);

        await Task.WhenAny(t1, t2).ConfigureAwait(false);
        relayCts.CancelAfter(500);
        try
        {
            await Task.WhenAll(t1, t2).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task CopyStreamAsync(Stream src, Stream dst, CancellationToken ct)
    {
        byte[] buffer = new byte[65536];
        try
        {
            int r;
            while ((r = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, r), ct).ConfigureAwait(false);
            }
            await dst.FlushAsync(ct).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int r = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), ct).ConfigureAwait(false);
            if (r == 0) throw new EndOfStreamException("Flujo cerrado prematuramente");
            total += r;
        }
    }

    private void Log(string msg)
    {
        OnLog?.Invoke(msg);
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        try { _cts?.Cancel(); } catch { }
        if (_listener != null)
        {
            try { _listener.Stop(); } catch { }
            try { _listener.Server.Close(); _listener.Server.Dispose(); } catch { }
            _listener = null;
        }
        try { _cts?.Dispose(); } catch { }
        _cts = null;
        _activeConnections = 0;
        OnActiveConnectionsChanged?.Invoke(0);
        Log("🛑 Proxy local detenido.");
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
