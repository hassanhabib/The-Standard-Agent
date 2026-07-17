// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Verifiers;

namespace Standard.Agents.Conformance;

public sealed class ApprovingVerifierBroker : IVerifierBroker
{
    public ValueTask<string> VerifyAsync(string candidate) =>
        ValueTask.FromResult("1.0");
    }
