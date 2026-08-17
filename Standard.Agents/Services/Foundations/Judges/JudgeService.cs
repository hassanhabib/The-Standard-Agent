// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Foundations.Judges;

namespace Standard.Agents.Services.Foundations.Judges;

public partial class JudgeService : IJudgeService
{
    private readonly IVerifierBroker verifierBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IRedactionBroker redactionBroker;

    public JudgeService(
        IVerifierBroker verifierBroker,
        ILoggingBroker loggingBroker,
        IRedactionBroker? redactionBroker = null)
    {
        this.verifierBroker = verifierBroker;
        this.loggingBroker = loggingBroker;
        this.redactionBroker = redactionBroker ?? new NotConfiguredRedactionBroker();
    }

    public ValueTask<Judgement> EvaluateAsync(string task, string candidate) =>
    TryCatch(async () =>
    {
        ValidateEvaluate(candidate);

        // One vault across both, so the same value redacts to the same token in the task and
        // the answer — otherwise the Judge cannot tell that the answer is about the person the
        // task asked about. The verdict is rehydrated because a rejection's reason quotes the
        // answer back, and that reason becomes the revision feedback the Brain reads (§4.3).
        var vault = new Dictionary<string, string>();

        string redactedTask = this.redactionBroker.Redact(task, vault);
        string redactedCandidate = this.redactionBroker.Redact(candidate, vault);

        string verdict = await this.verifierBroker.VerifyAsync(redactedTask, redactedCandidate);

        Judgement judgement = ParseJudgement(this.redactionBroker.Rehydrate(verdict, vault));

        ValidateScore(judgement.Score);

        return judgement;
    });
}
