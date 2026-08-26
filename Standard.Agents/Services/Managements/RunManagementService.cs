// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;

using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Generators;
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

    // What the deployment established, snapshotted at composition so precedence can be resolved
    // at the top of each run. Null means the host expressed no opinion — which is exactly what
    // lets a request's value take effect (docs/per-request-inference.md §4.2).
    private readonly string? contractSchema;
    private readonly double? configuredTemperature;
    private readonly int? configuredMaxTokens;

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
        bool screenToolOutput = false,
        string? contractSchema = null,
        double? configuredTemperature = null,
        int? configuredMaxTokens = null)
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
        this.contractSchema = contractSchema;
        this.configuredTemperature = configuredTemperature;
        this.configuredMaxTokens = configuredMaxTokens;
    }

    // Precedence, per field: configured → request → framework default
    // (docs/per-request-inference.md §4). What is established and hard-configured takes
    // precedence, always — a caller can never widen the boundary the deployment set. The
    // request's schema is never merged and never partially honored: one schema survives, and it
    // seeds the wire and the guardian alike (§4.1).
    private ResolvedInference Resolve(PromptRequest request) =>
        new()
        {
            Temperature = this.configuredTemperature
                ?? request.Temperature
                ?? ResolvedInference.DefaultTemperature,

            MaxTokens = this.configuredMaxTokens
                ?? request.MaxTokens
                ?? ResolvedInference.DefaultMaxTokens,
            Seed = request.Seed,
            Stop = request.Stop,

            ResponseSchemaJson = string.IsNullOrWhiteSpace(this.contractSchema)
                ? request.ResponseSchemaJson
                : this.contractSchema,

            CallerTools = request.CallerTools,
            ProviderOptionsJson = request.ProviderOptionsJson
        };

    public ValueTask<string> ProcessPromptAsync(string prompt) =>
        ProcessPromptAsync(prompt, string.Empty, CancellationToken.None);

    public ValueTask<string> ProcessPromptAsync(
        string prompt,
        CancellationToken cancellationToken) =>
        ProcessPromptAsync(prompt, string.Empty, cancellationToken);

    public async ValueTask<string> ProcessPromptAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
        (await RunAsync(prompt, sessionId, cancellationToken)).Result;

    public async ValueTask<string> ProcessPromptAsync(
        PromptRequest request,
        CancellationToken cancellationToken = default) =>
        (await RunAsync(request, cancellationToken)).Result;

    // The same run, reported with how it ended. ProcessPromptAsync projects the answer out of it,
    // because a caller who only wants the string should not have to know there was more — and a
    // caller who nests this agent inside another one cannot do without it (AgentTool).
    //
    // A plain prompt is a request that expressed no opinions — one path, not a simple mode and
    // an advanced one, which is what keeps every control identical on both.
    public ValueTask<AgentOutcome> RunAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
        RunAsync(
            new PromptRequest { Prompt = prompt, SessionId = sessionId },
            cancellationToken);

    private ValueTask<AgentOutcome> RunAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
    TryCatch(async () =>
    {
        ValidatePrompt(request.Prompt);

        string sessionId = request.SessionId;

        // Read before the run begins: a session that never delivered an answer was interrupted,
        // and the next prompt in it continues that run rather than starting a fresh one.
        AgentSession? session = await PeekSessionAsync(sessionId);

        // This prompt's run. SPEC.md §4.4: one instance serves prompts concurrently, and each
        // invocation establishes its own identity, so everything recorded below is credited to
        // this run and to no other.
        using IDisposable run = AgentRun.Begin(ResumedRunId(session));

        await this.loggingBroker.LogResetAsync();

        // Precedence resolved once, at the top of the run, and never again below it: a loop that
        // can re-resolve is a loop where two turns of one run can disagree
        // (docs/per-request-inference.md §2).
        AgentContext context = new()
        {
            Prompt = request.Prompt,
            SessionId = sessionId,
            Inference = Resolve(request)
        };

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

                return new AgentOutcome($"{context.Result} {unwound}".Trim(), context.Status);
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

        return new AgentOutcome(context.Result, context.Status);
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
