// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;

namespace Standard.Agents.Conformance;

public sealed class ScriptedGeneratorBroker : IGeneratorBroker
{
    private readonly IReadOnlyList<string> replies;
    private int index;
    private int remainingTransientFailures;

    public ScriptedGeneratorBroker(
        IReadOnlyList<string> replies,
        int transientFailures = 0)
    {
        this.replies = replies;
        this.remainingTransientFailures = transientFailures;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        // A scripted transient failure, thrown as the category the framework localizes a
        // dependency failure into — so retry is certified on the classification it actually
        // uses, not on a message it happens to match (SPEC.md §4.10).
        if (this.remainingTransientFailures > 0)
        {
            this.remainingTransientFailures--;

            throw new HttpRequestException(
                "scripted transient failure",
                inner: null,
                statusCode: HttpStatusCode.ServiceUnavailable);
        }

        string reply = this.replies[Math.Min(this.index, this.replies.Count - 1)];
        this.index++;

        return reply;
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
