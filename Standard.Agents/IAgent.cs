// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents;

public interface IAgent
{
    ValueTask<string> ProcessPromptAsync(string prompt);

    /// <summary>
    /// The same run, reported with <b>how it ended</b> as well as what it produced. A run can end
    /// answered, refused, held on an authority, or out of turns, and only the first makes the
    /// result an answer — a caller given nothing but the string cannot tell them apart.
    /// </summary>
    /// <remarks>
    /// The default assumes the run answered, which is exactly what a caller of
    /// <see cref="ProcessPromptAsync"/> already assumes, so an existing implementation keeps
    /// working and is no less accurate than it was. An implementation that knows better — and
    /// every implementation that runs the loop does — should override it. Nesting reads this
    /// (<c>AgentTool</c>), so an agent that leaves the default will have its held runs reported to
    /// an outer agent as answers.
    /// </remarks>
    async ValueTask<AgentOutcome> RunAsync(string prompt) =>
        new AgentOutcome(
            Result: await ProcessPromptAsync(prompt),
            Status: AgentStatus.Responded);

    /// <summary>
    /// The same run, stoppable: cancellation stops it at the next turn boundary, so no effect
    /// is left half-recorded (SPEC.md §4.10). Nesting reads this — <c>AgentTool</c> forwards
    /// the outer run's token here, so cancelling an outer run stops the whole tree.
    /// </summary>
    /// <remarks>
    /// The default ignores the token, which is no less accurate than an implementation that
    /// cannot stop mid-run was before. An implementation that runs the loop should override it.
    /// </remarks>
    ValueTask<AgentOutcome> RunAsync(string prompt, CancellationToken cancellationToken) =>
        RunAsync(prompt);

    IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
