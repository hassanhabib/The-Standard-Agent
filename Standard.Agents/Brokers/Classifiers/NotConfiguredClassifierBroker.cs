// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Classifiers;

public sealed class NotConfiguredClassifierBroker : IClassifierBroker
{
    private const string Allow = "allow";
    private const string NoConflict = "NONE";

    public async ValueTask<string> ClassifyAsync(string input) =>
        Allow;

    public async ValueTask<string> AssessAsync(string systemPrompt, string input) =>
        NoConflict;
}
