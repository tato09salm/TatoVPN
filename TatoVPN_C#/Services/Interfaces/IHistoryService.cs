using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface IHistoryService
{
    event Action? OnHistoryChanged;
    IReadOnlyList<ConnectionHistoryEntry> GetEntries();
    void AddEntry(ConnectionHistoryEntry entry);
    void Clear();
}
