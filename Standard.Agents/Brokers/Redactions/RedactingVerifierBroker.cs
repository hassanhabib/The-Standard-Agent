// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Verifiers;

namespace Standard.Agents.Brokers.Redactions;

public sealed class RedactingVerifierBroker : IVerifierBroker
{
    private readonly IVerifierBroker verifierBroker;
    private readonly IRedactionBroker redactionBroker;

    public RedactingVerifierBroker(
        IVerifierBroker verifierBroker,
        IRedactionBroker redactionBroker)
    {
        this.verifierBroker = verifierBroker;
        this.redactionBroker = redactionBroker;
    }

    // One vault across both, so the same value redacts to the same token in the task and in the
    // answer — otherwise the Judge cannot tell that the answer is about the person the task asked
    // about, and it scores a mismatch that is not there.
    //
    // The verdict is rehydrated because a rejection's reason quotes the answer back, and that
    // reason becomes the revision feedback the Brain reads (SPEC.md §4.3).
    public async ValueTask<string> VerifyAsync(string task, string candidate)
    {
        var vault = new Dictionary<string, string>();

        string verdict = await this.verifierBroker.VerifyAsync(
            this.redactionBroker.Redact(task, vault),
            this.redactionBroker.Redact(candidate, vault));

        return this.redactionBroker.Rehydrate(verdict, vault);
    }
}
