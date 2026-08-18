// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;

using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Coordinations.Data;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Coordinations.Direction;

namespace Standard.Agents.Services.Managements;

public partial class RunManagementService : IRunManagementService
{
    private const int DefaultMaxTurns = 7;

    private const string RetriesExhaustedMessage =
        "I can't help with that at the moment.";

    private readonly IDataCoordinationService dataCoordinationService;
    private readonly IDecisionCoordinationService decisionCoordinationService;
    private readonly IDirectionCoordinationService directionCoordinationService;
    private readonly ILoggingBroker loggingBroker;
    private readonly ITimeBroker timeBroker;
    private readonly AgentBudget? budget;

    // What may enter the context between turns is the loop's question. Direction used to answer
    // it by holding Decision's Gate; now the loop asks Decision, which owns it.
    private readonly bool screenToolOutput;
    private readonly int maxHistoryTurns;
    private readonly int maxTurns;
    private readonly bool compensateOnFailure;

    public RunManagementService(
        IDataCoordinationService dataCoordinationService,
        IDecisionCoordinationService decisionCoordinationService,
        IDirectionCoordinationService directionCoordinationService,
        ILoggingBroker loggingBroker,
        int maxTurns = DefaultMaxTurns,
        ITimeBroker? timeBroker = null,
        AgentBudget? budget = null,
        int maxHistoryTurns = 20,
        bool compensateOnFailure = false,
        bool screenToolOutput = false)
    {
        this.compensateOnFailure = compensateOnFailure;
        this.dataCoordinationService = dataCoordinationService;
        this.decisionCoordinationService = decisionCoordinationService;
        this.directionCoordinationService = directionCoordinationService;
        this.loggingBroker = loggingBroker;
        this.maxTurns = maxTurns;
        this.timeBroker = timeBroker ?? new TimeBroker();
        this.budget = budget;
        this.maxHistoryTurns = maxHistoryTurns;
        this.screenToolOutput = screenToolOutput;
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

        // Read before the run begins: a session that never delivered an answer was interrupted,
        // and the next prompt in it continues that run rather than starting a fresh one.
        AgentSession? session = await PeekSessionAsync(sessionId);

        // This prompt's run. SPEC.md §4.4: one instance serves prompts concurrently, and each
        // invocation establishes its own identity, so everything recorded below is credited to
        // this run and to no other.
        using IDisposable run = AgentRun.Begin(ResumedRunId(session));

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt, SessionId = sessionId };

        // The start-of-run checkpoint, written before any work is done (SPEC.md §4.11).
        await BeginSessionAsync(context, session);

        // What was said before, loaded before Decision runs so the Brain sees it (SPEC.md §4.11).
        context = await LoadSessionAsync(context, session);

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
                context = await this.dataCoordinationService.RecallAsync(context);

                await this.loggingBroker.LogStepAsync(AgentStep.Decision);
                context = await this.decisionCoordinationService.ThinkAsync(context);

                spend.AddTokens(context.PromptTokens, context.CompletionTokens);

                if (context.Status is AgentStatus.Revising)
                {
                    await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                    continue;
                }

                await this.loggingBroker.LogStepAsync(AgentStep.Direction);

                int observedBefore = context.Observations.Count;

                context = await this.directionCoordinationService.ActAsync(context);
                context = await ScreenedAsync(context, observedBefore);

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
            await this.dataCoordinationService.RememberAsync(context.Remember);
        }

        // Appended before the call returns, so the next prompt sees it (SPEC.md §4.11).
        await SaveSessionAsync(context, completed: true);

        return context.Result;
    });

    public IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default) =>
        ProcessPromptStreamAsync(prompt, string.Empty, cancellationToken);

    public async IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ValidatePrompt(prompt);

        AgentSession? session = await PeekSessionAsync(sessionId);

        // This prompt's run — see ProcessPromptAsync. A streamed prompt is a run like any other,
        // which is the whole point: every control below is the one the batched loop enforces.
        using IDisposable run = AgentRun.Begin(ResumedRunId(session));

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt, SessionId = sessionId };

        await BeginSessionAsync(context, session);
        context = await LoadSessionAsync(context, session);

        var spend = new AgentSpend();
        DateTimeOffset startedOn = this.timeBroker.GetCurrentDateTimeOffset();
        string? stoppedBecause = null;

        for (int turn = 0; turn < this.maxTurns; turn++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                stoppedBecause = CancelledMessage;

                break;
            }

            if (IsBudgetExhausted(spend, startedOn, out string exhaustion))
            {
                stoppedBecause = exhaustion;

                break;
            }

            await this.loggingBroker.LogTurnAsync(turn);

            await this.loggingBroker.LogStepAsync(AgentStep.Data);
            context = await RecallOrUnwindAsync(context);

            await this.loggingBroker.LogStepAsync(AgentStep.Decision);

            IDecisionStream decisionStream =
                this.decisionCoordinationService.ThinkStreamAsync(context, cancellationToken);

            await foreach (AgentStreamEvent decisionEvent in
                decisionStream.WithCancellation(cancellationToken))
            {
                yield return decisionEvent;
            }

            context = decisionStream.Result;

            spend.AddTokens(context.PromptTokens, context.CompletionTokens);

            if (context.Status is AgentStatus.Revising)
            {
                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                continue;
            }

            await this.loggingBroker.LogStepAsync(AgentStep.Direction);
            context = await ActOrUnwindAsync(context);

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

        // Cancelled or out of budget. Reported as a Status rather than a Response, because it is
        // not an answer — the same distinction the batched path draws by returning the message
        // instead of the result (SPEC.md §4.10).
        if (stoppedBecause is not null)
        {
            await this.loggingBroker.LogOutcomeAsync($"stopped: {stoppedBecause}");

            yield return new AgentStreamEvent(AgentStreamEventType.Status, stoppedBecause);

            context = context with { Status = AgentStatus.Failed };
        }

        if (context.Status is AgentStatus.Revising)
        {
            yield return new AgentStreamEvent(
                AgentStreamEventType.Status,
                "unable to satisfy review after retries; refusing");

            yield return new AgentStreamEvent(
                AgentStreamEventType.Response,
                RetriesExhaustedMessage);

            context = context with
            {
                Result = RetriesExhaustedMessage,
                Status = AgentStatus.Refused
            };
        }

        // The streamed loop unwinds on the same terms as the batched one. A control enforced on
        // one path and not the other is a control a caller can step around by changing method.
        if (context.Status is AgentStatus.Working or AgentStatus.Failed)
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
            await this.dataCoordinationService.RememberAsync(context.Remember);
        }

        // Recorded on the same terms as the batched path: only a run that delivered an answer.
        // A streamed answer that never joined the conversation would leave the next prompt
        // blind to it, and a cancelled one recorded would tell it something that never happened.
        await SaveSessionAsync(context, completed: stoppedBecause is null);
    }
}
