namespace miVPN.Services.Interfaces;

public interface ILoggerService
{
    event Action<string>? OnLog;
    void Log(string message);
    void Clear();
}
