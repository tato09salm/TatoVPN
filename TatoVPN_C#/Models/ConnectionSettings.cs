namespace miVPN.Models;

public class ConnectionSettings
{
    public string SshHost { get; set; } = string.Empty;
    public int SshPort { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SocksLocalIp { get; set; } = "127.0.0.1";
    public int SocksLocalPort { get; set; } = 1080;
    public bool UseSslTls { get; set; } = false;
    public int TlsPort { get; set; } = 443;
    public string TlsServerName { get; set; } = string.Empty;
    public string TlsVersion { get; set; } = "Default";
    public bool EnableTunMode { get; set; } = true;
}
