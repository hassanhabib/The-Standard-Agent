// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Decision;

public sealed class ClassifierBroker : IClassifierBroker
{
    public ValueTask<string> ClassifyAsync(string systemPrompt, string input) =>
        ValueTask.FromResult("allow");
}
