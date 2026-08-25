using miVPN.Models;
using Renci.SshNet;

namespace miVPN.Services.Interfaces;

public interface ISshService : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> TestConnectionAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task<Stream> CreateForwardedPortStreamAsync(string host, int port, CancellationToken cancellationToken = default);
    Task<(ForwardedPortLocal? Port, Stream Stream)> CreateForwardedPortAndStreamAsync(string host, int port, CancellationToken cancellationToken = default);
    void RemoveForwardedPort(ForwardedPortLocal port);
    bool IsDynamicPortStarted { get; }
    ForwardedPortDynamic? StartDynamicPortForwarding(string ip, int port);
    void StopDynamicPortForwarding();
}
