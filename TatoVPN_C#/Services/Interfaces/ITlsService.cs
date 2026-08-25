using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface ITlsService : IDisposable
{
    bool IsEnabled { get; }
    int LocalBridgePort { get; }
    Task<int> StartBridgeAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task ShutdownAsync();
}
