namespace Standard.Agents.Brokers.Decision;

public interface IClassifierBroker
{
    ValueTask<string> ClassifyAsync(string systemPrompt, string input);
}
