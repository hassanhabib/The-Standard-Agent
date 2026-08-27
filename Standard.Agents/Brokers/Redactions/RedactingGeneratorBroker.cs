// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Brokers.Redactions;

// Redaction at the wire, by decoration (SPEC.md §4.6).
//
// It used to live inside BrainService, GateService and JudgeService — three foundations each
// holding a second broker, and an invariant that read "every model call is redacted" but was
// really "three services each remembered to". Wrapping the generator instead makes it true by
// construction: the service above holds one broker and does not know redaction exists, and a
// fourth model call added tomorrow cannot forget.
public sealed class RedactingGeneratorBroker : IGeneratorBroker
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly IRedactionBroker redactionBroker;

    public RedactingGeneratorBroker(
        IGeneratorBroker generatorBroker,
        IRedactionBroker redactionBroker)
    {
        this.generatorBroker = generatorBroker;
        this.redactionBroker = redactionBroker;
    }

    // Explicit pass-throughs, never the interface's defaults: a decorator that leaned on the
    // default member would degrade HERE and silently drop the resolved options before the wire.
    public bool HonorsRequest => this.generatorBroker.HonorsRequest;

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        // The vault is per call. Two prompts share it, so the same value redacts to the same
        // token in both and the model can still tell they refer to one thing.
        var vault = new Dictionary<string, string>();

        string reply = await this.generatorBroker.GenerateAsync(
            this.redactionBroker.Redact(systemPrompt, vault),
            this.redactionBroker.Redact(userPrompt, vault));

        return this.redactionBroker.Rehydrate(reply, vault);
    }

    // The inference options travel unredacted: they are knobs and schemas the host or caller
    // wrote, not conversation carrying PII — and a redacted schema would constrain the model to
    // shapes nobody asked for.
    public async ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference)
    {
        var vault = new Dictionary<string, string>();

        string reply = await this.generatorBroker.GenerateAsync(
            this.redactionBroker.Redact(systemPrompt, vault),
            this.redactionBroker.Redact(userPrompt, vault),
            inference);

        return this.redactionBroker.Rehydrate(reply, vault);
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        GenerateStreamAsync(systemPrompt, userPrompt, inference: null, cancellationToken);

    IAsyncEnumerable<string> IGeneratorBroker.GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference,
        CancellationToken cancellationToken) =>
        GenerateStreamAsync(systemPrompt, userPrompt, inference, cancellationToken);

    private async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference? inference,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var vault = new Dictionary<string, string>();

        string redactedSystemPrompt = this.redactionBroker.Redact(systemPrompt, vault);
        string redactedUserPrompt = this.redactionBroker.Redact(userPrompt, vault);

        IAsyncEnumerable<string> tokens = inference is null
            ? this.generatorBroker.GenerateStreamAsync(
                redactedSystemPrompt, redactedUserPrompt, cancellationToken)
            : this.generatorBroker.GenerateStreamAsync(
                redactedSystemPrompt, redactedUserPrompt, inference, cancellationToken);

        var pending = new StringBuilder();

        await foreach (string token in tokens.WithCancellation(cancellationToken))
        {
            if (vault.Count == 0)
            {
                yield return token;

                continue;
            }

            pending.Append(token);
            string ready = DrainReady(pending, vault);

            if (ready.Length > 0)
            {
                yield return ready;
            }
        }

        if (vault.Count > 0 && pending.Length > 0)
        {
            yield return this.redactionBroker.Rehydrate(pending.ToString(), vault);
        }
    }

    // Streaming makes rehydration a boundary problem: a placeholder can arrive split across two
    // tokens, so anything from the last unmatched "{{" onward is held back until more text
    // arrives. Without it a token boundary mid-placeholder leaks "{{EMAIL_" to the caller and
    // never restores the value.
    //
    // This buffering belongs to redaction, which is why it moved here with it. A service that
    // knew nothing about redaction had no business owning it.
    private string DrainReady(StringBuilder pending, IReadOnlyDictionary<string, string> vault)
    {
        string buffer = this.redactionBroker.Rehydrate(pending.ToString(), vault);

        int hold = buffer.LastIndexOf('{');

        while (hold > 0 && buffer[hold - 1] == '{')
        {
            hold--;
        }

        string ready = hold >= 0 ? buffer[..hold] : buffer;
        string keep = hold >= 0 ? buffer[hold..] : string.Empty;

        pending.Clear();
        pending.Append(keep);

        return ready;
    }
}
