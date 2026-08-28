// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// The streamed run (SPEC.md §4.14): an enumeration of the run's events that, once the
/// enumeration completes, carries the same structured outcome the batched door returns — one
/// run, two readings: the events as they happen, and how it ended. The same shape the decision
/// tier has always had (a stream whose result rides beside it), one tier up.
/// </summary>
public sealed class AgentRunStream : IAsyncEnumerable<AgentStreamEvent>
{
    private readonly IAsyncEnumerable<AgentStreamEvent> events;

    /// <summary>
    /// How the run ended — defined once the enumeration completes (SPEC.md §4.14). Before
    /// that it reads as a failed run, exactly the seed the batched door starts from.
    /// </summary>
    public AgentOutcome Outcome { get; private set; } =
        new(string.Empty, AgentStatus.Failed);

    public AgentRunStream(
        Func<Action<AgentOutcome>, IAsyncEnumerable<AgentStreamEvent>> build) =>
        this.events = build(outcome => this.Outcome = outcome);

    public IAsyncEnumerator<AgentStreamEvent> GetAsyncEnumerator(
        CancellationToken cancellationToken = default) =>
        this.events.GetAsyncEnumerator(cancellationToken);
}
