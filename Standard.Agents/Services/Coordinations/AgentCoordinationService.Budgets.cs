// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService
{
    // Exhaustion and cancellation are reported distinguishably from a refusal. SPEC.md §4.10: a
    // caller that cannot tell "I will not" from "I ran out" cannot decide whether to retry, and
    // a cancelled run's result is not an answer.
    private const string CancelledMessage =
        "The request was cancelled before it completed.";

    private const string TokenBudgetMessage =
        "The token budget for this request was exhausted before it completed.";

    private const string CostBudgetMessage =
        "The cost budget for this request was exhausted before it completed.";

    private const string TimeBudgetMessage =
        "The time budget for this request was exhausted before it completed.";

    private bool IsBudgetExhausted(
        AgentSpend spend,
        DateTimeOffset startedOn,
        out string exhaustion)
    {
        exhaustion = string.Empty;

        if (this.budget is null || this.budget.IsBounded is false)
        {
            return false;
        }

        if (this.budget.MaxTokens is int maxTokens && spend.Tokens >= maxTokens)
        {
            exhaustion = TokenBudgetMessage;

            return true;
        }

        if (this.budget.MaxCostUsd is decimal maxCost
            && spend.CostUsd(this.budget.CostPerThousandTokens) >= maxCost)
        {
            exhaustion = CostBudgetMessage;

            return true;
        }

        TimeSpan elapsed = this.timeBroker.GetCurrentDateTimeOffset() - startedOn;

        if (this.budget.MaxWallClock is TimeSpan maxWallClock && elapsed >= maxWallClock)
        {
            exhaustion = TimeBudgetMessage;

            return true;
        }

        return false;
    }

    private async ValueTask<string> StopAsync(
        AgentContext context,
        string message,
        AgentStatus status)
    {
        await this.loggingBroker.LogOutcomeAsync($"stopped: {message}");

        string unwound = await UnwindAsync();

        return string.IsNullOrEmpty(unwound)
            ? message
            : $"{message} {unwound}";
    }

    // A run that stopped without delivering an answer may have left effects behind. Compensation
    // (SPEC.md §4.9) asks Direction to unwind them, and the caller is told what stood — reporting
    // a run cleanly unwound when it was not is worse than never offering compensation at all.
    private async ValueTask<string> UnwindAsync()
    {
        if (this.compensateOnFailure is false)
        {
            return string.Empty;
        }

        IReadOnlyList<CompensationOutcome> outcomes =
            await this.directionOrchestrationService.CompensateRunAsync();

        if (outcomes.Count is 0)
        {
            return string.Empty;
        }

        int undoneCount = outcomes.Count(outcome => outcome.Undone);
        string report = $"Unwound {undoneCount} of {outcomes.Count} effects.";

        string[] standing =
            [.. outcomes.Where(outcome => outcome.Undone is false).Select(outcome => outcome.Detail)];

        await this.loggingBroker.LogOutcomeAsync($"compensated: {report}");

        return standing.Length is 0
            ? report
            : $"{report} {string.Join(" ", standing)}";
    }
}
