// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService : IAgentCoordinationService
{
    private const int DefaultMaxTurns = 7;

    private const string RetriesExhaustedMessage =
        "I can't help with that at the moment.";

    private readonly IDataOrchestrationService dataOrchestrationService;
    private readonly IDecisionOrchestrationService decisionOrchestrationService;
    private readonly IDirectionOrchestrationService directionOrchestrationService;
    private readonly ILoggingBroker loggingBroker;
    private readonly ITimeBroker timeBroker;
    private readonly AgentBudget? budget;
    private readonly ISessionBroker sessionBroker;
    private readonly int maxHistoryTurns;
    private readonly int maxTurns;
    private readonly bool compensateOnFailure;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestrationService,
        IDecisionOrchestrationService decisionOrchestrationService,
        IDirectionOrchestrationService directionOrchestrationService,
        ILoggingBroker loggingBroker,
        int maxTurns = DefaultMaxTurns,
        ITimeBroker? timeBroker = null,
        AgentBudget? budget = null,
        ISessionBroker? sessionBroker = null,
        int maxHistoryTurns = 20,
        bool compensateOnFailure = false)
    {
        this.compensateOnFailure = compensateOnFailure;
        this.dataOrchestrationService = dataOrchestrationService;
        this.decisionOrchestrationService = decisionOrchestrationService;
        this.directionOrchestrationService = directionOrchestrationService;
        this.loggingBroker = loggingBroker;
        this.maxTurns = maxTurns;
        this.timeBroker = timeBroker ?? new TimeBroker();
        this.budget = budget;
        this.sessionBroker = sessionBroker ?? new NotConfiguredSessionBroker();
        this.maxHistoryTurns = maxHistoryTurns;
    }

    public ValueTask<string> ProcessPromptAsync(string prompt) =>
        ProcessPromptAsync(prompt, string.Empty, CancellationToken.None);

    public ValueTask<string> ProcessPromptAsync(
        string prompt,
        CancellationToken cancellationToken) =>
        ProcessPromptAsync(prompt, string.Empty, cancellationToken);

    public ValueTask<string> ProcessPromptAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
    TryCatch(async () =>
    {
        ValidatePrompt(prompt);

        // This prompt's run. SPEC.md §4.4: one instance serves prompts concurrently, and each
        // invocation establishes its own identity, so everything recorded below is credited to
        // this run and to no other.
        using IDisposable run = AgentRun.Begin();

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt, SessionId = sessionId };

        // What was said before, loaded before Decision runs so the Brain sees it (SPEC.md §4.11).
        context = await LoadSessionAsync(context);

        // Budgets and cancellation are both checked at the turn boundary (SPEC.md §4.10): a turn
        // is the smallest unit the loop can stop between without abandoning work mid-flight —
        // in particular without leaving an effect half-recorded.
        var spend = new AgentSpend();
        DateTimeOffset startedOn = this.timeBroker.GetCurrentDateTimeOffset();

        try
        {
            for (int turn = 0; turn < this.maxTurns; turn++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await StopAsync(context, CancelledMessage, AgentStatus.Failed);
                }

                if (IsBudgetExhausted(spend, startedOn, out string exhaustion))
                {
                    return await StopAsync(context, exhaustion, AgentStatus.Failed);
                }

                await this.loggingBroker.LogTurnAsync(turn);

                await this.loggingBroker.LogStepAsync(AgentStep.Data);
                context = await this.dataOrchestrationService.RecallAsync(context);

                await this.loggingBroker.LogStepAsync(AgentStep.Decision);
                context = await this.decisionOrchestrationService.ThinkAsync(context);

                spend.AddTokens(context.PromptTokens, context.CompletionTokens);

                if (context.Status is AgentStatus.Revising)
                {
                    await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                    continue;
                }

                await this.loggingBroker.LogStepAsync(AgentStep.Direction);
                context = await this.directionOrchestrationService.ActAsync(context);

                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: {context.Status}");

                if (context.Status != AgentStatus.Working)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // A run that faulted mid-flight is the case compensation exists for: the effects it
            // already performed are real, and nothing else will unwind them (SPEC.md §4.9).
            await UnwindAsync();

            throw;
        }

        // Turns ran out with the loop still Working: effects may have been performed and no answer
        // was ever delivered, which is a failed run however calmly it ended.
        if (context.Status is AgentStatus.Working)
        {
            string unwound = await UnwindAsync();

            if (string.IsNullOrEmpty(unwound) is false)
            {
                await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

                return $"{context.Result} {unwound}".Trim();
            }
        }

        if (context.Status is AgentStatus.Revising)
        {
            context = context with
            {
                Result = RetriesExhaustedMessage,
                Status = AgentStatus.Refused
            };
        }

        await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

        if (string.IsNullOrEmpty(context.Remember) is false)
        {
            await this.dataOrchestrationService.RememberAsync(context.Remember);
        }

        // Appended before the call returns, so the next prompt sees it (SPEC.md §4.11).
        await SaveSessionAsync(context, completed: true);

        return context.Result;
    });

    public async IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidatePrompt(prompt);

        // This prompt's run — see ProcessPromptAsync. A streamed prompt is a run like any other.
        using IDisposable run = AgentRun.Begin();

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 0; turn < this.maxTurns; turn++)
        {
            await this.loggingBroker.LogTurnAsync(turn);

            await this.loggingBroker.LogStepAsync(AgentStep.Data);
            context = await this.dataOrchestrationService.RecallAsync(context);

            await this.loggingBroker.LogStepAsync(AgentStep.Decision);

            IDecisionStream decisionStream =
                this.decisionOrchestrationService.ThinkStreamAsync(context, cancellationToken);

            await foreach (AgentStreamEvent decisionEvent in
                decisionStream.WithCancellation(cancellationToken))
            {
                yield return decisionEvent;
            }

            context = decisionStream.Result;

            if (context.Status is AgentStatus.Revising)
            {
                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                continue;
            }

            await this.loggingBroker.LogStepAsync(AgentStep.Direction);
            context = await this.directionOrchestrationService.ActAsync(context);

            await this.loggingBroker.LogOutcomeAsync($"turn {turn}: {context.Status}");

            if (context.Status is AgentStatus.Working
                && string.IsNullOrEmpty(context.Result) is false)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Tool,
                    $"{context.DirectionType}: {context.Result}");
            }

            if (context.Status is AgentStatus.Responded
                or AgentStatus.Refused
                or AgentStatus.AwaitingInput
                && string.IsNullOrEmpty(context.Result) is false)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Response,
                    context.Result);
            }

            if (context.Status is not AgentStatus.Working)
            {
                break;
            }
        }

        if (context.Status is AgentStatus.Revising)
        {
            yield return new AgentStreamEvent(
                AgentStreamEventType.Status,
                "unable to satisfy review after retries; refusing");

            yield return new AgentStreamEvent(
                AgentStreamEventType.Response,
                RetriesExhaustedMessage);
        }

        // The streamed loop unwinds on the same terms as the batched one. A control enforced on
        // one path and not the other is a control a caller can step around by changing method.
        if (context.Status is AgentStatus.Working)
        {
            string unwound = await UnwindAsync();

            if (string.IsNullOrEmpty(unwound) is false)
            {
                yield return new AgentStreamEvent(AgentStreamEventType.Status, unwound);
            }
        }

        await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

        if (string.IsNullOrEmpty(context.Remember) is false)
        {
            await this.dataOrchestrationService.RememberAsync(context.Remember);
        }
    }
}
