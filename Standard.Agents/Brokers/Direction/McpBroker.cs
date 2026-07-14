namespace Standard.Agents.Brokers.Direction;

// STUB — liaison to external MCP servers / remote APIs. Today reports "not
// configured"; swap the body for a real MCP / REST client.
public sealed class McpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}
