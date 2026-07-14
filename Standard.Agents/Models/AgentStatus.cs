namespace Standard.Agents.Models;

// The agent's lifecycle state. Working means keep looping; every other value is a
// terminal Direction — a reason the loop ended.
public enum AgentStatus
{
    Working,
    Responded,
    AwaitingInput,
    Refused,
    Failed
}
