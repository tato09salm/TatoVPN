using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class LoggerService : ILoggerService
{
    public event Action<string>? OnLog;

    public void Log(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (message.StartsWith("Server Message:\n", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("Server Message:\r\n", StringComparison.OrdinalIgnoreCase))
        {
            OnLog?.Invoke($"[{timestamp}] Server Message:");
            int idx = message.IndexOf('\n');
            string htmlBody = message.Substring(idx + 1);
            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                OnLog?.Invoke(htmlBody);
            }
            return;
        }

        if (message.Contains("<") && message.Contains(">"))
        {
            OnLog?.Invoke(message);
            return;
        }

        if (message.Contains('\n'))
        {
            var lines = message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("["))
                    OnLog?.Invoke(line);
                else
                    OnLog?.Invoke($"[{timestamp}] {line}");
            }
        }
        else
        {
            if (message.StartsWith("["))
                OnLog?.Invoke(message);
            else
                OnLog?.Invoke($"[{timestamp}] {message}");
        }
    }

    public void Clear()
    {
    }
}
