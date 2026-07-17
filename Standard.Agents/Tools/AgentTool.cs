// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Tools;

public sealed class AgentTool : ITool
{
    private readonly IAgent agent;

    public string Name { get; }

    public AgentTool(string name, IAgent agent)
    {
        this.Name = name;
        this.agent = agent;
    }

    // The nested agent runs its own full loop — its own Recall, Think, Act, its own
    // turn cap, its own guardians. The outer agent sees one tool call and a string
    // back, and cannot tell whether a function or a mind answered it.
    public async ValueTask<string> ExecuteAsync(string input) =>
        await this.agent.ProcessPromptAsync(input);
    }
