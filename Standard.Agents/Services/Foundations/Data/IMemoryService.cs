namespace Standard.Agents.Services.Foundations.Data;

public interface IMemoryService
{
    ValueTask<IReadOnlyList<string>> RecallMemoriesAsync();

    ValueTask RememberAsync(string memory);
}
