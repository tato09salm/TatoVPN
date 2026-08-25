namespace miVPN.Models;

public enum ConnectionResult
{
    Success,
    Failed,
    Cancelled,
    TestOk,
    TestFailed
}

public class ConnectionHistoryEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;

    public string TimestampDisplay => Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
}
