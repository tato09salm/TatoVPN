using System.Net;
using System.Net.Sockets;
using miVPN.Models;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class SocksProxyService : ISocksProxyService
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ILoggerService _logger;
    private ISshService? _sshService;
    private ConnectionSettings? _settings;
    private bool _isRunning;
    private readonly SemaphoreSlim _concurrencySemaphore = new(256, 256);

    public bool IsRunning => _isRunning;

    public SocksProxyService(ILoggerService logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(ConnectionSettings settings, ISshService sshService, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        _sshService = sshService;

        try
        {
            await EnsureStoppedAsync();
        }
        catch { }

        try
        {
            await Task.Delay(200, cancellationToken);
        }
        catch { }

        // Si no estamos en modo TUN, usar el puerto dinámico de SSH.NET para peticiones simples
        if (_sshService != null && _sshService.IsConnected && !settings.EnableTunMode)
        {
            try
            {
                _sshService.StartDynamicPortForwarding(settings.SocksLocalIp, settings.SocksLocalPort);
                _isRunning = true;
                await Task.CompletedTask;
                return;
            }
            catch (Exception ex)
            {
                _logger.Log($"Advertencia al iniciar túnel SSH.NET dinámico ({ex.Message}), intentando servidor SOCKS5 independiente...");
            }
        }

        var ipAddress = IPAddress.Parse(settings.SocksLocalIp);
        int port = settings.SocksLocalPort;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tokenToUse = _cts.Token;

        _listener = new TcpListener(ipAddress, port);
        try
        {
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }
        catch { }

        try
        {
            // Backlog de 200 conexiones entrantes concurrentes para modo TUN
            _listener.Start(200);
        }
        catch (SocketException se)
        {
            try { _listener.Server.Dispose(); } catch { }
            _listener = null;
            if (se.SocketErrorCode == SocketError.AccessDenied ||
                se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new InvalidOperationException(
                    $"El puerto {port} está ocupado por otra aplicación o quedó colgado de una ejecución anterior. " +
                    $"Cierra cualquier otra instancia de miVPN que esté corriendo o cambia el puerto SOCKS5. Detalle: {se.Message}");
            }
            throw;
        }

        _isRunning = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await AcceptClientsAsync(tokenToUse);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.Log($"Error en loop SOCKS: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
            }
        }, tokenToUse);

        await Task.CompletedTask;
    }

    private async Task EnsureStoppedAsync()
    {
        _isRunning = false;

        if (_sshService != null && _sshService.IsDynamicPortStarted)
        {
            try { _sshService.StopDynamicPortForwarding(); } catch { }
        }

        var oldCts = _cts;
        var oldListener = _listener;

        if (oldCts != null)
        {
            try { oldCts.Cancel(); } catch { }
        }

        if (oldListener != null)
        {
            try { oldListener.Stop(); } catch { }
            try { oldListener.Server.Close(); oldListener.Server.Dispose(); } catch { }
        }

        if (oldCts != null)
        {
            try { oldCts.Dispose(); } catch { }
        }

        if (_listener == oldListener) _listener = null;
        if (_cts == oldCts) _cts = null;

        _logger.Log("Proxy SOCKS5 detenido.");
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.NoBufferSpaceAvailable || se.NativeErrorCode == 10055)
            {
                _logger.Log("⚠️ Cola de sockets del sistema llena (WSAENOBUFS 10055). Pausando aceptación brevemente...");
                try { await Task.Delay(250, cancellationToken); } catch { }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error aceptando cliente SOCKS: {ex.Message}");
                try { await Task.Delay(100, cancellationToken); } catch { }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        if (!await _concurrencySemaphore.WaitAsync(2000, cancellationToken))
        {
            try { client.Close(); client.Dispose(); } catch { }
            return;
        }

        try
        {
            client.NoDelay = true;
            client.ReceiveBufferSize = 65536;
            client.SendBufferSize = 65536;
            try { client.LingerState = new LingerOption(false, 0); } catch { }

            using (client)
            {
                var clientStream = client.GetStream();

                if (!await PerformHandshakeAsync(clientStream, cancellationToken))
                {
                    return;
                }

                TcpClient? directTcpClient = null;
                Renci.SshNet.ForwardedPortLocal? forwardedPort = null;
                Stream? remoteStream = null;

                try
                {
                    var cmdResult = await TryHandleCommandAsync(clientStream, client, cancellationToken);
                    if (!cmdResult.Success)
                    {
                        if (!cmdResult.ReplyAlreadySent)
                        {
                            try { await SendReplyAsync(clientStream, 0x01, "0.0.0.0", 0, cancellationToken); } catch { }
                        }
                        return;
                    }

                    directTcpClient = cmdResult.DirectTcpClient;
                    forwardedPort = cmdResult.ForwardedPort;
                    remoteStream = cmdResult.RemoteStream;

                    using var relayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    try
                    {
                        await RelayDataSimpleAsync(clientStream, remoteStream!, relayCts.Token);
                    }
                    catch { }
                }
                catch
                {
                    // Manejar desconexiones limpias
                }
                finally
                {
                    try
                    {
                        if (client.Client.Connected) client.Client.Shutdown(SocketShutdown.Both);
                    }
                    catch { }

                    if (remoteStream != null)
                    {
                        try { remoteStream.Dispose(); } catch { }
                        remoteStream = null;
                    }
                    if (directTcpClient != null)
                    {
                        try { directTcpClient.Close(); directTcpClient.Dispose(); } catch { }
                        directTcpClient = null;
                    }
                    if (forwardedPort != null && _sshService != null)
                    {
                        try
                        {
                            if (forwardedPort.IsStarted) forwardedPort.Stop();
                            _sshService.RemoveForwardedPort(forwardedPort);
                        }
                        catch { }
                        forwardedPort = null;
                    }
                }
            }
        }
        catch { }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private sealed class SocksCommandResult
    {
        public bool Success { get; set; }
        public bool ReplyAlreadySent { get; set; }
        public TcpClient? DirectTcpClient { get; set; }
        public Renci.SshNet.ForwardedPortLocal? ForwardedPort { get; set; }
        public Stream? RemoteStream { get; set; }
    }

    private async Task<SocksCommandResult> TryHandleCommandAsync(
        NetworkStream clientStream,
        TcpClient client,
        CancellationToken cancellationToken)
    {
        var result = new SocksCommandResult();

        byte[] header = new byte[4];
        await ReadExactAsync(clientStream, header, 0, 4, cancellationToken);

        if (header[0] != 0x05)
            return result;

        byte command = header[1];
        byte addressType = header[3];

        if (command != 0x01)
        {
            await SendReplyAsync(clientStream, 0x07, "0.0.0.0", 0, cancellationToken);
            result.ReplyAlreadySent = true;
            return result;
        }

        string targetHost;
        int targetPort;

        switch (addressType)
        {
            case 0x01: // IPv4
                byte[] ipv4Buf = new byte[6];
                await ReadExactAsync(clientStream, ipv4Buf, 0, 6, cancellationToken);
                targetHost = $"{ipv4Buf[0]}.{ipv4Buf[1]}.{ipv4Buf[2]}.{ipv4Buf[3]}";
                targetPort = (ipv4Buf[4] << 8) | ipv4Buf[5];
                break;

            case 0x03: // Domain Name
                byte[] lenBuf = new byte[1];
                await ReadExactAsync(clientStream, lenBuf, 0, 1, cancellationToken);
                int domainLen = lenBuf[0];

                byte[] domainAndPort = new byte[domainLen + 2];
                await ReadExactAsync(clientStream, domainAndPort, 0, domainLen + 2, cancellationToken);

                targetHost = System.Text.Encoding.ASCII.GetString(domainAndPort, 0, domainLen);
                targetPort = (domainAndPort[domainLen] << 8) | domainAndPort[domainLen + 1];
                break;

            case 0x04: // IPv6
                byte[] ipv6Buf = new byte[18];
                await ReadExactAsync(clientStream, ipv6Buf, 0, 18, cancellationToken);
                byte[] ipBytes = new byte[16];
                Array.Copy(ipv6Buf, 0, ipBytes, 0, 16);
                targetHost = new IPAddress(ipBytes).ToString();
                targetPort = (ipv6Buf[16] << 8) | ipv6Buf[17];
                break;

            default:
                await SendReplyAsync(clientStream, 0x08, "0.0.0.0", 0, cancellationToken);
                result.ReplyAlreadySent = true;
                return result;
        }

        try
        {
            if (_sshService != null && _sshService.IsConnected)
            {
                var (port, stream) = await _sshService.CreateForwardedPortAndStreamAsync(targetHost, targetPort, cancellationToken);
                result.ForwardedPort = port;
                result.RemoteStream = stream;
            }
            else
            {
                result.DirectTcpClient = new TcpClient { NoDelay = true, ReceiveBufferSize = 65536, SendBufferSize = 65536 };
                try { result.DirectTcpClient.LingerState = new LingerOption(false, 0); } catch { }
                await result.DirectTcpClient.ConnectAsync(targetHost, targetPort, cancellationToken);
                result.RemoteStream = result.DirectTcpClient.GetStream();
            }

            await SendReplyAsync(clientStream, 0x00, "0.0.0.0", 0, cancellationToken);
            result.ReplyAlreadySent = true;
            result.Success = true;
            return result;
        }
        catch
        {
            try { await SendReplyAsync(clientStream, 0x04, "0.0.0.0", 0, cancellationToken); } catch { }
            result.ReplyAlreadySent = true;
            return result;
        }
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new System.IO.EndOfStreamException("El cliente SOCKS5 cerró el flujo prematuramente.");
            totalRead += read;
        }
    }

    private async Task<bool> PerformHandshakeAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            byte[] header = new byte[2];
            await ReadExactAsync(stream, header, 0, 2, cancellationToken);

            if (header[0] != 0x05)
                return false;

            int nMethods = header[1];
            byte[] methods = new byte[nMethods];
            await ReadExactAsync(stream, methods, 0, nMethods, cancellationToken);

            byte[] response = new byte[] { 0x05, 0x00 };
            await stream.WriteAsync(response, 0, response.Length, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task SendReplyAsync(NetworkStream stream, byte replyCode, string bindAddr, int bindPort, CancellationToken cancellationToken)
    {
        var addrBytes = IPAddress.Parse(bindAddr).GetAddressBytes();
        byte[] response = new byte[4 + addrBytes.Length + 2];
        response[0] = 0x05;
        response[1] = replyCode;
        response[2] = 0x00;
        response[3] = 0x01;
        Array.Copy(addrBytes, 0, response, 4, addrBytes.Length);
        response[4 + addrBytes.Length] = (byte)((bindPort >> 8) & 0xFF);
        response[4 + addrBytes.Length + 1] = (byte)(bindPort & 0xFF);

        await stream.WriteAsync(response, 0, response.Length, cancellationToken);
    }

    private static async Task RelayDataSimpleAsync(Stream clientStream, Stream remoteStream, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var t1 = CopyStreamAsync(clientStream, remoteStream, cts.Token);
        var t2 = CopyStreamAsync(remoteStream, clientStream, cts.Token);

        await Task.WhenAny(t1, t2).ConfigureAwait(false);

        // Dar 1 segundo para que la otra dirección transmita los datos residuales de carga/descarga antes de cerrar
        cts.CancelAfter(1000);
        try
        {
            await Task.WhenAll(t1, t2).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task CopyStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[65536];
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task StopAsync()
    {
        await EnsureStoppedAsync();
        _settings = null;
        _sshService = null;
    }

    public void Dispose()
    {
        try
        {
            _isRunning = false;
            _cts?.Cancel();
        }
        catch { }
        if (_listener != null)
        {
            try { _listener.Stop(); } catch { }
            try { _listener.Server.Close(); _listener.Server.Dispose(); } catch { }
            _listener = null;
        }
        try { _cts?.Dispose(); } catch { }
        _cts = null;
        try { _concurrencySemaphore.Dispose(); } catch { }
        _settings = null;
        _sshService = null;
        GC.SuppressFinalize(this);
    }
}
