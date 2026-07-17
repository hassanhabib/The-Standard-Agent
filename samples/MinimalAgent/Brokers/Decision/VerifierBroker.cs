// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Decision;

public sealed class VerifierBroker : IVerifierBroker
{
    public ValueTask<double> VerifyAsync(string systemPrompt, string candidate) =>
        ValueTask.FromResult(1.0);
}
