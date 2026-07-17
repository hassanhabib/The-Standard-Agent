// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Decision;

// The Gate screens the INPUT — accept, refuse, or route in. Invariant 7.6: a
// guardian must not be the Brain. That is enforced by composition (this takes the
// classifier, never the generator), not by a runtime check.
public partial class GateService : IGateService
{
    private readonly IClassifierBroker classifierBroker;
    private readonly ILoggingBroker loggingBroker;

    public GateService(
        IClassifierBroker classifierBroker,
        ILoggingBroker loggingBroker)
    {
        this.classifierBroker = classifierBroker;
        this.loggingBroker = loggingBroker;
    }

    public async ValueTask<string> ScreenAsync(string gatePrompt, string input) =>
        await this.classifierBroker.ClassifyAsync(gatePrompt, input);
}
