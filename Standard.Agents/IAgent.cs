namespace Standard.Agents;

// An agent takes a prompt and returns an answer. This is also the shape a tool has,
// which is why an agent can be wrapped as a tool (AgentTool) and nested — the fractal.
public interface IAgent
{
    ValueTask<string> ProcessPromptAsync(string prompt);
}
