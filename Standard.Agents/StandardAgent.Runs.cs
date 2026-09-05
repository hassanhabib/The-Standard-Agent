// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
// System.Threading.Lock arrived in .NET 9. On the net8.0 target a plain object under the
// same lock statements is the identical semantic; the alias keeps one body for both.
using Lock = System.Object;
#endif
using Microsoft.Extensions.Logging.Abstractions;
using Standard.Agents.Brokers.Agents;
using Standard.Agents.Brokers.Audits;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Contracts;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Brokers.Resiliences;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Usages;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Brokers.Tools;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Brokers.Agents;
using Standard.Agents.Models.Brokers.Audits;
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Coordinations.Agents;
using Standard.Agents.Models.Coordinations.Directions;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Prompts;
using Standard.Agents.Services.Managements;
using Standard.Agents.Services.Foundations.Brains;
using Standard.Agents.Services.Foundations.Contracts;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Judges;
using Standard.Agents.Services.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Memorys;
using Standard.Agents.Services.Foundations.Returns;
using Standard.Agents.Services.Foundations.Approvals;
using Standard.Agents.Services.Foundations.EffectLedgers;
using Standard.Agents.Services.Foundations.Policys;
using Standard.Agents.Services.Foundations.Sessions;
using Standard.Agents.Services.Foundations.Skills;
using Standard.Agents.Services.Foundations.Usages;
using Standard.Agents.Services.Coordinations.Data;
using Standard.Agents.Services.Orchestrations.Data.Recollections;
using Standard.Agents.Services.Orchestrations.Data.Retrievals;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;
using Standard.Agents.Services.Coordinations.Direction;
using Standard.Agents.Services.Orchestrations.Direction.Executions;
using Standard.Agents.Services.Orchestrations.Direction.Perimeters;
using Standard.Agents.Tools;

namespace Standard.Agents;

// The runs: every door a prompt enters by, batched and streamed, and the outcome each
// reports. The verbs that compose the agent live in StandardAgent.cs; the engine that turns
// them into a graph lives in StandardAgent.Composition.cs (principal review 2026-09-04, F-31).
public sealed partial class StandardAgent
{
    /// <summary>
    /// Runs the agent on a prompt to completion and returns the final answer. The first call
    /// composes the configured pieces (brain, skills, tools, guardians, memory, knowledge); later
    /// calls reuse that composition unless a builder method changed the configuration.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <returns>The agent's final answer.</returns>
    // async, so a composition failure surfaces on await rather than being thrown
    // synchronously out of a method whose signature promises a ValueTask. A caller
    // doing `var task = agent.ProcessPromptAsync(p); ... await task;` would otherwise
    // be hit at the assignment, nowhere near the await they were guarding.
    public async ValueTask<string> ProcessPromptAsync(string prompt)
    {
        return await (await ResolveAgentAsync()).ProcessPromptAsync(prompt);
    }

    /// <summary>
    /// Runs the agent and reports <b>how the run ended</b> as well as what it produced. Only
    /// <c>AgentStatus.Responded</c> makes the result an answer — a run held on an authority,
    /// refused, cancelled or out of turns produced prose about why, and the two read alike.
    /// </summary>
    /// <remarks>
    /// This is what nesting uses. <c>AgentTool</c> reads it so an outer agent cannot mistake a
    /// sub-agent that was held for one that finished, which the string on its own could never tell
    /// it.
    /// </remarks>
    /// <param name="prompt">What to work on.</param>
    /// <returns>The answer, and how the run ended.</returns>
    public async ValueTask<AgentOutcome> RunAsync(string prompt) =>
        await (await ResolveAgentAsync()).RunAsync(prompt, string.Empty, CancellationToken.None);

    /// <summary>
    /// The same run, stoppable: cancellation stops it at the next turn boundary, so no effect
    /// is left half-recorded (SPEC.md §4.10). This is the overload nesting calls —
    /// <c>AgentTool</c> forwards the outer run's token here, so cancelling an outer run stops
    /// the whole tree.
    /// </summary>
    /// <param name="prompt">What to work on.</param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The answer, and how the run ended.</returns>
    public async ValueTask<AgentOutcome> RunAsync(
        string prompt,
        CancellationToken cancellationToken) =>
        await (await ResolveAgentAsync()).RunAsync(prompt, string.Empty, cancellationToken);

    /// <summary>
    /// Runs a caller's request and reports <b>how the run ended</b> as well as what it produced.
    /// This is the exposer's read: a run that ended <c>AwaitingInput</c> holding a caller's tool
    /// call and a run that answered read alike as strings, and only the status tells them apart.
    /// </summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <returns>The answer, and how the run ended.</returns>
    public async ValueTask<AgentOutcome> RunAsync(PromptRequest request) =>
        await RunAsync(request, CancellationToken.None);

    /// <summary>The same request-carrying run, reported with how it ended, cancellable.</summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The answer, and how the run ended.</returns>
    public async ValueTask<AgentOutcome> RunAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
        await (await ResolveAgentAsync()).RunAsync(request, cancellationToken);

    /// <summary>
    /// Runs the agent on a prompt and stops when <paramref name="cancellationToken"/> is
    /// cancelled — at the next turn boundary at the latest, so no effect is left half-recorded
    /// (SPEC.md §4.10). A cancelled run returns a message saying so rather than an answer.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The agent's final answer, or a message explaining why it stopped.</returns>
    public async ValueTask<string> ProcessPromptAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        return await (await ResolveAgentAsync()).ProcessPromptAsync(prompt, cancellationToken);
    }

    /// <summary>
    /// Runs the agent as part of a <b>conversation</b>. What was said before is loaded before the
    /// brain thinks, and this exchange is appended when it answers — so a follow-up resolves
    /// against what came before instead of starting from nothing (SPEC.md §4.11).
    /// </summary>
    /// <remarks>
    /// The conversation lives in the session store, not in this instance, so it survives a
    /// restart and can be continued by a different process. Invariant 4 is intact: the agent is
    /// still stateless across prompts; the session is not part of the agent.
    /// <para>A cancelled or budget-stopped run is never recorded as an answer — the next prompt
    /// would otherwise be told the agent said something it never said.</para>
    /// </remarks>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="sessionId">Which conversation this belongs to.</param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The agent's final answer.</returns>
    public async ValueTask<string> ProcessPromptAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await (await ResolveAgentAsync())
            .ProcessPromptAsync(prompt, sessionId, cancellationToken);
    }

    /// <summary>
    /// Runs the agent on a caller's <b>request</b> — a prompt carrying its own inference options
    /// (docs/per-request-inference.md). What is established and hard-configured takes precedence,
    /// always: a request can shape a run only where the deployment expressed no opinion, and it
    /// can never widen the boundary the deployment set.
    /// </summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <returns>The agent's final answer.</returns>
    public async ValueTask<string> ProcessPromptAsync(PromptRequest request)
    {
        return await ProcessPromptAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// The same request-carrying run, stopped when <paramref name="cancellationToken"/> is
    /// cancelled — at the next turn boundary at the latest (SPEC.md §4.10).
    /// </summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The agent's final answer.</returns>
    public async ValueTask<string> ProcessPromptAsync(
        PromptRequest request,
        CancellationToken cancellationToken)
    {
        return await (await ResolveAgentAsync()).ProcessPromptAsync(request, cancellationToken);
    }

    /// <summary>
    /// Continues a session that stopped in the middle of something — waiting on a person, waiting
    /// on an authority, or killed outright (SPEC.md §4.9, §4.11).
    /// </summary>
    /// <remarks>
    /// This is the same call as <see cref="ProcessPromptAsync(string, string, CancellationToken)"/>
    /// and reads the answer the same way; it exists because <i>resuming</i> is what the caller is
    /// doing, and a verb that says so is worth more than one saved line of documentation.
    /// <para>A session that never delivered an answer keeps the interrupted run's identity, so an
    /// act it already performed is recognised as its own and replayed rather than performed twice.
    /// That only holds across processes if the ledger is durable — see
    /// <see cref="EffectLedger(string)"/>; the built-in one lives in memory and dies with the
    /// instance.</para>
    /// <para>Nothing is required of the caller beyond the answer. There is no separate resume
    /// mode and no state to hand back: the session already holds everything, which is what makes
    /// a different process able to pick it up.</para>
    /// </remarks>
    /// <param name="sessionId">The conversation to continue.</param>
    /// <param name="answer">
    /// What the agent was waiting for — a reply to its question, or an authority's decision.
    /// </param>
    /// <param name="cancellationToken">Token to stop the run.</param>
    /// <returns>The agent's answer, now that it can finish.</returns>
    public async ValueTask<string> ResumeAsync(
        string sessionId,
        string answer,
        CancellationToken cancellationToken = default)
    {
        return await (await ResolveAgentAsync())
            .ProcessPromptAsync(answer, sessionId, cancellationToken);
    }


    /// <summary>
    /// Runs the agent on a prompt and streams its progress as it happens — status updates, the
    /// brain's thinking, and response text arrive as <see cref="AgentStreamEvent"/> values rather
    /// than waiting for the final answer. Use this to surface a live view of the agent's work.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="cancellationToken">Token to stop streaming early.</param>
    /// <returns>An async stream of events describing the agent's run.</returns>
    // async iterator, so a composition failure surfaces when the caller starts
    // enumerating rather than at the call site — mirroring ProcessPromptAsync.
    public async IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<AgentStreamEvent> events =
            (await ResolveAgentAsync()).ProcessPromptStreamAsync(prompt, cancellationToken);

        await foreach (AgentStreamEvent streamEvent in events.WithCancellation(cancellationToken))
        {
            yield return streamEvent;
        }
    }

    /// <summary>
    /// Streams the agent's work as part of a <b>conversation</b> — the streamed equivalent of
    /// <see cref="ProcessPromptAsync(string, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Every control the batched call enforces holds here too: budgets, cancellation, session
    /// loading and the record of the exchange. A control a caller can step around by changing
    /// method is not a control (SPEC.md §7.6).
    /// </remarks>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="sessionId">Which conversation this belongs to.</param>
    /// <param name="cancellationToken">Token to stop streaming early.</param>
    /// <returns>An async stream of events describing the agent's run.</returns>
    public async IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<AgentStreamEvent> events = (await ResolveAgentAsync())
            .ProcessPromptStreamAsync(prompt, sessionId, cancellationToken);

        await foreach (AgentStreamEvent streamEvent in events.WithCancellation(cancellationToken))
        {
            yield return streamEvent;
        }
    }

    /// <summary>
    /// Streams the agent's work on a caller's <b>request</b> — the streamed equivalent of
    /// <see cref="ProcessPromptAsync(PromptRequest, CancellationToken)"/>. The same precedence,
    /// the same controls: a control a caller can step around by changing method is not a control
    /// (SPEC.md §7.6).
    /// </summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <returns>An async stream of events describing the agent's run.</returns>
    public IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(PromptRequest request) =>
        StreamPromptAsync(request, CancellationToken.None);

    /// <summary>The same request-carrying stream, cancellable.</summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <param name="cancellationToken">Token to stop streaming early.</param>
    /// <returns>An async stream of events describing the agent's run.</returns>
    public async IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        PromptRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<AgentStreamEvent> events =
            (await ResolveAgentAsync()).ProcessPromptStreamAsync(request, cancellationToken);

        await foreach (AgentStreamEvent streamEvent in events.WithCancellation(cancellationToken))
        {
            yield return streamEvent;
        }
    }

    /// <summary>
    /// The streamed outcome (SPEC.md §4.14): the same run as <see cref="StreamPromptAsync(string, CancellationToken)"/>,
    /// whose enumeration also carries — once it completes — the same structured outcome
    /// <see cref="RunAsync(PromptRequest, CancellationToken)"/> returns: status, result, and any
    /// pending effect with its model-minted call id. One run, two readings; a caller never
    /// chooses between the answer's structure and the run's story.
    /// </summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="cancellationToken">Token to stop the run at its next turn boundary.</param>
    /// <returns>The run's events, carrying its outcome at completion.</returns>
    public AgentRunStream RunStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default) =>
        RunStreamAsync(new PromptRequest { Prompt = prompt }, cancellationToken);

    /// <summary>The same streamed outcome, inside a conversation.</summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="sessionId">Which conversation this belongs to.</param>
    /// <returns>The run's events, carrying its outcome at completion.</returns>
    public AgentRunStream RunStreamAsync(string prompt, string sessionId) =>
        RunStreamAsync(prompt, sessionId, CancellationToken.None);

    /// <summary>The same conversation-carrying streamed outcome, cancellable.</summary>
    /// <param name="prompt">The user's prompt.</param>
    /// <param name="sessionId">Which conversation this belongs to.</param>
    /// <param name="cancellationToken">Token to stop the run at its next turn boundary.</param>
    /// <returns>The run's events, carrying its outcome at completion.</returns>
    public AgentRunStream RunStreamAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken) =>
        RunStreamAsync(
            new PromptRequest { Prompt = prompt, SessionId = sessionId },
            cancellationToken);

    /// <summary>The same streamed outcome, asked for by a caller's request.</summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <returns>The run's events, carrying its outcome at completion.</returns>
    public AgentRunStream RunStreamAsync(PromptRequest request) =>
        RunStreamAsync(request, CancellationToken.None);

    /// <summary>The same request-carrying streamed outcome, cancellable.</summary>
    /// <param name="request">The caller's prompt and per-request inference options.</param>
    /// <param name="cancellationToken">Token to stop the run at its next turn boundary.</param>
    /// <returns>The run's events, carrying its outcome at completion.</returns>
    public AgentRunStream RunStreamAsync(
        PromptRequest request,
        CancellationToken cancellationToken) =>
        new(setOutcome => ResolvedRunStreamAsync(request, setOutcome, cancellationToken));

    // The inner stream's outcome is handed outward when the enumeration ends, so the wrapper
    // the caller holds carries what the composed agent's run concluded — same seam, one tier up.
    private async IAsyncEnumerable<AgentStreamEvent> ResolvedRunStreamAsync(
        PromptRequest request,
        Action<AgentOutcome> setOutcome,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentRunStream events =
            (await ResolveAgentAsync()).RunStreamAsync(request, cancellationToken);

        await foreach (AgentStreamEvent streamEvent in events.WithCancellation(cancellationToken))
        {
            yield return streamEvent;
        }

        setOutcome(events.Outcome);
    }

}
