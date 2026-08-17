// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Classifiers;

namespace Standard.Agents.Brokers.Redactions;

// The Gate screens the raw task, so it sees exactly what redaction exists to hide, and it may run
// on a different host than the Brain (SPEC.md §4.6).
//
// The verdict is rehydrated on the way back: a classification carries no data, but a refusal
// reason quotes the input, and that reason is read by a human.
public sealed class RedactingClassifierBroker : IClassifierBroker
{
    private readonly IClassifierBroker classifierBroker;
    private readonly IRedactionBroker redactionBroker;

    public RedactingClassifierBroker(
        IClassifierBroker classifierBroker,
        IRedactionBroker redactionBroker)
    {
        this.classifierBroker = classifierBroker;
        this.redactionBroker = redactionBroker;
    }

    public async ValueTask<string> ClassifyAsync(string input)
    {
        var vault = new Dictionary<string, string>();

        string verdict = await this.classifierBroker.ClassifyAsync(
            this.redactionBroker.Redact(input, vault));

        return this.redactionBroker.Rehydrate(verdict, vault);
    }

    public async ValueTask<string> AssessAsync(string systemPrompt, string input)
    {
        var vault = new Dictionary<string, string>();

        string verdict = await this.classifierBroker.AssessAsync(
            systemPrompt,
            this.redactionBroker.Redact(input, vault));

        return this.redactionBroker.Rehydrate(verdict, vault);
    }
}
