using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface ITunVpnService : IDisposable
{
    bool IsRunning { get; }
    bool IsProcessAlive { get; }
    event Action<string>? OnTunProcessExited;
    Task StartAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task StopAsync();
}
