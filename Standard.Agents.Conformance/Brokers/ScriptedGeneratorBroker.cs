// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;

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

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string reply = this.replies[Math.Min(this.index, this.replies.Count - 1)];
        this.index++;

        await Task.CompletedTask;

        yield return reply;
    }
    }
