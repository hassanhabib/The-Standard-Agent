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
    private readonly bool reversible;
    private readonly List<string>? compensationOrder;

    public string Name { get; }

    // A stub returns a fixed output, but it records every input it was handed — so a vector
    // can assert what the tool was actually called with. Without this, a tool that ignores
    // its input hides parsing bugs: corrupted input produces the same output, and the vector
    // still passes (conformance issue #78).
    public IReadOnlyList<string> ReceivedInputs => this.receivedInputs;

    public StubTool(
        string name,
        string output,
        bool reversible = false,
        List<string>? compensationOrder = null)
    {
        this.Name = name;
        this.output = output;
        this.reversible = reversible;
        this.compensationOrder = compensationOrder;
    }

    public async ValueTask<string> ExecuteAsync(string input)
    {
        this.receivedInputs.Add(input);

        return this.output;
    }

    // A stub that was declared reversible reverses itself and writes its name to the shared
    // order, so a vector can certify that the unwind ran in reverse. A stub that was not keeps
    // the interface default, returning null — the case a vector needs to prove is reported as
    // an effect that stands rather than silently counted as undone (SPEC.md §4.9).
    public ValueTask<string?> CompensateAsync(string input, string outcome)
    {
        if (this.reversible is false)
        {
            return ValueTask.FromResult<string?>(null);
        }

        this.compensationOrder?.Add(Name);

        return ValueTask.FromResult<string?>($"reversed {outcome}");
    }
}
