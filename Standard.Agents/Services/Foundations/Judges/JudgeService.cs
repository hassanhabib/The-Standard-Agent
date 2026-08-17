// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Foundations.Judges;

namespace Standard.Agents.Services.Foundations.Judges;

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

    public ValueTask<Judgement> EvaluateAsync(string task, string candidate) =>
    TryCatch(async () =>
    {
        ValidateEvaluate(candidate);

        string verdict = await this.verifierBroker.VerifyAsync(task, candidate);

        Judgement judgement = ParseJudgement(verdict);

        ValidateScore(judgement.Score);

        return judgement;
    });
}
