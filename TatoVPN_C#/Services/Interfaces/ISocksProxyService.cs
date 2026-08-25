using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface ISocksProxyService : IDisposable
{
    bool IsRunning { get; }
    Task StartAsync(ConnectionSettings settings, ISshService sshService, CancellationToken cancellationToken = default);
    Task StopAsync();
}
