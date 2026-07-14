namespace Standard.Agents.Brokers.Data;

public interface IMemoryBroker
{
    ValueTask<IReadOnlyList<string>> SelectMemoriesAsync();

    ValueTask InsertMemoryAsync(string memory);
}
