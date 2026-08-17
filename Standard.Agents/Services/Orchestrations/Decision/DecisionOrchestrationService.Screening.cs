// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Loggings;

namespace Standard.Agents.Services.Orchestrations.Decision;

// Screening the same unchanged prompt on every turn is waste, not safety (SPEC.md §4.10).
//
// The prompt does not change across the turns of one run — only the observations do — so the
// Gate was asked the identical question up to MAX_TURNS times and returned the identical verdict,
// at full model cost each time. A seven-turn prompt paid for seven screenings of one string.
//
// This weakens nothing. The guarantee is that the task is screened before the Brain sees it, and
// it still is. What changes every turn is untrusted inbound (§4.9), and that is screened every
// time it appears, because it is different text each time.
//
// The verdict is remembered on the run, not in a service-level cache, so it is evicted by the
// run ending — a cache keyed by run in a long-lived service is a leak waiting for a busy day.
public partial class DecisionOrchestrationService
{
    private async ValueTask<string> ScreenOncePerPromptAsync(string prompt)
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
    }
}
