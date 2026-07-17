// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

// The doubles the runner contract calls for (conformance/CONFORMANCE.md step 3).
//
// Every one of these replaces a broker, never a service. That is what makes the
// vectors meaningful: the whole 1-3-9 above the broker line — the loop, the
// interpretation, the routing, the guardians — is the REAL library. Only the
// resources are scripted, so what conformance proves is the agent, not the harness.

// The scripted Brain. Agent behavior involves an LLM and is non-deterministic, so it
// cannot be asserted directly; scripting the Brain makes the deterministic contracts
// assertable.
//
// Repeats the last reply when exhausted — CONFORMANCE.md requires this, and vector 06
// depends on it: one non-terminal reply then becomes an infinite one, which is how a
// never-terminating Brain is simulated.
public sealed class ScriptedGeneratorBroker : IGeneratorBroker
{
    private readonly IReadOnlyList<string> replies;
    private int index;

    public ScriptedGeneratorBroker(IReadOnlyList<string> replies) =>
        this.replies = replies;

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        string reply = this.replies[Math.Min(this.index, this.replies.Count - 1)];
        this.index++;

        return ValueTask.FromResult(reply);
    }
}

// A tool that returns a fixed output regardless of input.
public sealed class StubTool : ITool
{
    private readonly string output;

    public string Name { get; }

    public StubTool(string name, string output)
    {
        this.Name = name;
        this.output = output;
    }

    public ValueTask<string> ExecuteAsync(string input) =>
        ValueTask.FromResult(this.output);
}

// "Skill returns any text" — the scripted Brain ignores it.
public sealed class StubSkillBroker : ISkillBroker
{
    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult("You are a test agent.");
}

// "Memory and Knowledge empty."
public sealed class StubMemoryBroker : IMemoryBroker
{
    public ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}

public sealed class StubKnowledgeBroker : IKnowledgeBroker
{
    public ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}

// "Gate allows." A pass-through Gate is Core-profile behaviour (SPEC.md 8.1) and is
// stated explicitly here rather than shipped as a library default — #50.
public sealed class AllowingClassifierBroker : IClassifierBroker
{
    public ValueTask<string> ClassifyAsync(string systemPrompt, string input) =>
        ValueTask.FromResult("allow");
}

// "Judge returns 1.0."
public sealed class ApprovingVerifierBroker : IVerifierBroker
{
    public ValueTask<double> VerifyAsync(string systemPrompt, string candidate) =>
        ValueTask.FromResult(1.0);
}

// "External reports 'not configured'." It REPORTS rather than throws — vector 05
// routes an unknown tool here and expects the agent to recover, which it can only do
// if the report arrives as data.
public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}

// "Log is a no-op" — conformance runs touch no files.
public sealed class NullLogBroker : ILogBroker
{
    public string LogPath => string.Empty;

    public ValueTask ResetAsync() =>
        ValueTask.CompletedTask;

    public ValueTask WriteAsync(string content) =>
        ValueTask.CompletedTask;
}

// The vector schema — matches conformance/vectors/*.json.
public sealed record Vector(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    Dictionary<string, string>? Tools,
    string Prompt,
    Expectation Expect);

public sealed record Expectation(
    string? Result,
    string? ResultContains);
