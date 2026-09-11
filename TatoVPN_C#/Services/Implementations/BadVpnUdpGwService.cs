using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace miVPN.Services.Implementations;

/// <summary>
/// Servidor BadVPN UDP Gateway (udpgw) nativo en C#.
/// Escucha en 127.0.0.1:7300 y multiplexa paquetes UDP (DNS, llamadas, juegos) sobre TCP.
/// Provee compatibilidad nativa con HTTP Injector sin requerir ejecutables externos.
/// </summary>
public class BadVpnUdpGwService : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _port = 7300;

    public bool IsRunning => _isRunning;
    public event Action<string>? OnLog;

    public void Start(int port = 7300)
    {
        Stop();

        _port = port;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start(50);
            _isRunning = true;
            Log($"🛡️ BadVPN UDPGW activo en 127.0.0.1:{port} (Soporte UDP/DNS para HTTP Injector)");

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
                            Log($"⚠️ Error en listener UDPGW: {ex.Message}");
                        }
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

        tcpClient.NoDelay = true;
        tcpClient.ReceiveBufferSize = 65536;
        tcpClient.SendBufferSize = 65536;

        var udpSockets = new ConcurrentDictionary<ushort, UdpClient>();
        var tcpStream = tcpClient.GetStream();
        var writeLock = new SemaphoreSlim(1, 1);

        try
        {
            using (tcpClient)
            {
                byte[] lenBuf = new byte[2];

                while (!ct.IsCancellationRequested)
                {
                    // 1. Leer longitud del paquete (2 bytes big-endian)
                    if (!await ReadExactAsync(tcpStream, lenBuf, 0, 2, ct)) break;
                    ushort packetLen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);

                    if (packetLen < 3 || packetLen > 32768)
                    {
                        break; // Paquete inválido
                    }

                    // 2. Leer cuerpo del paquete udpgw
                    byte[] packetBody = new byte[packetLen];
                    if (!await ReadExactAsync(tcpStream, packetBody, 0, packetLen, ct)) break;

                    byte flags = packetBody[0];
                    ushort conid = BinaryPrimitives.ReadUInt16LittleEndian(packetBody.AsSpan(1, 2));

                    // Flag 0x01 = Keepalive
                    if ((flags & 0x01) != 0)
                    {
                        byte[] keepAliveResp = new byte[5];
                        BinaryPrimitives.WriteUInt16BigEndian(keepAliveResp.AsSpan(0, 2), 3);
                        keepAliveResp[2] = 0x01; // flag keepalive
                        BinaryPrimitives.WriteUInt16LittleEndian(keepAliveResp.AsSpan(3, 2), conid);

                        await writeLock.WaitAsync(ct);
                        try { await tcpStream.WriteAsync(keepAliveResp, ct); }
                        finally { writeLock.Release(); }
                        continue;
                    }

                    bool isIpv6 = (flags & 0x08) != 0;
                    int headerSize = isIpv6 ? (1 + 2 + 16 + 2) : (1 + 2 + 4 + 2); // 21 o 9 bytes

                    if (packetLen < headerSize) continue;

                    IPAddress destIp;
                    ushort destPort;
                    if (isIpv6)
                    {
                        destIp = new IPAddress(packetBody.AsSpan(3, 16));
                        destPort = BinaryPrimitives.ReadUInt16BigEndian(packetBody.AsSpan(19, 2));
                    }
                    else
                    {
                        destIp = new IPAddress(packetBody.AsSpan(3, 4));
                        destPort = BinaryPrimitives.ReadUInt16BigEndian(packetBody.AsSpan(7, 2));
                    }

                    int payloadLen = packetLen - headerSize;
                    byte[] payload = new byte[payloadLen];
                    Buffer.BlockCopy(packetBody, headerSize, payload, 0, payloadLen);

                    // Flag 0x02 = Rebind (cerrar socket previo si existía)
                    if ((flags & 0x02) != 0 && udpSockets.TryRemove(conid, out var oldUdp))
                    {
                        try { oldUdp.Dispose(); } catch { }
                    }

                    var udp = udpSockets.GetOrAdd(conid, id =>
                    {
                        var u = new UdpClient();
                        try { u.Client.ReceiveBufferSize = 65536; } catch { }
                        try { u.Client.SendBufferSize = 65536; } catch { }

                        // Escuchar respuestas UDP remotas y reenviarlas al túnel TCP
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                while (!ct.IsCancellationRequested)
                                {
                                    var result = await u.ReceiveAsync(ct);
                                    byte[] respData = result.Buffer;
                                    IPEndPoint remoteEp = result.RemoteEndPoint;

                                    bool respIsIpv6 = remoteEp.AddressFamily == AddressFamily.InterNetworkV6;
                                    int respHeaderSize = respIsIpv6 ? 21 : 9;
                                    ushort totalFrameLen = (ushort)(respHeaderSize + respData.Length);

                                    byte[] wireFrame = new byte[2 + totalFrameLen];
                                    BinaryPrimitives.WriteUInt16BigEndian(wireFrame.AsSpan(0, 2), totalFrameLen);
                                    wireFrame[2] = respIsIpv6 ? (byte)0x08 : (byte)0x00; // flags
                                    BinaryPrimitives.WriteUInt16LittleEndian(wireFrame.AsSpan(3, 2), id);

                                    if (respIsIpv6)
                                    {
                                        byte[] ipBytes = remoteEp.Address.GetAddressBytes();
                                        Buffer.BlockCopy(ipBytes, 0, wireFrame, 5, 16);
                                        BinaryPrimitives.WriteUInt16BigEndian(wireFrame.AsSpan(21, 2), (ushort)remoteEp.Port);
                                        Buffer.BlockCopy(respData, 0, wireFrame, 23, respData.Length);
                                    }
                                    else
                                    {
                                        byte[] ipBytes = remoteEp.Address.GetAddressBytes();
                                        Buffer.BlockCopy(ipBytes, 0, wireFrame, 5, 4);
                                        BinaryPrimitives.WriteUInt16BigEndian(wireFrame.AsSpan(9, 2), (ushort)remoteEp.Port);
                                        Buffer.BlockCopy(respData, 0, wireFrame, 11, respData.Length);
                                    }

                                    await writeLock.WaitAsync(ct);
                                    try
                                    {
                                        await tcpStream.WriteAsync(wireFrame, ct);
                                    }
                                    finally
                                    {
                                        writeLock.Release();
                                    }
                                }
                            }
                            catch { }
                            finally
                            {
                                udpSockets.TryRemove(id, out _);
                                try { u.Dispose(); } catch { }
                            }
                        }, ct);

                        return u;
                    });

                    // Enviar datagrama UDP hacia el destino (ej. servidor DNS 8.8.8.8:53)
                    try
                    {
                        await udp.SendAsync(payload, payloadLen, new IPEndPoint(destIp, destPort));
                    }
                    catch { }
                }
            }
        }
        catch { }
        finally
        {
            clientCts.Cancel();
            foreach (var kv in udpSockets)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            udpSockets.Clear();
            writeLock.Dispose();
        }
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct);
            if (read <= 0) return false;
            totalRead += read;
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
}
