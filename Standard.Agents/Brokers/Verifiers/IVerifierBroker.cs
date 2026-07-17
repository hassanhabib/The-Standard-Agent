// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Verifiers;

public interface IVerifierBroker
{
    ValueTask<double> VerifyAsync(string systemPrompt, string candidate);
}
