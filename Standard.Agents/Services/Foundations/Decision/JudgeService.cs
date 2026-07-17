// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Decision;

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
    TryCatch(async () =>
    {
        ValidateEvaluate(judgePrompt, candidate);

        double score = await this.verifierBroker.VerifyAsync(judgePrompt, candidate);

        ValidateScore(score);

        return score;
    });
}
