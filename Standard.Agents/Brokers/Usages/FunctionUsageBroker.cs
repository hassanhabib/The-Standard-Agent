// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Usages;

// The Custom mode's substrate: a host-supplied counter, for a tokenizer the host already has.
public sealed class FunctionUsageBroker : IUsageBroker
{
    private readonly Func<string, ValueTask<int>> count;

    public FunctionUsageBroker(Func<string, ValueTask<int>> count) =>
        this.count = count;

    public ValueTask<int> CountTokensAsync(string text) =>
        this.count(text);
}
