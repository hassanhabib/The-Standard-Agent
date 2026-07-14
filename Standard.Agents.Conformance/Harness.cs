// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

// A Brain scripted to return fixed replies in order (repeating the last when
// exhausted). This makes the otherwise non-deterministic Decision deterministic, so
// conformance can assert on the loop, interpretation, and routing.
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

// A skill source that returns fixed text. The scripted Brain ignores it.
public sealed class StubSkillBroker : ISkillBroker
{
    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult("You are a test agent.");
}

// A no-op log broker so conformance runs touch no files.
public sealed class NullLogBroker : ILogBroker
{
    public string LogPath => string.Empty;

    public ValueTask ResetAsync() => ValueTask.CompletedTask;

    public ValueTask WriteAsync(string content) => ValueTask.CompletedTask;
}

// The vector schema (matches conformance/vectors/*.json).
public sealed record Vector(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    Dictionary<string, string>? Tools,
    string Prompt,
    Expectation Expect);

public sealed record Expectation(string? Result, string? ResultContains);
