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

    // The handoff is what the outer agent tells this sub-agent to do: a template whose
    // "{input}" is replaced with the string the outer agent placed after its ACTION.
    // Left at its default it is exactly "{input}", so the raw input passes through
    // unchanged and the sub-agent behaves as before.
    public AgentTool(string name, IAgent agent, string handoff = InputPlaceholder)
    {
        this.Name = name;
        this.agent = agent;
        this.handoff = handoff;
    }

    // The nested agent runs its own full loop — its own Recall, Think, Act, its own
    // turn cap, its own guardians. The outer agent sees one tool call and a string
    // back, and cannot tell whether a function or a mind answered it.
    public async ValueTask<string> ExecuteAsync(string input) =>
        await this.agent.ProcessPromptAsync(input);
}
