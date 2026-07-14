namespace Standard.Agents.Tools;

// The fractal bridge: wraps a whole Agent as a tool, so a Direction can be another
// Agent.
public sealed class AgentTool : ITool
{
    private readonly IAgent agent;

    public string Name { get; }

    public AgentTool(string name, IAgent agent)
    {
        this.Name = name;
        this.agent = agent;
    }

    public ValueTask<string> ExecuteAsync(string input) =>
        this.agent.ProcessPromptAsync(input);
}
