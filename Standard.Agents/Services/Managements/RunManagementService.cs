// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;

using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
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

    private const string TurnsExhaustedMessage =
        "I ran out of turns before an answer was ready; nothing was delivered.";

    private readonly IDataCoordinationService dataCoordinationService;
    private readonly IDecisionCoordinationService decisionCoordinationService;
    private readonly IDirectionCoordinationService directionCoordinationService;
    private readonly ILoggingBroker loggingBroker;
    private readonly ITimeBroker timeBroker;
    private readonly ITelemetryBroker telemetryBroker;
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
    private readonly HashSet<string> configuredToolNames;

    // What the tools declared they say (ITool.NarrationStarting/Observed), keyed by tool name —
    // derived data, like risk and scope, so the loop can voice acts without holding the tools.
    private readonly IReadOnlyDictionary<string, ToolNarration> toolNarrations;

    // Selection (SPEC.md §4.15): the host's judgment of what a run is OFFERED, and the described
    // names it judges over. Null selector = no selection = every described tool offered.
    private readonly ToolSelector? toolSelector;
    private readonly IReadOnlyList<string> describedToolNames;

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
        ITelemetryBroker? telemetryBroker = null,
        string? contractSchema = null,
        double? configuredTemperature = null,
        int? configuredMaxTokens = null,
        IEnumerable<string>? configuredToolNames = null,
        IReadOnlyDictionary<string, ToolNarration>? toolNarrations = null,
        ToolSelector? toolSelector = null,
        IEnumerable<string>? describedToolNames = null)
    {
        this.compensateOnFailure = compensateOnFailure;
        this.dataCoordinationService = dataCoordinationService;
        this.decisionCoordinationService = decisionCoordinationService;
        this.directionCoordinationService = directionCoordinationService;
        this.loggingBroker = loggingBroker;
        this.maxTurns = maxTurns;
        this.timeBroker = timeBroker ?? new TimeBroker();
        this.telemetryBroker = telemetryBroker ?? new NotConfiguredTelemetryBroker();
        this.budget = budget;
        this.maxHistoryTurns = maxHistoryTurns;
        this.screenToolOutput = screenToolOutput;
        this.contractSchema = contractSchema;
        this.configuredTemperature = configuredTemperature;
        this.configuredMaxTokens = configuredMaxTokens;

        this.configuredToolNames = new HashSet<string>(
            configuredToolNames ?? [], StringComparer.OrdinalIgnoreCase);

        this.toolNarrations = toolNarrations
            ?? new Dictionary<string, ToolNarration>(StringComparer.OrdinalIgnoreCase);

        this.toolSelector = toolSelector;
        this.describedToolNames = [.. describedToolNames ?? []];
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

            CallerTools = WithoutConfiguredNames(request.CallerTools),

            // Sanitized here, at the boundary, so no broker — built-in or third-party — can
            // ever be handed a passthrough carrying a core-owned key (§4.4).
            ProviderOptionsJson = ProviderOptions.Sanitize(request.ProviderOptionsJson).Json
        };

    // Say so in the trace (docs/per-request-inference.md §4.3): a rejection the trace does not
    // explain is a turn nobody can account for, and a caller whose schema was discarded or whose
    // passthrough key was stripped deserves the same courtesy the guardians already extend.
    private async ValueTask AnnounceResolutionAsync(PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(this.contractSchema) is false
            && string.IsNullOrWhiteSpace(request.ResponseSchemaJson) is false)
        {
            await this.loggingBroker.LogProcessAsync(
                "Run",
                "Contract → request schema discarded; the configured Contract wins");
        }

        SanitizedProviderOptions sanitized =
            ProviderOptions.Sanitize(request.ProviderOptionsJson);

        if (sanitized.Malformed)
        {
            await this.loggingBroker.LogProcessAsync(
                "Run", "Provider options → ignored: not a JSON object");
        }

        if (sanitized.StrippedKeys.Count > 0)
        {
            await this.loggingBroker.LogProcessAsync(
                "Run",
                "Provider options → stripped core-owned key(s): "
                    + string.Join(", ", sanitized.StrippedKeys));
        }

        foreach (ToolDefinition tool in request.CallerTools)
        {
            if (this.configuredToolNames.Contains(tool.Name))
            {
                await this.loggingBroker.LogProcessAsync(
                    "Run",
                    $"Caller tools → '{tool.Name}' dropped; a configured tool owns that name");
            }
        }
    }

    // Name collision resolved by the perimeter rule (design §6.1): a caller tool that shares a
    // configured tool's name is dropped, so a tool_call carrying that name has exactly one
    // meaning — the configured tool, executed locally, under every configured control. Dropped
    // HERE, before Direction ever reads the list, because a foreign classification downstream
    // would otherwise let a caller shadow the deployment's own tool. MCP tool names are not
    // known at composition; a collision with one degrades toward the caller — the agent then
    // performs nothing, which is the safe direction to fail in.
    private IReadOnlyList<ToolDefinition> WithoutConfiguredNames(
        IReadOnlyList<ToolDefinition> callerTools) =>
        callerTools.Count == 0
            ? callerTools
            : [.. callerTools.Where(tool =>
                this.configuredToolNames.Contains(tool.Name) is false)];

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

    public async ValueTask<string> ProcessPromptAsync(PromptRequest request) =>
        (await RunAsync(request, CancellationToken.None)).Result;

    public async ValueTask<string> ProcessPromptAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
        (await RunAsync(request, cancellationToken)).Result;

    // The batched projection of the one loop: drain the events, keep how it ended.
    // ProcessPromptAsync projects the answer out of it, because a caller who only wants the
    // string should not have to know there was more — and a caller who nests this agent inside
    // another one cannot do without it (AgentTool).
    // A plain prompt is a request that expressed no opinions — one path, not a simple mode
    // and an advanced one, which is what keeps every control identical on both.
    public ValueTask<AgentOutcome> RunAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
        RunAsync(
            new PromptRequest { Prompt = prompt, SessionId = sessionId },
            cancellationToken);

    public ValueTask<AgentOutcome> RunAsync(PromptRequest request) =>
        RunAsync(request, CancellationToken.None);

    public ValueTask<AgentOutcome> RunAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
    TryCatch(async () =>
    {
        AgentOutcome outcome = new(string.Empty, AgentStatus.Failed);

        await foreach (AgentStreamEvent _ in RunCoreAsync(
            request,
            streaming: false,
            setOutcome: ended => outcome = ended,
            cancellationToken))
        {
        }

        return outcome;
    });

    public IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default) =>
        ProcessPromptStreamAsync(prompt, string.Empty, cancellationToken);

    // The streamed projection of the same loop, with the same exception mapping the batched
    // door gets from TryCatch. An async iterator cannot put a catch around a yield — but the
    // failure points are the awaits, not the yields, so the enumeration advances inside the
    // catch and yields outside it. Cancellation passes through unmapped: the caller asked the
    // run to stop and gets the stop, not a service error.
    public IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
        ProcessPromptStreamAsync(
            new PromptRequest { Prompt = prompt, SessionId = sessionId },
            cancellationToken);

    public IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        PromptRequest request) =>
        ProcessPromptStreamAsync(request, CancellationToken.None);

    public IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
        MappedStreamAsync(request, setOutcome: _ => { }, cancellationToken);

    // The third reading of the one loop (SPEC.md §4.14): every event live, and the outcome the
    // batched door returns. RunCoreAsync has always computed it on this door too — this routine
    // simply stops discarding it, so a caller never chooses between the answer's structure and
    // the run's story.
    public AgentRunStream RunStreamAsync(PromptRequest request) =>
        RunStreamAsync(request, CancellationToken.None);

    public AgentRunStream RunStreamAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
        new(setOutcome => MappedStreamAsync(request, setOutcome, cancellationToken));

    private async IAsyncEnumerable<AgentStreamEvent> MappedStreamAsync(
        PromptRequest request,
        Action<AgentOutcome> setOutcome,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using IAsyncEnumerator<AgentStreamEvent> events =
            RunCoreAsync(
                request,
                streaming: true,
                setOutcome,
                cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            AgentStreamEvent streamed;

            try
            {
                if (await events.MoveNextAsync() is false)
                {
                    break;
                }

                streamed = events.Current;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw await MappedAndLoggedAsync(exception);
            }

            yield return streamed;
        }
    }

    // The seam between the one loop and its two doors. The loop itself (PumpRunAsync below) is
    // a plain async method, NOT an iterator, for a reason found the hard way: an async
    // iterator's execution context does not survive a yield boundary, so an AsyncLocal set
    // inside one — the ambient AgentRun that run-once keys, approval grants, compensation and
    // record attribution all hang off — silently reverts to null after the first event. The old
    // streamed loop had exactly that shape, which means those controls were quietly detached
    // from the run identity between events; unifying the loops surfaced it instantly, because
    // the batched tests began exercising the shared code. A plain method's context flows across
    // its whole body, so the pump owns the run and writes events to a channel; this iterator
    // only reads and yields.
    private async IAsyncEnumerable<AgentStreamEvent> RunCoreAsync(
        PromptRequest request,
        bool streaming,
        Action<AgentOutcome> setOutcome,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var events = System.Threading.Channels.Channel.CreateBounded<AgentStreamEvent>(1);

        // Fired only when the caller abandons the enumeration, so the pump is not left awaiting
        // a write nobody will read. Deliberately NOT linked to the caller's token: a cancelled
        // RUN still owes its final Status events — cancellation stops the loop at the turn
        // boundary, and the reader stays open to deliver the stop.
        using var abandoned = new CancellationTokenSource();

        Task pump = PumpRunAsync(
            request,
            streaming,
            events.Writer,
            setOutcome,
            cancellationToken,
            abandoned.Token);

        try
        {
            // Never cancelled directly: a cancelled RUN still owes the caller its final Status
            // events, so the reader stays open until the pump completes the channel.
            await foreach (AgentStreamEvent streamEvent in
                events.Reader.ReadAllAsync(CancellationToken.None))
            {
                yield return streamEvent;
            }

            await pump;
        }
        finally
        {
            abandoned.Cancel();

            try
            {
                await pump;
            }
            catch
            {
                // The pump's fault already surfaced through the channel to the enumeration
                // above; this await only prevents an abandoned pump from going unobserved.
            }
        }
    }

    // THE loop — deliberately the only copy (SPEC.md §7.6). Six controls were found enforced on
    // one door and not the other, and every one had been introduced by editing one loop and not
    // its twin: two loops that must agree is a discipline, one loop is a fact. Exactly two
    // things differ between the doors, and both are named here — which Brain protocol Decision
    // drives (a host's generator sees the same calls it always saw on each door), and whether
    // anybody reads the events (the batched caller drains them).
    private async Task PumpRunAsync(
        PromptRequest request,
        bool streaming,
        System.Threading.Channels.ChannelWriter<AgentStreamEvent> events,
        Action<AgentOutcome> setOutcome,
        CancellationToken cancellationToken,
        CancellationToken abandoned)
    {
        try
        {
            await RunTheLoopAsync(
                request, streaming, events, setOutcome, cancellationToken, abandoned);

            events.TryComplete();
        }
        catch (Exception exception)
        {
            events.TryComplete(exception);

            throw;
        }
    }

    private async Task RunTheLoopAsync(
        PromptRequest request,
        bool streaming,
        System.Threading.Channels.ChannelWriter<AgentStreamEvent> events,
        Action<AgentOutcome> setOutcome,
        CancellationToken cancellationToken,
        CancellationToken abandoned)
    {
        ValidatePrompt(request.Prompt);

        string prompt = request.Prompt;
        string sessionId = request.SessionId;

        // Read before the run begins: a session that never delivered an answer was interrupted,
        // and the next prompt in it continues that run rather than starting a fresh one.
        AgentSession? session = await PeekSessionAsync(sessionId);

        // This prompt's run. SPEC.md §4.4: one instance serves prompts concurrently, and each
        // invocation establishes its own identity, so everything recorded below is credited to
        // this run and to no other.
        using IDisposable run = AgentRun.Begin(ResumedRunId(session), cancellationToken);

        // The run scope opens beside the run identity and closes with it, so every turn span
        // lands inside it. Null when nothing is listening — a scope nobody observes costs nothing.
        using IDisposable? telemetryRun = this.telemetryBroker.StartRun(sessionId);

        // The prompt rides the run the way its token does, so a handoff template's {prompt}
        // can ground a sub-agent in the real goal (SPEC.md §6.1).
        if (AgentRun.Current is AgentRun ambientRun)
        {
            ambientRun.Prompt = prompt;
        }

        await this.loggingBroker.LogResetAsync();
        await AnnounceResolutionAsync(request);

        // Selection (SPEC.md §4.15): what this run is OFFERED, resolved once at the top and
        // carried on the run — the offering is rendered two tiers below (the text catalog and
        // the native tool list), and neither renderer should gain a parameter for it. Recorded,
        // because a review of a run that lacked a capability must see that it was withheld by
        // selection rather than missing from the agent.
        if (this.toolSelector is not null && AgentRun.Current is AgentRun selectingRun)
        {
            IReadOnlyList<string> chosen =
                await this.toolSelector.SelectAsync(prompt, this.describedToolNames);

            // Names the agent does not carry are ignored, and the record below states the
            // TRUTH of the offering — never the selector's claim (SPEC.md §4.15).
            IReadOnlyList<string> offered = [.. this.describedToolNames
                .Where(name => chosen.Contains(name, StringComparer.OrdinalIgnoreCase))];

            selectingRun.OfferedTools = offered;

            await this.loggingBroker.LogProcessAsync(
                "Run",
                $"Selection → offered [{string.Join(", ", offered)}]; withheld "
                    + $"[{string.Join(", ", this.describedToolNames.Except(offered, StringComparer.OrdinalIgnoreCase))}]");
        }

        // Precedence resolved once, at the top of the run, and never again below it: a loop
        // that can re-resolve is a loop where two turns of one run can disagree
        // (docs/per-request-inference.md §2).
        // The caller-owned transcript seeds the run; a session, when one exists, overwrites
        // the history below — the deployment's record of the conversation beats the caller's
        // retelling of it, the same precedence every request field obeys
        // (docs/per-request-inference.md §4).
        AgentContext context = new()
        {
            Prompt = prompt,
            SessionId = sessionId,
            History = request.History,
            ToolExchanges = request.ToolExchanges,
            Inference = Resolve(request)
        };

        // The start-of-run checkpoint, written before any work is done (SPEC.md §4.11), and the
        // conversation so far, loaded before Decision runs so the Brain sees it.
        await BeginSessionAsync(context, session);
        context = await LoadSessionAsync(context, session);

        // Budgets and cancellation are both checked at the turn boundary (SPEC.md §4.10): a turn
        // is the smallest unit the loop can stop between without abandoning work mid-flight —
        // in particular without leaving an effect half-recorded.
        var spend = new AgentSpend();
        DateTimeOffset startedOn = this.timeBroker.GetCurrentDateTimeOffset();
        string? stoppedBecause = null;

        // AgentSpend keeps one number because the budget bounds one number; telemetry reports
        // input and output apart because that is how a collector prices and reasons about them.
        int runPromptTokens = 0;
        int runCompletionTokens = 0;

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

            // Scoped to the iteration: `continue` and `break` both close the turn span.
            using IDisposable? telemetryTurn = this.telemetryBroker.StartTurn(turn);

            await this.loggingBroker.LogTurnAsync(turn);

            await this.loggingBroker.LogStepAsync(AgentStep.Data);
            context = await RecallOrUnwindAsync(context);

            await this.loggingBroker.LogStepAsync(AgentStep.Decision);

            if (streaming)
            {
                IDecisionStream decisionStream =
                    this.decisionCoordinationService.ThinkStreamAsync(context, cancellationToken);

                await foreach (AgentStreamEvent decisionEvent in
                    UnwoundOnFaultAsync(decisionStream, cancellationToken))
                {
                    await events.WriteAsync(decisionEvent, abandoned);
                }

                context = decisionStream.Result;
            }
            else
            {
                context = await ThoughtOrUnwindAsync(context);
            }

            spend.AddTokens(context.PromptTokens, context.CompletionTokens);
            runPromptTokens += context.PromptTokens;
            runCompletionTokens += context.CompletionTokens;

            this.telemetryBroker.RecordTurnUsage(
                context.PromptTokens, context.CompletionTokens, context.UsageIsEstimated);

            if (context.Status is AgentStatus.Revising)
            {
                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                continue;
            }

            await this.loggingBroker.LogStepAsync(AgentStep.Direction);

            // Voiced before the act it announces, screened first (SPEC.md Invariant 5) — see
            // RunManagementService.Narration.cs.
            await VoiceNarrationAsync(context, events, abandoned);

            // Cleared before the act, so a transfer below reads how THIS act ended and never a
            // handoff from an earlier turn.
            if (AgentRun.Current is AgentRun actingRun)
            {
                actingRun.HandoffOutcome = null;
            }

            // Screened before the Tool event below: a caller watching the stream must not
            // receive the text the Brain was protected from (SPEC.md §4.9).
            int observedBefore = context.Observations.Count;

            context = await ActOrUnwindAsync(context);
            context = await ScreenedOrUnwindAsync(context, observedBefore);

            // A transfer is terminal by meaning: the specialist's answer IS the answer,
            // delivered as delivered — the outer Brain never gets a synthesis turn to rewrite
            // it. Adopted only when the specialist actually responded; a handoff that was held,
            // refused or failed stays an observation, and the loop keeps working the task
            // rather than presenting a refusal as the user's answer. The perimeter has already
            // ruled by this point — a denied or approval-held transfer never reaches here as
            // Working-with-an-answer.
            if (context.Transferring
                && context.Status is AgentStatus.Working
                && AgentRun.Current?.HandoffOutcome?.Status is AgentStatus.Responded)
            {
                context = context with { Status = AgentStatus.Responded };
            }

            await this.loggingBroker.LogOutcomeAsync($"turn {turn}: {context.Status}");

            if (context.Status is AgentStatus.Working
                && string.IsNullOrEmpty(context.Result) is false)
            {
                await VoiceObservedNarrationAsync(context, events, abandoned);

                await events.WriteAsync(
                    new AgentStreamEvent(
                        AgentStreamEventType.Tool,
                        $"{context.DirectionType}: {context.Result}"),
                    abandoned);
            }

            // A held act is announced before its message: the Status event says WHAT happened
            // for a consumer that switches on kinds, and the Response carries the same words the
            // batched caller receives — filtering a stream to Response equals what
            // ProcessPromptAsync returns, held runs included.
            if (context.Status is AgentStatus.AwaitingApproval)
            {
                await events.WriteAsync(
                    new AgentStreamEvent(
                        AgentStreamEventType.Status,
                        "an act is waiting for approval; the run is held"),
                    abandoned);
            }

            if (context.Status is AgentStatus.Responded
                or AgentStatus.Refused
                or AgentStatus.AwaitingInput
                or AgentStatus.AwaitingApproval
                && string.IsNullOrEmpty(context.Result) is false)
            {
                await events.WriteAsync(
                    new AgentStreamEvent(AgentStreamEventType.Response, context.Result),
                    abandoned);
            }

            if (context.Status is not AgentStatus.Working)
            {
                break;
            }
        }

        // Cancelled or out of budget. Reported as a Status rather than a Response, because it is
        // not an answer — and never remembered or written back as one: the next prompt would
        // otherwise be told the agent said something it never said (SPEC.md §4.10, §4.11).
        if (stoppedBecause is not null)
        {
            await this.loggingBroker.LogOutcomeAsync($"stopped: {stoppedBecause}");

            await events.WriteAsync(
                new AgentStreamEvent(AgentStreamEventType.Status, stoppedBecause), abandoned);

            context = context with { Status = AgentStatus.Failed };

            // A run that stopped without delivering an answer may have left effects behind
            // (SPEC.md §4.9); the caller is told what was unwound and what stood.
            string stoppedUnwound = await UnwindAsync();

            if (string.IsNullOrEmpty(stoppedUnwound) is false)
            {
                await events.WriteAsync(
                    new AgentStreamEvent(AgentStreamEventType.Status, stoppedUnwound), abandoned);
            }

            await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

            this.telemetryBroker.RecordRunOutcome(
                context.Status.ToString(), runPromptTokens, runCompletionTokens);

            setOutcome(new AgentOutcome(
                string.IsNullOrEmpty(stoppedUnwound)
                    ? stoppedBecause
                    : $"{stoppedBecause} {stoppedUnwound}",
                AgentStatus.Failed));

            return;
        }

        // Turns ran out with the loop still Working: no answer was ever delivered, and the last
        // tool output is not one. The same truth the budget stop tells, told the same way — a
        // Status, prose about why for the string-typed caller, and no session write, so the next
        // prompt resumes the interrupted run instead of being told the agent said something it
        // never said (SPEC.md §4.10, §4.11). Status stays Working: the run stopped mid-work.
        if (context.Status is AgentStatus.Working)
        {
            await this.loggingBroker.LogOutcomeAsync($"stopped: {TurnsExhaustedMessage}");

            await events.WriteAsync(
                new AgentStreamEvent(AgentStreamEventType.Status, TurnsExhaustedMessage),
                abandoned);

            string cappedUnwound = await UnwindAsync();

            if (string.IsNullOrEmpty(cappedUnwound) is false)
            {
                await events.WriteAsync(
                    new AgentStreamEvent(AgentStreamEventType.Status, cappedUnwound), abandoned);
            }

            await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

            // Working, not Responded: the run stopped mid-work, and the span says so the same
            // way the caller is told.
            this.telemetryBroker.RecordRunOutcome(
                context.Status.ToString(), runPromptTokens, runCompletionTokens);

            setOutcome(new AgentOutcome(
                string.IsNullOrEmpty(cappedUnwound)
                    ? TurnsExhaustedMessage
                    : $"{TurnsExhaustedMessage} {cappedUnwound}",
                context.Status));

            return;
        }

        if (context.Status is AgentStatus.Revising)
        {
            await events.WriteAsync(
                new AgentStreamEvent(
                    AgentStreamEventType.Status,
                    "unable to satisfy review after retries; refusing"),
                abandoned);

            await events.WriteAsync(
                new AgentStreamEvent(AgentStreamEventType.Response, RetriesExhaustedMessage),
                abandoned);

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

        // Appended before the run ends, so the next prompt sees it (SPEC.md §4.11).
        await SaveSessionAsync(context, completed: true);

        this.telemetryBroker.RecordRunOutcome(
            context.Status.ToString(), runPromptTokens, runCompletionTokens);

        // The pending effect rides the outcome as well as the session (SPEC.md §4.11):
        // a stateless deployment has no session, and an exposer that cannot reach the
        // pending call cannot yield it to the caller (docs/per-request-inference.md §6.2).
        setOutcome(new AgentOutcome(context.Result, context.Status, context.PendingEffect));
    }
}
