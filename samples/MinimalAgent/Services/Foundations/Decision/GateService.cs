// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Brokers.Decision;

namespace MinimalAgent.Services.Foundations.Decision;

public sealed class GateService : IGateService
{
    private readonly IClassifierBroker classifierBroker;

    public GateService(IClassifierBroker classifierBroker) =>
        this.classifierBroker = classifierBroker;

    public async ValueTask<string> ScreenAsync(string gatePrompt, string input) =>
        await this.classifierBroker.ClassifyAsync(gatePrompt, input);
}
