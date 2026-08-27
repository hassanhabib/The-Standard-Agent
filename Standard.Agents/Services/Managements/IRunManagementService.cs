// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;

namespace Standard.Agents.Services.Managements;

public interface IRunManagementService
{
    ValueTask<string> ProcessPromptAsync(string prompt);

    ValueTask<string> ProcessPromptAsync(string prompt, CancellationToken cancellationToken);

    ValueTask<string> ProcessPromptAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The run, asked for by a <b>request</b> — a prompt carrying its own inference options
    /// (docs/per-request-inference.md). Precedence is resolved once at the top of the run;
    /// every tier below receives the resolution's output on the context.
    /// </summary>
    ValueTask<string> ProcessPromptAsync(
        PromptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same run, reported with <b>how it ended</b>. Every <c>ProcessPromptAsync</c> above is
    /// this with the answer projected out of it — which is all a caller wants until the agent is
    /// nested inside another one, where "held" and "answered" have to be told apart.
    /// </summary>
    ValueTask<AgentOutcome> RunAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The run asked for by a request, reported with how it ended. An exposer cannot do without
    /// this: a run that ended <c>AwaitingInput</c> holding a caller's tool call and a run that
    /// answered read alike as strings.
    /// </summary>
    ValueTask<AgentOutcome> RunAsync(
        PromptRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The streamed loop, in a conversation. Every control the batched loop enforces — budgets,
    /// cancellation, sessions — holds here too: a control a caller can step around by changing
    /// method is not a control (SPEC.md §7.6, §4.10, §4.11).
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The streamed loop, asked for by a request. The same resolution, the same controls: a
    /// control a caller can step around by changing method is not a control (SPEC.md §7.6).
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        PromptRequest request,
        CancellationToken cancellationToken = default);
}
