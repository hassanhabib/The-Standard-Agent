// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Tools;

// The fractal bridge. Theory Ch.4: "a Direction can be another whole Agent (an agent
// wrapped as a tool). Agents nest inside agents. Turtles up."
//
// This is the whole mechanism, and it is deliberately this small. An agent already
// satisfies "text in, text out"; ITool already asks for exactly that. Nesting needs
// no new machinery because the shapes already agree — which is the fractal being a
// property of the design rather than a feature bolted onto it.
//
// It is also how Theory Ch.8 resolves guardianship at scale: a compliance sub-agent
// is a distinct conscience — "the fractal" — rather than the same brain grading
// itself. And Ch.5: "An agent has one brain. Multiplicity of brains is not an
// intra-agent feature — it is the fractal." Want a second brain? Nest an agent. Do
// not add one to Decision.
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
