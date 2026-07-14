using Standard.Agents.Brokers.Data;

namespace Standard.Agents.Services.Foundations.Data;

// Foundation over ONE broker (the memory store). Persistent memory lives outside the
// agent; this is the only way it enters (Recall) and is written (Remember).
public sealed class MemoryService : IMemoryService
{
    private readonly IMemoryBroker memoryBroker;

    public MemoryService(IMemoryBroker memoryBroker) =>
        this.memoryBroker = memoryBroker;

    public async ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
        await this.memoryBroker.SelectMemoriesAsync();

    public async ValueTask RememberAsync(string memory) =>
        await this.memoryBroker.InsertMemoryAsync(memory);
}
