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

    // The task travels with the candidate: an answer is good or bad FOR a question, and a verdict
    // on a fit the verifier cannot see is noise dressed as a number (SPEC.md §4.2).
    //
    // Redaction across the pair — one vault, so the same value tokenizes identically in the task
    // and in the answer — is a decoration on the verifier below, applied at composition. This
    // service holds one broker and does not know redaction exists.
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
