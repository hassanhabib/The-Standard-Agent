// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Classifiers;

public sealed class FunctionClassifierBroker : IClassifierBroker
{
    private readonly Func<string, string, ValueTask<string>> screen;
    private readonly string systemPrompt;

    public FunctionClassifierBroker(
        Func<string, string, ValueTask<string>> screen,
        string systemPrompt)
    {
        this.screen = screen;
        this.systemPrompt = systemPrompt;
    }

    public async ValueTask<string> ClassifyAsync(string input) =>
        await this.screen(this.systemPrompt, input);
}
