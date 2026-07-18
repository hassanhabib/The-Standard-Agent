// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

public sealed class StubTool : ITool
{
    private readonly string output;
    private readonly List<string> receivedInputs = [];

    public string Name { get; }

    // A stub returns a fixed output, but it records every input it was handed — so a vector
    // can assert what the tool was actually called with. Without this, a tool that ignores
    // its input hides parsing bugs: corrupted input produces the same output, and the vector
    // still passes (conformance issue #78).
    public IReadOnlyList<string> ReceivedInputs => this.receivedInputs;

    public StubTool(string name, string output)
    {
        this.Name = name;
        this.output = output;
    }

    public ValueTask<string> ExecuteAsync(string input)
    {
        this.receivedInputs.Add(input);

        return ValueTask.FromResult(this.output);
    }
}
