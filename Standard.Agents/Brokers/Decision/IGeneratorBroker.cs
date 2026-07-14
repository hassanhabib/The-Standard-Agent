namespace Standard.Agents.Brokers.Decision;

public interface IGeneratorBroker
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);
}
