// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Classifiers;

public sealed class NotConfiguredClassifierBroker : IClassifierBroker
{
    private const string Allow = "allow";

    public ValueTask<string> ClassifyAsync(string input) =>
        ValueTask.FromResult(Allow);
}
