// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Classifiers;

namespace Standard.Agents.Conformance;

public sealed class AllowingClassifierBroker : IClassifierBroker
{
    public ValueTask<string> ClassifyAsync(string input) =>
        ValueTask.FromResult("allow");
}
