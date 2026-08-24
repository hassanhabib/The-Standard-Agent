// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;

namespace Standard.Agents.Evals;

// A Brain that answers from the case's script, in order, holding the last reply once the
// script runs out. Deliberately its own copy rather than a reference into the conformance
// harness: the two runners answer different questions and must be free to drift apart.
public sealed class ScriptedGeneratorBroker : IGeneratorBroker
{
    private readonly IReadOnlyList<string> replies;
    private int index;

    public ScriptedGeneratorBroker(IReadOnlyList<string> replies) =>
        this.replies = replies;

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        string reply = this.replies[Math.Min(this.index, this.replies.Count - 1)];
        this.index++;

        return reply;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        yield return await GenerateAsync(systemPrompt, userPrompt);
    }
}
