namespace Standard.Agents.Brokers.Data;

// STUB — the durable, external memory store (SQLite / JSON / Redis / vector).
// Today a no-op so the tier is wired end to end; swap the body for a real store.
public sealed class MemoryBroker : IMemoryBroker
{
    public ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}
