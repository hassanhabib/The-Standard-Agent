// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Conformance;

// The scripted Brain that OPTS IN (docs/per-request-inference.md §5): it implements the
// request-carrying overloads and records every ResolvedInference it is handed, so a vector can
// certify what actually reached the wire — post-precedence, post-sanitization. Its sibling
// ScriptedGeneratorBroker deliberately does not opt in, which is how graceful degradation is
// certified against the interface's real default members rather than a simulation of them.
public sealed class ScriptedHonoringGeneratorBroker : IGeneratorBroker
{
    private readonly ScriptedGeneratorBroker inner;
    private readonly List<ResolvedInference> inferences = [];

    public ScriptedHonoringGeneratorBroker(
        IReadOnlyList<string> replies,
        int transientFailures = 0) =>
        this.inner = new ScriptedGeneratorBroker(replies, transientFailures);

    public IReadOnlyList<ResolvedInference> Inferences
    {
        get
        {
            lock (this.inferences)
            {
                return [.. this.inferences];
            }
        }
    }

    public bool HonorsRequest => true;

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        this.inner.GenerateAsync(systemPrompt, userPrompt);

    public ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference)
    {
        lock (this.inferences)
        {
            this.inferences.Add(inference);
        }

        return this.inner.GenerateAsync(systemPrompt, userPrompt);
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        this.inner.GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (this.inferences)
        {
            this.inferences.Add(inference);
        }

        IAsyncEnumerable<string> tokens =
            this.inner.GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);

        await foreach (string token in tokens.WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }
}
