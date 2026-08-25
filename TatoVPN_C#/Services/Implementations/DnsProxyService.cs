using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using miVPN.Models;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class DnsProxyService : IDnsProxyService
{
    private readonly ILoggerService _logger;
    private UdpClient? _udpListener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private string _socksIp = "127.0.0.1";
    private int _socksPort = 1080;

    // Cache simple en memoria: (QueryBytesMinusTxId) -> (ResponseBytesMinusTxId, ExpiryTime)
    private readonly ConcurrentDictionary<string, (byte[] Response, DateTime Expiry)> _dnsCache = new();

    public bool IsRunning => _isRunning;

    public DnsProxyService(ILoggerService logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        _socksIp = string.IsNullOrEmpty(settings.SocksLocalIp) ? "127.0.0.1" : settings.SocksLocalIp;
        _socksPort = settings.SocksLocalPort > 0 ? settings.SocksLocalPort : 1080;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        try
        {
            // Escuchar en el puerto 53 UDP en todas las interfaces de red locales
            var localEp = new IPEndPoint(IPAddress.Any, 53);
            _udpListener = new UdpClient();

            try
            {
                _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch { }

            // Deshabilitar la excepción de Winsock SIO_UDP_CONNRESET (10054 ICMP Port Unreachable)
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                _udpListener.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, new byte[] { 0 });
            }
            catch { }

            _udpListener.Client.Bind(localEp);
            _isRunning = true;

            _logger.Log("🌐 Proxy DNS UDP-a-TCP iniciado en puerto 53 (Loopback 127.0.0.1:53 activo).");

            _ = Task.Run(() => ListenUdpLoopAsync(token), token);
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ No se pudo iniciar el servidor DNS en UDP:53 ({ex.Message}). Es posible que otro servicio lo esté ocupando.");
            _isRunning = false;
        }

        await Task.CompletedTask;
    }

    private async Task ListenUdpLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpListener != null)
        {
            try
            {
                var result = await _udpListener.ReceiveAsync(cancellationToken);
                _ = ProcessDnsRequestAsync(result.Buffer, result.RemoteEndPoint, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset || se.NativeErrorCode == 10054)
            {
                // Ignorar excepción Winsock 10054 (ICMP Port Unreachable) y continuar escuchando consultas DNS
                continue;
            }
            catch (Exception ex)
            {
                if (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    _logger.Log($"Error en recepción UDP DNS: {ex.Message}");
                }
            }
        }
    }

    private async Task ProcessDnsRequestAsync(byte[] queryBuffer, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        if (queryBuffer.Length < 12 || _udpListener == null) return;

        ushort txId = (ushort)((queryBuffer[0] << 8) | queryBuffer[1]);
        ushort qType = GetQueryType(queryBuffer);

        // Bloquear/filtrar registros AAAA (IPv6 = 28) y HTTPS/SVCB (65)
        // Esto desactiva QUIC/HTTP3 por UDP en navegadores y los fuerza a usar TCP HTTP/2 a máxima velocidad por la VPN.
        if (qType == 28 || qType == 65)
        {
            byte[] emptyResp = BuildEmptyDnsResponse(queryBuffer);
            try
            {
                await _udpListener.SendAsync(emptyResp, emptyResp.Length, remoteEndPoint);
            }
            catch { }
            return;
        }

        string cacheKey = Convert.ToBase64String(queryBuffer, 2, queryBuffer.Length - 2);

        // 1. Verificar Caché en memoria
        if (_dnsCache.TryGetValue(cacheKey, out var cachedEntry))
        {
            if (DateTime.UtcNow < cachedEntry.Expiry)
            {
                byte[] cachedResp = (byte[])cachedEntry.Response.Clone();
                cachedResp[0] = (byte)((txId >> 8) & 0xFF);
                cachedResp[1] = (byte)(txId & 0xFF);
                try
                {
                    await _udpListener.SendAsync(cachedResp, cachedResp.Length, remoteEndPoint);
                }
                catch { }
                return;
            }
            else
            {
                _dnsCache.TryRemove(cacheKey, out _);
            }
        }

        // 2. Consultar DNS sobre TCP usando el Túnel SOCKS5/SSH
        byte[]? responseBuffer = await ForwardDnsQueryOverTcpAsync(queryBuffer, cancellationToken);

        if (responseBuffer != null && responseBuffer.Length >= 12)
        {
            // Guardar en Caché por 60 segundos
            byte[] cacheRespPayload = (byte[])responseBuffer.Clone();
            _dnsCache[cacheKey] = (cacheRespPayload, DateTime.UtcNow.AddSeconds(60));

            // Asegurar que el Transaction ID coincida con la consulta original
            responseBuffer[0] = (byte)((txId >> 8) & 0xFF);
            responseBuffer[1] = (byte)(txId & 0xFF);

            try
            {
                await _udpListener.SendAsync(responseBuffer, responseBuffer.Length, remoteEndPoint);
            }
            catch { }
        }
    }

    private static ushort GetQueryType(byte[] queryBuffer)
    {
        if (queryBuffer.Length < 15) return 0;
        int pos = 12; // Inicio de la sección de preguntas
        while (pos < queryBuffer.Length)
        {
            byte len = queryBuffer[pos];
            if (len == 0)
            {
                pos++;
                break;
            }
            pos += len + 1;
        }
        if (pos + 2 <= queryBuffer.Length)
        {
            return (ushort)((queryBuffer[pos] << 8) | queryBuffer[pos + 1]);
        }
        return 0;
    }

    private static byte[] BuildEmptyDnsResponse(byte[] queryBuffer)
    {
        byte[] resp = (byte[])queryBuffer.Clone();
        // QR=1, Opcode=0, AA=0, TC=0, RD=1, RA=1, RCODE=0 (No error) => 0x8180
        resp[2] = 0x81;
        resp[3] = 0x80;
        // ANCOUNT = 0
        resp[6] = 0x00;
        resp[7] = 0x00;
        // NSCOUNT = 0
        resp[8] = 0x00;
        resp[9] = 0x00;
        // ARCOUNT = 0
        resp[10] = 0x00;
        resp[11] = 0x00;
        return resp;
    }

    private async Task<byte[]?> ForwardDnsQueryOverTcpAsync(byte[] queryBuffer, CancellationToken cancellationToken)
    {
        string[] dnsServers = new[] { "1.1.1.1", "8.8.8.8", "1.0.0.1" };

        foreach (var dnsIp in dnsServers)
        {
            try
            {
                using var stream = await ConnectTcpDnsOverSocks5Async(dnsIp, 53, cancellationToken);
                if (stream == null) continue;

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(3500);

                // Marco de longitud de 2 bytes para DNS sobre TCP (RFC 1035)
                byte[] tcpQuery = new byte[2 + queryBuffer.Length];
                tcpQuery[0] = (byte)((queryBuffer.Length >> 8) & 0xFF);
                tcpQuery[1] = (byte)(queryBuffer.Length & 0xFF);
                Array.Copy(queryBuffer, 0, tcpQuery, 2, queryBuffer.Length);

                await stream.WriteAsync(tcpQuery, 0, tcpQuery.Length, cts.Token);
                await stream.FlushAsync(cts.Token);

                // Leer longitud de 2 bytes de la respuesta
                byte[] lenBytes = new byte[2];
                await ReadExactAsync(stream, lenBytes, 0, 2, cts.Token);
                int respLen = (lenBytes[0] << 8) | lenBytes[1];

                if (respLen <= 0 || respLen > 65535)
                    continue;

                byte[] respBuffer = new byte[respLen];
                await ReadExactAsync(stream, respBuffer, 0, respLen, cts.Token);

                return respBuffer;
            }
            catch
            {
                // Si falla un DNS público, probar con el siguiente
            }
        }

        return null;
    }

    private async Task<NetworkStream?> ConnectTcpDnsOverSocks5Async(string targetDnsIp, int targetDnsPort, CancellationToken cancellationToken)
    {
        var tcpClient = new TcpClient { NoDelay = true };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3500);

            await tcpClient.ConnectAsync(_socksIp, _socksPort, cts.Token);
            var stream = tcpClient.GetStream();

            // 1. Handshake SOCKS5
            byte[] handshake = new byte[] { 0x05, 0x01, 0x00 };
            await stream.WriteAsync(handshake, 0, handshake.Length, cts.Token);

            byte[] handshakeReply = new byte[2];
            await ReadExactAsync(stream, handshakeReply, 0, 2, cts.Token);
            if (handshakeReply[0] != 0x05 || handshakeReply[1] != 0x00)
            {
                tcpClient.Close();
                return null;
            }

            // 2. Solicitud SOCKS5 CONNECT
            var targetIpBytes = IPAddress.Parse(targetDnsIp).GetAddressBytes();
            byte[] connectCmd = new byte[4 + targetIpBytes.Length + 2];
            connectCmd[0] = 0x05; // Versión
            connectCmd[1] = 0x01; // CONNECT
            connectCmd[2] = 0x00; // Reservado
            connectCmd[3] = 0x01; // IPv4
            Array.Copy(targetIpBytes, 0, connectCmd, 4, targetIpBytes.Length);
            connectCmd[4 + targetIpBytes.Length] = (byte)((targetDnsPort >> 8) & 0xFF);
            connectCmd[4 + targetIpBytes.Length + 1] = (byte)(targetDnsPort & 0xFF);

            await stream.WriteAsync(connectCmd, 0, connectCmd.Length, cts.Token);

            byte[] connectReplyHeader = new byte[4];
            await ReadExactAsync(stream, connectReplyHeader, 0, 4, cts.Token);
            if (connectReplyHeader[1] != 0x00)
            {
                tcpClient.Close();
                return null;
            }

            int addressType = connectReplyHeader[3];
            int remainingLen = addressType switch
            {
                0x01 => 4 + 2, // IPv4: 4 bytes IP + 2 bytes Puerto
                0x03 => 1,     // Dominio: primer byte indica longitud
                0x04 => 16 + 2, // IPv6: 16 bytes IP + 2 bytes Puerto
                _ => 0
            };

            if (addressType == 0x03)
            {
                byte[] lenBuf = new byte[1];
                await ReadExactAsync(stream, lenBuf, 0, 1, cts.Token);
                remainingLen = lenBuf[0] + 2;
            }

            if (remainingLen > 0)
            {
                byte[] remBuf = new byte[remainingLen];
                await ReadExactAsync(stream, remBuf, 0, remainingLen, cts.Token);
            }

            return stream;
        }
        catch
        {
            tcpClient.Close();
            return null;
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("La conexión SOCKS5 se cerró inesperadamente.");
            totalRead += read;
        }
    }

    public async Task StopAsync()
    {
        _isRunning = false;

        if (_cts != null)
        {
            try { _cts.Cancel(); } catch { }
            try { _cts.Dispose(); } catch { }
            _cts = null;
        }

        if (_udpListener != null)
        {
            try { _udpListener.Close(); } catch { }
            try { _udpListener.Dispose(); } catch { }
            _udpListener = null;
        }

        _dnsCache.Clear();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
