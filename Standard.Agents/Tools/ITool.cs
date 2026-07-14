namespace Standard.Agents.Tools;

// The fractal contract. Every effector is a tool: input in, output out. A leaf tool
// (a calculator) satisfies it; a whole agent satisfies it too (via AgentTool).
public interface ITool
{
    string Name { get; }

    ValueTask<string> ExecuteAsync(string input);
}
