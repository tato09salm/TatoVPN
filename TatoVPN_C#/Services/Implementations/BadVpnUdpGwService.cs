using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace miVPN.Services.Implementations;

/// <summary>
/// Servidor BadVPN UDP Gateway (udpgw) nativo en C#.
/// Escucha en 0.0.0.0:7300 y multiplexa paquetes UDP (DNS, videollamadas, juegos) sobre TCP.
///
/// Mejoras para juegos y videollamadas:
///   • Timeout de 90 s en sockets UDP inactivos (libera recursos de juegos terminados)
///   • Límite de 512 canales UDP simultáneos por cliente TCP
///   • Paquetes UDP de hasta 65 507 bytes (máximo UDP sobre IP)
///   • Escucha en 0.0.0.0 para aceptar conexiones remotas además de loopback
///   • TTL y buffer optimizados para baja latencia (juegos en tiempo real)
///   • SIO_UDP_CONNRESET desactivado en Windows para no cortar el socket en ICMP port-unreachable
/// </summary>
public class BadVpnUdpGwService : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _port = 7300;

    private const int MaxChannelsPerClient  = 512;
    private const int UdpIdleTimeoutSeconds = 90;
    private const int MaxUdpPayload         = 65507;

    public bool IsRunning => _isRunning;
    public event Action<string>? OnLog;

    public void Start(int port = 7300)
    {
        Stop();

        _port = port;
        _cts  = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // Escuchar en todas las interfaces para aceptar conexiones desde el túnel SSH
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start(100);
            _isRunning = true;

            Log($"🎮 BadVPN UDPGW activo en 0.0.0.0:{port} — soporte UDP para juegos, videollamadas y DNS");

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _listener != null)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(token);
                        _ = HandleClientAsync(client, token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                            Log($"⚠️ Error en listener UDPGW: {ex.Message}");
                        try { await Task.Delay(200, token); } catch { }
                    }
                }
            }, token);
        }
        catch (Exception ex)
        {
            Log($"⚠️ No se pudo iniciar BadVPN UDPGW en puerto {port}: {ex.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken globalCt)
    {
        using var clientCts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);
        var ct = clientCts.Token;

        // NoDelay = true es crítico para juegos en tiempo real (elimina Nagle delay)
        tcpClient.NoDelay            = true;
        tcpClient.ReceiveBufferSize  = 131072;  // 128 KB
        tcpClient.SendBufferSize     = 131072;

        // Estructura: conid → (UdpClient, LastActivity)
        var channels  = new ConcurrentDictionary<ushort, UdpChannel>();
        var tcpStream = tcpClient.GetStream();
        var writeLock = new SemaphoreSlim(1, 1);

        // Timer que cierra canales UDP inactivos (libera puertos de juegos terminados)
        using var idleTimer = new System.Threading.Timer(_ => CleanupIdleChannels(channels, ct), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        try
        {
            using (tcpClient)
            {
                var lenBuf = new byte[2];

                while (!ct.IsCancellationRequested)
                {
                    // 1. Leer longitud del paquete (2 bytes big-endian)
                    if (!await ReadExactAsync(tcpStream, lenBuf, 0, 2, ct)) break;
                    ushort packetLen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);

                    // Mínimo 3 bytes (flags + conid), máximo = header + payload UDP máximo
                    if (packetLen < 3 || packetLen > MaxUdpPayload + 21) break;

                    byte[] packetBody = new byte[packetLen];
                    if (!await ReadExactAsync(tcpStream, packetBody, 0, packetLen, ct)) break;

                    byte   flags = packetBody[0];
                    ushort conid = BinaryPrimitives.ReadUInt16LittleEndian(packetBody.AsSpan(1, 2));

                    // ── Keepalive ──────────────────────────────────────────────────────────
                    if ((flags & 0x01) != 0)
                    {
                        var resp = new byte[5];
                        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(0, 2), 3);
                        resp[2] = 0x01;
                        BinaryPrimitives.WriteUInt16LittleEndian(resp.AsSpan(3, 2), conid);

                        await writeLock.WaitAsync(ct);
                        try   { await tcpStream.WriteAsync(resp, ct); }
                        finally { writeLock.Release(); }
                        continue;
                    }

                    // ── Parsear cabecera destino ───────────────────────────────────────────
                    bool isIpv6     = (flags & 0x08) != 0;
                    int  headerSize = isIpv6 ? 21 : 9;  // IPv6: 1+2+16+2  |  IPv4: 1+2+4+2

                    if (packetLen < headerSize) continue;

                    IPAddress destIp;
                    ushort    destPort;
                    if (isIpv6)
                    {
                        destIp   = new IPAddress(packetBody.AsSpan(3, 16));
                        destPort = BinaryPrimitives.ReadUInt16BigEndian(packetBody.AsSpan(19, 2));
                    }
                    else
                    {
                        destIp   = new IPAddress(packetBody.AsSpan(3, 4));
                        destPort = BinaryPrimitives.ReadUInt16BigEndian(packetBody.AsSpan(7, 2));
                    }

                    int    payloadLen = packetLen - headerSize;
                    byte[] payload    = new byte[payloadLen];
                    if (payloadLen > 0)
                        Buffer.BlockCopy(packetBody, headerSize, payload, 0, payloadLen);

                    // ── Rebind (flag 0x02): cerrar socket anterior para este conid ─────────
                    if ((flags & 0x02) != 0 && channels.TryRemove(conid, out var oldCh))
                        oldCh.Dispose();

                    // ── Límite de canales por cliente ─────────────────────────────────────
                    if (channels.Count >= MaxChannelsPerClient)
                    {
                        // Eliminar el canal más antiguo para hacer espacio
                        var oldest = channels.OrderBy(kv => kv.Value.LastActivity).FirstOrDefault();
                        if (channels.TryRemove(oldest.Key, out var oldestCh))
                            oldestCh.Dispose();
                    }

                    // ── Obtener o crear socket UDP para este conid ─────────────────────────
                    var ch = channels.GetOrAdd(conid, id =>
                    {
                        var udp = new UdpClient(AddressFamily.InterNetwork);
                        try
                        {
                            udp.Client.ReceiveBufferSize = 131072;
                            udp.Client.SendBufferSize    = 131072;

                            // Desactivar SIO_UDP_CONNRESET en Windows:
                            // evita que un ICMP "port unreachable" cierre el socket UDP del juego.
                            if (OperatingSystem.IsWindows())
                            {
                                const uint IOC_IN            = 0x80000000;
                                const uint IOC_VENDOR       = 0x18000000;
                                const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                                udp.Client.IOControl(unchecked((int)SIO_UDP_CONNRESET), [0x00], null);
                            }

                            // TTL alto para evitar drops en redes móviles
                            udp.Ttl = 128;
                        }
                        catch { }

                        // Escuchar respuestas del servidor remoto (juego / DNS / videollamada)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                while (!ct.IsCancellationRequested)
                                {
                                    var result   = await udp.ReceiveAsync(ct);
                                    var respData = result.Buffer;
                                    var remoteEp = result.RemoteEndPoint;

                                    // Actualizar timestamp de actividad
                                    if (channels.TryGetValue(id, out var chRef))
                                        chRef.LastActivity = DateTime.UtcNow;

                                    // Construir frame de respuesta udpgw
                                    bool  rIsIpv6    = remoteEp.AddressFamily == AddressFamily.InterNetworkV6;
                                    int   rHdrSize   = rIsIpv6 ? 21 : 9;
                                    ushort frameLen  = (ushort)(rHdrSize + respData.Length);
                                    var   wire       = new byte[2 + frameLen];

                                    BinaryPrimitives.WriteUInt16BigEndian(wire.AsSpan(0, 2), frameLen);
                                    wire[2] = rIsIpv6 ? (byte)0x08 : (byte)0x00;
                                    BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(3, 2), id);

                                    byte[] ipBytes = remoteEp.Address.GetAddressBytes();
                                    if (rIsIpv6)
                                    {
                                        Buffer.BlockCopy(ipBytes, 0, wire, 5, 16);
                                        BinaryPrimitives.WriteUInt16BigEndian(wire.AsSpan(21, 2), (ushort)remoteEp.Port);
                                        Buffer.BlockCopy(respData, 0, wire, 23, respData.Length);
                                    }
                                    else
                                    {
                                        Buffer.BlockCopy(ipBytes, 0, wire, 5, 4);
                                        BinaryPrimitives.WriteUInt16BigEndian(wire.AsSpan(9, 2), (ushort)remoteEp.Port);
                                        Buffer.BlockCopy(respData, 0, wire, 11, respData.Length);
                                    }

                                    await writeLock.WaitAsync(ct);
                                    try   { await tcpStream.WriteAsync(wire, ct); }
                                    finally { writeLock.Release(); }
                                }
                            }
                            catch { }
                            finally
                            {
                                channels.TryRemove(id, out _);
                                try { udp.Dispose(); } catch { }
                            }
                        }, ct);

                        return new UdpChannel(udp);
                    });

                    // ── Actualizar actividad y enviar datagrama al destino ─────────────────
                    ch.LastActivity = DateTime.UtcNow;
                    try
                    {
                        await ch.Udp.SendAsync(payload, payloadLen, new IPEndPoint(destIp, destPort));
                    }
                    catch { }
                }
            }
        }
        catch { }
        finally
        {
            clientCts.Cancel();
            foreach (var kv in channels)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            channels.Clear();
            writeLock.Dispose();
        }
    }

    /// <summary>Cierra canales UDP que no han tenido actividad en UdpIdleTimeoutSeconds.</summary>
    private static void CleanupIdleChannels(ConcurrentDictionary<ushort, UdpChannel> channels, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        var cutoff = DateTime.UtcNow.AddSeconds(-UdpIdleTimeoutSeconds);
        foreach (var kv in channels.Where(c => c.Value.LastActivity < cutoff).ToList())
        {
            if (channels.TryRemove(kv.Key, out var ch))
                ch.Dispose();
        }
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), ct);
            if (read <= 0) return false;
            total += read;
        }
        return true;
    }

    public void Stop()
    {
        if (!_isRunning) return;
        try
        {
            _isRunning = false;
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;
            Log("🛑 BadVPN UDPGW detenido.");
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }

    private void Log(string message) => OnLog?.Invoke(message);

    // ── Clase auxiliar de canal UDP ───────────────────────────────────────────
    private sealed class UdpChannel(UdpClient udp) : IDisposable
    {
        public readonly UdpClient Udp = udp;
        public DateTime LastActivity  = DateTime.UtcNow;

        public void Dispose()
        {
            try { Udp.Dispose(); } catch { }
        }
    }
}
