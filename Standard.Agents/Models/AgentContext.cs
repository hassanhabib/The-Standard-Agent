namespace Standard.Agents.Models;

// Everything the agent HAS at a point in the loop — Data in flight. Each nature
// reads it and returns an updated copy. One record because all content is Data; the
// regions mark which nature last wrote them.
public sealed record AgentContext
{
    public string Prompt { get; init; } = string.Empty;

    // DATA — Recall
    public string SystemPrompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Observations { get; init; } = [];

    // DECISION — Think
    public string Intent { get; init; } = string.Empty;
    public string DirectionType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string RawReply { get; init; } = string.Empty;

    // DIRECTION — Act
    public string Result { get; init; } = string.Empty;
    public AgentStatus Status { get; init; }
}
