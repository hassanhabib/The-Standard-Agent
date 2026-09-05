// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents;

/// <summary>
/// An agent, as a host or a nested agent sees it. The request-rich members are the contract:
/// a run carries a session, a caller-owned transcript, caller tools, resumed tool exchanges and
/// inference controls, and ends with a structured outcome that says HOW it ended. The
/// string members are convenience adapters over them, so an adapter can never report a held
/// or refused run as an answer (principal review 2026-09-04, F-09).
/// </summary>
public interface IAgent
{
    /// <summary>
    /// The run, in full: everything about the request that is data, and an outcome that says
    /// how the run ended — answered, refused, held on an authority, waiting on the caller, or
    /// out of turns. Only <see cref="AgentStatus.Responded"/> makes the result an answer.
    /// Cancellation stops it at the next turn boundary, so no effect is left half-recorded
    /// (SPEC.md §4.10).
    /// </summary>
    ValueTask<AgentOutcome> RunAsync(
        PromptRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// The same run, streamed (SPEC.md §4.14): every event as it happens, for a request
    /// carrying everything <see cref="RunAsync(PromptRequest, CancellationToken)"/> accepts.
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        PromptRequest request,
        CancellationToken cancellationToken);

    /// <summary>The one-line door: a prompt in, the answer's text out.</summary>
    async ValueTask<string> ProcessPromptAsync(string prompt) =>
        (await RunAsync(new PromptRequest { Prompt = prompt }, CancellationToken.None)).Result;

    /// <summary>
    /// The same run, reported with <b>how it ended</b> as well as what it produced. A run can end
    /// answered, refused, held on an authority, or out of turns, and only the first makes the
    /// result an answer — a caller given nothing but the string cannot tell them apart.
    /// </summary>
    async ValueTask<AgentOutcome> RunAsync(string prompt) =>
        new AgentOutcome(
            Result: await ProcessPromptAsync(prompt),
            Status: AgentStatus.Responded);

    /// <summary>
    /// The same run, stoppable: cancellation stops it at the next turn boundary, so no effect
    /// is left half-recorded (SPEC.md §4.10). Nesting reads this — <c>AgentTool</c> forwards
    /// the outer run's token here, so cancelling an outer run stops the whole tree.
    /// </summary>
    ValueTask<AgentOutcome> RunAsync(string prompt, CancellationToken cancellationToken) =>
        RunAsync(prompt);

    /// <summary>The streamed run for a bare prompt, riding the request-rich stream.</summary>
    IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        CancellationToken cancellationToken = default) =>
        StreamPromptAsync(new PromptRequest { Prompt = prompt }, cancellationToken);

    /// <summary>
    /// The streamed run (SPEC.md §4.14): every event live, and — once the enumeration
    /// completes — the same structured outcome <see cref="RunAsync(string)"/> returns. One
    /// run, two readings; a caller never chooses between the answer's structure and the
    /// run's story.
    /// </summary>
    /// <remarks>
    /// The default adapts <see cref="StreamPromptAsync(string, CancellationToken)"/> and
    /// assumes the run answered, because it cannot know a pending effect or a refusal from the
    /// events alone. An implementation that runs the loop — and every implementation that runs
    /// the loop knows how its run ended — overrides it; <c>StandardAgent</c> does.
    /// </remarks>
    AgentRunStream RunStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default) =>
        new(setOutcome => AdaptedStreamAsync(prompt, setOutcome, cancellationToken));

    private async IAsyncEnumerable<AgentStreamEvent> AdaptedStreamAsync(
        string prompt,
        Action<AgentOutcome> setOutcome,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var answer = new System.Text.StringBuilder();

        await foreach (AgentStreamEvent streamEvent in
            StreamPromptAsync(prompt, cancellationToken))
        {
            if (streamEvent.Type is AgentStreamEventType.Response)
            {
                answer.Append(streamEvent.Content);
            }

            yield return streamEvent;
        }

        setOutcome(new AgentOutcome(answer.ToString(), AgentStatus.Responded));
    }
}
