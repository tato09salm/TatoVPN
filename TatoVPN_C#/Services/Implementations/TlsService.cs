using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using miVPN.Models;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class TlsService : ITlsService
{
    private readonly ILoggerService _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _bridgeCts;
    private ConnectionSettings? _settings;

    public bool IsEnabled => _listener != null;
    public int LocalBridgePort { get; private set; }

    public TlsService(ILoggerService logger)
    {
        _logger = logger;
    }

    public static SslProtocols ParseSslProtocols(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return SslProtocols.Tls12 | SslProtocols.Tls13;

        return version.Trim().ToLowerInvariant() switch
        {
            "tlsv1.3" or "tls 1.3" or "1.3" => SslProtocols.Tls13,
            "tlsv1.2" or "tls 1.2" or "1.2" => SslProtocols.Tls12,
            "tlsv1.1" or "tls 1.1" or "1.1" => SslProtocols.Tls11,
            "tlsv1" or "tlsv1.0" or "tls 1.0" or "1.0" => SslProtocols.Tls,
            "default" => SslProtocols.Tls12 | SslProtocols.Tls13,
            _ => SslProtocols.Tls12 | SslProtocols.Tls13
        };
    }

    public async Task<int> StartBridgeAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        await ShutdownAsync();

        _settings = settings;
        _bridgeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Bind local loopback on an ephemeral port
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        LocalBridgePort = ((IPEndPoint)_listener.LocalEndpoint).Port;

        var forcedProto = ParseSslProtocols(settings.TlsVersion);
        string protoStr = settings.TlsVersion switch
        {
            "TLSv1.3" => "TLS 1.3 (Forzado)",
            "TLSv1.2" => "TLS 1.2 (Forzado)",
            "TLSv1.1" => "TLS 1.1 (Forzado)",
            "TLSv1" => "TLS 1.0 (Forzado)",
            _ => "Default (TLS 1.2 + TLS 1.3)"
        };

        _ = AcceptBridgeClientsAsync(_listener, _settings, _bridgeCts.Token);

        return LocalBridgePort;
    }

    private async Task AcceptBridgeClientsAsync(TcpListener listener, ConnectionSettings settings, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var localClient = await listener.AcceptTcpClientAsync(token);
                _ = HandleBridgeClientAsync(localClient, settings, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                _logger.Log($"Aviso puente SSL/TLS: {ex.Message}");
            }
        }
    }

    private async Task HandleBridgeClientAsync(TcpClient localClient, ConnectionSettings settings, CancellationToken token)
    {
        using (localClient)
        using (var remoteClient = new TcpClient())
        {
            try
            {
            int targetPort = settings.TlsPort > 0 ? settings.TlsPort : 443;
            var sni = string.IsNullOrWhiteSpace(settings.TlsServerName) ? settings.SshHost : settings.TlsServerName.Trim();
            _logger.Log($"SNI hostname: {sni}");
            _logger.Log("CN=TatoVPN, OU=VPN, O=TatoVPN, L=TatoVPN, ST=Madrid, C=ES");
            _logger.Log("Start SSL handshake");

            await remoteClient.ConnectAsync(settings.SshHost, targetPort, token);

            var sslStream = new SslStream(
                remoteClient.GetStream(),
                false,
                ValidateServerCertificate);

            var protocols = ParseSslProtocols(settings.TlsVersion);

            var authOptions = new SslClientAuthenticationOptions
            {
                TargetHost = sni,
                EnabledSslProtocols = protocols,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                RemoteCertificateValidationCallback = ValidateServerCertificate
            };

            await sslStream.AuthenticateAsClientAsync(authOptions, token);

            _logger.Log($"Established {sslStream.SslProtocol} connection with {settings.SshHost}:{targetPort} using {sslStream.CipherAlgorithm}_{sslStream.CipherStrength}");

            using var localStream = localClient.GetStream();
            using var remoteSsl = sslStream;

            var copyToRemote = localStream.CopyToAsync(remoteSsl, 81920, token);
            var copyToLocal = remoteSsl.CopyToAsync(localStream, 81920, token);

            await Task.WhenAny(copyToRemote, copyToLocal);
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                _logger.Log($"❌ Error en conexión SSL/TLS: {ex.Message}");
            }
        }
        }
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // En túneles SSH sobre SSL/TLS (Stunnel), los servidores suelen usar certificados autofirmados
        return true;
    }

    public async Task ShutdownAsync()
    {
        if (_bridgeCts != null)
        {
            try
            {
                _bridgeCts.Cancel();
                _bridgeCts.Dispose();
            }
            catch { }
            _bridgeCts = null;
        }

        if (_listener != null)
        {
            try
            {
                _listener.Stop();
            }
            catch { }
            _listener = null;
        }

        LocalBridgePort = 0;
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _ = ShutdownAsync();
        GC.SuppressFinalize(this);
    }
}
