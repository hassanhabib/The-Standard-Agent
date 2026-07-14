namespace Standard.Agents.Brokers.Direction;

public interface IMcpBroker
{
    ValueTask<string> CallAsync(string name, string input);
}
