// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Verifiers;

public sealed class FunctionVerifierBroker : IVerifierBroker
{
    private readonly Func<string, string, ValueTask<string>> evaluate;
    private readonly string systemPrompt;

    public FunctionVerifierBroker(
        Func<string, string, ValueTask<string>> evaluate,
        string systemPrompt)
    {
        this.evaluate = evaluate;
        this.systemPrompt = systemPrompt;
    }

    public async ValueTask<string> VerifyAsync(string candidate) =>
        await this.evaluate(this.systemPrompt, candidate);
}
