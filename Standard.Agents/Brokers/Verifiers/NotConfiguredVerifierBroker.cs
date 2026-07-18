// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;

namespace Standard.Agents.Brokers.Verifiers;

public sealed class NotConfiguredVerifierBroker : IVerifierBroker
{
    private static readonly string PassingScore =
        (1.0).ToString(CultureInfo.InvariantCulture);

    public ValueTask<string> VerifyAsync(string candidate) =>
        ValueTask.FromResult(PassingScore);
}
