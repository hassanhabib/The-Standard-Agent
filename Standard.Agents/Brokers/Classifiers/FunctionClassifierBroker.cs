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

    public ValueTask<string> ClassifyAsync(string input) =>
        AssessAsync(this.systemPrompt, input);

    public async ValueTask<string> AssessAsync(string systemPrompt, string input) =>
        await this.screen(systemPrompt, input);
}
