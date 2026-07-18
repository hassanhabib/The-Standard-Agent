// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Tools;

public sealed class AgentTool : ITool
{
    private const string InputPlaceholder = "{input}";

    private readonly IAgent agent;
    private readonly string handoff;

    public string Name { get; }

    public string Description { get; }

    public string Parameters { get; }

    // The three things that define the sub-agent as a tool (SPEC §6.1): the handoff (what
    // the outer agent tells it to do — a template whose "{input}" is replaced with the
    // string the outer agent supplied), a description (what it does / when to use it), and
    // parameters (a schema of its inputs). Handoff left at its default is exactly "{input}",
    // so the raw input passes through unchanged and the sub-agent behaves as before.
    public AgentTool(
        string name,
        IAgent agent,
        string handoff = InputPlaceholder,
        string description = "",
        string parameters = "{}")
    {
        this.Name = name;
        this.agent = agent;
        this.handoff = handoff;
        this.Description = description;
        this.Parameters = parameters;
    }

    // The nested agent runs its own full loop — its own Recall, Think, Act, its own
    // turn cap, its own guardians. The outer agent sees one tool call and a string
    // back, and cannot tell whether a function or a mind answered it.
    public async ValueTask<string> ExecuteAsync(string input) =>
        await this.agent.ProcessPromptAsync(this.handoff.Replace(InputPlaceholder, input));
}
