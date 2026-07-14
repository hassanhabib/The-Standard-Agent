using Standard.Agents.Tools;

namespace Standard.Agents.Brokers.Direction;

// Liaison to the in-process tool registry. Holds the registered ITools and runs one
// by name. Which tool to run (and whether it's internal vs external) is the
// Direction orchestration's decision, not the broker's.
public sealed class ToolBroker : IToolBroker
{
    private readonly IReadOnlyDictionary<string, ITool> tools;

    public ToolBroker(IEnumerable<ITool> tools) =>
        this.tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);

    public bool Has(string name) =>
        this.tools.ContainsKey(name);

    public ValueTask<string> RunAsync(string name, string input) =>
        this.tools[name].ExecuteAsync(input);
}
