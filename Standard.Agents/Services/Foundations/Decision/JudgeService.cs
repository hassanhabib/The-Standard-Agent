// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Decision;

// The Judge screens the OUTPUT. Invariant 7.6 is enforced by composition: this
// takes the verifier and cannot reach the generator, so a draft is never graded by
// the mind that wrote it.
public partial class JudgeService : IJudgeService
{
    private readonly IVerifierBroker verifierBroker;
    private readonly ILoggingBroker loggingBroker;

    public JudgeService(
        IVerifierBroker verifierBroker,
        ILoggingBroker loggingBroker)
    {
        this.verifierBroker = verifierBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<double> EvaluateAsync(string judgePrompt, string candidate) =>
        throw new NotImplementedException();
}
