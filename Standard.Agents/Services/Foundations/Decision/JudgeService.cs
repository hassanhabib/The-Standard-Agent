// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;

namespace Standard.Agents.Services.Foundations.Decision;

public sealed class JudgeService : IJudgeService
{
    private readonly IVerifierBroker verifierBroker;

    public JudgeService(IVerifierBroker verifierBroker) =>
        this.verifierBroker = verifierBroker;

    public async ValueTask<double> EvaluateAsync(string judgePrompt, string candidate) =>
        await this.verifierBroker.VerifyAsync(judgePrompt, candidate);
}
