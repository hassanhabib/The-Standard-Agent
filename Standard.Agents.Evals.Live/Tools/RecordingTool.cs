// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Evals.Live;

// A tool that answers with a fixed output and remembers being called, so a case can say
// which tools a correct run selects without depending on a real tool's behaviour.
public sealed class RecordingTool : ITool
{
    private readonly string output;
    private readonly List<string> receivedInputs = [];

    public RecordingTool(string name, string output, string description)
    {
        this.Name = name;
        this.output = output;
        this.Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    public string Parameters => "{\"type\":\"object\",\"properties\":{\"input\":{\"type\":\"string\"}}}";

    public IReadOnlyList<string> ReceivedInputs => this.receivedInputs;

    public async ValueTask<string> ExecuteAsync(string input)
    {
        this.receivedInputs.Add(input);

        return this.output;
    }
}
