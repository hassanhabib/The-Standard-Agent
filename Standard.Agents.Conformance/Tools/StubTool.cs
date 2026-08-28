// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

public sealed class StubTool : ITool
{
    private readonly string output;
    private readonly List<string> receivedInputs = [];
    private readonly bool reversible;
    private readonly List<string>? compensationOrder;
    private readonly RiskLevel risk;
    private readonly bool scopeIsFirstWord;

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
        List<string>? compensationOrder = null,
        RiskLevel risk = RiskLevel.Safe,
        bool scopeIsFirstWord = false,
        string narrationStarting = "",
        string narrationObserved = "",
        string description = "")
    {
        this.Name = name;
        this.output = output;
        this.reversible = reversible;
        this.compensationOrder = compensationOrder;
        this.risk = risk;
        this.scopeIsFirstWord = scopeIsFirstWord;
        this.NarrationStarting = narrationStarting;
        this.NarrationObserved = narrationObserved;
        this.Description = description;
    }

    // Empty by default — a stub is callable but unadvertised, exactly as before. A vector that
    // needs the ADVERTISEMENT observable (selection, §4.15) declares a description, because a
    // description is the opt-in (§6.1).
    public string Description { get; }

    // Declared, like risk and scope: the tool is what knows what its act means in the
    // user's language (SPEC.md §6.0).
    public string NarrationStarting { get; }

    public string NarrationObserved { get; }

    // Declared, because permission is what AND where and only the tool knows either.
    public RiskLevel Risk => this.risk;

    // One named strategy, so a runner in any language implements the same thing: the scope is
    // the first whitespace-separated word of the input - enough to express a path.
    public string ScopeOf(string input) =>
        this.scopeIsFirstWord
            ? input.Split(' ')[0]
            : string.Empty;

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
