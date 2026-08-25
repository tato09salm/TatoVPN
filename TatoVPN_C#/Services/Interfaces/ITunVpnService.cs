using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface ITunVpnService : IDisposable
{
    bool IsRunning { get; }
    Task StartAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task StopAsync();
}
