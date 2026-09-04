namespace miVPN.Enums;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    SshAuthenticated,
    SocksProxyActive,
    Reconnecting,
    Error
}
