// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Evals;

// A tool that answers from the case's script and records that it ran, which is what the
// tool-selection metric is measured against: the tools that actually executed for a prompt
// versus the tools the golden case says the task needs.
public sealed class RecordingTool : ITool
{
    private readonly string output;
    private readonly List<string> receivedInputs = [];

    public string Name { get; }

    public string Description { get; }

    public string Parameters => "{}";

    public IReadOnlyList<string> ReceivedInputs => this.receivedInputs;

    public RecordingTool(string name, string output, string description = "")
    {
        this.Name = name;
        this.output = output;
        this.Description = description;
    }

    public async ValueTask<string> ExecuteAsync(string input)
    {
        this.receivedInputs.Add(input);

        return this.output;
    }
}
