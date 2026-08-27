// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Coordinations.Decision;

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

    // An async iterator cannot put a catch clause around a yield — but the failure points are
    // the awaits, not the yields, so every step of the one loop unwinds through a wrapper that
    // catches around the await and yields outside it. These are the only copies: the loop that
    // used to wrap its whole batched body in one try unwound steps its streamed twin did not.
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

    private async ValueTask<AgentContext> ThoughtOrUnwindAsync(AgentContext context)
    {
        try
        {
            return await this.decisionCoordinationService.ThinkAsync(context);
        }
        catch (Exception)
        {
            await UnwindAsync();

            throw;
        }
    }

    // Think's streamed protocol faults inside an enumeration, so the wrapper advances the
    // enumerator inside the catch and yields outside it — the same rule as everywhere else:
    // a fault in any step of a run that performed effects unwinds before it surfaces.
    private async IAsyncEnumerable<AgentStreamEvent> UnwoundOnFaultAsync(
        IDecisionStream decisionStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using IAsyncEnumerator<AgentStreamEvent> events =
            decisionStream.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            AgentStreamEvent decisionEvent;

            try
            {
                if (await events.MoveNextAsync() is false)
                {
                    yield break;
                }

                decisionEvent = events.Current;
            }
            catch (Exception)
            {
                await UnwindAsync();

                throw;
            }

            yield return decisionEvent;
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
