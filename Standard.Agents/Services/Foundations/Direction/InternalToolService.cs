using Standard.Agents.Brokers.Direction;

namespace Standard.Agents.Services.Foundations.Direction;

// Foundation over ONE broker (the tool registry). Acts WITHIN the agent's own
// environment — in-process tools.
public sealed class InternalToolService : IInternalToolService
{
    private readonly IToolBroker toolBroker;

    public InternalToolService(IToolBroker toolBroker) =>
        this.toolBroker = toolBroker;

    public bool Handles(string toolName) =>
        this.toolBroker.Has(toolName);

    public async ValueTask<string> RunAsync(string toolName, string input) =>
        await this.toolBroker.RunAsync(toolName, input);
}
