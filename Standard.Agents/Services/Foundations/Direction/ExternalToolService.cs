using Standard.Agents.Brokers.Direction;

namespace Standard.Agents.Services.Foundations.Direction;

// Foundation over ONE broker (MCP). Acts OUTWARD across the boundary — remote tools,
// APIs, other agents/services.
public sealed class ExternalToolService : IExternalToolService
{
    private readonly IMcpBroker mcpBroker;

    public ExternalToolService(IMcpBroker mcpBroker) =>
        this.mcpBroker = mcpBroker;

    public async ValueTask<string> CallAsync(string toolName, string input) =>
        await this.mcpBroker.CallAsync(toolName, input);
}
