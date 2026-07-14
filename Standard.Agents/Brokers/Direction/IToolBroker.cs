namespace Standard.Agents.Brokers.Direction;

public interface IToolBroker
{
    bool Has(string name);

    ValueTask<string> RunAsync(string name, string input);
}
