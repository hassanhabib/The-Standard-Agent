// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.Judges;

namespace Standard.Agents.Services.Orchestrations.Decision.Guardians;

public partial class GuardianOrchestrationService : IGuardianOrchestrationService
{
    private readonly IGateService gateService;
    private readonly IJudgeService judgeService;
    private readonly ILoggingBroker loggingBroker;

    public GuardianOrchestrationService(
        IGateService gateService,
        IJudgeService judgeService,
        ILoggingBroker loggingBroker)
    {
        this.gateService = gateService;
        this.judgeService = judgeService;
        this.loggingBroker = loggingBroker;
    }

    // The prompt does not change across the turns of one run - only the observations do - so the
    // Gate was being asked the identical question up to MaxTurns times and returning the identical
    // verdict, at full model cost each time.
    //
    // This weakens nothing. The guarantee is that the task is screened before the Brain sees it,
    // and it still is. What changes every turn is untrusted inbound (SPEC.md 4.9), and that is
    // screened every time it appears, because it is different text each time.
    //
    // The verdict is remembered on the RUN, not in a service-level cache, so it is evicted by the
    // run ending - a cache keyed by run in a long-lived service is a leak waiting for a busy day.
    public ValueTask<string> ScreenAsync(string prompt) =>
    TryCatch(async () =>
    {
        AgentRun? run = AgentRun.Current;

        if (run is null)
        {
            return await this.gateService.ScreenAsync(prompt);
        }

        if (run.TryGetVerdict(prompt, out string cachedVerdict))
        {
            return cachedVerdict;
        }

        string verdict = await this.gateService.ScreenAsync(prompt);
        run.RememberVerdict(prompt, verdict);

        return verdict;
    });

    public ValueTask<string> DetectConflictAsync(string instructions) =>
        TryCatch(async () => await this.gateService.DetectConflictAsync(instructions));

    public ValueTask<Judgement> EvaluateAsync(string task, string candidate) =>
        TryCatch(async () => await this.judgeService.EvaluateAsync(task, candidate));
}
