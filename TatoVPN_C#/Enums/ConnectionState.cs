namespace miVPN.Enums;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    SshAuthenticated,
    SocksProxyActive,
    Error
}
