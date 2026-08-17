// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;

namespace Standard.Agents.Conformance;

// Wraps the scripted Brain and records everything it is shown, so a vector can certify what a
// model was and was not allowed to see. It wraps rather than replaces, so the real Brain path —
// redaction, rehydration, the streaming placeholder buffer — is what gets certified.
public sealed class RecordingGeneratorBroker : IGeneratorBroker
{
    private readonly IGeneratorBroker inner;
    private readonly Action<string> record;

    public RecordingGeneratorBroker(IGeneratorBroker inner, Action<string> record)
    {
        this.inner = inner;
        this.record = record;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        this.record(systemPrompt);
        this.record(userPrompt);

        return await this.inner.GenerateAsync(systemPrompt, userPrompt);
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.record(systemPrompt);
        this.record(userPrompt);

        IAsyncEnumerable<string> tokens =
            this.inner.GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);

        await foreach (string token in tokens.WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }
}
