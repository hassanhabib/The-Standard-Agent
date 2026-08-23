// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Managements;

public partial class RunManagementService
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

    // The status was taken and never used: a cancelled or budget-exhausted run reported its
    // message and left how it ended inside this method. Nothing above could tell "I ran out" from
    // an answer, which is the same flattening that let a held sub-agent read like a result.
    private async ValueTask<AgentOutcome> StopAsync(
        AgentContext context,
        string message,
        AgentStatus status)
    {
        await this.loggingBroker.LogOutcomeAsync($"stopped: {message}");

        string unwound = await UnwindAsync();

        string result = string.IsNullOrEmpty(unwound)
            ? message
            : $"{message} {unwound}";

        return new AgentOutcome(result, status);
    }

    // An async iterator cannot put a catch clause around a yield, so the streamed loop unwinds
    // through these instead of through one try around the whole body. They cover the two steps
    // that do not yield — and Direction, the one that performs effects, is among them.
    private async ValueTask<AgentContext> RecallOrUnwindAsync(AgentContext context)
    {
        try
        {
            return await this.dataCoordinationService.RecallAsync(context);
        }
        catch (Exception)
        {
            await UnwindAsync();

            throw;
        }
    }

    private async ValueTask<AgentContext> ActOrUnwindAsync(AgentContext context)
    {
        try
        {
            return await this.directionCoordinationService.ActAsync(context);
        }
        catch (Exception)
        {
            await UnwindAsync();

            throw;
        }
    }

    // Screening asks Decision for a verdict, and a model call can fail like any other — so the
    // streamed loop unwinds through this exactly as it does for Recall and Act above.
    private async ValueTask<AgentContext> ScreenedOrUnwindAsync(
        AgentContext context,
        int observedBefore)
    {
        try
        {
            return await ScreenedAsync(context, observedBefore);
        }
        catch (Exception)
        {
            await UnwindAsync();

            throw;
        }
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
            await this.directionCoordinationService.CompensateRunAsync();

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
