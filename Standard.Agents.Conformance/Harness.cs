// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Tools;

namespace Standard.Agents.Conformance;

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

public sealed class StubSkillBroker : ISkillBroker
{
    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult("You are a test agent.");
}

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

public sealed class AllowingClassifierBroker : IClassifierBroker
{
    public ValueTask<string> ClassifyAsync(string systemPrompt, string input) =>
        ValueTask.FromResult("allow");
}

public sealed class ApprovingVerifierBroker : IVerifierBroker
{
    public ValueTask<double> VerifyAsync(string systemPrompt, string candidate) =>
        ValueTask.FromResult(1.0);
}

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}

public sealed class NullLogBroker : ILogBroker
{
    public string LogPath => string.Empty;

    public ValueTask ResetAsync() =>
        ValueTask.CompletedTask;

    public ValueTask WriteAsync(string content) =>
        ValueTask.CompletedTask;
}

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
