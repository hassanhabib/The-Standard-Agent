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

    public ValueTask<string> ExecuteAsync(string input) =>
        this.agent.ProcessPromptAsync(input);
}
